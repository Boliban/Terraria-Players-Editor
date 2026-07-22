namespace Terraria_Players_Editor.Models;

/// <summary>
/// Represents a single item slot in a Terraria player's inventory or equipment.
/// </summary>
public sealed class ItemData
{
    /// <summary>Item ID (0 = empty slot).</summary>
    public int ItemId { get; set; }

    /// <summary>Stack size (1 for equipment, up to 9999 for inventory items).</summary>
    public int StackSize { get; set; }

    /// <summary>Prefix/modifier ID (0 = no prefix).</summary>
    public byte Prefix { get; set; }

    /// <summary>Whether the item is favorited (inventory slots only).</summary>
    public bool Favorited { get; set; }

    public bool IsEmpty => ItemId == 0;

    public string ItemName => ItemDatabase.GetName(ItemId);

    public string PrefixName => PrefixData.GetName(Prefix);

    public string Category => ItemDatabase.GetCategory(ItemId);

    public ItemData Clone() => new()
    {
        ItemId = ItemId,
        StackSize = StackSize,
        Prefix = Prefix,
        Favorited = Favorited
    };
}
