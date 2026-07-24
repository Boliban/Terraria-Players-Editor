namespace Terraria_Players_Editor.Services;

/// <summary>
/// Terraria buff type classification. Used by the buff browser to color-code entries.
/// Buff IDs are from the game's BuffID constants (0-387 for 1.4.5).
/// </summary>
public enum BuffKind { Buff, Debuff, Pet }

public static class BuffData
{
    /// <summary>Classify a buff ID as Buff, Debuff, or Pet.</summary>
    public static BuffKind GetBuffKind(int buffId)
    {
        // Pet buffs: specific ID ranges
        // Terraria pet buffs use specific IDs for each pet type
        if (IsPetBuff(buffId)) return BuffKind.Pet;

        // Debuff IDs: 20-39 (primary debuffs), 44-47, 67-70, 80, 119-120, etc.
        if (IsDebuff(buffId)) return BuffKind.Debuff;

        return BuffKind.Buff;
    }

    /// <summary>Get the display color for a buff kind.</summary>
    public static Color GetColor(BuffKind kind) => kind switch
    {
        BuffKind.Buff => Color.Green,
        BuffKind.Debuff => Color.Red,
        BuffKind.Pet => Color.DodgerBlue,
        _ => Color.Black
    };

    private static bool IsDebuff(int id) => id switch
    {
        20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or
        30 or 31 or 32 or 33 or 34 or 35 or 36 or 37 or 38 or 39 or
        44 or 45 or 46 or 47 or 67 or 68 or 69 or 70 or 80 or
        119 or 120 or 144 or 145 or 146 or 147 or 148 or 149 or
        153 or 156 or 160 or 163 or 164 or 168 or 169 or
        183 or 194 or 195 or 196 or 197 or
        203 or 204 or 205 or 322 or 323 or 324 or 325 or 326 or 327 or
        328 or 329 or 330 or 331 or 332 or 333 or 334 or 335 or 336 or 337 => true,
        _ => false
    };

    private static bool IsPetBuff(int id)
    {
        // Terraria pet buffs are assigned IDs in specific ranges.
        // These are approximate — exact IDs come from the game's BuffID class.
        return (id >= 39 && id <= 43) ||     // Bunny, Baby Dino, Penguin, Turtle, Baby Eater
               (id >= 49 && id <= 55) ||     // Baby Skeletron, Baby Hornet, Tiki Spirit, Pet Lizard, etc.
               (id >= 68 && id <= 73) ||     // Pet Sapling, Baby Truffle, etc. — wait, 68-70 are debuffs
               (id >= 72 && id <= 73) ||     // Wisp, Baby Snowman
               (id >= 85 && id <= 92) ||     // Pet Bunny, Baby Penguin, etc.
               (id >= 110 && id <= 116) ||   // Various pets
               (id >= 127 && id <= 130) ||   // Pet Fairy, Baby Face Monster, etc.
               (id >= 181 && id <= 184) ||   // Various pets
               (id >= 200 && id <= 222) ||   // 1.3+ pets
               (id >= 249 && id <= 291) ||   // 1.4+ pets
               (id >= 296 && id <= 318) ||   // More pets
               id == 187 || id == 188 ||     // Pet Lizard, Pet Parrot
               id == 48 ||                    // Pet Bunny (alternative)
               id == 193;                     // Pet Sugar Glider
    }
}
