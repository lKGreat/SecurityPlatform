using Atlas.LicenseIssuer.Models;
using Atlas.LicenseIssuer.Services;

namespace Atlas.LicenseIssuer.Forms;

public sealed class CustomerForm : Form
{
    private readonly CustomerService _customerService;
    private readonly CustomerRecord? _existing;
    private TextBox _nameBox = null!;
    private TextBox _contactBox = null!;
    private TextBox _remarkBox = null!;

    public CustomerRecord? Result { get; private set; }

    public CustomerForm(CustomerService customerService, CustomerRecord? existing = null)
    {
        _customerService = customerService;
        _existing = existing;
        InitializeComponent();
        if (existing is not null)
        {
            _nameBox.Text = existing.Name;
            _contactBox.Text = existing.Contact ?? "";
            _remarkBox.Text = existing.Remark ?? "";
        }
    }

    private void InitializeComponent()
    {
        Text = _existing is null ? "新建客户" : "编辑客户";
        Size = new Size(400, 260);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        AddField("客户名称 *：", out _nameBox, 20, 44);
        AddField("联系方式：", out _contactBox, 20, 104);
        AddField("备注：", out _remarkBox, 20, 164);

        var okBtn = new Button { Text = "保存", Location = new Point(230, 210), Width = 70, Height = 30 };
        var cancelBtn = new Button { Text = "取消", Location = new Point(310, 210), Width = 60, Height = 30 };

        okBtn.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageBox.Show("请输入客户名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var record = _existing ?? new CustomerRecord();
            record.Name = _nameBox.Text.Trim();
            record.Contact = _contactBox.Text.Trim();
            record.Remark = _remarkBox.Text.Trim();

            if (_existing is null)
                _customerService.Add(record);
            else
                _customerService.Update(record);

            Result = record;
            DialogResult = DialogResult.OK;
            Close();
        };

        cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.AddRange([okBtn, cancelBtn]);
    }

    private void AddField(string labelText, out TextBox textBox, int x, int labelY)
    {
        var label = new Label { Text = labelText, AutoSize = true, Location = new Point(x, labelY) };
        var tb = new TextBox { Location = new Point(x, labelY + 22), Width = 340, Height = 28 };
        Controls.AddRange([label, tb]);
        textBox = tb;
    }
}
