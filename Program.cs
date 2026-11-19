namespace DesktopNotesSwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(true, "Local\\DesktopNotesSwitcher", out bool firstInstance);
        if (!firstInstance)
            return;

        ApplicationConfiguration.Initialize();
        using Stream iconStream = typeof(Program).Assembly.GetManifestResourceStream("DesktopNotesSwitcher.NotesIcon")!;
        using var icon = new Icon(iconStream);
        using var app = new SwitcherApplication(icon);
        Application.Run(app);
    }
}
