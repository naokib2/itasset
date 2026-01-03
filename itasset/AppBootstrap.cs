using System.Drawing;
using System.Windows.Forms;

namespace itasset {
    internal static class AppBootstrap {
        public static void Initialize() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // 高DPI対応とフォント設定を手動指定
            var defaultFont = new Font("Yu Gothic UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        }
    }
}
