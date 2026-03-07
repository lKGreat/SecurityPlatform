using System.Text;
using System.Text.Json;
using Atlas.LicenseIssuer.Models;
using Atlas.LicenseIssuer.Services;

namespace Atlas.LicenseIssuer.Forms;

public sealed class MainForm : Form
{
    private readonly CustomerService _customerService;
    private readonly IssuanceLogService _logService;
    private readonly LicenseSigningService _signingService;
    private readonly KeyManagementService _keyMgmt;

    // 左侧客户列表
    private ListBox _customerList = null!;
    private TextBox _searchBox = null!;

    // 右侧详情
    private Label _customerDetailLabel = null!;
    private DataGridView _licenseGrid = null!;

    private List<CustomerRecord> _allCustomers = [];
    private string? _selectedCustomerId;

    // 每位客户的颁发历史（本次会话内缓存）
    private readonly Dictionary<string, List<IssuanceLogEntry>> _logCache = new();

    public MainForm(
        CustomerService customerService,
        IssuanceLogService logService,
        LicenseSigningService signingService,
        KeyManagementService keyMgmt)
    {
        _customerService = customerService;
        _logService = logService;
        _signingService = signingService;
        _keyMgmt = keyMgmt;
        InitializeComponent();
        LoadCustomers();
    }

    private void InitializeComponent()
    {
        Text = "Atlas License Issuer";
        Size = new Size(1000, 620);
        StartPosition = FormStartPosition.CenterScreen;

        // 菜单栏
        var menuStrip = new MenuStrip();
        var toolsMenu = new ToolStripMenuItem("工具(&T)");
        toolsMenu.DropDownItems.Add("密钥管理", null, (s, e) => OpenKeyManagement());
        toolsMenu.DropDownItems.Add("颁发日志", null, (s, e) => OpenIssuanceLog());
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("退出", null, (s, e) => Application.Exit());
        menuStrip.Items.Add(toolsMenu);
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);

