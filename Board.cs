using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DesktopNotesSwitcher;

internal sealed class Board : Form
{
    private static readonly Color Paper = Color.FromArgb(255, 248, 223);
    private static readonly Color Ink = Color.FromArgb(38, 47, 54);
    private readonly ToolTip _tips = new();
    private readonly Control _grip = new Grip { Cursor = Cursors.SizeAll, AccessibleName = "便笺悬浮条" };
    private readonly FlowLayoutPanel _numbers = new() { WrapContents = false, AutoScroll = true };
    private readonly Dictionary<Guid, NumberButton> _buttons = new();
    private readonly uint _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private Placement _placement;
    private Point? _dragOrigin;
    private Point _windowOrigin;
    private bool _dragging;

    public event Action<Guid, int>? NoteRequested;
    public event Action<Guid>? SwitchRequested;
    public event Action<Placement>? PlacementChanged;
    public event Action? ExplorerRestarted;
    public bool Snap => _placement.Snap;
    public void SetGripMenu(ContextMenuStrip menu) => _grip.ContextMenuStrip = menu;

    public Board(Placement placement)
    {
        _placement = placement;
        Text = "桌面便笺";
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Paper;
        Controls.Add(_numbers);
        Controls.Add(_grip);
        _tips.SetToolTip(_grip, "拖动移动 · 右键打开菜单");
        _grip.MouseDown += StartDrag;
        _grip.MouseMove += Drag;
        _grip.MouseUp += FinishDrag;
        _grip.MouseCaptureChanged += (_, _) => FinishDrag();
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        DpiChanged += (_, _) =>
        {
            if (!_dragging)
                BeginInvoke(FitToDisplay);
        };
    }

    protected override bool ShowWithoutActivation => true;

    public void SetDesktops(Guid[] desktops, Guid current)
    {
        _numbers.SuspendLayout();
        foreach (NumberButton old in _buttons.Values)
        {
            _tips.SetToolTip(old, null);
            old.Dispose();
        }
        _buttons.Clear();

        for (int index = 0; index < desktops.Length; index++)
            AddNumber(desktops[index], index + 1);
        if (desktops.Length == 0)
            AddNumber(Guid.Empty, 0);

        _numbers.ResumeLayout();
        Highlight(current);
        FitToDisplay();
    }

    private void AddNumber(Guid desktop, int number)
    {
        string label = number == 0 ? "便笺" : number.ToString();
        string tooltip = number == 0 ? "打开便笺" : $"桌面 {number} · 左键便笺，右键切换";
        var button = new NumberButton
        {
            Text = label,
            AccessibleName = number == 0 ? label : $"桌面 {number} 便笺",
            AccessibleDescription = tooltip,
            ForeColor = Ink,
            TabStop = false,
            Cursor = Cursors.Hand
        };
        button.Click += (_, _) => NoteRequested?.Invoke(desktop, number);
        button.MouseUp += (_, args) =>
        {
            if (args.Button == MouseButtons.Right && button.ClientRectangle.Contains(args.Location))
                SwitchRequested?.Invoke(desktop);
        };
        _tips.SetToolTip(button, tooltip);
        _buttons.Add(desktop, button);
        _numbers.Controls.Add(button);
    }

    public void Highlight(Guid current)
    {
        foreach ((Guid id, NumberButton button) in _buttons)
            button.SetCurrent(id != Guid.Empty && id == current);
    }

    public void FitToDisplay()
    {
        if (_dragging || IsDisposed)
            return;
        Screen display = Screen.AllScreens.FirstOrDefault(screen => screen.DeviceName == _placement.Display)
            ?? Screen.PrimaryScreen!;
        LayoutOn(display.WorkingArea, _placement);
    }

    public void ResetPosition()
    {
        _placement = Placement.Default;
        FitToDisplay();
        PlacementChanged?.Invoke(_placement);
    }

    public void SetSnap(bool enabled)
    {
        DockSide side = enabled ? FindSide(Screen.FromControl(this).WorkingArea) : DockSide.None;
        CommitPosition(side, enabled);
    }

