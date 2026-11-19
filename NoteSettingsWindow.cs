namespace DesktopNotesSwitcher;

internal sealed class NoteSettingsWindow : SettingsDialog
{
    public NoteSettingsWindow(Preferences preferences, Action<Preferences> apply)
        : base("桌面便笺 · 便笺设置", 2, preferences, apply)
    {
        var fontSize = new NumericUpDown
        {
            Minimum = Preferences.MinNoteFontSize,
            Maximum = Preferences.MaxNoteFontSize,
            Value = preferences.NoteFontSize,
            Width = 74,
            AccessibleName = "便笺字号"
        };
        CheckBox wrap = Toggle("自动换行", preferences.WrapNotes);
        AddSetting("字号", "调整所有便笺的正文大小。", fontSize);
        AddSetting("自动换行", "长行随便笺宽度折行。", wrap);

        fontSize.ValueChanged += (_, _) =>
            Save(Current with { NoteFontSize = (int)fontSize.Value }, () => fontSize.Value = Current.NoteFontSize);
        wrap.CheckedChanged += (_, _) =>
            Save(Current with { WrapNotes = wrap.Checked }, () => wrap.Checked = Current.WrapNotes);
    }
}
