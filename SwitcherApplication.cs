using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;
using static DesktopNotesSwitcher.UiText;

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
    private Preferences _preferences;
    private SettingsDialog? _settings;
    private DesktopSnapshot? _snapshot;
    private nint _pinnedWindow;
    private string? _problem;

    public SwitcherApplication(Icon icon)
    {
        _preferences = _data.ReadPreferences();
        Skin skin = Skin.Find(_preferences.SkinId);
        _note = new NoteWindow(_data, skin, pinned => SavePreferences(_preferences with { NoteTopMost = pinned })) { Icon = icon };
        _note.ApplyTextOptions(_preferences);
        _note.ApplyTopMost(_preferences.NoteTopMost);
        _board = new Board(_data.ReadPlacement(), skin,
            position => _preferences.NoteFollowsBar && _note.IsPointerOver(position)) { Icon = icon };
        _board.ApplyEdgeOptions(_preferences);
        MainForm = _board;
        _board.NoteRequested += (id, number) => _note.Toggle(_board, id, number);
        _board.SwitchRequested += SwitchTo;
        _board.NoteSettingsRequested += () => ShowSettings(note: true);
        _board.ApplicationSettingsRequested += () => ShowSettings(note: false);
        _board.CollapseChanged += collapsed => _note.SetBarCollapsed(collapsed && _preferences.NoteFollowsBar);
        _board.PlacementChanged += SavePlacement;
        _board.ExplorerRestarted += ResetConnection;
        _board.FormClosing += (_, args) =>
        {
            bool saved = _note.SaveNow();
            if (!saved && args.CloseReason == CloseReason.UserClosing)
            {
                args.Cancel = true;
                _board.RevealBoard();
                _note.Reveal(_board);
            }
        };

        _menu.Items.Add(T("TrayShowBar"), null, (_, _) => ShowBoard());
        _menu.Items.Add(T("TrayResetPosition"), null, (_, _) => _board.ResetPosition());
        var snap = new ToolStripMenuItem(T("EdgeSnap")) { CheckOnClick = true };
        snap.Click += (_, _) => SetSnap(snap.Checked);
        _menu.Items.Add(snap);
        _menu.Items.Add(T("TrayNoteSettings"), null, (_, _) => ShowSettings(note: true));
        _menu.Items.Add(T("TrayAppSettings"), null, (_, _) => ShowSettings(note: false));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_status);
        _menu.Items.Add(T("TrayExit"), null, (_, _) => _board.Close());
        _menu.Opening += (_, _) =>
        {
            snap.Checked = _board.Snap;
            _status.Text = _problem ?? T("TrayInstructions");
        };
        _tray = new NotifyIcon { Icon = icon, Text = T("AppName"), ContextMenuStrip = _menu, Visible = true };
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
        _board.RevealBoard(fromTray: true);
        _board.BringToFront();
        RefreshDesktops();
    }

    private void ShowSettings(bool note)
    {
        if (_settings is not null && (_settings is NoteSettingsWindow) != note)
            _settings.Close();
        if (_settings is null)
        {
            _settings = note
                ? new NoteSettingsWindow(_preferences, SavePreferences)
                : new SettingsWindow(_preferences, _board.Snap, SavePreferences, SetSnap);
            _settings.Icon = _board.Icon;
            _settings.FormClosed += (_, _) => _settings = null;
            _settings.Show();
        }
        _settings.Activate();
    }

    private void SetSnap(bool enabled)
    {
        _board.SetSnap(enabled);
        if (_settings is SettingsWindow settings)
            settings.DisplaySnap(_board.Snap);
    }

    private void SavePreferences(Preferences updated)
    {
        _data.WritePreferences(updated);
        Preferences previous = _preferences;
        _preferences = updated;
        _settings?.SyncPreferences(updated);
        if (updated.CollapseAtEdge != previous.CollapseAtEdge || updated.RevealDelayMs != previous.RevealDelayMs)
            _board.ApplyEdgeOptions(updated);
        if (updated.NoteFontSize != previous.NoteFontSize || updated.WrapNotes != previous.WrapNotes)
            _note.ApplyTextOptions(updated);
        if (updated.NoteTopMost != previous.NoteTopMost)
            _note.ApplyTopMost(updated.NoteTopMost);
        if (updated.NoteFollowsBar != previous.NoteFollowsBar)
            _note.SetBarCollapsed(_board.IsCollapsed && updated.NoteFollowsBar);
        if (updated.SkinId != previous.SkinId)
        {
            Skin skin = Skin.Find(updated.SkinId);
            _board.ApplySkin(skin);
            _note.ApplySkin(skin);
        }
    }

    private void SwitchTo(Guid id)
    {
        if (id == Guid.Empty || id == _snapshot?.CurrentId || _problem is not null)
            return;
        _note.HideForDesktopChange();
        try
        {
            _desktops.SwitchTo(id);
            RefreshDesktops();
        }
        catch (COMException)
        {
            ResetConnection();
            _problem = T("SwitchRetry");
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
                _note.HideForDesktopChange();
                _settings?.Close();
            }

            _snapshot = snapshot;
            if (_pinnedWindow != _board.Handle && _desktops.Pin(_board.Handle))
                _pinnedWindow = _board.Handle;
            _problem = _pinnedWindow != 0 ? null : T("PinPending");
        }
        catch (PlatformNotSupportedException error)
        {
            _problem = error.Message;
        }
        catch (Exception error) when (error is COMException or InvalidCastException)
        {
            ResetConnection();
            _problem = T("ConnectError");
        }
        _tray.Text = _problem is null ? T("AppName") : T("TrayStatus", T("AppName"), _problem);
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
            _tray.ShowBalloonTip(3500, T("AppName"), T("PositionSaveError"), ToolTipIcon.Warning);
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
