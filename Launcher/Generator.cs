using System;
using System.IO;

namespace ErrorReportLauncher
{
    /// <summary>
    /// 生成单文件播放器：复制启动器 exe，复制 ErrorReportMain.dll 到目标目录，
    /// 然后把配置追加到 exe 末尾。
    /// </summary>
    public static class Generator
    {
        public static void Generate(
            string launcherPath,
            string dllPath,
            string outputPath,
            PlayerConfig config)
        {
            if (string.IsNullOrWhiteSpace(launcherPath) || !File.Exists(launcherPath))
            {
                throw new FileNotFoundException("找不到启动器自身。", launcherPath);
            }
            if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
            {
                throw new FileNotFoundException("找不到 ErrorReportMain.dll。", dllPath);
            }
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("输出路径为空。", nameof(outputPath));
            }
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string fullLauncher = Path.GetFullPath(launcherPath);
            string fullOutput = Path.GetFullPath(outputPath);
            if (string.Equals(fullLauncher, fullOutput, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("输出文件不能覆盖正在运行的启动器。");
            }

            string? outputDirectory = Path.GetDirectoryName(fullOutput);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new InvalidOperationException("无法确定输出目录。");
            }
            Directory.CreateDirectory(outputDirectory);

            File.Copy(fullLauncher, fullOutput, overwrite: true);

            string dllDestination = Path.Combine(outputDirectory, Path.GetFileName(dllPath));
            if (!string.Equals(
                    Path.GetFullPath(dllPath),
                    Path.GetFullPath(dllDestination),
                    StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(dllPath, dllDestination, overwrite: true);
            }

            PlayerConfig.AppendTo(fullOutput, config);
        }
    }
}
