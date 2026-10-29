using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ErrorReportLauncher
{
    /// <summary>
    /// 生成 exe 的播放模式：优先加载 exe 内嵌的 x64 DLL；
    /// 旧格式没有内嵌 DLL 时，退回加载同目录的 ErrorReportMain.dll。
    /// </summary>
    internal static class PlayerRunner
    {
        private const string DllName = "ErrorReportMain.dll";
        private static IntPtr _module;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int DllMissingErrorFn(
            [MarshalAs(UnmanagedType.LPWStr)] string errorProgram,
            [MarshalAs(UnmanagedType.LPWStr)] string missingDll);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int ExeErrorFn(
            [MarshalAs(UnmanagedType.LPWStr)] string errorProgram,
            [MarshalAs(UnmanagedType.LPWStr)] string errorName,
            [MarshalAs(UnmanagedType.LPWStr)] string errorCode,
            [MarshalAs(UnmanagedType.LPWStr)] string address);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int MemoryErrorFn(
            [MarshalAs(UnmanagedType.LPWStr)] string errorProgram,
            [MarshalAs(UnmanagedType.LPWStr)] string operate,
            [MarshalAs(UnmanagedType.LPWStr)] string address,
            [MarshalAs(UnmanagedType.LPWStr)] string cannotBe);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int CustomErrorFn(
            [MarshalAs(UnmanagedType.LPWStr)] string title,
            [MarshalAs(UnmanagedType.LPWStr)] string text,
            [MarshalAs(UnmanagedType.LPWStr)] string icon,
            [MarshalAs(UnmanagedType.LPWStr)] string buttons,
            int hideCloseButton);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string fileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);

        public static void Run(PlayerConfig config)
        {
            string logFolder = ResolveLogFolder(config);
            string? error = null;
            int result = -2;

            try
            {
                if (string.IsNullOrWhiteSpace(logFolder))
                {
                    throw new InvalidOperationException("日志文件夹为空。");
                }

                Directory.CreateDirectory(logFolder);
                EnsureModule(config);

                var dllMissing = GetFunction<DllMissingErrorFn>("DLL_MISSING_ERROR");
                var exeError = GetFunction<ExeErrorFn>("EXE_ERROR");
                var memoryError = GetFunction<MemoryErrorFn>("MEMORY_ERROR");
                var customError = GetFunction<CustomErrorFn>("CUSTOM_ERROR");

                result = config.Function switch
                {
                    ReportFunction.DllMissing => dllMissing(
                        config.ErrorProgram, config.MissingDll),
                    ReportFunction.ExeError => exeError(
                        config.ErrorProgram, config.ErrorName, config.ErrorCode, config.Address),
                    ReportFunction.MemoryError => memoryError(
                        config.ErrorProgram, config.Operate, config.MemoryAddress, config.CannotBe),
                    ReportFunction.Custom => customError(
                        config.Title, config.Text, config.Icon, config.Buttons, config.HideCloseButton),
                    _ => -2,
                };
            }
            catch (Exception ex)
            {
                error = ex.ToString();
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                    WriteLog(logFolder, config, result, error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "播放弹窗失败，日志也无法写入：\n" + ex.Message,
                    "ErrorReportMain",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (error is not null)
            {
                MessageBox.Show(
                    "播放弹窗失败：\n" + error,
                    "ErrorReportMain",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string ResolveLogFolder(PlayerConfig config)
        {
            if (config.UseRuntimeFolder)
            {
                // 勾选“日志生成在运行文件夹”时，日志跟随播放器自身所在的文件夹。
                return AppContext.BaseDirectory;
            }

            return config.LogFolder ?? string.Empty;
        }

        private static void EnsureModule(PlayerConfig config)
        {
            if (_module != IntPtr.Zero)
            {
                return;
            }

            string? path = null;
            if (config.EmbeddedDll is { Length: > 0 })
            {
                path = ExtractEmbeddedDll(config.EmbeddedDll);
            }
            else
            {
                string localPath = Path.Combine(AppContext.BaseDirectory, DllName);
                if (File.Exists(localPath))
                {
                    path = localPath;
                }
            }

            _module = path is null ? LoadLibraryW(DllName) : LoadLibraryW(path);
            if (_module == IntPtr.Zero)
            {
                int code = Marshal.GetLastWin32Error();
                string detail = path is null ? string.Empty : "：" + path;
                throw new Win32Exception(
                    code,
                    "无法加载 " + DllName + detail);
            }
        }

        private static string ExtractEmbeddedDll(byte[] bytes)
        {
            string hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = ToHex(sha256.ComputeHash(bytes));
            }

            string cacheRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(cacheRoot))
            {
                cacheRoot = Path.GetTempPath();
            }

            string cacheDirectory = Path.Combine(cacheRoot, "ErrorReportMain", "Native");
            Directory.CreateDirectory(cacheDirectory);

            string dllPath = Path.Combine(cacheDirectory, "ErrorReportMain-" + hash + ".dll");
            if (!File.Exists(dllPath) || new FileInfo(dllPath).Length != bytes.Length)
            {
                string tempPath = dllPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(tempPath, bytes);
                try
                {
                    File.Move(tempPath, dllPath);
                }
                catch (IOException)
                {
                    if (!File.Exists(dllPath))
                    {
                        throw;
                    }

                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }

            return dllPath;
        }

        private static T GetFunction<T>(string name) where T : Delegate
        {
            IntPtr address = GetProcAddress(_module, name);
            if (address == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException("找不到 DLL 导出函数：" + name);
            }
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }
            return builder.ToString();
        }

        private static void WriteLog(string folder, PlayerConfig config, int result, string? error)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string path = Path.Combine(folder, $"ErrorReport-{timestamp}.log");
            for (int index = 1; File.Exists(path); index++)
            {
                path = Path.Combine(folder, $"ErrorReport-{timestamp}-{index}.log");
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Local Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Function: {GetFunctionName(config.Function)}");

            switch (config.Function)
            {
                case ReportFunction.DllMissing:
                    builder.AppendLine($"ErrorProgram: {config.ErrorProgram}");
                    builder.AppendLine($"MissingDll: {config.MissingDll}");
                    break;
                case ReportFunction.ExeError:
                    builder.AppendLine($"ErrorProgram: {config.ErrorProgram}");
                    builder.AppendLine($"ErrorName: {config.ErrorName}");
                    builder.AppendLine($"ErrorCode: {config.ErrorCode}");
                    builder.AppendLine($"Address: {config.Address}");
                    break;
                case ReportFunction.MemoryError:
                    builder.AppendLine($"ErrorProgram: {config.ErrorProgram}");
                    builder.AppendLine($"Operate: {config.Operate}");
                    builder.AppendLine($"Address: {config.MemoryAddress}");
                    builder.AppendLine($"CannotBe: {config.CannotBe}");
                    break;
                case ReportFunction.Custom:
                    builder.AppendLine($"Title: {config.Title}");
                    builder.AppendLine($"Text: {config.Text}");
                    builder.AppendLine($"Icon: {config.Icon}");
                    builder.AppendLine($"Buttons: {config.Buttons}");
                    builder.AppendLine($"HideCloseButton: {config.HideCloseButton}");
                    break;
            }

            builder.AppendLine($"User Choice: {result}");
            if (!string.IsNullOrEmpty(error))
            {
                builder.AppendLine($"Error: {error}");
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static string GetFunctionName(ReportFunction function)
        {
            return function switch
            {
                ReportFunction.DllMissing => "DLL_MISSING_ERROR",
                ReportFunction.ExeError => "EXE_ERROR",
                ReportFunction.MemoryError => "MEMORY_ERROR",
                ReportFunction.Custom => "CUSTOM_ERROR",
                _ => function.ToString(),
            };
        }
    }
}