        // 主分割面板
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 240,
            Panel1MinSize = 180,
            Panel2MinSize = 400
        };

        // ── 左侧：客户列表 ──
        var leftPanel = new Panel { Dock = DockStyle.Fill };

        _searchBox = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "搜索客户...",
            Height = 28
        };
        _searchBox.TextChanged += (s, e) => FilterCustomers();

        _customerList = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = "Name"
        };
        _customerList.SelectedIndexChanged += OnCustomerSelected;

        var addCustomerBtn = new Button
        {
            Text = "+ 新建客户",
            Dock = DockStyle.Bottom,
            Height = 30
        };
        addCustomerBtn.Click += OnAddCustomer;

        leftPanel.Controls.AddRange([_searchBox, _customerList, addCustomerBtn]);
        splitContainer.Panel1.Controls.Add(leftPanel);

        // ── 右侧：客户详情 + 颁发历史 ──
        var rightPanel = new Panel { Dock = DockStyle.Fill };

        _customerDetailLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Font = new Font("Microsoft YaHei UI", 10),
            Padding = new Padding(8),
            Text = "← 请先选择客户"
        };

        _licenseGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false
        };
        _licenseGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "证书ID", DataPropertyName = "LicenseId", Width = 200 },
            new DataGridViewTextBoxColumn { HeaderText = "套餐", DataPropertyName = "Edition", Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = "有效期", DataPropertyName = "ExpiresAt", Width = 120 },
            new DataGridViewTextBoxColumn { HeaderText = "操作", DataPropertyName = "Action", Width = 70 },
            new DataGridViewTextBoxColumn { HeaderText = "颁发时间", DataPropertyName = "IssuedAt" });

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(4) };
        var newLicenseBtn = new Button { Text = "颁发新证书", Width = 100, Height = 30 };
        var renewBtn = new Button { Text = "续签/升级", Width = 90, Height = 30 };
        var exportBtn = new Button { Text = "导出最新证书", Width = 110, Height = 30 };
        newLicenseBtn.Click += OnNewLicense;
        renewBtn.Click += OnRenew;
        exportBtn.Click += OnExportLatest;
        btnPanel.Controls.AddRange([newLicenseBtn, renewBtn, exportBtn]);

        rightPanel.Controls.AddRange([_customerDetailLabel, _licenseGrid, btnPanel]);
        splitContainer.Panel2.Controls.Add(rightPanel);

        Controls.AddRange([menuStrip, splitContainer]);
    }

    private void LoadCustomers()
    {
        _allCustomers = _customerService.GetAll();
        FilterCustomers();
    }

    private void FilterCustomers()
    {
        var keyword = _searchBox.Text.Trim().ToLowerInvariant();
        _customerList.DataSource = null;
        _customerList.DataSource = string.IsNullOrEmpty(keyword)
            ? _allCustomers
            : _allCustomers.Where(c => c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void OnCustomerSelected(object? sender, EventArgs e)
    {
        if (_customerList.SelectedItem is not CustomerRecord customer)
        {
            _selectedCustomerId = null;
            return;
        }

        _selectedCustomerId = customer.Id;
        _customerDetailLabel.Text = $"客户：{customer.Name}  联系方式：{customer.Contact ?? "—"}  创建：{customer.CreatedAt[..10]}";
        LoadCustomerLogs(customer.Id);
    }

    private void LoadCustomerLogs(string customerId)
    {
        if (!_logCache.TryGetValue(customerId, out var logs))
        {
            logs = _logService.GetByCustomer(customerId);
            _logCache[customerId] = logs;
        }

        _licenseGrid.DataSource = null;
        _licenseGrid.DataSource = logs;
    }

    private void OnAddCustomer(object? sender, EventArgs e)
    {
        using var dlg = new CustomerForm(_customerService);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _logCache.Clear();
            LoadCustomers();
        }
    }

    private void OnNewLicense(object? sender, EventArgs e)
    {
        if (_selectedCustomerId is null)
        {
            MessageBox.Show("请先选择客户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var customer = _customerService.GetById(_selectedCustomerId);
        if (customer is null) return;

        using var dlg = new NewLicenseForm(_signingService, _logService, customer);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _logCache.Remove(_selectedCustomerId);
            LoadCustomerLogs(_selectedCustomerId);
        }
    }

    private void OnRenew(object? sender, EventArgs e)
    {
        if (_selectedCustomerId is null || _licenseGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择客户并在历史列表中选中要续签的证书记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var customer = _customerService.GetById(_selectedCustomerId);
        if (customer is null) return;

        // 查找最新颁发记录
        var logs = _logService.GetByCustomer(_selectedCustomerId);
        if (logs.Count == 0) return;
        var latest = logs.OrderByDescending(x => x.Revision).First();

        // 构造续签 payload 默认值（仅提供 LicenseId 和 Revision）
        var renewFrom = new LicensePayload
        {
            LicenseId = Guid.TryParse(latest.LicenseId, out var gid) ? gid : Guid.NewGuid(),
            Revision = latest.Revision,
            Edition = latest.Edition,
            IsPermanent = latest.IsPermanent,
            ExpiresAt = string.IsNullOrEmpty(latest.ExpiresAt) ? null
                : DateTimeOffset.TryParse(latest.ExpiresAt, out var dt) ? dt : null
        };

        using var dlg = new NewLicenseForm(_signingService, _logService, customer, renewFrom);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _logCache.Remove(_selectedCustomerId);
            LoadCustomerLogs(_selectedCustomerId);
        }
    }

    private void OnExportLatest(object? sender, EventArgs e)
    {
        MessageBox.Show("请通过「续签/升级」重新颁发并导出最新证书。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenKeyManagement()
    {
        using var dlg = new KeyManagementForm(_keyMgmt);
        dlg.ShowDialog();
    }

    private void OpenIssuanceLog()
    {
        using var dlg = new IssuanceLogForm(_logService);
        dlg.ShowDialog();
    }
}
