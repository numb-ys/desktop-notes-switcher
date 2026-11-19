using static DesktopNotesSwitcher.UiText;

namespace DesktopNotesSwitcher;

internal sealed class SettingsWindow : SettingsDialog
{
    private sealed record DesktopChoice(Guid Id, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly CheckBox _startup = Toggle(T("Startup"));
    private readonly CheckBox _snap = Toggle(T("EdgeSnap"));
    private readonly CheckBox _collapse = Toggle(T("EdgeCollapse"));
    private readonly CheckBox _quickEnabled = Toggle(T("QuickSwitchShow"));
    private readonly ComboBox _quickTarget = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 134,
        AccessibleName = T("QuickSwitchTarget")
    };
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
    private Guid[] _desktopIds = [];

    public SettingsWindow(Preferences preferences, bool snap, Guid[] desktops, Action<Preferences> apply, Action<bool> setSnap)
        : base(T("AppSettingsTitle"), 6, preferences, apply)
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
        var quickInput = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0)
        };
        _quickEnabled.Margin = new Padding(0, 5, 6, 0);
        quickInput.Controls.Add(_quickEnabled);
        quickInput.Controls.Add(_quickTarget);
        AddSetting(T("EdgeSnap"), T("EdgeSnapDescription"), _snap);
        AddSetting(T("EdgeCollapse"), T("EdgeCollapseDescription"), _collapse);
        AddSetting(T("RevealDelay"), T("RevealDelayDescription"), delayInput);
        AddSetting(T("QuickSwitch"), T("QuickSwitchDescription"), quickInput);
        AddSetting(T("Startup"), T("StartupDescription"), _startup);
        AddSetting(T("Skin"), T("SkinDescription"), _skinPicker);

        _snap.Checked = snap;
        _collapse.Checked = preferences.CollapseAtEdge;
        _delay.Value = preferences.RevealDelayMs;
        _quickEnabled.Checked = preferences.QuickSwitchEnabled;
        DisplayDesktops(desktops);
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
        _quickEnabled.CheckedChanged += (_, _) =>
        {
            if (Updating)
                return;
            Guid? selected = _quickTarget.SelectedItem is DesktopChoice choice && choice.Id != Guid.Empty ? choice.Id : null;
            Save(Current with
            {
                QuickSwitchEnabled = _quickEnabled.Checked,
                QuickSwitchDesktopId = Current.QuickSwitchDesktopId ?? selected
            }, () => _quickEnabled.Checked = Current.QuickSwitchEnabled);
        };
        _quickTarget.SelectedIndexChanged += (_, _) =>
        {
            if (Updating || _quickTarget.SelectedItem is not DesktopChoice choice || choice.Id == Guid.Empty)
                return;
            int previous = TargetIndex(Current.QuickSwitchDesktopId);
            Save(Current with { QuickSwitchDesktopId = choice.Id }, () => _quickTarget.SelectedIndex = previous);
            DisplayDesktops(_desktopIds);
        };
        _skinPicker.SelectedIndexChanged += (_, _) =>
        {
            if (_skinPicker.SelectedItem is Skin skin)
                Save(Current with { SkinId = skin.Id }, () => _skinPicker.SelectedItem = Skin.Find(Current.SkinId));
        };
    }

    public void DisplaySnap(bool snap) => LoadControls(() => _snap.Checked = snap);

    public void DisplayDesktops(Guid[] desktops)
    {
        _desktopIds = desktops;
        LoadControls(() =>
        {
            _quickTarget.BeginUpdate();
            try
            {
                _quickTarget.Items.Clear();
                for (int index = 0; index < desktops.Length; index++)
                    _quickTarget.Items.Add(new DesktopChoice(desktops[index], T("NumberedDesktop", index + 1)));
                int selected = TargetIndex(Current.QuickSwitchDesktopId);
                if (selected < 0)
                {
                    _quickTarget.Items.Insert(0, new DesktopChoice(Guid.Empty, T("QuickSwitchChoose")));
                    selected = 0;
                }
                _quickTarget.SelectedIndex = selected;
                _quickTarget.Enabled = desktops.Length > 0;
                _quickEnabled.Enabled = desktops.Length > 0 || _quickEnabled.Checked;
            }
            finally
            {
                _quickTarget.EndUpdate();
            }
        });
    }

    private int TargetIndex(Guid? id)
    {
        if (_quickTarget.Items.Count == 0)
            return -1;
        if (id is null)
            return 0;
        for (int index = 0; index < _quickTarget.Items.Count; index++)
        {
            if (_quickTarget.Items[index] is DesktopChoice choice && choice.Id == id)
                return index;
        }
        return _quickTarget.Items[0] is DesktopChoice first && first.Id == Guid.Empty ? 0 : -1;
    }

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
