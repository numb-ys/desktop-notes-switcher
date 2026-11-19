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
            throw new IOException("找不到可注册为启动项的程序。");
        if (command.Length > 260)
            throw new IOException("程序路径过长，无法注册为 Windows 启动项。");

        using RegistryKey run = Registry.CurrentUser.CreateSubKey(KeyPath);
        run.SetValue(ValueName, command, RegistryValueKind.String);
    }
}
