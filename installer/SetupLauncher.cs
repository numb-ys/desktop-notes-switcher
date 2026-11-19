using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class SetupLauncher
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        bool silent = Array.Exists(arguments, argument => Matches(argument, "/SILENT") || Matches(argument, "/VERYSILENT"));
        bool explicitLanguage = Array.Exists(arguments, argument => Matches(argument, "/LANG=chs") || Matches(argument, "/LANG=en"));
        string parameters = CommandLineTail();

        if (!silent && !explicitLanguage)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var picker = new LanguageWindow(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh"))
            {
                if (picker.ShowDialog() != DialogResult.OK)
                    return 2;
                parameters += " /LANG=" + picker.Language;
            }
        }

        try
        {
            return RunInstaller(parameters, silent);
        }
        catch (Exception error)
        {
            if (!(error is IOException || error is UnauthorizedAccessException ||
                  error is System.ComponentModel.Win32Exception))
                throw;
            if (!silent)
            {
                MessageBox.Show("无法启动安装向导。\r\nCould not start the installer.",
                    "桌面便笺 / Desktop Notes Switcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 1;
        }
    }

    private static bool Matches(string argument, string expected)
    {
        return string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string CommandLineTail()
    {
        // Keep the original quoting of installer options such as /DIR="C:\Path With Spaces".
        string command = Environment.CommandLine;
        bool quoted = false;
        for (int index = 0; index < command.Length; index++)
        {
            char character = command[index];
            if (character == '"')
                quoted = !quoted;
            else if (!quoted && char.IsWhiteSpace(character))
                return command.Substring(index);
        }
        return string.Empty;
    }

    private static int RunInstaller(string parameters, bool silent)
    {
        string folder = Path.Combine(Path.GetTempPath(), "DesktopNotesSwitcher-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string installer = Path.Combine(folder, "DesktopNotesSwitcher-Installer.exe");
        try
        {
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("InstallerPayload"))
            using (var output = new FileStream(installer, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (resource == null)
                    throw new InvalidDataException("The installer payload is missing.");
                resource.CopyTo(output);
            }

            var start = new ProcessStartInfo(installer, parameters)
            {
                UseShellExecute = false,
                WindowStyle = silent ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
            };
            using (Process process = Process.Start(start))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }
        finally
        {
            try
            {
                File.Delete(installer);
                Directory.Delete(folder);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
