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

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr dc, int index);

        private const int LogPixelsX = 88;

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
                    // Windows 7 没有 GetDpiForWindow，下面用 GDI 回退。
                }
                catch (DllNotFoundException)
                {
                    // 理论上 user32 总是存在；保留回退路径。
                }
            }

            if (dpi == 0 && hwnd != IntPtr.Zero)
            {
                IntPtr dc = GetDC(hwnd);
                if (dc != IntPtr.Zero)
                {
                    int gdiDpi = GetDeviceCaps(dc, LogPixelsX);
                    ReleaseDC(hwnd, dc);
                    if (gdiDpi > 0)
                    {
                        dpi = (uint)gdiDpi;
                    }
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
