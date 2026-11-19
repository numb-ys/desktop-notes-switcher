namespace DesktopNotesSwitcher;

internal sealed record Skin(
    string Id, string Name, Color Paper, Color Ink, Color Outline,
    Color Grip, Color GripInk, Color Number, Color NumberHover,
    Color Selected, Color SelectedHover, Color SelectedInk,
    Color Heading, Color Editor, Color Hint, Color Error)
{
    public static Skin Default { get; } = new(
        "default", "默认便笺", Color.FromArgb(255, 248, 223), Color.FromArgb(38, 47, 54),
        Color.FromArgb(221, 208, 169), Color.FromArgb(250, 210, 103), Color.FromArgb(135, 103, 32),
        Color.FromArgb(246, 229, 180), Color.FromArgb(248, 217, 137), Color.FromArgb(44, 64, 75),
        Color.FromArgb(64, 88, 103), Color.White, Color.FromArgb(64, 53, 28),
        Color.FromArgb(255, 253, 241), Color.FromArgb(101, 89, 60), Color.FromArgb(141, 38, 24));

    public static IReadOnlyList<Skin> Available { get; } = [Default];

    public static Skin Find(string id) => Available.FirstOrDefault(skin => skin.Id == id) ?? Default;
}
