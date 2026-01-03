using System;
using System.Collections.Generic;

namespace itasset {
    public static class RiskEvaluator {

        // ShareAuditInfo（共有=1行）に対して、ランサムウェア視点のリスクを付与
        public static void Evaluate(ShareAuditInfo s) {
            var effEveryone = Parse(s.Effective_Everyone);
            var effAuth = Parse(s.Effective_AuthenticatedUsers);
            var effUsers = Parse(s.Effective_Users);

            bool writeEveryone = PermissionUtils.IsWrite(effEveryone);
            bool writeAuth = PermissionUtils.IsWrite(effAuth);
            bool writeUsers = PermissionUtils.IsWrite(effUsers);

            // 優先順位：Critical > High > Mid > Low
            if (writeEveryone) {
                s.Risk_Level = "Critical";
                s.Risk_Reason = "Everyone can write via SMB (effective)";
                return;
            }

            if (writeAuth) {
                s.Risk_Level = "Critical";
                s.Risk_Reason = "Authenticated Users can write via SMB (effective)";
                return;
            }

            if (writeUsers) {
                s.Risk_Level = "Critical";
                s.Risk_Reason = "Users can write via SMB (effective)";
                return;
            }

            if (string.Equals(s.NTFS_OtherWrite, "TRUE", StringComparison.OrdinalIgnoreCase)) {
                s.Risk_Level = "High";
                s.Risk_Reason = "Unexpected NTFS write permission detected (other principals)";
                return;
            }

            // Share Everyone FULL is an operational smell (easy to become risky by NTFS drift)
            if (string.Equals(s.Share_Everyone, "FULL", StringComparison.OrdinalIgnoreCase)) {
                s.Risk_Level = "Mid";
                s.Risk_Reason = "Share permission has Everyone=FULL; ensure NTFS is strictly controlled";
                return;
            }

            s.Risk_Level = "Low";
            s.Risk_Reason = "No SMB write permission for common user principals";
        }

        // 端末単位の「Share関連総合リスク」を最大値で返す
        public static string SummarizeOverallRisk(List<ShareAuditInfo> shares) {
            if (shares == null || shares.Count == 0) return "Low";

            int best = 0;
            string bestLabel = "Low";

            foreach (var s in shares) {
                // 未評価なら評価
                if (string.IsNullOrWhiteSpace(s.Risk_Level)) {
                    Evaluate(s);
                }

                int rank = Rank(s.Risk_Level);
                if (rank > best) {
                    best = rank;
                    bestLabel = NormalizeLabel(s.Risk_Level);
                }
            }
            return bestLabel;
        }

        private static int Rank(string level) {
            level = NormalizeLabel(level);
            switch (level) {
                case "Critical": return 4;
                case "High": return 3;
                case "Mid": return 2;
                case "Low": return 1;
                default: return 0;
            }
        }

        private static string NormalizeLabel(string level) {
            if (string.IsNullOrWhiteSpace(level)) return "Low";
            var s = level.Trim();

            // 旧表記互換
            if (s.Equals("CRITICAL", StringComparison.OrdinalIgnoreCase)) return "Critical";
            if (s.Equals("HIGH", StringComparison.OrdinalIgnoreCase)) return "High";
            if (s.Equals("MEDIUM", StringComparison.OrdinalIgnoreCase)) return "Mid";
            if (s.Equals("MID", StringComparison.OrdinalIgnoreCase)) return "Mid";
            if (s.Equals("LOW", StringComparison.OrdinalIgnoreCase)) return "Low";

            // 既に期待表記ならそのまま
            if (s.Equals("Critical", StringComparison.OrdinalIgnoreCase)) return "Critical";
            if (s.Equals("High", StringComparison.OrdinalIgnoreCase)) return "High";
            if (s.Equals("Mid", StringComparison.OrdinalIgnoreCase)) return "Mid";
            if (s.Equals("Low", StringComparison.OrdinalIgnoreCase)) return "Low";

            return "Low";
        }

        private static PermLevel Parse(string s) {
            if (string.IsNullOrWhiteSpace(s)) return PermLevel.NONE;
            PermLevel p;
            if (Enum.TryParse<PermLevel>(s.Trim(), true, out p)) return p;
            return PermLevel.NONE;
        }
    }
}
