using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopNotesSwitcher;

internal enum DockSide { None, Left, Right, Top, Bottom }

internal sealed record Placement(string Display, double X, double Y, DockSide Side, bool Snap)
{
    public static Placement Default { get; } = new("", 1, .24, DockSide.Right, true);
}

internal sealed record Preferences
{
    public const int MaxRevealDelay = 1000;
    public const int MinNoteFontSize = 9;
    public const int MaxNoteFontSize = 24;
    public string SkinId { get; init; } = Skin.Default.Id;
    public bool CollapseAtEdge { get; init; }
    public int RevealDelayMs { get; init; } = 200;
    public int NoteFontSize { get; init; } = MinNoteFontSize;
    public bool WrapNotes { get; init; } = true;
    public bool NoteFollowsBar { get; init; }
    public bool NoteTopMost { get; init; } = true;
}

internal sealed class LocalData
{
    private readonly string _root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopNotesSwitcher");
    private readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Placement ReadPlacement()
    {
        try
        {
            string path = Path.Combine(_root, "placement.json");
            if (!File.Exists(path))
                return Placement.Default;
            Placement? placement = JsonSerializer.Deserialize<Placement>(File.ReadAllText(path), _json);
            return placement is { Display: not null } && Enum.IsDefined(placement.Side)
                ? placement
                : Placement.Default;
        }
        catch (Exception error) when (IsStorageError(error) || error is JsonException)
        {
            return Placement.Default;
        }
    }

    public void WritePlacement(Placement placement) =>
        Write(Path.Combine(_root, "placement.json"), JsonSerializer.Serialize(placement, _json));

    public Preferences ReadPreferences()
    {
        try
        {
            string path = Path.Combine(_root, "preferences.json");
            if (File.Exists(path) &&
                JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path), _json) is Preferences preferences)
                return preferences with
                {
                    RevealDelayMs = Math.Clamp(preferences.RevealDelayMs, 0, Preferences.MaxRevealDelay),
                    NoteFontSize = Math.Clamp(preferences.NoteFontSize, Preferences.MinNoteFontSize, Preferences.MaxNoteFontSize)
                };
        }
        catch (Exception error) when (IsStorageError(error) || error is JsonException)
        {
            // Use the default when a preference from disk cannot be loaded.
        }
        return new Preferences();
    }

    public void WritePreferences(Preferences preferences) =>
        Write(Path.Combine(_root, "preferences.json"), JsonSerializer.Serialize(preferences, _json));

    public string ReadNote(Guid desktop)
    {
        try
        {
            return File.ReadAllText(NotePath(desktop));
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            return "";
        }
    }

    public void WriteNote(Guid desktop, string note) => Write(NotePath(desktop), note);

    public static bool IsStorageError(Exception error) => error is IOException or UnauthorizedAccessException;

    private string NotePath(Guid desktop) => Path.Combine(_root, "notes", $"{desktop:D}.txt");

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, text);
        File.Move(temporary, path, overwrite: true);
    }
}
