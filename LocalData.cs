using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopNotesSwitcher;

internal enum DockSide { None, Left, Right, Top, Bottom }

internal sealed record Placement(string Display, double X, double Y, DockSide Side, bool Snap)
{
    public static Placement Default { get; } = new("", 1, .24, DockSide.Right, true);
}

internal sealed class LocalData
{
    private sealed record Preferences(string SkinId);

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

    public Skin ReadSkin()
    {
        try
        {
            string path = Path.Combine(_root, "preferences.json");
            if (File.Exists(path))
            {
                string? id = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path), _json)?.SkinId;
                return Skin.Available.FirstOrDefault(skin => skin.Id == id) ?? Skin.Default;
            }
        }
        catch (Exception error) when (IsStorageError(error) || error is JsonException)
        {
            // Use the default when a preference from disk cannot be loaded.
        }
        return Skin.Default;
    }

    public void WriteSkin(Skin skin) =>
        Write(Path.Combine(_root, "preferences.json"), JsonSerializer.Serialize(new Preferences(skin.Id), _json));

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