    private void LayoutOn(Rectangle work, Placement placement, Point? drop = null)
    {
        bool vertical = placement.Side is DockSide.Left or DockSide.Right;
        int cellWidth = Scale(_buttons.ContainsKey(Guid.Empty) ? 46 : 34);
        int cellHeight = Scale(29);
        int padding = Scale(4);
        int grip = Scale(27);
        int count = Math.Max(_buttons.Count, 1);
        int width = vertical ? cellWidth : count * cellWidth;
        int height = vertical ? count * cellHeight : cellHeight;

        // Reserve scrollbar space only when the desktop list exceeds the monitor.
        if (vertical && height + grip + padding * 2 > work.Height)
        {
            height = Math.Max(cellHeight, work.Height - grip - padding * 2);
            width += Scale(20);
        }
        if (!vertical && width + grip + padding * 2 > work.Width)
        {
            width = Math.Max(cellWidth, work.Width - grip - padding * 2);
            height += Scale(20);
        }

        _numbers.SuspendLayout();
        _numbers.FlowDirection = vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight;
        _grip.SetBounds(padding, padding, vertical ? width : grip, vertical ? grip : height);
        _numbers.SetBounds(vertical ? padding : padding + grip, vertical ? padding + grip : padding, width, height);
        foreach (NumberButton button in _buttons.Values)
        {
            button.Margin = new Padding(Scale(2));
            button.Size = new Size(cellWidth - button.Margin.Horizontal, cellHeight - button.Margin.Vertical);
        }
        _numbers.ResumeLayout();

        int outerWidth = width + padding * 2 + (vertical ? 0 : grip);
        int outerHeight = height + padding * 2 + (vertical ? grip : 0);
        int left = Position(work.Left, work.Width - outerWidth, placement.X, drop?.X);
        int top = Position(work.Top, work.Height - outerHeight, placement.Y, drop?.Y);
        if (placement.Side == DockSide.Left) left = work.Left;
        if (placement.Side == DockSide.Right) left = work.Right - outerWidth;
        if (placement.Side == DockSide.Top) top = work.Top;
        if (placement.Side == DockSide.Bottom) top = work.Bottom - outerHeight;
        SetBounds(left, top, outerWidth, outerHeight);

        using GraphicsPath shape = Rounded(new Rectangle(0, 0, Width, Height), Scale(18));
        Region? previous = Region;
        Region = new Region(shape);
        previous?.Dispose();
        Invalidate();
    }

    private static int Position(int start, int travel, double fraction, int? absolute)
    {
        travel = Math.Max(0, travel);
        return absolute is int position
            ? Math.Clamp(position, start, start + travel)
            : start + (int)Math.Round(travel * Math.Clamp(fraction, 0, 1));
    }

    private int Scale(int value) => LogicalToDeviceUnits(value);

    private void StartDrag(object? sender, MouseEventArgs args)
    {
        if (args.Button != MouseButtons.Left)
            return;
        _dragOrigin = _grip.PointToScreen(args.Location);
        _windowOrigin = Location;
        _grip.Capture = true;
    }

    private void Drag(object? sender, MouseEventArgs args)
    {
        if (_dragOrigin is not Point origin)
            return;
        Point cursor = Cursor.Position;
        var offset = new Size(cursor.X - origin.X, cursor.Y - origin.Y);
        if (!_dragging && Math.Abs(offset.Width) < Scale(3) && Math.Abs(offset.Height) < Scale(3))
            return;
        _dragging = true;
        Location = _windowOrigin + offset;
    }

    private void FinishDrag(object? sender, MouseEventArgs args)
    {
        if (args.Button == MouseButtons.Left)
            FinishDrag();
    }

    private void FinishDrag()
    {
        if (_dragOrigin is null)
            return;
        _dragOrigin = null;
        _grip.Capture = false;
        if (!_dragging)
            return;
        _dragging = false;
        Rectangle work = Screen.FromControl(this).WorkingArea;
        CommitPosition(Snap ? FindSide(work) : DockSide.None, Snap);
    }

