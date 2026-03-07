using Atlas.LicenseIssuer.Models;
using Atlas.LicenseIssuer.Services;

namespace Atlas.LicenseIssuer.Forms;

public sealed class NewLicenseForm : Form
{
    private readonly LicenseSigningService _signingService;
    private readonly IssuanceLogService _logService;
    private readonly CustomerRecord _customer;
    private readonly LicensePayload? _renewFrom;

    // 控件
    private ComboBox _editionCombo = null!;
    private RadioButton _fixedRadio = null!;
    private RadioButton _permanentRadio = null!;
    private DateTimePicker _expiryPicker = null!;
    private CheckBox _chkLowCode = null!, _chkWorkflow = null!, _chkApproval = null!,
                    _chkAlert = null!, _chkOffline = null!, _chkMultiTenant = null!;
    private NumericUpDown _maxApps = null!, _maxUsers = null!, _maxTenants = null!;
    private RadioButton _noBindRadio = null!, _bindRadio = null!;
    private TextBox _fingerprintBox = null!;
    private TextBox _remarkBox = null!;

    public NewLicenseForm(
        LicenseSigningService signingService,
        IssuanceLogService logService,
        CustomerRecord customer,
        LicensePayload? renewFrom = null)
    {
        _signingService = signingService;
        _logService = logService;
        _customer = customer;
        _renewFrom = renewFrom;
        InitializeComponent();
        ApplyDefaults();
    }

    private void InitializeComponent()
    {
        Text = _renewFrom is null ? $"新建证书 — {_customer.Name}" : $"续签证书 — {_customer.Name}";
        Size = new Size(560, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(16),
            AutoScroll = true,
            WrapContents = false
        };

        panel.Controls.Add(MakeLabel("套餐版本："));
        _editionCombo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        _editionCombo.Items.AddRange(["Trial", "Pro", "Enterprise"]);
        _editionCombo.SelectedIndex = 0;
        _editionCombo.SelectedIndexChanged += OnEditionChanged;
        panel.Controls.Add(_editionCombo);

        panel.Controls.Add(MakeLabel("有效期类型："));
        _permanentRadio = new RadioButton { Text = "永久", AutoSize = true };
        _fixedRadio = new RadioButton { Text = "固定期限", AutoSize = true, Checked = true };
        var radioPanel = new FlowLayoutPanel { AutoSize = true };
        radioPanel.Controls.AddRange([_fixedRadio, _permanentRadio]);
        panel.Controls.Add(radioPanel);
        _permanentRadio.CheckedChanged += (s, e) => _expiryPicker.Enabled = !_permanentRadio.Checked;

        panel.Controls.Add(MakeLabel("到期日期："));
        _expiryPicker = new DateTimePicker { Width = 200, Value = DateTime.Today.AddYears(1) };
        panel.Controls.Add(_expiryPicker);

        panel.Controls.Add(new Label { Text = "── 功能开关 ──", AutoSize = true, ForeColor = Color.Gray });
        var featurePanel = new FlowLayoutPanel { AutoSize = true };
        _chkLowCode = new CheckBox { Text = "低代码", AutoSize = true, Checked = true };
        _chkWorkflow = new CheckBox { Text = "工作流", AutoSize = true };
        _chkApproval = new CheckBox { Text = "审批流", AutoSize = true };
        _chkAlert = new CheckBox { Text = "告警管理", AutoSize = true };
        _chkOffline = new CheckBox { Text = "离线部署", AutoSize = true };
        _chkMultiTenant = new CheckBox { Text = "多租户", AutoSize = true };
        featurePanel.Controls.AddRange([_chkLowCode, _chkWorkflow, _chkApproval,
                                         _chkAlert, _chkOffline, _chkMultiTenant]);
        panel.Controls.Add(featurePanel);

        panel.Controls.Add(new Label { Text = "── 数量限制（0 或 -1 表示不限）──", AutoSize = true, ForeColor = Color.Gray });
        panel.Controls.Add(MakeLimitRow("最大应用数：", out _maxApps, 3));
        panel.Controls.Add(MakeLimitRow("最大用户数：", out _maxUsers, 10));
        panel.Controls.Add(MakeLimitRow("最大租户数：", out _maxTenants, 1));

        panel.Controls.Add(new Label { Text = "── 机器绑定 ──", AutoSize = true, ForeColor = Color.Gray });
        _noBindRadio = new RadioButton { Text = "不绑定", AutoSize = true, Checked = true };
        _bindRadio = new RadioButton { Text = "绑定指定机器码", AutoSize = true };
        var bindPanel = new FlowLayoutPanel { AutoSize = true };
        bindPanel.Controls.AddRange([_noBindRadio, _bindRadio]);
        panel.Controls.Add(bindPanel);
        _bindRadio.CheckedChanged += (s, e) => _fingerprintBox.Enabled = _bindRadio.Checked;

        _fingerprintBox = new TextBox { Width = 480, Multiline = true, Height = 40,
            PlaceholderText = "粘贴来自平台「获取机器码」接口的机器码", Enabled = false };
        panel.Controls.Add(_fingerprintBox);

        panel.Controls.Add(MakeLabel("颁发备注（内部可见）："));
        _remarkBox = new TextBox { Width = 480, Multiline = true, Height = 40 };
        panel.Controls.Add(_remarkBox);

        var btnRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var signBtn = new Button { Text = "生成并导出证书", Width = 120, Height = 32 };
        var cancelBtn = new Button { Text = "取消", Width = 70, Height = 32 };
        signBtn.Click += OnSign;
        cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btnRow.Controls.AddRange([signBtn, cancelBtn]);
        panel.Controls.Add(btnRow);

        Controls.Add(panel);
    }

