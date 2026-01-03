using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace itasset {
    /// <summary>
    /// 共有監査CSV：1共有=1行（追記）
    /// </summary>
    public static class ShareCsvAppender {
        public static string DefaultFileName = AppConfig.ShareCsvOutputFile;

        public static string Append(string outputDirectory, AssetInfo asset) {
            if (asset == null) throw new ArgumentNullException(nameof(asset));

            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = ".";

            Directory.CreateDirectory(outputDirectory);

            var outPath = Path.Combine(outputDirectory, DefaultFileName);
            bool exists = File.Exists(outPath);

            var list = new List<ShareAuditInfo>();

            if (asset.ShareAudits != null && asset.ShareAudits.Count > 0) {
                foreach (var s in asset.ShareAudits) {
                    // Ensure asset identity fields are present
                    s.Hostname = asset.Hostname;
                    s.Domain = asset.Domain;
                    s.OS = asset.OS;
                    s.OSVersion = asset.OSVersion;
                    s.IP = asset.IP;
                    list.Add(s);
                }
            } else {
                // 共有が無い端末も 1行は残す（監査で未設定を把握できる）
                list.Add(new ShareAuditInfo {
                    Hostname = asset.Hostname,
                    Domain = asset.Domain,
                    OS = asset.OS,
                    OSVersion = asset.OSVersion,
                    IP = asset.IP,
                    ShareName = "",
                    SharePath = "",
                    Share_Everyone = "NONE",
                    Share_AuthenticatedUsers = "NONE",
                    Share_Users = "NONE",
                    Share_Admins = "NONE",
                    NTFS_Everyone = "NONE",
                    NTFS_AuthenticatedUsers = "NONE",
                    NTFS_Users = "NONE",
                    NTFS_Admins = "NONE",
                    NTFS_OtherWrite = "FALSE",
                    Effective_Everyone = "NONE",
                    Effective_AuthenticatedUsers = "NONE",
                    Effective_Users = "NONE",
                    Risk_Level = "Low",
                    Risk_Reason = "No SMB shares"
                });
            }

            using (var fs = new FileStream(outPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: !exists))) {
                if (!exists) {
                    sw.WriteLine(Header());
                }
                foreach (var s in list) {
                    sw.WriteLine(Row(s));
                }
            }

            return outPath;
        }

        private static string Header() {
            return string.Join(",",
                "ホスト名","ドメイン","OS","OSバージョン","IPアドレス",
                "共有名","共有パス",
                "共有権限_Everyone","共有権限_認証済みユーザー","共有権限_Users","共有権限_Administrators",
                "NTFS権限_Everyone","NTFS権限_認証済みユーザー","NTFS権限_Users","NTFS権限_Administrators","NTFS_その他書込あり",
                "実効権限_Everyone","実効権限_認証済みユーザー","実効権限_Users",
                "リスクレベル","判定理由"
            );
        }

        private static string Row(ShareAuditInfo s) {
            return string.Join(",",
                Escape(s.Hostname),
                Escape(s.Domain),
                Escape(s.OS),
                Escape(s.OSVersion),
                Escape(s.IP),

                Escape(s.ShareName),
                Escape(s.SharePath),

                Escape(s.Share_Everyone),
                Escape(s.Share_AuthenticatedUsers),
                Escape(s.Share_Users),
                Escape(s.Share_Admins),

                Escape(s.NTFS_Everyone),
                Escape(s.NTFS_AuthenticatedUsers),
                Escape(s.NTFS_Users),
                Escape(s.NTFS_Admins),
                Escape(s.NTFS_OtherWrite),

                Escape(s.Effective_Everyone),
                Escape(s.Effective_AuthenticatedUsers),
                Escape(s.Effective_Users),

                Escape(s.Risk_Level),
                Escape(s.Risk_Reason)
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
