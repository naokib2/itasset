using System;
using System.IO;
using System.Windows.Forms;

namespace itasset {
    internal static class Program {
        [STAThread]
        static void Main() {

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.ToString(), "ThreadException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show(e.ExceptionObject?.ToString() ?? "(null)", "UnhandledException", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new itasset.MainForm()); // ← ここでフォームを必ず起動
        }



        static void ShowCrash(Exception ex) {
            try {
                var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                var log = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(log, ex.ToString());
                MessageBox.Show(
                    "起動時にエラーが発生しました。\n\n" + ex.Message +
                    $"\n\nログ: {log}", "UsbAssetCollector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch {
                MessageBox.Show(ex.ToString(), "UsbAssetCollector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Environment.Exit(1);
        }
    }
}

