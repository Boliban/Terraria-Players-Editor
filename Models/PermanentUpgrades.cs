namespace Terraria_Players_Editor.Models;

/// <summary>
/// Permanent player upgrades from consumable items and unlocks.
/// </summary>
public sealed class PermanentUpgrades
{
    // Demon Heart (extra accessory slot)
    public bool ExtraAccessory { get; set; }

    // Consumable upgrades
    public bool UsedAegisCrystal { get; set; }
    public bool UsedAegisFruit { get; set; }
    public bool UsedArcaneCrystal { get; set; }
    public bool UsedGalaxyPearl { get; set; }
    public bool UsedGummyWorm { get; set; }
    public bool UsedAmbrosia { get; set; }
    public bool AteArtisanBread { get; set; }

    // Torch favoriting
    public bool UnlockedBiomeTorches { get; set; }
    public bool UsingBiomeTorches { get; set; }

    // Minecart upgrade
    public byte UnlockedSuperCart { get; set; }
    public bool EnabledSuperCart { get; set; }
}
