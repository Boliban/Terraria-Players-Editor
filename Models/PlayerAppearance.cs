namespace Terraria_Players_Editor.Models;

/// <summary>
/// Player appearance data: hair, skin, colors, and visibility flags.
/// </summary>
public sealed class PlayerAppearance
{
    // Hair / skin
    public int HairStyle { get; set; }
    public byte HairDye { get; set; }
    public byte SkinVariant { get; set; }  // 0 = Female, 1 = Male

    // Visibility toggles
    public bool[] HideVisual { get; set; } = new bool[10];
    public bool[] HideMisc { get; set; } = new bool[5];  // pet, light pet, minecart, mount, hook

    // Colors (RGB bytes)
    public byte[] HairColor { get; set; } = new byte[3];
    public byte[] SkinColor { get; set; } = new byte[3];
    public byte[] EyeColor { get; set; } = new byte[3];
    public byte[] ShirtColor { get; set; } = new byte[3];
    public byte[] UnderShirtColor { get; set; } = new byte[3];
    public byte[] PantsColor { get; set; } = new byte[3];
    public byte[] ShoeColor { get; set; } = new byte[3];
}
