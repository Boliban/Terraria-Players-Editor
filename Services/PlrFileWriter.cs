using Terraria_Players_Editor.Models;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Serializes a PlayerData model back to a .plr binary format and encrypts it.
/// Mirrors the read order in PlrFileReader exactly.
/// </summary>
public static class PlrFileWriter
{
    /// <summary>Serialize player data to an encrypted .plr byte array.</summary>
    public static byte[] Write(PlayerData player)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // === Header ===
        writer.Write(player.FileVersion);
        writer.Write("relogic");
        writer.Write((byte)0x03);     // fileType = player
        writer.Write(player.Revision);
        writer.Write(0L);              // favorite (always 0)

        // === Identity ===
        writer.Write(player.Name);
        writer.Write(player.Difficulty);
        writer.Write(player.PlayTime);

        // === Appearance ===
        writer.Write(player.Appearance.HairStyle);
        writer.Write(player.Appearance.HairDye);
        for (int i = 0; i < 10; i++)
            writer.Write(i < player.Appearance.HideVisual.Length ? player.Appearance.HideVisual[i] : false);
        for (int i = 0; i < 5; i++)
            writer.Write(i < player.Appearance.HideMisc.Length ? player.Appearance.HideMisc[i] : false);
        writer.Write(player.Appearance.SkinVariant);

        WriteColor(writer, player.Appearance.HairColor);
        WriteColor(writer, player.Appearance.SkinColor);
        WriteColor(writer, player.Appearance.EyeColor);
        WriteColor(writer, player.Appearance.ShirtColor);
        WriteColor(writer, player.Appearance.UnderShirtColor);
        WriteColor(writer, player.Appearance.PantsColor);
        WriteColor(writer, player.Appearance.ShoeColor);

        // === Stats ===
        writer.Write(player.Stats.Health);
        writer.Write(player.Stats.MaxHealth);
        writer.Write(player.Stats.Mana);
        writer.Write(player.Stats.MaxMana);

        // === Permanent Upgrades ===
        writer.Write(player.Upgrades.ExtraAccessory);
        writer.Write(false); // downedDD2EventAnyDifficulty
        writer.Write(player.Upgrades.UnlockedBiomeTorches);
        writer.Write(player.Upgrades.UsingBiomeTorches);
        writer.Write(player.Upgrades.AteArtisanBread);
        writer.Write(player.Upgrades.UsedAegisCrystal);
        writer.Write(player.Upgrades.UsedAegisFruit);
        writer.Write(player.Upgrades.UsedArcaneCrystal);
        writer.Write(player.Upgrades.UsedGalaxyPearl);
        writer.Write(player.Upgrades.UsedGummyWorm);
        writer.Write(player.Upgrades.UsedAmbrosia);
        writer.Write(player.Upgrades.UnlockedSuperCart);
        writer.Write(player.Upgrades.EnabledSuperCart);

        // === Tax Money ===
        writer.Write(player.TaxMoney);

        // === Equipment ===
        WriteEquipmentItems(writer, player.Armor, 3);
        WriteEquipmentItems(writer, player.VanityArmor, 3);
        writer.Write(player.Accessories.Count);
        WriteEquipmentItems(writer, player.Accessories, player.Accessories.Count);
        writer.Write(player.VanityAccessories.Count);
        WriteEquipmentItems(writer, player.VanityAccessories, player.VanityAccessories.Count);
        WriteEquipmentItems(writer, player.ArmorDyes, 10);

        // === Inventory ===
        WriteInventoryItems(writer, player.MainInventory, 50);
        WriteInventoryItems(writer, player.Coins, 4);
        WriteInventoryItems(writer, player.Ammo, 4);

        // === Misc Equipment ===
        WriteEquipmentItems(writer, player.MiscEquips, 5);
        WriteEquipmentItems(writer, player.MiscEquipDyes, 5);

        // === Storage ===
        WriteStorageItems(writer, player.PiggyBank, 40);
        WriteStorageItems(writer, player.Safe, 40);
        WriteStorageItems(writer, player.DefenderForge, 40);
        if (player.FileVersion >= 269)
            WriteStorageItems(writer, player.VoidVault, 40);

        // === Trash ===
        var trash = player.TrashItem ?? new ItemData();
        writer.Write(trash.ItemId);
        writer.Write((short)trash.StackSize);
        writer.Write(trash.Prefix);

        // === Buffs ===
        int buffCount = player.FileVersion >= 269 ? 44 : 22;
        for (int i = 0; i < buffCount; i++)
            writer.Write(i < player.BuffTypes.Length ? player.BuffTypes[i] : 0);
        for (int i = 0; i < buffCount; i++)
            writer.Write(i < player.BuffTimes.Length ? player.BuffTimes[i] : 0);

