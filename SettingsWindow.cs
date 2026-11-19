using System.Security;

namespace DesktopNotesSwitcher;

internal sealed class SettingsWindow : Form
{
    private static readonly Color Ink = Color.FromArgb(35, 45, 56);
    private static readonly Color Hint = Color.FromArgb(88, 100, 114);
    private static readonly Color Error = Color.FromArgb(159, 42, 34);
    private readonly LocalData _data;
    private readonly Action<Skin> _applySkin;
    private readonly Font _headingFont;
    private readonly CheckBox _startup = new()
    {
        Text = "启用",
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        AccessibleName = "开机启动"
    };
    private readonly ComboBox _skinPicker = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 142,
        Anchor = AnchorStyles.Right,
        DisplayMember = nameof(Skin.Name),
        AccessibleName = "皮肤"
    };
    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AccessibleName = "设置保存状态"
    };
    private Skin _selectedSkin;
    private bool _startupEnabled;
    private bool _loading;

    public SettingsWindow(LocalData data, Skin skin, Action<Skin> applySkin)
    {
        _data = data;
        _selectedSkin = skin;
        _applySkin = applySkin;
        _headingFont = new Font(Font, FontStyle.Bold);
        Text = "桌面便笺设置";
        ClientSize = new Size(460, 224);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(242, 245, 248);
        ForeColor = Ink;

        var done = new Button { Text = "完成", Size = new Size(88, 33), Anchor = AnchorStyles.Right };
        done.Click += (_, _) => Close();
        CancelButton = done;
        AcceptButton = done;

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(_status, 0, 0);
        footer.Controls.Add(done, 1, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(14, 14, 14, 10)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 77));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 77));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(SettingRow("开机启动", "登录 Windows 后自动显示悬浮条。", _startup), 0, 0);
        layout.Controls.Add(SettingRow("皮肤", "应用于悬浮条和便笺。", _skinPicker), 0, 1);
        layout.Controls.Add(footer, 0, 2);
        Controls.Add(layout);

        _skinPicker.Items.AddRange(Skin.Available.ToArray());
        _skinPicker.SelectedItem = skin;
        try
        {
            _startupEnabled = StartupRegistration.IsEnabled();
            _startup.Checked = _startupEnabled;
        }
        catch (Exception error) when (IsSettingsError(error))
        {
            _startup.Enabled = false;
            SetStatus("无法读取 Windows 启动项。", error: true);
        }
        _startup.CheckedChanged += (_, _) => SaveStartup();
        _skinPicker.SelectedIndexChanged += (_, _) => SaveSkin();
    }

    private TableLayoutPanel SettingRow(string title, string details, Control input)
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 9),
            Padding = new Padding(11, 7, 11, 7),
            BackColor = Color.White
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var label = new Label { Text = title, AutoSize = true, Font = _headingFont, Anchor = AnchorStyles.Left };
        var description = new Label { Text = details, AutoSize = true, ForeColor = Hint, Anchor = AnchorStyles.Left };
        card.Controls.Add(label, 0, 0);
        card.Controls.Add(description, 0, 1);
        card.Controls.Add(input, 1, 0);
        card.SetRowSpan(input, 2);
        return card;
    }

    private void SaveStartup()
    {
        if (_loading)
            return;
        try
        {
            StartupRegistration.SetEnabled(_startup.Checked);
            _startupEnabled = _startup.Checked;
            SetStatus(_startupEnabled ? "已开启开机启动。" : "已关闭开机启动。");
        }
        catch (Exception error) when (IsSettingsError(error))
        {
            _loading = true;
            _startup.Checked = _startupEnabled;
            _loading = false;
            SetStatus(error is IOException ? error.Message : "无法更新 Windows 启动项。", error: true);
        }
    }

    private void SaveSkin()
    {
        if (_loading || _skinPicker.SelectedItem is not Skin skin || skin == _selectedSkin)
            return;
        try
        {
            _data.WriteSkin(skin);
            _selectedSkin = skin;
            _applySkin(skin);
            SetStatus("已应用皮肤。");
        }
        catch (Exception error) when (IsSettingsError(error))
        {
            _loading = true;
            _skinPicker.SelectedItem = _selectedSkin;
            _loading = false;
            SetStatus("无法保存皮肤设置。", error: true);
        }
    }

    private static bool IsSettingsError(Exception error) => LocalData.IsStorageError(error) || error is SecurityException;

    private void SetStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.ForeColor = error ? Error : Hint;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _headingFont.Dispose();
    }
}
