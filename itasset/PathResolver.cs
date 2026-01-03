using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace itasset {
    internal static class PathResolver {
        /// <summary>
        /// USBメモリ上の作業ルートを返す。
        /// 優先順：
        /// 1) 実行中のEXEがUSB上 → そのドライブ\UsbAssetCollector
        /// 2) 挿さっているUSBが1本 → そのドライブ\UsbAssetCollector
        /// 3) フォルダ選択ダイアログでユーザー指定
        /// </summary>
        public static string GetUsbWorkRoot(IWin32Window owner, string leaf = "UsbAssetCollector") {
            // 1) 実行中EXEのドライブがUSBなら、そこを使う
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var exeRoot = Path.GetPathRoot(exeDir) ?? "";
            if (exeRoot.Length > 0) {
                var d = DriveInfo.GetDrives()
                                 .FirstOrDefault(x => string.Equals(x.Name, exeRoot, StringComparison.OrdinalIgnoreCase));
                if (d != null && d.IsReady && d.DriveType == DriveType.Removable)
                    return EnsureDir(Path.Combine(d.RootDirectory.FullName, leaf));
            }

            // 2) USBが1本だけ挿さっている場合はそれを使う
            var usbs = DriveInfo.GetDrives()
                                .Where(x => x.IsReady && x.DriveType == DriveType.Removable)
                                .ToList();
            if (usbs.Count == 1) {
                return EnsureDir(Path.Combine(usbs[0].RootDirectory.FullName, leaf));
            }

            // 3) ユーザーに選ばせる（USB以外も選べるが、ここではユーザー判断を優先）
            using (var fbd = new FolderBrowserDialog()) {
                fbd.Description = "出力先（USBメモリ）フォルダを選択してください";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog(owner) == DialogResult.OK &&
                    !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                    return EnsureDir(Path.Combine(fbd.SelectedPath, leaf));
                }
            }

            // フォールバック：EXEディレクトリ配下（最悪時）
            return EnsureDir(Path.Combine(exeDir, leaf));
        }

        private static string EnsureDir(string path) {
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