        // === Spawn Points ===
        writer.Write(player.SpawnPoints.Count);
        foreach (var sp in player.SpawnPoints)
        {
            writer.Write(sp.X);
            writer.Write(sp.Y);
            writer.Write(sp.WorldId);
            writer.Write(sp.WorldName);
        }

        // === Toggles / Info ===
        writer.Write(player.HotbarLocked);
        for (int i = 0; i < 13; i++)
            writer.Write(i < player.HideInfo.Length ? player.HideInfo[i] : false);
        writer.Write(player.AnglerQuestsFinished);
        writer.Write(0); // savedBartender
        writer.Write(player.GolferScoreAccumulated);

        // === Builder Toggles ===
        int toggleCount = 12;
        for (int i = 0; i < toggleCount; i++)
            writer.Write(i < player.BuilderToggles.Length ? player.BuilderToggles[i] : false);

        // === Dpad Bindings ===
        for (int i = 0; i < 4; i++)
            writer.Write(i < player.DPadRadialBindings.Length ? player.DPadRadialBindings[i] : 0);
        for (int i = 0; i < 4; i++)
            writer.Write(i < player.BuilderAccStatus.Length ? player.BuilderAccStatus[i] : 0);

        writer.Write(0); // bartenderQuestLog

        // === Death Counts ===
        writer.Write(player.NumberOfDeathsPvE);
        writer.Write(player.NumberOfDeathsPvP);

        // === Cooldowns ===
        writer.Write(player.PotionDelay);
        writer.Write(player.ManaPotionDelay);
        writer.Write(player.RestorationPotionCd);

        // === Emotes (v220+) ===
        if (player.FileVersion >= 220)
        {
            writer.Write(player.UnlockedEmotes.Count);
            foreach (var emote in player.UnlockedEmotes)
                writer.Write(emote);
        }

        // === Loadouts (v269+) ===
        if (player.FileVersion >= 269)
        {
            writer.Write(player.CurrentLoadout);
            WriteSavedLoadout(writer, player.Loadout2);
            WriteSavedLoadout(writer, player.Loadout3);
        }

        // === Journey Research (v230+) ===
        if (player.FileVersion >= 230)
        {
            writer.Write(player.ResearchedItems.Count);
            foreach (var kv in player.ResearchedItems)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }
        }

        writer.Flush();
        var plainBytes = ms.ToArray();
        return PlrCrypto.Encrypt(plainBytes);
    }

    private static void WriteColor(BinaryWriter writer, byte[] color)
    {
        for (int i = 0; i < 3; i++)
            writer.Write(i < color.Length ? color[i] : (byte)0);
    }

    private static void WriteEquipmentItems(BinaryWriter writer, List<ItemData> items, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i < items.Count)
            {
                writer.Write(items[i].ItemId);
                writer.Write(items[i].Prefix);
            }
            else
            {
                writer.Write(0);   // ItemId = empty
                writer.Write((byte)0);
            }
        }
    }

    private static void WriteInventoryItems(BinaryWriter writer, List<ItemData> items, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i < items.Count)
            {
                writer.Write(items[i].ItemId);
                writer.Write((short)items[i].StackSize);
                writer.Write(items[i].Prefix);
                writer.Write(items[i].Favorited);
            }
            else
            {
                writer.Write(0);
                writer.Write((short)0);
                writer.Write((byte)0);
                writer.Write(false);
            }
        }
    }

    private static void WriteStorageItems(BinaryWriter writer, List<ItemData> items, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i < items.Count)
            {
                writer.Write(items[i].ItemId);
                writer.Write((short)items[i].StackSize);
                writer.Write(items[i].Prefix);
            }
            else
            {
                writer.Write(0);
                writer.Write((short)0);
                writer.Write((byte)0);
            }
        }
    }

    private static void WriteSavedLoadout(BinaryWriter writer, PlayerLoadout? loadout)
    {
        loadout ??= new PlayerLoadout();
        WriteEquipmentItems(writer, loadout.Armor, 3);
        WriteEquipmentItems(writer, loadout.VanityArmor, 3);
        writer.Write(loadout.Accessories.Count);
        WriteEquipmentItems(writer, loadout.Accessories, loadout.Accessories.Count);
        writer.Write(loadout.VanityAccessories.Count);
        WriteEquipmentItems(writer, loadout.VanityAccessories, loadout.VanityAccessories.Count);
        WriteEquipmentItems(writer, loadout.ArmorDyes, 10);
        WriteEquipmentItems(writer, loadout.MiscEquips, 5);
        WriteEquipmentItems(writer, loadout.MiscEquipDyes, 5);
    }
}
