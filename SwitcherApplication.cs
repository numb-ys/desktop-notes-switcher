using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace DesktopNotesSwitcher;

internal sealed class SwitcherApplication : ApplicationContext
{
    private readonly VirtualDesktops _desktops = new();
    private readonly LocalData _data = new();
    private readonly Board _board;
    private readonly NoteWindow _note;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _status = new() { Enabled = false };
    private readonly Timer _refresh = new() { Interval = 650 };
    private Skin _skin;
    private SettingsWindow? _settings;
    private DesktopSnapshot? _snapshot;
    private nint _pinnedWindow;
    private string? _problem;

    public SwitcherApplication(Icon icon)
    {
        _skin = _data.ReadSkin();
        _board = new Board(_data.ReadPlacement(), _skin) { Icon = icon };
        _note = new NoteWindow(_data, _skin) { Icon = icon };
        MainForm = _board;
        _board.NoteRequested += (id, number) => _note.Toggle(_board, id, number);
        _board.SwitchRequested += SwitchTo;
        _board.PlacementChanged += SavePlacement;
        _board.ExplorerRestarted += ResetConnection;
        _board.FormClosing += (_, args) =>
        {
            bool saved = _note.SaveNow();
            if (!saved && args.CloseReason == CloseReason.UserClosing)
            {
                args.Cancel = true;
                _note.Reveal(_board);
            }
        };

        _menu.Items.Add("显示悬浮条", null, (_, _) => ShowBoard());
        _menu.Items.Add("定位到主屏右侧", null, (_, _) => _board.ResetPosition());
        var snap = new ToolStripMenuItem("自动吸附屏幕边缘") { CheckOnClick = true };
        snap.Click += (_, _) => _board.SetSnap(snap.Checked);
        _menu.Items.Add(snap);
        _menu.Items.Add("设置…", null, (_, _) => ShowSettings());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_status);
        _menu.Items.Add("退出", null, (_, _) => _board.Close());
        _menu.Opening += (_, _) =>
        {
            snap.Checked = _board.Snap;
            _status.Text = _problem ?? "右键切换 · 左键打开便笺";
        };
        _board.SetGripMenu(_menu);

        _tray = new NotifyIcon { Icon = icon, Text = "桌面便笺", ContextMenuStrip = _menu, Visible = true };
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
                ShowBoard();
        };

        _board.SetDesktops([], Guid.Empty);
        _board.Show();
        RefreshDesktops();
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26100))
        {
            _refresh.Tick += (_, _) => RefreshDesktops();
            _refresh.Start();
        }
    }

    private void ShowBoard()
    {
        _board.FitToDisplay();
        _board.Show();
        _board.BringToFront();
        RefreshDesktops();
    }

    private void ShowSettings()
    {
        if (_settings is null)
        {
            _settings = new SettingsWindow(_data, _skin, ApplySkin) { Icon = _board.Icon };
            _settings.FormClosed += (_, _) => _settings = null;
            _settings.Show();
        }
        _settings.Activate();
    }

    private void ApplySkin(Skin skin)
    {
        _skin = skin;
        _board.ApplySkin(skin);
        _note.ApplySkin(skin);
    }

    private void SwitchTo(Guid id)
    {
        if (id == Guid.Empty || id == _snapshot?.CurrentId || _problem is not null)
            return;
        _note.SaveNow();
        _note.Hide();
        try
        {
            _desktops.SwitchTo(id);
            RefreshDesktops();
        }
        catch (COMException)
        {
            ResetConnection();
            _problem = "桌面已变动或暂时无法切换，正在重新连接";
        }
    }

    private void RefreshDesktops()
    {
        try
        {
            DesktopSnapshot snapshot = _desktops.Read();
            if (_snapshot is null || !_snapshot.DesktopIds.SequenceEqual(snapshot.DesktopIds))
                _board.SetDesktops(snapshot.DesktopIds, snapshot.CurrentId);
            else if (_snapshot.CurrentId != snapshot.CurrentId)
                _board.Highlight(snapshot.CurrentId);

            if (_snapshot is not null && _snapshot.CurrentId != snapshot.CurrentId)
            {
                _note.SaveNow();
                _note.Hide();
                _settings?.Close();
            }

            _snapshot = snapshot;
            if (_pinnedWindow != _board.Handle && _desktops.Pin(_board.Handle))
                _pinnedWindow = _board.Handle;
            _problem = _pinnedWindow != 0 ? null : "正在等待 Windows 固定悬浮条";
        }
        catch (PlatformNotSupportedException error)
        {
            _problem = error.Message;
        }
        catch (Exception error) when (error is COMException or InvalidCastException)
        {
            ResetConnection();
            _problem = "无法连接 Windows 虚拟桌面";
        }
        _tray.Text = _problem is null ? "桌面便笺" : $"桌面便笺 · {_problem}";
    }

    private void ResetConnection()
    {
        _desktops.Dispose();
        _pinnedWindow = 0;
    }

    private void SavePlacement(Placement placement)
    {
        try
        {
            _data.WritePlacement(placement);
        }
        catch (Exception error) when (LocalData.IsStorageError(error))
        {
            _tray.ShowBalloonTip(3500, "桌面便笺", "无法保存悬浮条位置。", ToolTipIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refresh.Dispose();
            _tray.Dispose();
            _settings?.Dispose();
            _note.Dispose();
            _board.Dispose();
            _menu.Dispose();
            _desktops.Dispose();
        }
        base.Dispose(disposing);
    }
}