    private DockSide FindSide(Rectangle work)
    {
        (DockSide side, int distance) = new[]
        {
            (DockSide.Left, Math.Abs(Left - work.Left)),
            (DockSide.Right, Math.Abs(work.Right - Right)),
            (DockSide.Top, Math.Abs(Top - work.Top)),
            (DockSide.Bottom, Math.Abs(work.Bottom - Bottom))
        }.MinBy(edge => edge.Item2);
        return distance <= Scale(24) ? side : DockSide.None;
    }

    private void CommitPosition(DockSide side, bool snap)
    {
        Screen screen = Screen.FromControl(this);
        Rectangle work = screen.WorkingArea;
        _placement = new Placement(screen.DeviceName, 0, 0, side, snap);
        LayoutOn(work, _placement, Location);
        _placement = _placement with
        {
            X = Math.Clamp((double)(Left - work.Left) / Math.Max(1, work.Width - Width), 0, 1),
            Y = Math.Clamp((double)(Top - work.Top) / Math.Max(1, work.Height - Height), 0, 1)
        };
        PlacementChanged?.Invoke(_placement);
    }

    private void DisplayChanged(object? sender, EventArgs args)
    {
        if (IsHandleCreated)
            BeginInvoke(FitToDisplay);
    }

    protected override void WndProc(ref Message message)
    {
        const int mouseActivate = 0x0021;
        const int rightButtonDown = 0x0204;
        if (message.Msg == mouseActivate && (message.LParam.ToInt64() >> 16) == rightButtonDown)
        {
            message.Result = 3; // MA_NOACTIVATE
            return;
        }
        if ((uint)message.Msg == _taskbarCreated)
            ExplorerRestarted?.Invoke();
        base.WndProc(ref message);
    }

    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var outline = new Pen(Color.FromArgb(221, 208, 169), Scale(1));
        using GraphicsPath shape = Rounded(new Rectangle(1, 1, Width - 2, Height - 2), Scale(16));
        args.Graphics.DrawPath(outline, shape);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.DisplaySettingsChanged -= DisplayChanged;
            _tips.Dispose();
        }
        base.Dispose(disposing);
    }

    private static GraphicsPath Rounded(Rectangle bounds, int diameter)
    {
        var path = new GraphicsPath();
        path.AddRoundedRectangle(bounds, new Size(diameter, diameter));
        return path;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);

    private sealed class Grip : Control
    {
        protected override void OnPaint(PaintEventArgs args)
        {
            var box = new Rectangle((Width - LogicalToDeviceUnits(14)) / 2,
                (Height - LogicalToDeviceUnits(16)) / 2, LogicalToDeviceUnits(14), LogicalToDeviceUnits(16));
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Color.FromArgb(250, 210, 103));
            using var outline = new Pen(Color.FromArgb(135, 103, 32), LogicalToDeviceUnits(1));
            using GraphicsPath shape = Rounded(box, LogicalToDeviceUnits(4));
            args.Graphics.FillPath(fill, shape);
            args.Graphics.DrawPath(outline, shape);
            int inset = LogicalToDeviceUnits(4);
            args.Graphics.DrawLine(outline, box.Left + inset, box.Top + inset + 1, box.Right - inset, box.Top + inset + 1);
            args.Graphics.DrawLine(outline, box.Left + inset, box.Top + inset * 2 + 1, box.Right - inset, box.Top + inset * 2 + 1);
        }
    }

    private sealed class NumberButton : Button
    {
        private bool _current;
        private bool _hovered;

        public void SetCurrent(bool current)
        {
            _current = current;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs args)
        {
            base.OnMouseEnter(args);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs args)
        {
            base.OnMouseLeave(args);
            _hovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            args.Graphics.Clear(Paper);
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fillColor = _current ? Color.FromArgb(44, 64, 75) : Color.FromArgb(246, 229, 180);
            if (_hovered)
                fillColor = _current ? Color.FromArgb(64, 88, 103) : Color.FromArgb(248, 217, 137);
            using var fill = new SolidBrush(fillColor);
            using GraphicsPath shape = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), LogicalToDeviceUnits(12));
            args.Graphics.FillPath(fill, shape);
            TextRenderer.DrawText(args.Graphics, Text, Font, ClientRectangle, _current ? Color.White : ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
