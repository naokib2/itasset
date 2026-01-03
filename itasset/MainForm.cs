using itasset;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;


namespace itasset {
    public partial class MainForm : Form {
        private AssetInfo _current;         // ← C#7.3 では ? を使わない（null 可）
        private readonly string _workRoot;  // USB作業ルート

        public MainForm() {
            try {
                InitializeComponent();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "InitializeComponent 例外", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }

            // ここで USB 作業ルートを決定（PathResolver は後述）
            _workRoot = PathResolver.GetUsbWorkRoot(this);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            TopMost = false;
            MinimumSize = new Size(800, 500);

            this.Text = "UsbAssetCollector - 起動中...";
            this.Load += MainForm_Load;
            this.Shown += delegate { this.Activate(); };
        }

        private void MainForm_Load(object sender, EventArgs e) // ← ? を外す
        {
            try {
                // もし “実行ディレクトリ側にDBを置きたい” 仕様ならこちらを使う
                // _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AssetInventory.db");
                // Db.Init(_dbPath);

                this.Text = "UsbAssetCollector - Ready,  "+ "UsbAssetCollector - WorkRoot: " + _workRoot; 
            }
            catch (Exception ex) {
                MessageBox.Show(this, "初期化に失敗しました。\n\n" + ex,
                    "初期化エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnCollect_Click(object sender, EventArgs e) // ← ? を外す
        {
            try {
                ToggleUi(false);
                lblStatus.Text = "収集中...";
                // 収集クラスは衝突回避で AssetCollectorRuntime に改名済み
                _current = await AssetCollectorRuntime.CollectAsync();
                grid.DataSource = _current.ToDataTable();
                lblStatus.Text = "収集完了";
            }
            catch (Exception ex) {
                lblStatus.Text = "収集失敗";
                MessageBox.Show(this, ex.ToString(), "収集エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { ToggleUi(true); }
        }

        private void BtnSave_Click(object sender, EventArgs e) // ← ? を外す
        {
            if (_current == null) {
                MessageBox.Show(this, "先に「① 収集」を実行してください。", "情報",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try {
                // CSV（追記のみ）
                var assetCsv = AssetCsvAppender.Append(_workRoot, _current);
                var shareCsv = ShareCsvAppender.Append(_workRoot, _current);

                lblStatus.Text = "CSV追記済み（Asset + ShareAudit）";

                // 確認のため自動オープン（不要ならコメントアウト）
                try {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = assetCsv,
                        UseShellExecute = true
                    });
                }
                catch { }

                MessageBox.Show(this, "CSV に追記しました。\r\nAsset: " + assetCsv + "\r\nShare: " + shareCsv,
                    "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) {
                lblStatus.Text = "出力失敗";
                MessageBox.Show(this, ex.Message, "保存エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleUi(bool enabled) {
            btnCollect.Enabled = enabled;
            btnSave.Enabled = enabled;
            UseWaitCursor = !enabled;
        }
    }
}
