using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ErrorReportLauncher
{
    /// <summary>
    /// 不依赖 WinForms 的 App.config DpiAwareness 设置，直接从窗口读取实际 DPI。
    /// 这样启动器即使没有 ErrorReportLauncher.exe.config 也能按系统 DPI 缩放。
    /// </summary>
    internal static class DpiHelper
    {
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public static float GetScale(IntPtr hwnd)
        {
            uint dpi = 0;
            if (hwnd != IntPtr.Zero)
            {
                try
                {
                    dpi = GetDpiForWindow(hwnd);
                }
                catch (EntryPointNotFoundException)
                {
                    // Windows 7 没有 GetDpiForWindow，下面用 GDI+ 回退。
                }
            }

            if (dpi == 0)
            {
                using var graphics = Graphics.FromHwnd(hwnd);
                dpi = (uint)Math.Round(graphics.DpiX);
            }

            return dpi > 0 ? dpi / 96f : 1f;
        }
    }
}
