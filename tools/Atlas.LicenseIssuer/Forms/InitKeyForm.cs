using Atlas.LicenseIssuer.Services;

namespace Atlas.LicenseIssuer.Forms;

public sealed class InitKeyForm : Form
{
    private readonly KeyManagementService _keyMgmt;
    private TextBox _passwordBox = null!;
    private TextBox _confirmBox = null!;

    public InitKeyForm(KeyManagementService keyMgmt)
    {
        _keyMgmt = keyMgmt;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "初始化颁发密钥对";
        Size = new Size(380, 220);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var pwdLabel = new Label { Text = "设置颁发密码（建议16位以上）：", AutoSize = true, Location = new Point(20, 20) };
        _passwordBox = new TextBox { PasswordChar = '●', Location = new Point(20, 44), Width = 320, Height = 28 };

        var confirmLabel = new Label { Text = "确认密码：", AutoSize = true, Location = new Point(20, 82) };
        _confirmBox = new TextBox { PasswordChar = '●', Location = new Point(20, 104), Width = 320, Height = 28 };

        var statusLabel = new Label { ForeColor = Color.Red, AutoSize = true, Location = new Point(20, 140) };

        var okBtn = new Button { Text = "生成密钥对", Location = new Point(200, 148), Width = 80, Height = 30 };
        var cancelBtn = new Button { Text = "取消", Location = new Point(290, 148), Width = 60, Height = 30 };

        okBtn.Click += (s, e) =>
        {
            if (_passwordBox.Text != _confirmBox.Text)
            {
                statusLabel.Text = "两次密码不一致";
                return;
            }

            if (_passwordBox.Text.Length < 8)
            {
                statusLabel.Text = "密码长度不能少于8位";
                return;
            }

            try
            {
                _keyMgmt.GenerateKeyPair(_passwordBox.Text);
                _keyMgmt.TryLoadPrivateKey(_passwordBox.Text);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"生成失败：{ex.Message}";
            }
        };

        cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([pwdLabel, _passwordBox, confirmLabel, _confirmBox, statusLabel, okBtn, cancelBtn]);
    }
}
