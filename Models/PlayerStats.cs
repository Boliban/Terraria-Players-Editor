namespace Terraria_Players_Editor.Models;

/// <summary>
/// Core player stats: health and mana.
/// </summary>
public sealed class PlayerStats
{
    public int Health { get; set; }
    public int MaxHealth { get; set; } = 100;
    public int Mana { get; set; }
    public int MaxMana { get; set; } = 20;
}
