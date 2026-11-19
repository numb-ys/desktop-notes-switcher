using static DesktopNotesSwitcher.UiText;

namespace DesktopNotesSwitcher;

internal sealed class NoteSettingsWindow : SettingsDialog
{
    public NoteSettingsWindow(Preferences preferences, Action<Preferences> apply)
        : base(T("NoteSettingsTitle"), 3, preferences, apply)
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
        var collapseBehavior = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 151,
            AccessibleName = T("NoteCollapseBehavior")
        };
        collapseBehavior.Items.AddRange([T("NoteKeepVisible"), T("NoteFollowBar")]);
        collapseBehavior.SelectedIndex = preferences.NoteFollowsBar ? 1 : 0;
        AddSetting(T("FontSize"), T("FontSizeDescription"), fontSize);
        AddSetting(T("WordWrap"), T("WordWrapDescription"), wrap);
        AddSetting(T("NoteCollapseBehavior"), T("NoteCollapseDescription"), collapseBehavior);

        fontSize.ValueChanged += (_, _) =>
            Save(Current with { NoteFontSize = (int)fontSize.Value }, () => fontSize.Value = Current.NoteFontSize);
        wrap.CheckedChanged += (_, _) =>
            Save(Current with { WrapNotes = wrap.Checked }, () => wrap.Checked = Current.WrapNotes);
        collapseBehavior.SelectedIndexChanged += (_, _) =>
            Save(Current with { NoteFollowsBar = collapseBehavior.SelectedIndex == 1 },
                () => collapseBehavior.SelectedIndex = Current.NoteFollowsBar ? 1 : 0);
    }
}
