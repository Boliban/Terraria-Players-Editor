namespace Terraria_Players_Editor.Models;

/// <summary>
/// A player spawn/bed point tied to a specific world.
/// </summary>
public sealed class SpawnPointData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int WorldId { get; set; }
    public string WorldName { get; set; } = "";
}
