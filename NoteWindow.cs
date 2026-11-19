using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;
using static DesktopNotesSwitcher.UiText;

namespace DesktopNotesSwitcher;

internal sealed class NoteWindow : Form
{
    private const uint RootWindow = 2; // GA_ROOT
    private readonly LocalData _data;
    private readonly Action<bool> _savePin;
    private readonly TextBox _editor;
    private readonly Button _close;
    private readonly CheckBox _pin;
    private readonly ToolTip _pinTip = new();
    private readonly Label _heading = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Left
    };
    private readonly Label _status = new() { AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Timer _autosave = new() { Interval = 350 };
    private Skin _skin;
    private Font? _editorFont;
    private Guid? _desktop;
    private bool _loading;
    private bool _dirty;
    private bool _statusError;
    private bool _updatingPin;
    private bool _hiddenWithBar;

    public NoteWindow(LocalData data, Skin skin, Action<bool> savePin)
    {
        _data = data;
        _skin = skin;
        _savePin = savePin;
        Text = T("AppName");
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(320, 235);

        _close = new Button
        {
            Text = "×",
            AccessibleName = T("CloseNote"),
            Size = new Size(30, 28),
            Anchor = AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat
        };
        _close.FlatAppearance.BorderSize = 0;
        _close.Click += (_, _) => Dismiss();

        _pin = new CheckBox
        {
            Appearance = Appearance.Button,
            Text = T("PinButton"),
            AccessibleName = T("PinNote"),
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(48, 28),
            Anchor = AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _pin.FlatAppearance.BorderSize = 0;
        _pin.CheckedChanged += (_, _) => SavePin();
        _pinTip.SetToolTip(_pin, T("PinNote"));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(_heading, 0, 0);
        header.Controls.Add(_pin, 1, 0);
        header.Controls.Add(_close, 2, 0);

        _editor = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            AcceptsReturn = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            AccessibleName = T("NoteContent")
        };
        _editor.TextChanged += (_, _) =>
        {
            if (_loading)
                return;
            _dirty = true;
            SetStatus(T("NoteSaving"));
            _autosave.Stop();
            _autosave.Start();
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(_editor, 0, 1);
        layout.Controls.Add(_status, 0, 2);
        Controls.Add(layout);
        ApplySkin(skin);

        _autosave.Tick += (_, _) => SaveNow();
        KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Escape)
            {
                Dismiss();
                args.SuppressKeyPress = true;
            }
            else if (args.Control && args.KeyCode == Keys.S)
            {
                SaveNow();
                args.SuppressKeyPress = true;
            }
        };
    }

    protected override bool ShowWithoutActivation => true;

    public void ApplySkin(Skin skin)
    {
        _skin = skin;
        BackColor = skin.Paper;
        _close.BackColor = skin.Number;
        _close.ForeColor = skin.Ink;
        ApplyPinColors();
        _heading.ForeColor = skin.Heading;
        _editor.BackColor = skin.Editor;
        _editor.ForeColor = skin.Ink;
        _status.ForeColor = _statusError ? skin.Error : skin.Hint;
    }

    public void ApplyTopMost(bool pinned)
    {
        TopMost = pinned;
        _updatingPin = true;
        _pin.Checked = pinned;
        _updatingPin = false;
        ApplyPinColors();
    }

    private void ApplyPinColors()
    {
        _pin.BackColor = _pin.Checked ? _skin.Selected : _skin.Number;
        _pin.ForeColor = _pin.Checked ? _skin.SelectedInk : _skin.Ink;
        _pin.FlatAppearance.CheckedBackColor = _skin.Selected;
        _pin.FlatAppearance.MouseOverBackColor = _pin.Checked ? _skin.SelectedHover : _skin.NumberHover;
        _pin.FlatAppearance.MouseDownBackColor = _pin.FlatAppearance.MouseOverBackColor;
    }

    private void SavePin()
    {
        if (_updatingPin)
            return;
        try
        {
            _savePin(_pin.Checked);
        }
        catch (Exception error) when (LocalData.IsStorageError(error))
        {
            ApplyTopMost(TopMost);
            SetStatus(T("SettingsSaveError"), error: true);
        }
    }

    public bool IsPointerOver(Point position) =>
        Visible && Bounds.Contains(position) && GetAncestor(WindowFromPoint(position), RootWindow) == Handle;

    public void SetBarCollapsed(bool collapse)
    {
        if (collapse)
        {
            if (Visible && SaveNow())
            {
                _hiddenWithBar = true;
                Hide();
            }
        }
        else if (_hiddenWithBar)
        {
            _hiddenWithBar = false;
            Show();
        }
    }

    public void HideForDesktopChange()
    {
        _hiddenWithBar = false;
        SaveNow();
        Hide();
    }

    public void ApplyTextOptions(Preferences preferences)
    {
        _editor.WordWrap = preferences.WrapNotes;
        _editor.ScrollBars = preferences.WrapNotes ? ScrollBars.Vertical : ScrollBars.Both;
        if (_editorFont?.SizeInPoints == preferences.NoteFontSize)
            return;

        Font? previous = _editorFont;
        _editorFont = new Font(Font.FontFamily, preferences.NoteFontSize, FontStyle.Regular, GraphicsUnit.Point);
        _editor.Font = _editorFont;
        previous?.Dispose();
    }

    public void Toggle(Form anchor, Guid desktop, int number)
    {
        if (Visible && _desktop == desktop && !_editor.ReadOnly)
        {
            Dismiss();
            return;
        }
        if (!SaveNow())
        {
            Reveal(anchor);
            return;
        }

        _desktop = desktop;
        _heading.Text = number == 0 ? T("Note") : T("NumberedNote", number);
        Text = _heading.Text;
        _loading = true;
        try
        {
            _editor.ReadOnly = false;
            _editor.Text = _data.ReadNote(desktop);
            SetStatus(T(_editor.TextLength == 0 ? "NoteEmptyHint" : "NoteSaved"));
        }
        catch (Exception error) when (LocalData.IsStorageError(error))
        {
            _editor.Text = "";
            _editor.ReadOnly = true;
            SetStatus(T("NoteReadError"), error: true);
        }
        finally
        {
            _loading = false;
        }
        Reveal(anchor);
        _editor.Select(_editor.TextLength, 0);
    }

    public void Reveal(Form anchor)
    {
        _hiddenWithBar = false;
        Rectangle work = Screen.FromControl(anchor).WorkingArea;
        int gap = anchor.LogicalToDeviceUnits(8);
        Location = new Point(anchor.Left, anchor.Top);
        int x = anchor.Left - Width - gap;
        int y = anchor.Top;
        if (x < work.Left)
            x = anchor.Right + gap;
        if (x + Width > work.Right)
        {
            x = Math.Clamp(anchor.Right - Width, work.Left, Math.Max(work.Left, work.Right - Width));
            y = anchor.Bottom + gap;
            if (y + Height > work.Bottom)
                y = anchor.Top - Height - gap;
        }
        Location = new Point(Math.Clamp(x, work.Left, Math.Max(work.Left, work.Right - Width)),
            Math.Clamp(y, work.Top, Math.Max(work.Top, work.Bottom - Height)));
        if (!Visible)
            Show();
        Activate();
        _editor.Focus();
    }

    public bool SaveNow()
    {
        _autosave.Stop();
        if (!_dirty || _desktop is not Guid desktop)
            return true;
        try
        {
            _data.WriteNote(desktop, _editor.Text);
            _dirty = false;
            SetStatus(T("NoteSaved"));
            return true;
        }
        catch (Exception error) when (LocalData.IsStorageError(error))
        {
            SetStatus(T("NoteSaveError"), error: true);
            return false;
        }
    }

    private void Dismiss()
    {
        if (SaveNow())
        {
            _hiddenWithBar = false;
            Hide();
        }
    }

    private void SetStatus(string text, bool error = false)
    {
        _statusError = error;
        _status.Text = text;
        _status.ForeColor = error ? _skin.Error : _skin.Hint;
    }

    protected override void OnFormClosing(FormClosingEventArgs args)
    {
        if (args.CloseReason == CloseReason.UserClosing)
        {
            args.Cancel = true;
            Dismiss();
        }
        base.OnFormClosing(args);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autosave.Dispose();
            _pinTip.Dispose();
        }
        base.Dispose(disposing);
        if (disposing)
            _editorFont?.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);
}
