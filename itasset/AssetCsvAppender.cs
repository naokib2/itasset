using System;
using System.IO;
using System.Text;

namespace itasset {
    /// <summary>
    /// 資産CSV：端末=1行（追記）
    /// </summary>
    public static class AssetCsvAppender {
        public static string DefaultFileName = AppConfig.AssetCsvOutputFile;

        public static string Append(string outputDirectory, AssetInfo asset) {
            if (asset == null) throw new ArgumentNullException(nameof(asset));

            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = ".";

            Directory.CreateDirectory(outputDirectory);

            var outPath = Path.Combine(outputDirectory, DefaultFileName);
            bool exists = File.Exists(outPath);

            // Share関連の総合リスク（最大値）
            var shareRisk = RiskEvaluator.SummarizeOverallRisk(asset.ShareAudits);

            using (var fs = new FileStream(outPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: !exists))) {
                if (!exists) {
                    sw.WriteLine(Header());
                }
                sw.WriteLine(Row(asset, shareRisk));
            }

            return outPath;
        }

        private static string Header() {
            // AssetInfo の主要列 + ShareRisk
            return string.Join(",",
                "シリアル番号","ホスト名","ドメイン",
                "メーカー","モデル","CPU","メモリ(GB)",
                "OS","OSバージョン","GPU",
                "ディスク総容量(GB)","ディスク空き容量(GB)",
                "IPアドレス","MACアドレス",
                "TPM","セキュアブート","BitLocker",
                "最終取得日時",
                "AV製品","AV状態","Defender有効","Defenderリアルタイム保護","Defender定義日","DefenderエンジンVer",
                "共有リスク"
            );
        }

        private static string Row(AssetInfo a, string shareRisk) {
            return string.Join(",",
                Escape(a.SerialNumber),
                Escape(a.Hostname),
                Escape(a.Domain),

                Escape(a.Manufacturer),
                Escape(a.Model),
                Escape(a.CPU),
                Escape(a.RAM_GB.ToString("0.##")),

                Escape(a.OS),
                Escape(a.OSVersion),
                Escape(a.GPU),

                Escape(a.Disk_TotalGB.ToString()),
                Escape(a.Disk_FreeGB.ToString()),

                Escape(a.IP),
                Escape(a.MAC),

                Escape(a.TPM),
                Escape(a.SecureBoot),
                Escape(a.BitLocker),

                Escape(a.LastInventoryDate),

                Escape(a.AV_Products),
                Escape(a.AV_ProductStates),
                Escape(a.Defender_Enabled),
                Escape(a.Defender_RTEnabled),
                Escape(a.Defender_SigDate),
                Escape(a.Defender_EngineVer),

                Escape(shareRisk)
            );
        }

        public static string Escape(string s)
        {
            if (s == null) return "";
            // RFC4180 style escaping
            if (s.Contains("\"")) s = s.Replace("\"", "\"\"");
            if (s.Contains(",") || s.Contains("\r") || s.Contains("\n"))
                return "\"" + s + "\"";
            return s;
        }
    }
}
