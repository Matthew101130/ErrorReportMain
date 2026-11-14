using System;
using System.Collections.Generic;
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int CustomMessageBoxFn(
            [MarshalAs(UnmanagedType.LPWStr)] string title,
            [MarshalAs(UnmanagedType.LPWStr)] string text,
            [MarshalAs(UnmanagedType.LPWStr)] string icon,
            [MarshalAs(UnmanagedType.LPWStr)] string buttons);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string fileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);

        public static void Run(PlayerConfig config)
        {
            try
            {
                string logFolder = ResolveLogFolder(config);
                if (string.IsNullOrWhiteSpace(logFolder))
                {
                    throw new InvalidOperationException("日志文件夹为空。");
                }

                Directory.CreateDirectory(logFolder);
                EnsureModule(config);

                List<PopupDefinition> popups = config.Popups.Count > 0
                    ? config.Popups
                    : new List<PopupDefinition> { config };
                ValidatePopups(popups);

                string logPath = CreateLogPath(logFolder);
                WriteLogHeader(logPath, popups.Count);

                for (int popupIndex = 0; popupIndex < popups.Count; popupIndex++)
                {
                    PopupDefinition popup = popups[popupIndex];
                    int iteration = 0;
                    while (popup.Infinite || iteration < Math.Max(1, popup.RepeatCount))
                    {
                        iteration++;
                        int result = -2;
                        string? error = null;
                        try
                        {
                            result = Invoke(popup);
                        }
                        catch (Exception ex)
                        {
                            error = ex.ToString();
                        }

                        AppendLogEntry(
                            logPath, popup, popupIndex + 1, popups.Count, iteration, result, error);

                        if (error is not null)
                        {
                            MessageBox.Show(
                                "播放弹窗失败：\n" + error,
                                "ErrorReportMain",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "播放弹窗组失败：\n" + ex.Message,
                    "ErrorReportMain",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static int Invoke(PopupDefinition popup)
        {
            return popup.Function switch
            {
                ReportFunction.DllMissing => GetFunction<DllMissingErrorFn>("DLL_MISSING_ERROR")(
                    popup.ErrorProgram, popup.MissingDll),
                ReportFunction.ExeError => GetFunction<ExeErrorFn>("EXE_ERROR")(
                    popup.ErrorProgram, popup.ErrorName, popup.ErrorCode, popup.Address),
                ReportFunction.MemoryError => GetFunction<MemoryErrorFn>("MEMORY_ERROR")(
                    popup.ErrorProgram, popup.Operate, popup.MemoryAddress, popup.CannotBe),
                ReportFunction.Custom => GetFunction<CustomErrorFn>("CUSTOM_ERROR")(
                    popup.Title, popup.Text, popup.Icon, popup.Buttons, popup.HideCloseButton),
                ReportFunction.CustomMessageBox => GetFunction<CustomMessageBoxFn>("CUSTOM_MESSAGEBOX")(
                    popup.Title, popup.Text, popup.Icon, popup.Buttons),
                _ => -2,
            };
        }

        private static void ValidatePopups(IReadOnlyList<PopupDefinition> popups)
        {
            if (popups.Count == 0)
            {
                throw new InvalidOperationException("弹窗组为空。");
            }

            for (int i = 0; i < popups.Count; i++)
            {
                PopupDefinition popup = popups[i];
                if (!popup.Infinite && popup.RepeatCount < 1)
                {
                    throw new InvalidOperationException("弹窗次数必须是正整数。");
                }
                if (popup.Infinite && i != popups.Count - 1)
                {
                    throw new InvalidOperationException(
                        "无限循环弹窗必须是最后一个；它后面的弹窗永远不会执行。");
                }
            }
        }

        private static string ResolveLogFolder(PlayerConfig config)
        {
            if (config.UseRuntimeFolder)
            {
                // 日志跟随播放器自身所在的文件夹。
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

        private static string CreateLogPath(string folder)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string path = Path.Combine(folder, $"ErrorReport-{timestamp}.log");
            for (int index = 1; File.Exists(path); index++)
            {
                path = Path.Combine(folder, $"ErrorReport-{timestamp}-{index}.log");
            }
            return path;
        }

        private static void WriteLogHeader(string path, int popupCount)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Local Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Popup Count: {popupCount}");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static void AppendLogEntry(
            string path,
            PopupDefinition popup,
            int popupIndex,
            int popupCount,
            int iteration,
            int result,
            string? error)
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine($"--- Popup {popupIndex}/{popupCount} ---");
            builder.AppendLine($"Function: {GetFunctionName(popup.Function)}");

            switch (popup.Function)
            {
                case ReportFunction.DllMissing:
                    builder.AppendLine($"ErrorProgram: {popup.ErrorProgram}");
                    builder.AppendLine($"MissingDll: {popup.MissingDll}");
                    break;
                case ReportFunction.ExeError:
                    builder.AppendLine($"ErrorProgram: {popup.ErrorProgram}");
                    builder.AppendLine($"ErrorName: {popup.ErrorName}");
                    builder.AppendLine($"ErrorCode: {popup.ErrorCode}");
                    builder.AppendLine($"Address: {popup.Address}");
                    break;
                case ReportFunction.MemoryError:
                    builder.AppendLine($"ErrorProgram: {popup.ErrorProgram}");
                    builder.AppendLine($"Operate: {popup.Operate}");
                    builder.AppendLine($"Address: {popup.MemoryAddress}");
                    builder.AppendLine($"CannotBe: {popup.CannotBe}");
                    break;
                case ReportFunction.Custom:
                    builder.AppendLine($"Title: {popup.Title}");
                    builder.AppendLine($"Text: {popup.Text}");
                    builder.AppendLine($"Icon: {popup.Icon}");
                    builder.AppendLine($"Buttons: {popup.Buttons}");
                    builder.AppendLine($"HideCloseButton: {popup.HideCloseButton}");
                    break;
                case ReportFunction.CustomMessageBox:
                    builder.AppendLine($"Title: {popup.Title}");
                    builder.AppendLine($"Text: {popup.Text}");
                    builder.AppendLine($"Icon: {popup.Icon}");
                    builder.AppendLine($"Buttons: {popup.Buttons}");
                    break;
            }

            builder.AppendLine(popup.Infinite
                ? $"Repeat: {iteration}/∞"
                : $"Repeat: {iteration}/{Math.Max(1, popup.RepeatCount)}");
            builder.AppendLine($"User Choice: {result}");
            if (!string.IsNullOrEmpty(error))
            {
                builder.AppendLine($"Error: {error}");
            }

            File.AppendAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static string GetFunctionName(ReportFunction function)
        {
            return function switch
            {
                ReportFunction.DllMissing => "DLL_MISSING_ERROR",
                ReportFunction.ExeError => "EXE_ERROR",
                ReportFunction.MemoryError => "MEMORY_ERROR",
                ReportFunction.Custom => "CUSTOM_ERROR",
                ReportFunction.CustomMessageBox => "CUSTOM_MESSAGEBOX",
                _ => function.ToString(),
            };
        }
    }
}
