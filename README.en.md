# Desktop Notes Switcher

[简体中文说明](README.md)

A floating bar for Windows virtual desktops. It shows the available desktops, highlights the active one, and stays in place as you switch.

## Install and use

Download the `Setup-win-x64.exe` installer from [Releases](../../releases/latest). Select the **English** or **简体中文** card and click **Continue**. Setup and the app both use the selected language. If the app is running, exit it from the notification area before installing.

| Action | Result |
| --- | --- |
| Right-click a number | Switch directly to that desktop |
| Left-click a number | Open or close that desktop's note |
| Left-click the note icon | Open note settings for text size, word wrap, and visibility when the bar hides |
| Right-click the note icon | Open app settings |
| Drag the note icon | Move the bar or snap it to a screen edge |
| Right-click the notification-area icon | Open settings, move the bar to the primary display, or exit |

In App Settings, turn on **Snap to edge** and **Auto-hide at edge** to leave a narrow tab when you move the pointer away. Hover over the tab to show the bar. The reveal delay is adjustable from 0 to 1000 ms. App Settings also offers launch at sign-in and theme selection.

Enable **Quick switch** in App Settings and choose a desktop. When the bar hides, the numbered button blends into the edge tab with rounded corners. Left-click the button to jump directly to the desktop. The number updates when desktops are reordered. If the selected desktop is deleted, choose a new target to show the button again.

Notes appear as **Note 1**, **Note 2**, and so on. Each note is saved as plain text in `%LOCALAPPDATA%\DesktopNotesSwitcher\notes`. Use `Ctrl+S` to save immediately and `Esc` to close a note. Windows Settings can uninstall the app while keeping your local notes and preferences.

In Note Settings, choose **Keep visible** or **Hide with the bar**. With the latter option, the note hides when the pointer leaves both the note and the bar. When the bar appears again, the open note returns. The **Pin** button beside the close button toggles whether notes stay above other windows. Notes stay visible and pinned by default.

## Requirements and build

The installer targets x64 editions of Windows 11 24H2 and later. Direct switching and keeping the bar on every desktop use Windows Shell interfaces that can change with OS updates.

To run from source, install the .NET 10 SDK and run `dotnet run`. To build the installer, use PowerShell 7 and [Inno Setup 7](https://jrsoftware.org/inno-setup/downloads).

```powershell
pwsh -File installer/Build-Installer.ps1
```

For new UI text, add matching entries to the [English](Resources/Strings.resx) and [Simplified Chinese](Resources/Strings.zh-CN.resx) resources. The installer build checks both sets of entries and their format placeholders.

Licensed under the [MIT License](LICENSE).
