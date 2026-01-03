using System;

namespace itasset {
    // 1共有 = 1監査レコード（CSV 1行）
    public class ShareAuditInfo {
        // Asset identity
        public string Hostname { get; set; }
        public string Domain { get; set; }
        public string OS { get; set; }
        public string OSVersion { get; set; }
        public string IP { get; set; }

        // Share
        public string ShareName { get; set; }
        public string SharePath { get; set; }

        // Share permission (Advanced Sharing -> Permissions)
        public string Share_Everyone { get; set; }
        public string Share_AuthenticatedUsers { get; set; }
        public string Share_Users { get; set; }
        public string Share_Admins { get; set; }

        // NTFS permission (Security tab)
        public string NTFS_Everyone { get; set; }
        public string NTFS_AuthenticatedUsers { get; set; }
        public string NTFS_Users { get; set; }
        public string NTFS_Admins { get; set; }
        public string NTFS_OtherWrite { get; set; } // TRUE/FALSE

        // Effective permission (Share ∧ NTFS)
        public string Effective_Everyone { get; set; }
        public string Effective_AuthenticatedUsers { get; set; }
        public string Effective_Users { get; set; }

        // Ransomware-oriented risk evaluation
        public string Risk_Level { get; set; }
        public string Risk_Reason { get; set; }

        public static string CsvHeader() {
            return string.Join(",",
                "Hostname","Domain","OS","OSVersion","IP",
                "ShareName","SharePath",
                "Share_Everyone","Share_AuthenticatedUsers","Share_Users","Share_Admins",
                "NTFS_Everyone","NTFS_AuthenticatedUsers","NTFS_Users","NTFS_Admins","NTFS_OtherWrite",
                "Effective_Everyone","Effective_AuthenticatedUsers","Effective_Users",
                "Risk_Level","Risk_Reason"
            );
        }
    }
}
