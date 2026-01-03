namespace itasset {
    internal static class AppConfig {
        // CSV 出力（追記）
        public const string AssetCsvOutputFile = "IT_Asset_Register_Collected.csv";
        public const string ShareCsvOutputFile = "IT_Asset_Share_Audit.csv";

        // 自動化用 環境変数（設定されていれば優先）
        public const string EnvExcelPw = "ASSET_EXCEL_PW";
    }
}
