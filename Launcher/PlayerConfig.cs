using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ErrorReportLauncher
{
    public enum ReportFunction
    {
        DllMissing = 0,
        ExeError = 1,
        MemoryError = 2,
        Custom = 3,
    }

    /// <summary>
    /// 追加在生成 exe 末尾的播放配置。启动器把自己复制成目标 exe 后写入这段数据，
    /// 目标 exe 启动时先检查这段数据；存在就进入播放模式，不存在就显示启动器界面。
    /// </summary>
    public sealed class PlayerConfig
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("ERLPAY01");
        private const int FormatVersion = 1;

        public ReportFunction Function { get; set; }
        public string LogFolder { get; set; } = string.Empty;

        public string ErrorProgram { get; set; } = string.Empty;
        public string MissingDll { get; set; } = string.Empty;
        public string ErrorName { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Operate { get; set; } = string.Empty;
        public string MemoryAddress { get; set; } = string.Empty;
        public string CannotBe { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = "none";
        public string Buttons { get; set; } = string.Empty;
        public int HideCloseButton { get; set; }

        /// <summary>把配置追加到可执行文件末尾。</summary>
        public static void AppendTo(string executablePath, PlayerConfig config)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("可执行文件路径为空。", nameof(executablePath));
            }
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(FormatVersion);
                writer.Write((int)config.Function);
                writer.Write(config.LogFolder ?? string.Empty);
                writer.Write(config.ErrorProgram ?? string.Empty);
                writer.Write(config.MissingDll ?? string.Empty);
                writer.Write(config.ErrorName ?? string.Empty);
                writer.Write(config.ErrorCode ?? string.Empty);
                writer.Write(config.Address ?? string.Empty);
                writer.Write(config.Operate ?? string.Empty);
                writer.Write(config.MemoryAddress ?? string.Empty);
                writer.Write(config.CannotBe ?? string.Empty);
                writer.Write(config.Title ?? string.Empty);
                writer.Write(config.Text ?? string.Empty);
                writer.Write(config.Icon ?? "none");
                writer.Write(config.Buttons ?? string.Empty);
                writer.Write(config.HideCloseButton);
                payload = stream.ToArray();
            }

            using var file = new FileStream(
                executablePath, FileMode.Append, FileAccess.Write, FileShare.None);
            file.Write(payload, 0, payload.Length);
            file.Write(Magic, 0, Magic.Length);
            byte[] lengthBytes = BitConverter.GetBytes(payload.Length);
            file.Write(lengthBytes, 0, lengthBytes.Length);
        }

        /// <summary>读取 exe 末尾的配置；没有配置时返回 false。</summary>
        public static bool TryReadFrom(string executablePath, out PlayerConfig config)
        {
            config = new PlayerConfig();
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                return false;
            }

            try
            {
                using var file = new FileStream(
                    executablePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                const int headerLength = 8 + 4;
                if (file.Length <= headerLength)
                {
                    return false;
                }

                file.Seek(-headerLength, SeekOrigin.End);
                byte[] magic = ReadExactly(file, Magic.Length);
                if (!BytesEqual(magic, Magic))
                {
                    return false;
                }

                int payloadLength = BitConverter.ToInt32(ReadExactly(file, 4), 0);
                if (payloadLength <= 0 || payloadLength > file.Length - headerLength)
                {
                    return false;
                }

                file.Seek(-(headerLength + (long)payloadLength), SeekOrigin.End);
                byte[] payload = ReadExactly(file, payloadLength);

                using var stream = new MemoryStream(payload, writable: false);
                using var reader = new BinaryReader(stream, Encoding.UTF8);
                if (reader.ReadInt32() != FormatVersion)
                {
                    return false;
                }

                var result = new PlayerConfig
                {
                    Function = (ReportFunction)reader.ReadInt32(),
                    LogFolder = reader.ReadString(),
                    ErrorProgram = reader.ReadString(),
                    MissingDll = reader.ReadString(),
                    ErrorName = reader.ReadString(),
                    ErrorCode = reader.ReadString(),
                    Address = reader.ReadString(),
                    Operate = reader.ReadString(),
                    MemoryAddress = reader.ReadString(),
                    CannotBe = reader.ReadString(),
                    Title = reader.ReadString(),
                    Text = reader.ReadString(),
                    Icon = reader.ReadString(),
                    Buttons = reader.ReadString(),
                    HideCloseButton = reader.ReadInt32(),
                };

                if (!Enum.IsDefined(typeof(ReportFunction), result.Function))
                {
                    return false;
                }

                config = result;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static byte[] ReadExactly(Stream stream, int count)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
            }
            return buffer;
        }

        private static bool BytesEqual(IReadOnlyList<byte> left, IReadOnlyList<byte> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
