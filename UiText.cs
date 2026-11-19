using System.Globalization;
using System.Resources;

namespace DesktopNotesSwitcher;

internal static class UiText
{
    private static readonly ResourceManager Strings = new("DesktopNotesSwitcher.Resources.Strings", typeof(UiText).Assembly);

    public static void Initialize()
    {
        string? language = null;
        try
        {
            language = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ui-language.txt")).Trim();
        }
        catch (Exception error) when (LocalData.IsStorageError(error))
        {
            // Source builds and older installations follow the Windows display language.
        }

        CultureInfo culture = language switch
        {
            "zh-CN" => CultureInfo.GetCultureInfo("zh-CN"),
            "en" => CultureInfo.GetCultureInfo("en"),
            _ => CultureInfo.GetCultureInfo(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh-CN" : "en")
        };
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string T(string key) => Strings.GetString(key)
        ?? throw new InvalidOperationException($"Missing UI string: {key}");

    public static string T(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, T(key), args);
}
