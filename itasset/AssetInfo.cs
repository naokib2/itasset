using System;
using System.Collections.Generic;
using System.Data;

namespace itasset {
    public class AssetInfo {
        public string Hostname { get; set; }
        public string Domain { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string CPU { get; set; }
        public double RAM_GB { get; set; }
        public string OS { get; set; }
        public string OSVersion { get; set; }
        public string SerialNumber { get; set; }
        public string MAC { get; set; }
        public string IP { get; set; }
        public string GPU { get; set; }
        public int Disk_TotalGB { get; set; }
        public int Disk_FreeGB { get; set; }
        public string TPM { get; set; }
        public string SecureBoot { get; set; }
        public string BitLocker { get; set; }
        public string Category { get; set; }
        public string LifecycleStatus { get; set; }
        public string Notes { get; set; }
        public string LastInventoryDate { get; set; }

        // Defender / AV 情報
        public string AV_Products { get; set; }
        public string AV_ProductStates { get; set; }
        public string Defender_Enabled { get; set; }
        public string Defender_RTEnabled { get; set; }
        public string Defender_SigDate { get; set; }
        public string Defender_EngineVer { get; set; }

        public string WindowsUpdateLatestDate { get; set; }

        // 共有監査（1共有=1行でCSV出力する）
        public List<ShareAuditInfo> ShareAudits { get; set; } = new List<ShareAuditInfo>();

        // 端末単位の共有リスク（ShareAudits から集計）
        public string ShareRisk {
            get {
                try { return RiskEvaluator.SummarizeOverallRisk(ShareAudits); }
                catch { return "Low"; }
            }
        }

        // 画面表示用（端末単位の概要）
        public DataTable ToDataTable() {
            var dt = new DataTable();
            dt.Columns.Add("シリアル番号");
            dt.Columns.Add("ホスト名");
            dt.Columns.Add("メーカー");
            dt.Columns.Add("モデル");
            dt.Columns.Add("OS");
            dt.Columns.Add("OSバージョン");
            dt.Columns.Add("CPU");
            dt.Columns.Add("メモリ(GB)");
            dt.Columns.Add("ディスク総容量(GB)");
            dt.Columns.Add("ディスク空き容量(GB)");
            dt.Columns.Add("GPU");
            dt.Columns.Add("IPアドレス");
            dt.Columns.Add("MACアドレス");
            dt.Columns.Add("ドメイン");
            dt.Columns.Add("BitLocker");
            dt.Columns.Add("TPM");
            dt.Columns.Add("セキュアブート");
            dt.Columns.Add("最終取得日時");
            dt.Columns.Add("Windows Update 最終更新日");
            dt.Columns.Add("AV製品");
            dt.Columns.Add("AV状態");
            dt.Columns.Add("Defender有効");
            dt.Columns.Add("Defenderリアルタイム保護");
            dt.Columns.Add("Defender定義日");
            dt.Columns.Add("DefenderエンジンVer");
            dt.Columns.Add("共有リスク");

            var row = dt.NewRow();
            row["シリアル番号"] = SerialNumber ?? "";
            row["ホスト名"] = Hostname ?? "";
            row["メーカー"] = Manufacturer ?? "";
            row["モデル"] = Model ?? "";
            row["OS"] = OS ?? "";
            row["OSバージョン"] = OSVersion ?? "";
            row["CPU"] = CPU ?? "";
            row["メモリ(GB)"] = RAM_GB.ToString();
            row["ディスク総容量(GB)"] = Disk_TotalGB.ToString();
            row["ディスク空き容量(GB)"] = Disk_FreeGB.ToString();
            row["GPU"] = GPU ?? "";
            row["IPアドレス"] = IP ?? "";
            row["MACアドレス"] = MAC ?? "";
            row["ドメイン"] = Domain ?? "";
            row["BitLocker"] = BitLocker ?? "";
            row["TPM"] = TPM ?? "";
            row["セキュアブート"] = SecureBoot ?? "";
            row["最終取得日時"] = LastInventoryDate ?? "";
            row["Windows Update 最終更新日"] = WindowsUpdateLatestDate ?? "";
            row["AV製品"] = AV_Products ?? "";
            row["AV状態"] = AV_ProductStates ?? "";
            row["Defender有効"] = Defender_Enabled ?? "";
            row["Defenderリアルタイム保護"] = Defender_RTEnabled ?? "";
            row["Defender定義日"] = Defender_SigDate ?? "";
            row["DefenderエンジンVer"] = Defender_EngineVer ?? "";
            row["共有リスク"] = ShareRisk ?? "";

            dt.Rows.Add(row);
            return dt;
        }
    }
}
