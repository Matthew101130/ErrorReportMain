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
        CustomMessageBox = 4,
    }

    /// <summary>
    /// 追加在生成 exe 末尾的播放配置。启动器把自己复制成目标 exe 后写入这段数据，
    /// 目标 exe 启动时先检查这段数据；存在就进入播放模式，不存在就显示启动器界面。
    /// </summary>
    public sealed class PlayerConfig : PopupDefinition
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("ERLPAY04");
        private static readonly byte[] LegacyMagic3 = Encoding.ASCII.GetBytes("ERLPAY03");
        private static readonly byte[] LegacyMagic2 = Encoding.ASCII.GetBytes("ERLPAY02");
        private static readonly byte[] LegacyMagic = Encoding.ASCII.GetBytes("ERLPAY01");
        private const int FormatVersion = 4;
        private const int LegacyFormatVersion3 = 3;
        private const int LegacyFormatVersion2 = 2;
        private const int LegacyFormatVersion = 1;
        private const int MaxPopups = 1000;

        public string LogFolder { get; set; } = string.Empty;

        /// <summary>true 时忽略 LogFolder，日志写在播放器自己所在的文件夹。</summary>
        public bool UseRuntimeFolder { get; set; }

        /// <summary>弹窗组；为空时按旧的单弹窗配置播放。</summary>
        public List<PopupDefinition> Popups { get; set; } = new List<PopupDefinition>();

        /// <summary>生成时嵌入的 x64 DLL；播放器会先释放到本地缓存再加载。</summary>
        public byte[]? EmbeddedDll { get; set; }

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
                WritePopupFields(writer, config);
                writer.Write(config.UseRuntimeFolder);

                List<PopupDefinition> popups = config.Popups ?? new List<PopupDefinition>();
                if (popups.Count > MaxPopups)
                {
                    throw new InvalidOperationException("弹窗数量过多。");
                }
                writer.Write(popups.Count);
                foreach (PopupDefinition popup in popups)
                {
                    WritePopup(writer, popup);
                }

                byte[] embeddedDll = config.EmbeddedDll ?? Array.Empty<byte>();
                writer.Write(embeddedDll.Length);
                writer.Write(embeddedDll);
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
                byte[] magic = ReadExactly(file, 8);
                int formatVersion;
                if (BytesEqual(magic, Magic))
                {
                    formatVersion = FormatVersion;
                }
                else if (BytesEqual(magic, LegacyMagic3))
                {
                    formatVersion = LegacyFormatVersion3;
                }
                else if (BytesEqual(magic, LegacyMagic2))
                {
                    formatVersion = LegacyFormatVersion2;
                }
                else if (BytesEqual(magic, LegacyMagic))
                {
                    formatVersion = LegacyFormatVersion;
                }
                else
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
                if (reader.ReadInt32() != formatVersion)
                {
                    return false;
                }

                var result = new PlayerConfig
                {
                    Function = (ReportFunction)reader.ReadInt32(),
                    LogFolder = reader.ReadString(),
                };
                if (!ReadPopupFields(reader, result))
                {
                    return false;
                }

                if (formatVersion >= 3)
                {
                    result.UseRuntimeFolder = reader.ReadBoolean();
                }

                if (formatVersion >= 4)
                {
                    int popupCount = reader.ReadInt32();
                    if (popupCount < 0 || popupCount > MaxPopups)
                    {
                        return false;
                    }
                    for (int i = 0; i < popupCount; i++)
                    {
                        PopupDefinition? popup = ReadPopup(reader);
                        if (popup is null)
                        {
                            return false;
                        }
                        result.Popups.Add(popup);
                    }
                }

                if (formatVersion >= 2)
                {
                    int dllLength = reader.ReadInt32();
                    if (dllLength < 0 || dllLength > stream.Length - stream.Position)
                    {
                        return false;
                    }
                    result.EmbeddedDll = reader.ReadBytes(dllLength);
                }

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

        private static void WritePopup(BinaryWriter writer, PopupDefinition popup)
        {
            writer.Write((int)popup.Function);
            WritePopupFields(writer, popup);
            writer.Write(popup.RepeatCount < 1 ? 1 : popup.RepeatCount);
            writer.Write(popup.Infinite);
        }

        private static PopupDefinition? ReadPopup(BinaryReader reader)
        {
            var popup = new PopupDefinition
            {
                Function = (ReportFunction)reader.ReadInt32(),
            };
            if (!ReadPopupFields(reader, popup))
            {
                return null;
            }

            int repeatCount = reader.ReadInt32();
            bool infinite = reader.ReadBoolean();
            if (!infinite && repeatCount < 1)
            {
                return null;
            }

            popup.RepeatCount = repeatCount < 1 ? 1 : repeatCount;
            popup.Infinite = infinite;

            if (!Enum.IsDefined(typeof(ReportFunction), popup.Function))
            {
                return null;
            }
            return popup;
        }

        private static void WritePopupFields(BinaryWriter writer, PopupDefinition popup)
        {
            writer.Write(popup.ErrorProgram ?? string.Empty);
            writer.Write(popup.MissingDll ?? string.Empty);
            writer.Write(popup.ErrorName ?? string.Empty);
            writer.Write(popup.ErrorCode ?? string.Empty);
            writer.Write(popup.Address ?? string.Empty);
            writer.Write(popup.Operate ?? string.Empty);
            writer.Write(popup.MemoryAddress ?? string.Empty);
            writer.Write(popup.CannotBe ?? string.Empty);
            writer.Write(popup.Title ?? string.Empty);
            writer.Write(popup.Text ?? string.Empty);
            writer.Write(popup.Icon ?? "none");
            writer.Write(popup.Buttons ?? string.Empty);
            writer.Write(popup.HideCloseButton);
        }

        private static bool ReadPopupFields(BinaryReader reader, PopupDefinition popup)
        {
            popup.ErrorProgram = reader.ReadString();
            popup.MissingDll = reader.ReadString();
            popup.ErrorName = reader.ReadString();
            popup.ErrorCode = reader.ReadString();
            popup.Address = reader.ReadString();
            popup.Operate = reader.ReadString();
            popup.MemoryAddress = reader.ReadString();
            popup.CannotBe = reader.ReadString();
            popup.Title = reader.ReadString();
            popup.Text = reader.ReadString();
            popup.Icon = reader.ReadString();
            popup.Buttons = reader.ReadString();
            popup.HideCloseButton = reader.ReadInt32();
            return true;
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
