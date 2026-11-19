using static DesktopNotesSwitcher.UiText;

namespace DesktopNotesSwitcher;

internal sealed class SettingsWindow : SettingsDialog
{
    private readonly CheckBox _startup = Toggle(T("Startup"));
    private readonly CheckBox _snap = Toggle(T("EdgeSnap"));
    private readonly CheckBox _collapse = Toggle(T("EdgeCollapse"));
    private readonly TrackBar _delay = new()
    {
        Maximum = Preferences.MaxRevealDelay,
        SmallChange = 50,
        LargeChange = 200,
        TickFrequency = 200,
        AutoSize = false,
        Size = new Size(113, 33),
        AccessibleName = T("RevealDelayAccessibility")
    };
    private readonly Label _delayValue = new() { Size = new Size(67, 33), TextAlign = ContentAlignment.MiddleRight };
    private readonly ComboBox _skinPicker = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 142,
        DisplayMember = nameof(Skin.Name),
        AccessibleName = T("Skin")
    };
    private bool _startupEnabled;

    public SettingsWindow(Preferences preferences, bool snap, Action<Preferences> apply, Action<bool> setSnap)
        : base(T("AppSettingsTitle"), 5, preferences, apply)
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
        AddSetting(T("EdgeSnap"), T("EdgeSnapDescription"), _snap);
        AddSetting(T("EdgeCollapse"), T("EdgeCollapseDescription"), _collapse);
        AddSetting(T("RevealDelay"), T("RevealDelayDescription"), delayInput);
        AddSetting(T("Startup"), T("StartupDescription"), _startup);
        AddSetting(T("Skin"), T("SkinDescription"), _skinPicker);

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
            SetStatus(T("StartupReadError"), error: true);
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
        _delayValue.Text = _delay.Value == 0 ? T("RevealImmediately") : T("DelayMilliseconds", _delay.Value);
    }

    private void SaveStartup()
    {
        if (Updating)
            return;
        try
        {
            StartupRegistration.SetEnabled(_startup.Checked);
            _startupEnabled = _startup.Checked;
            SetStatus(T(_startupEnabled ? "StartupEnabled" : "StartupDisabled"));
        }
        catch (Exception error) when (IsSettingsError(error))
        {
            LoadControls(() => _startup.Checked = _startupEnabled);
            SetStatus(T("StartupUpdateError"), error: true);
        }
    }
}
