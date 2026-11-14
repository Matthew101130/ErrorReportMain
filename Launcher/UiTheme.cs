using System;

namespace ErrorReportLauncher
{
    internal static class UiTheme
    {
        /// <summary>
        /// Windows 7/8/8.1 没有 Win11 的圆角输入框样式，
        /// 这里退回标准 Win32 EDIT，让输入框跟随系统视觉样式。
        /// </summary>
        public static bool UseNativeTextStyle { get; } = UseLegacyNativeTextStyle();

        public static bool UseLegacyNativeTextStyle()
        {
            Version version = Environment.OSVersion.Version;
            return version.Major < 10;
        }
    }
}
