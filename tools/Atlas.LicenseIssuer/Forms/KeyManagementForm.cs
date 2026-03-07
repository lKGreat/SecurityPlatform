using Atlas.LicenseIssuer.Services;

namespace Atlas.LicenseIssuer.Forms;

public sealed class KeyManagementForm : Form
{
    private readonly KeyManagementService _keyMgmt;

    public KeyManagementForm(KeyManagementService keyMgmt)
    {
        _keyMgmt = keyMgmt;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "密钥管理";
        Size = new Size(620, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(16),
            WrapContents = false
        };

        panel.Controls.Add(new Label
        {
            Text = "当前公钥（嵌入平台的 ECDSA P-256 公钥）：",
            AutoSize = true
        });

        var pubkeyBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Width = 560,
            Height = 120,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9),
            Text = _keyMgmt.IsKeyInitialized()
                ? _keyMgmt.ExportPublicKeyPem()
                : "（尚未初始化密钥对）"
        };
        panel.Controls.Add(pubkeyBox);

        var btnRow = new FlowLayoutPanel { AutoSize = true };

        var copyBtn = new Button { Text = "复制公钥", Width = 90, Height = 32 };
        copyBtn.Click += (s, e) =>
        {
            Clipboard.SetText(pubkeyBox.Text);
            MessageBox.Show("公钥已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var regenBtn = new Button { Text = "重新生成密钥对", Width = 130, Height = 32, ForeColor = Color.DarkRed };
        regenBtn.Click += (s, e) =>
        {
            var confirm = MessageBox.Show(
                "重新生成密钥对后，旧公钥将失效，已颁发的证书无法被新平台验证。\n确定继续吗？",
                "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            using var dlg = new InitKeyForm(_keyMgmt);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                pubkeyBox.Text = _keyMgmt.ExportPublicKeyPem();
                MessageBox.Show("密钥对已重新生成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        btnRow.Controls.AddRange([copyBtn, regenBtn]);
        panel.Controls.Add(btnRow);

        panel.Controls.Add(new Label
        {
            Text = "⚠ 请将公钥交给平台方，嵌入 LicenseSignatureService.cs 中的 EmbeddedPublicKeyPem 常量。",
            AutoSize = true,
            ForeColor = Color.DarkOrange
        });

        Controls.Add(panel);
    }
}
