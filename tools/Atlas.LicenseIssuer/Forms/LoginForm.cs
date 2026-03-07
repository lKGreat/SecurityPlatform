using Atlas.LicenseIssuer.Services;

namespace Atlas.LicenseIssuer.Forms;

public sealed class LoginForm : Form
{
    private readonly KeyManagementService _keyMgmt;
    private TextBox _passwordBox = null!;
    private Button _loginBtn = null!;
    private Button _initBtn = null!;
    private Label _statusLabel = null!;

    public LoginForm(KeyManagementService keyMgmt)
    {
        _keyMgmt = keyMgmt;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Atlas License Issuer — 验证颁发密码";
        Size = new Size(400, 220);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var titleLabel = new Label
        {
            Text = "Atlas 证书颁发工具",
            Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var pwdLabel = new Label { Text = "颁发密码：", AutoSize = true, Location = new Point(20, 55) };

        _passwordBox = new TextBox
        {
            PasswordChar = '●',
            Location = new Point(20, 75),
            Width = 340,
            Height = 28
        };
        _passwordBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TryLogin(); };

        _statusLabel = new Label
        {
            ForeColor = Color.Red,
            AutoSize = true,
            Location = new Point(20, 110)
        };

        _loginBtn = new Button
        {
            Text = "进入",
            Location = new Point(220, 140),
            Width = 70,
            Height = 30
        };
        _loginBtn.Click += (s, e) => TryLogin();

        _initBtn = new Button
        {
            Text = "初始化密钥",
            Location = new Point(300, 140),
            Width = 80,
            Height = 30,
            Visible = !_keyMgmt.IsKeyInitialized()
        };
        _initBtn.Click += OnInitKey;

        Controls.AddRange([titleLabel, pwdLabel, _passwordBox, _statusLabel, _loginBtn, _initBtn]);
    }

    private void TryLogin()
    {
        var password = _passwordBox.Text;
        if (string.IsNullOrWhiteSpace(password))
        {
            _statusLabel.Text = "请输入颁发密码";
            return;
        }

        if (!_keyMgmt.IsKeyInitialized())
        {
            _statusLabel.Text = "尚未初始化密钥对，请先点击「初始化密钥」";
            return;
        }

        if (_keyMgmt.TryLoadPrivateKey(password))
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _statusLabel.Text = "密码错误，请重试";
            _passwordBox.Clear();
            _passwordBox.Focus();
        }
    }

    private void OnInitKey(object? sender, EventArgs e)
    {
        using var dlg = new InitKeyForm(_keyMgmt);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _initBtn.Visible = false;
            _statusLabel.Text = "密钥初始化成功，请使用新设置的密码登录";
            _statusLabel.ForeColor = Color.Green;
        }
    }
}
