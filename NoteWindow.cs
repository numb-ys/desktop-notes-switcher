using Timer = System.Windows.Forms.Timer;

namespace DesktopNotesSwitcher;

internal sealed class NoteWindow : Form
{
    private readonly LocalData _data;
    private readonly TextBox _editor;
    private readonly Label _heading = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        ForeColor = Color.FromArgb(64, 53, 28)
    };
    private readonly Label _status = new() { AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Timer _autosave = new() { Interval = 350 };
    private Guid? _desktop;
    private bool _loading;
    private bool _dirty;

    public NoteWindow(LocalData data)
    {
        _data = data;
        Text = "桌面便笺";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(320, 235);
        BackColor = Color.FromArgb(255, 248, 223);

        var close = new Button
        {
            Text = "×",
            AccessibleName = "收起便笺",
            Size = new Size(30, 28),
            Anchor = AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(246, 229, 180)
        };
        close.FlatAppearance.BorderSize = 0;
        close.Click += (_, _) => Dismiss();

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(_heading, 0, 0);
        header.Controls.Add(close, 1, 0);

        _editor = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            AcceptsReturn = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(255, 253, 241),
            ForeColor = Color.FromArgb(38, 47, 54),
            AccessibleName = "便笺内容"
        };
        _editor.TextChanged += (_, _) =>
        {
            if (_loading)
                return;
            _dirty = true;
            SetStatus("保存中…");
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
        _heading.Text = number == 0 ? "便笺" : $"桌面 {number} 的便笺";
        Text = _heading.Text;
        _loading = true;
        try
        {
            _editor.ReadOnly = false;
            _editor.Text = _data.ReadNote(desktop);
            SetStatus(_editor.TextLength == 0 ? "输入内容后自动保存到本机" : "已保存到本机");
        }
        catch (Exception error) when (LocalData.IsStorageError(error))
        {
            _editor.Text = "";
            _editor.ReadOnly = true;
            SetStatus("无法读取便笺，重新打开可重试。", error: true);
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
            Show(anchor);
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
            SetStatus("已保存到本机");
            return true;
        }
        catch (Exception error) when (LocalData.IsStorageError(error))
        {
            SetStatus("保存失败。按 Ctrl+S 重试。", error: true);
            return false;
        }
    }

    private void Dismiss()
    {
        if (SaveNow())
            Hide();
    }

    private void SetStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.ForeColor = error ? Color.FromArgb(141, 38, 24) : Color.FromArgb(101, 89, 60);
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
            _autosave.Dispose();
        base.Dispose(disposing);
    }
}
