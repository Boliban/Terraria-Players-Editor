using Terraria_Players_Editor.Models;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Reads and parses Terraria .plr player files (1.4.4+ format).
/// Handles version-specific branching for backwards compatibility.
/// </summary>
public static class PlrFileReader
{
    /// <summary>Parse a .plr file from decrypted bytes.</summary>
    public static PlayerData Read(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var player = new PlayerData();

        try
        {
            // === Header ===
            player.FileVersion = reader.ReadInt32();
            string magic = reader.ReadString();       // "relogic"
            byte fileType = reader.ReadByte();         // 0x03 = player
            player.Revision = reader.ReadInt32();
            reader.ReadInt64();                        // favorite (unused, always 0)

            // === Identity ===
            player.Name = reader.ReadString();
            player.Difficulty = reader.ReadByte();
            player.PlayTime = reader.ReadInt64();

            // === Appearance ===
            player.Appearance.HairStyle = reader.ReadInt32();
            player.Appearance.HairDye = reader.ReadByte();

            // HideVisual (10 bools)
            for (int i = 0; i < 10; i++)
                player.Appearance.HideVisual[i] = reader.ReadBoolean();

            // HideMisc (5 bools)
            for (int i = 0; i < 5; i++)
                player.Appearance.HideMisc[i] = reader.ReadBoolean();

            player.Appearance.SkinVariant = reader.ReadByte();

            // Colors (7 × 3 bytes RGB)
            player.Appearance.HairColor = reader.ReadBytes(3);
            player.Appearance.SkinColor = reader.ReadBytes(3);
            player.Appearance.EyeColor = reader.ReadBytes(3);
            player.Appearance.ShirtColor = reader.ReadBytes(3);
            player.Appearance.UnderShirtColor = reader.ReadBytes(3);
            player.Appearance.PantsColor = reader.ReadBytes(3);
            player.Appearance.ShoeColor = reader.ReadBytes(3);

            // === Stats ===
            player.Stats.Health = reader.ReadInt32();
            player.Stats.MaxHealth = reader.ReadInt32();
            player.Stats.Mana = reader.ReadInt32();
            player.Stats.MaxMana = reader.ReadInt32();

            // === Permanent Upgrades ===
            player.Upgrades.ExtraAccessory = reader.ReadBoolean();
            // downedDD2EventAnyDifficulty - skip
            reader.ReadBoolean();
            player.Upgrades.UnlockedBiomeTorches = reader.ReadBoolean();
            player.Upgrades.UsingBiomeTorches = reader.ReadBoolean();
            player.Upgrades.AteArtisanBread = reader.ReadBoolean();
            player.Upgrades.UsedAegisCrystal = reader.ReadBoolean();
            player.Upgrades.UsedAegisFruit = reader.ReadBoolean();
            player.Upgrades.UsedArcaneCrystal = reader.ReadBoolean();
            player.Upgrades.UsedGalaxyPearl = reader.ReadBoolean();
            player.Upgrades.UsedGummyWorm = reader.ReadBoolean();
            player.Upgrades.UsedAmbrosia = reader.ReadBoolean();
            player.Upgrades.UnlockedSuperCart = reader.ReadByte();
            player.Upgrades.EnabledSuperCart = reader.ReadBoolean();

            // === Tax Money ===
            player.TaxMoney = reader.ReadInt32();

            // === Equipment (id + prefix only, no stack/favorited) ===
            player.Armor = ReadEquipmentItems(reader, 3);
            player.VanityArmor = ReadEquipmentItems(reader, 3);
            // Accessory count: read the count from stream
            int accCount = reader.ReadInt32();
            player.Accessories = ReadEquipmentItems(reader, accCount);
            int vanityAccCount = reader.ReadInt32();
            player.VanityAccessories = ReadEquipmentItems(reader, vanityAccCount);
            player.ArmorDyes = ReadEquipmentItems(reader, 10);    // 3 armor + 7 acc dyes

            // === Inventory (with stack + favorited) ===
            player.MainInventory = ReadInventoryItems(reader, 50);
            player.Coins = ReadInventoryItems(reader, 4);
            player.Ammo = ReadInventoryItems(reader, 4);

            // === Misc Equipment ===
            player.MiscEquips = ReadEquipmentItems(reader, 5);     // pet, light pet, minecart, mount, hook
            player.MiscEquipDyes = ReadEquipmentItems(reader, 5);

            // === Storage (with stack, no favorited) ===
            player.PiggyBank = ReadStorageItems(reader, 40);
            player.Safe = ReadStorageItems(reader, 40);
            player.DefenderForge = ReadStorageItems(reader, 40);
            if (player.FileVersion >= 269)
                player.VoidVault = ReadStorageItems(reader, 40);

            // === Trash ===
            int trashId = reader.ReadInt32();
            int trashStack = reader.ReadInt16();
            byte trashPrefix = reader.ReadByte();
            player.TrashItem = new ItemData
            {
                ItemId = trashId,
                StackSize = trashStack,
                Prefix = trashPrefix
            };

            // === Buffs ===
            int buffCount = player.FileVersion >= 269 ? 44 : 22;
            player.BuffTypes = new int[44];
            player.BuffTimes = new int[44];
            for (int i = 0; i < buffCount; i++)
                player.BuffTypes[i] = reader.ReadInt32();
            for (int i = 0; i < buffCount; i++)
                player.BuffTimes[i] = reader.ReadInt32();

            // === Spawn Points ===
            int spawnCount = reader.ReadInt32();
            for (int i = 0; i < spawnCount; i++)
            {
                player.SpawnPoints.Add(new SpawnPointData
                {
                    X = reader.ReadInt32(),
                    Y = reader.ReadInt32(),
                    WorldId = reader.ReadInt32(),
                    WorldName = reader.ReadString()
                });
            }

            // === Toggles / Info ===
            player.HotbarLocked = reader.ReadBoolean();
            for (int i = 0; i < 13; i++)
                player.HideInfo[i] = reader.ReadBoolean();
            player.AnglerQuestsFinished = reader.ReadInt32();
            reader.ReadInt32(); // savedBartender
            player.GolferScoreAccumulated = reader.ReadInt32();

            // === Builder Toggles (version-dependent count) ===
            int toggleCount = 12; // v230+
            if (player.FileVersion < 200)
                toggleCount = 10;
            else if (player.FileVersion < 230)
                toggleCount = 11;
            for (int i = 0; i < toggleCount; i++)
                player.BuilderToggles[i] = reader.ReadBoolean();

            // === Dpad Bindings / Builder Acc ===
            for (int i = 0; i < 4; i++)
                player.DPadRadialBindings[i] = reader.ReadInt32();
            for (int i = 0; i < 4; i++)
                player.BuilderAccStatus[i] = reader.ReadInt32();

            reader.ReadInt32(); // bartenderQuestLog

            // === Death Counts ===
            player.NumberOfDeathsPvE = reader.ReadInt32();
            player.NumberOfDeathsPvP = reader.ReadInt32();

            // === Cooldowns ===
            player.PotionDelay = reader.ReadInt32();
            player.ManaPotionDelay = reader.ReadInt32();
            player.RestorationPotionCd = reader.ReadInt32();

            // === Emotes (v220+) ===
            if (player.FileVersion >= 220)
            {
                int emoteCount = reader.ReadInt32();
                for (int i = 0; i < emoteCount; i++)
                    player.UnlockedEmotes.Add(reader.ReadInt32());
            }

            // === Loadouts (v269+) ===
            if (player.FileVersion >= 269)
            {
                player.CurrentLoadout = reader.ReadInt32();
                player.Loadout2 = ReadSavedLoadout(reader);
                player.Loadout3 = ReadSavedLoadout(reader);
            }

            // === Journey Research (v230+) ===
            if (player.FileVersion >= 230)
            {
                int researchedCount = reader.ReadInt32();
                for (int i = 0; i < researchedCount; i++)
                {
                    string itemName = reader.ReadString();
                    int researchCount = reader.ReadInt32();
                    player.ResearchedItems[itemName] = researchCount;
                }
            }
        }
        catch (EndOfStreamException)
        {
            // File truncated — return what we've read so far
        }

        return player;
    }

