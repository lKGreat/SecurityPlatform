using Atlas.LicenseIssuer.Services;

namespace Atlas.LicenseIssuer.Forms;

public sealed class IssuanceLogForm : Form
{
    private readonly IssuanceLogService _logService;
    private DataGridView _grid = null!;

    public IssuanceLogForm(IssuanceLogService logService)
    {
        _logService = logService;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text = "颁发日志";
        Size = new Size(900, 500);
        StartPosition = FormStartPosition.CenterParent;

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false
        };

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "操作类型", DataPropertyName = "Action", Width = 70 },
            new DataGridViewTextBoxColumn { HeaderText = "客户ID", DataPropertyName = "CustomerId", Width = 120 },
            new DataGridViewTextBoxColumn { HeaderText = "证书ID", DataPropertyName = "LicenseId", Width = 240 },
            new DataGridViewTextBoxColumn { HeaderText = "版本", DataPropertyName = "Revision", Width = 50 },
            new DataGridViewTextBoxColumn { HeaderText = "套餐", DataPropertyName = "Edition", Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = "颁发时间", DataPropertyName = "IssuedAt", Width = 160 },
            new DataGridViewTextBoxColumn { HeaderText = "到期时间", DataPropertyName = "ExpiresAt", Width = 160 },
            new DataGridViewTextBoxColumn { HeaderText = "备注", DataPropertyName = "Remark" });

        var refreshBtn = new Button { Text = "刷新", Dock = DockStyle.Bottom, Height = 32 };
        refreshBtn.Click += (s, e) => LoadData();

        Controls.AddRange([_grid, refreshBtn]);
    }

    private void LoadData()
    {
        var logs = _logService.GetAll(200);
        _grid.DataSource = logs;
    }
}
