using System.Security;
using static DesktopNotesSwitcher.UiText;

namespace DesktopNotesSwitcher;

internal abstract class SettingsDialog : Form
{
    private static readonly Color Hint = Color.FromArgb(88, 100, 114);
    private static readonly Color Error = Color.FromArgb(159, 42, 34);
    private readonly Action<Preferences> _apply;
    private readonly Font _headingFont;
    private readonly TableLayoutPanel _layout;
    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AccessibleName = T("SettingsSaveStatus")
    };
    private int _row;

    protected Preferences Current { get; private set; }
    protected bool Updating { get; private set; }

    protected SettingsDialog(string title, int sections, Preferences preferences, Action<Preferences> apply)
    {
        Current = preferences;
        _apply = apply;
        _headingFont = new Font(Font, FontStyle.Bold);
        Text = title;
        ClientSize = new Size(480, sections * 72 + 68);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(242, 245, 248);
        ForeColor = Color.FromArgb(35, 45, 56);

        var done = new Button { Text = T("Done"), Size = new Size(88, 33), Anchor = AnchorStyles.Right };
        done.Click += (_, _) => Close();
        CancelButton = done;
        AcceptButton = done;

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0), TabIndex = sections };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(_status, 0, 0);
        footer.Controls.Add(done, 1, 0);

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = sections + 1,
            ColumnCount = 1,
            Padding = new Padding(14, 14, 14, 10)
        };
        for (int row = 0; row < sections; row++)
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.Controls.Add(footer, 0, sections);
        Controls.Add(_layout);
    }

    protected void AddSetting(string title, string details, Control input)
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 9),
            Padding = new Padding(11, 7, 11, 7),
            BackColor = Color.White,
            TabIndex = _row
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var label = new Label { Text = title, AutoSize = true, Font = _headingFont, Anchor = AnchorStyles.Left };
        var description = new Label { Text = details, AutoSize = true, ForeColor = Hint, Anchor = AnchorStyles.Left };
        input.Anchor = AnchorStyles.Right;
        card.Controls.Add(label, 0, 0);
        card.Controls.Add(description, 0, 1);
        card.Controls.Add(input, 1, 0);
        card.SetRowSpan(input, 2);
        _layout.Controls.Add(card, 0, _row++);
    }

    protected static CheckBox Toggle(string name, bool enabled = false) => new()
    {
        Text = T("Enable"),
        AutoSize = true,
        AccessibleName = name,
        Checked = enabled
    };

    public void SyncPreferences(Preferences saved) => Current = saved;

    protected void Save(Preferences updated, Action restore)
    {
        if (Updating || Current == updated)
            return;
        try
        {
            _apply(updated);
            Current = updated;
            SetStatus(T("SettingsSaved"));
        }
        catch (Exception error) when (IsSettingsError(error))
        {
            LoadControls(restore);
            SetStatus(T("SettingsSaveError"), error: true);
        }
    }

    protected void LoadControls(Action load)
    {
        Updating = true;
        try
        {
            load();
        }
        finally
        {
            Updating = false;
        }
    }

    protected static bool IsSettingsError(Exception error) =>
        LocalData.IsStorageError(error) || error is SecurityException;

    protected void SetStatus(string text, bool error = false)
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