    /// <summary>Read equipment items (id + prefix only).</summary>
    private static List<ItemData> ReadEquipmentItems(BinaryReader reader, int count)
    {
        var items = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(new ItemData
            {
                ItemId = reader.ReadInt32(),
                Prefix = reader.ReadByte(),
                StackSize = 1,
                Favorited = false
            });
        }
        return items;
    }

    /// <summary>Read inventory items (id + stack + prefix + favorited).</summary>
    private static List<ItemData> ReadInventoryItems(BinaryReader reader, int count)
    {
        var items = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(new ItemData
            {
                ItemId = reader.ReadInt32(),
                StackSize = reader.ReadInt16(),
                Prefix = reader.ReadByte(),
                Favorited = reader.ReadBoolean()
            });
        }
        return items;
    }

    /// <summary>Read storage items (id + stack + prefix, no favorited).</summary>
    private static List<ItemData> ReadStorageItems(BinaryReader reader, int count)
    {
        var items = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(new ItemData
            {
                ItemId = reader.ReadInt32(),
                StackSize = reader.ReadInt16(),
                Prefix = reader.ReadByte(),
                Favorited = false
            });
        }
        return items;
    }

    /// <summary>Read a saved equipment loadout.</summary>
    private static PlayerLoadout ReadSavedLoadout(BinaryReader reader)
    {
        var loadout = new PlayerLoadout();
        loadout.Armor = ReadEquipmentItems(reader, 3);
        loadout.VanityArmor = ReadEquipmentItems(reader, 3);
        int accCount = reader.ReadInt32();
        loadout.Accessories = ReadEquipmentItems(reader, accCount);
        int vanityAccCount = reader.ReadInt32();
        loadout.VanityAccessories = ReadEquipmentItems(reader, vanityAccCount);
        loadout.ArmorDyes = ReadEquipmentItems(reader, 10);
        loadout.MiscEquips = ReadEquipmentItems(reader, 5);
        loadout.MiscEquipDyes = ReadEquipmentItems(reader, 5);
        return loadout;
    }
}