    private void ApplyDefaults()
    {
        if (_renewFrom is null) return;

        _editionCombo.SelectedItem = _renewFrom.Edition;
        _permanentRadio.Checked = _renewFrom.IsPermanent;
        if (_renewFrom.ExpiresAt.HasValue)
            _expiryPicker.Value = _renewFrom.ExpiresAt.Value.LocalDateTime;

        var features = _renewFrom.Features;
        _chkLowCode.Checked = features.TryGetValue("lowCode", out var v) && v;
        _chkWorkflow.Checked = features.TryGetValue("workflow", out v) && v;
        _chkApproval.Checked = features.TryGetValue("approval", out v) && v;
        _chkAlert.Checked = features.TryGetValue("alert", out v) && v;
        _chkOffline.Checked = features.TryGetValue("offlineDeploy", out v) && v;
        _chkMultiTenant.Checked = features.TryGetValue("multiTenant", out v) && v;

        var limits = _renewFrom.Limits;
        if (limits.TryGetValue("maxApps", out var la)) _maxApps.Value = la;
        if (limits.TryGetValue("maxUsers", out var lu)) _maxUsers.Value = lu;
        if (limits.TryGetValue("maxTenants", out var lt)) _maxTenants.Value = lt;

        if (!string.IsNullOrWhiteSpace(_renewFrom.MachineFingerprint))
        {
            _bindRadio.Checked = true;
            _fingerprintBox.Text = _renewFrom.MachineFingerprint;
        }
    }

    private void OnEditionChanged(object? sender, EventArgs e)
    {
        var edition = _editionCombo.SelectedItem?.ToString() ?? "Trial";
        var features = LicenseFeatures.ForEdition(edition);
        _chkLowCode.Checked = features.LowCode;
        _chkWorkflow.Checked = features.Workflow;
        _chkApproval.Checked = features.Approval;
        _chkAlert.Checked = features.Alert;
        _chkOffline.Checked = features.OfflineDeploy;
        _chkMultiTenant.Checked = features.MultiTenant;

        var limits = LicenseLimits.ForEdition(edition);
        _maxApps.Value = limits.MaxApps < 0 ? 0 : limits.MaxApps;
        _maxUsers.Value = limits.MaxUsers < 0 ? 0 : limits.MaxUsers;
        _maxTenants.Value = limits.MaxTenants < 0 ? 0 : limits.MaxTenants;
    }

    private void OnSign(object? sender, EventArgs e)
    {
        var edition = _editionCombo.SelectedItem?.ToString() ?? "Trial";
        var isPermanent = _permanentRadio.Checked;
        DateTimeOffset? expiresAt = isPermanent ? null : new DateTimeOffset(_expiryPicker.Value.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);

        var payload = new LicensePayload
        {
            LicenseId = _renewFrom?.LicenseId ?? Guid.NewGuid(),
            Revision = (_renewFrom?.Revision ?? 0) + 1,
            CustomerId = _customer.Id,
            TenantName = _customer.Name,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            IsPermanent = isPermanent,
            Edition = edition,
            MachineFingerprint = _bindRadio.Checked ? _fingerprintBox.Text.Trim() : null,
            Features = new Dictionary<string, bool>
            {
                ["lowCode"] = _chkLowCode.Checked,
                ["workflow"] = _chkWorkflow.Checked,
                ["approval"] = _chkApproval.Checked,
                ["alert"] = _chkAlert.Checked,
                ["offlineDeploy"] = _chkOffline.Checked,
                ["multiTenant"] = _chkMultiTenant.Checked,
                ["audit"] = true
            },
            Limits = new Dictionary<string, int>
            {
                ["maxApps"] = (int)_maxApps.Value == 0 ? -1 : (int)_maxApps.Value,
                ["maxUsers"] = (int)_maxUsers.Value == 0 ? -1 : (int)_maxUsers.Value,
                ["maxTenants"] = (int)_maxTenants.Value == 0 ? -1 : (int)_maxTenants.Value,
                ["auditRetentionDays"] = 180,
            }
        };

        try
        {
            var exportedContent = _signingService.SignAndExport(payload);

            // 验证签名
            if (!_signingService.VerifyExported(exportedContent))
            {
                MessageBox.Show("签名自检失败，请联系技术支持", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 保存文件
            var defaultName = $"{_customer.Name}_{edition}_{DateTime.Today:yyyyMMdd}.atlaslicense";
            using var dlg = new SaveFileDialog
            {
                Filter = "Atlas License (*.atlaslicense)|*.atlaslicense",
                FileName = defaultName
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            File.WriteAllText(dlg.FileName, exportedContent);

            // 写颁发日志
            _logService.Append(new IssuanceLogEntry
            {
                CustomerId = _customer.Id,
                LicenseId = payload.LicenseId.ToString(),
                Revision = payload.Revision,
                Edition = edition,
                Action = _renewFrom is null ? "NEW" : "RENEW",
                IssuedAt = DateTimeOffset.UtcNow.ToString("o"),
                ExpiresAt = expiresAt?.ToString("o"),
                IsPermanent = isPermanent,
                Remark = _remarkBox.Text.Trim()
            });

            MessageBox.Show($"证书已成功导出至：\n{dlg.FileName}", "成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"颁发失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Label MakeLabel(string text) => new() { Text = text, AutoSize = true };

    private static Panel MakeLimitRow(string label, out NumericUpDown updown, decimal defaultValue)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        panel.Controls.Add(new Label { Text = label, Width = 120, TextAlign = ContentAlignment.MiddleLeft });
        var ud = new NumericUpDown { Minimum = -1, Maximum = 999999, Value = defaultValue, Width = 100 };
        panel.Controls.Add(ud);
        updown = ud;
        return panel;
    }
}
