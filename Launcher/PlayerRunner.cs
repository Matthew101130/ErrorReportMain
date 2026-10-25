using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ErrorReportLauncher
{
    /// <summary>
    /// 生成 exe 的播放模式：加载同目录的 ErrorReportMain.dll，调用选中的接口，
    /// 等用户关闭弹窗后把结果写入日志文件夹。
    /// </summary>
    internal static class PlayerRunner
    {
        private const string DllName = "ErrorReportMain.dll";

        [DllImport(DllName, CharSet = CharSet.Unicode, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int DLL_MISSING_ERROR(string errorProgram, string missingDll);

        [DllImport(DllName, CharSet = CharSet.Unicode, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int EXE_ERROR(string errorProgram, string errorName,
            string errorCode, string address);

        [DllImport(DllName, CharSet = CharSet.Unicode, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int MEMORY_ERROR(string errorProgram, string operate,
            string address, string cannotBe);

        [DllImport(DllName, CharSet = CharSet.Unicode, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int CUSTOM_ERROR(string title, string text,
            string icon, string buttons, int hideCloseButton);

        public static void Run(PlayerConfig config)
        {
            string logFolder = config.LogFolder ?? string.Empty;
            string? error = null;
            int result = -2;

            try
            {
                if (string.IsNullOrWhiteSpace(logFolder))
                {
                    throw new InvalidOperationException("日志文件夹为空。");
                }

                Directory.CreateDirectory(logFolder);
                result = config.Function switch
                {
                    ReportFunction.DllMissing => DLL_MISSING_ERROR(
                        config.ErrorProgram, config.MissingDll),
                    ReportFunction.ExeError => EXE_ERROR(
                        config.ErrorProgram, config.ErrorName, config.ErrorCode, config.Address),
                    ReportFunction.MemoryError => MEMORY_ERROR(
                        config.ErrorProgram, config.Operate, config.MemoryAddress, config.CannotBe),
                    ReportFunction.Custom => CUSTOM_ERROR(
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
