using System.Windows.Forms;

namespace itasset {
    public partial class SecurePrompt : Form {
        private readonly TextBox _tb;
        private readonly Button _ok;
        private readonly Button _cancel;
        public string PasswordText => _tb.Text;

        public SecurePrompt(string title) {
            Text = title;
            Width = 420; Height = 140; StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;

            var lbl = new Label { Text = "入力:", Left = 12, Top = 16, AutoSize = true };
            _tb = new TextBox { Left = 60, Top = 12, Width = 330, UseSystemPasswordChar = true };

            _ok = new Button { Text = "OK", Left = 220, Top = 50, Width = 80, DialogResult = DialogResult.OK };
            _cancel = new Button { Text = "キャンセル", Left = 310, Top = 50, Width = 80, DialogResult = DialogResult.Cancel };

            Controls.Add(lbl); Controls.Add(_tb); Controls.Add(_ok); Controls.Add(_cancel);
            AcceptButton = _ok; CancelButton = _cancel;
        }
    }
}
