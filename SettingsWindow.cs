namespace DesktopNotesSwitcher;

internal sealed class SettingsWindow : SettingsDialog
{
    private readonly CheckBox _startup = Toggle("开机启动");
    private readonly CheckBox _snap = Toggle("边缘吸附");
    private readonly CheckBox _collapse = Toggle("贴边收起");
    private readonly TrackBar _delay = new()
    {
        Maximum = Preferences.MaxRevealDelay,
        SmallChange = 50,
        LargeChange = 200,
        TickFrequency = 200,
        AutoSize = false,
        Size = new Size(113, 33),
        AccessibleName = "弹出延迟（毫秒）"
    };
    private readonly Label _delayValue = new() { Size = new Size(67, 33), TextAlign = ContentAlignment.MiddleRight };
    private readonly ComboBox _skinPicker = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 142,
        DisplayMember = nameof(Skin.Name),
        AccessibleName = "皮肤"
    };
    private bool _startupEnabled;

    public SettingsWindow(Preferences preferences, bool snap, Action<Preferences> apply, Action<bool> setSnap)
        : base("桌面便笺 · 程序设置", 5, preferences, apply)
    {
        var delayInput = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0)
        };
        delayInput.Controls.Add(_delay);
        delayInput.Controls.Add(_delayValue);
        AddSetting("边缘吸附", "拖近屏幕边缘时自动贴边。", _snap);
        AddSetting("贴边收起", "离开悬浮条后只露出一小条。", _collapse);
        AddSetting("弹出延迟", "鼠标停在边缘后等待的时间。", delayInput);
        AddSetting("开机启动", "登录 Windows 后自动显示悬浮条。", _startup);
        AddSetting("皮肤", "应用于悬浮条和便笺。", _skinPicker);

        _snap.Checked = snap;
        _collapse.Checked = preferences.CollapseAtEdge;
        _delay.Value = preferences.RevealDelayMs;
        RefreshEdgeControls();
        _skinPicker.Items.AddRange(Skin.Available.ToArray());
        _skinPicker.SelectedItem = Skin.Find(preferences.SkinId);
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

        _snap.CheckedChanged += (_, _) =>
        {
            if (!Updating)
                setSnap(_snap.Checked);
            RefreshEdgeControls();
        };
        _collapse.CheckedChanged += (_, _) =>
        {
            Save(Current with { CollapseAtEdge = _collapse.Checked }, () => _collapse.Checked = Current.CollapseAtEdge);
            RefreshEdgeControls();
        };
        _delay.ValueChanged += (_, _) =>
        {
            Save(Current with { RevealDelayMs = _delay.Value }, () => _delay.Value = Current.RevealDelayMs);
            RefreshEdgeControls();
        };
        _startup.CheckedChanged += (_, _) => SaveStartup();
        _skinPicker.SelectedIndexChanged += (_, _) =>
        {
            if (_skinPicker.SelectedItem is Skin skin)
                Save(Current with { SkinId = skin.Id }, () => _skinPicker.SelectedItem = Skin.Find(Current.SkinId));
        };
    }

    public void DisplaySnap(bool snap) => LoadControls(() => _snap.Checked = snap);

    private void RefreshEdgeControls()
    {
        _collapse.Enabled = _snap.Checked;
        _delay.Enabled = _snap.Checked && _collapse.Checked;
        _delayValue.Enabled = _delay.Enabled;
        _delayValue.Text = _delay.Value == 0 ? "立即" : $"{_delay.Value} 毫秒";
    }

    private void SaveStartup()
    {
        if (Updating)
            return;
        try
        {
            StartupRegistration.SetEnabled(_startup.Checked);
            _startupEnabled = _startup.Checked;
            SetStatus(_startupEnabled ? "已开启开机启动。" : "已关闭开机启动。");
        }
        catch (Exception error) when (IsSettingsError(error))
        {
            LoadControls(() => _startup.Checked = _startupEnabled);
            SetStatus(error is IOException ? error.Message : "无法更新 Windows 启动项。", error: true);
        }
    }
}
