using Microsoft.Win32;

namespace DesktopNotesSwitcher;

internal static class StartupRegistration
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopNotesSwitcher";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(ValueName) is string { Length: > 0 };
    }

    public static void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        string executable = Path.Combine(AppContext.BaseDirectory, "DesktopNotesSwitcher.exe");
        string command = $"\"{executable}\"";
        if (!File.Exists(executable))
            throw new IOException("The executable for the startup entry could not be found.");
        if (command.Length > 260)
            throw new IOException("The executable path exceeds the Windows startup entry limit.");

        using RegistryKey run = Registry.CurrentUser.CreateSubKey(KeyPath);
        run.SetValue(ValueName, command, RegistryValueKind.String);
    }
}
