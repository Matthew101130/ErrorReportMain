using System;
using System.Windows.Forms;

namespace ErrorReportLauncher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (PlayerConfig.TryReadFrom(Application.ExecutablePath, out PlayerConfig config))
            {
                PlayerRunner.Run(config);
                return;
            }

            Application.Run(new MainForm());
        }
    }
}
