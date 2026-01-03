
namespace itasset {
    partial class MainForm {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnCollect;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent() {
            btnCollect = new System.Windows.Forms.Button { Text = "① 収集", Width = 120, Left = 12, Top = 12 };
            btnSave = new System.Windows.Forms.Button { Text = "② CSV追記", Width = 160, Left = 140, Top = 12 };
            lblStatus = new System.Windows.Forms.Label { Text = "待機中", Left = 320, Top = 18, AutoSize = true };
            grid = new System.Windows.Forms.DataGridView {
                Left = 12,
                Top = 48,
                Width = 1100,
                Height = 560,
                ReadOnly = true,
                AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells
            };

            Controls.AddRange(new System.Windows.Forms.Control[] { btnCollect, btnSave, lblStatus, grid });
            Text = "USB Asset Collector (CSV Append + Share Audit)";
            Width = 1140; Height = 660; StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            btnCollect.Click += BtnCollect_Click;
            btnSave.Click += BtnSave_Click;
        }
    }
}
