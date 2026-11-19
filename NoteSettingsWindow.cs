using static DesktopNotesSwitcher.UiText;

namespace DesktopNotesSwitcher;

internal sealed class NoteSettingsWindow : SettingsDialog
{
    public NoteSettingsWindow(Preferences preferences, Action<Preferences> apply)
        : base(T("NoteSettingsTitle"), 2, preferences, apply)
    {
        var fontSize = new NumericUpDown
        {
            Minimum = Preferences.MinNoteFontSize,
            Maximum = Preferences.MaxNoteFontSize,
            Value = preferences.NoteFontSize,
            Width = 74,
            AccessibleName = T("NoteFontSize")
        };
        CheckBox wrap = Toggle(T("WordWrap"), preferences.WrapNotes);
        AddSetting(T("FontSize"), T("FontSizeDescription"), fontSize);
        AddSetting(T("WordWrap"), T("WordWrapDescription"), wrap);

        fontSize.ValueChanged += (_, _) =>
            Save(Current with { NoteFontSize = (int)fontSize.Value }, () => fontSize.Value = Current.NoteFontSize);
        wrap.CheckedChanged += (_, _) =>
            Save(Current with { WrapNotes = wrap.Checked }, () => wrap.Checked = Current.WrapNotes);
    }
}
