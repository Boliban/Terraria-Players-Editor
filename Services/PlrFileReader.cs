using Terraria_Players_Editor.Models;
using System.Text;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Reads and parses Terraria .plr player files using sequential BinaryReader,
/// matching Terraria's actual Player.Deserialize method for v319.
/// </summary>
public static class PlrFileReader
{
    private const int ItemSlots = 58;
    private const int ArmorSlots = 20;
    private const int DyeSlots = 10;
    private const int MiscEquipCount = 5;
    private const int StorageSlots = 40;
    private const int BuffCount = 44;

    /// <summary>
    /// Read player data from already-decrypted plaintext bytes.
    /// Decryption is handled by the caller (MainForm.OnOpen).
    /// </summary>
    public static PlayerData Read(byte[] plainData)
    {
        DebugLog.Clear();
        DebugLog.LogHex("Plaintext input to PlrFileReader", plainData);
        var player = new PlayerData();
        try
        {
            using var ms = new MemoryStream(plainData);
            using var r = new BinaryReader(ms);

            // === Original PLR header format ===
            // version(4) + "relogic"(7 raw bytes) + fileType(1) + revision(4) + favorites(8) = 24 bytes
            int version = r.ReadInt32();
            DebugLog.Log($"Version: {version} (0x{version:X8})");
            if (version < 1 || version > 1000) return player;
            player.FileVersion = version;

            // Read "relogic" as 7 raw bytes (NOT a length-prefixed string)
            byte[] magic = r.ReadBytes(7);
            r.ReadByte(); // fileType (0x03)
            player.Revision = r.ReadInt32();
            r.ReadInt64(); // favorites (skip)

            // === Player data starts here ===
            long posName = ms.Position;
            player.Name = r.ReadString();
            DebugLog.Log($"Name: '{player.Name}' at pos {posName}");
            player.Difficulty = r.ReadByte();
            player.PlayTime = r.ReadInt64();
            player.Appearance.HairStyle = r.ReadInt32();
            if (player.Appearance.HairStyle >= 228) player.Appearance.HairStyle = 0;
            player.Appearance.HairDye = r.ReadByte();
            r.ReadByte(); // team

            // === Hide visual accessory: 10 bits in 2 bytes ===
            byte hva1 = r.ReadByte();
            for (int i = 0; i < 8; i++) player.Appearance.HideVisual[i] = (hva1 & (1 << i)) != 0;
            byte hva2 = r.ReadByte();
            for (int i = 0; i < 2; i++) if (i + 8 < player.Appearance.HideVisual.Length) player.Appearance.HideVisual[i + 8] = (hva2 & (1 << i)) != 0;

            // hideMisc: unpack 1 byte into 5 bools
            byte hideMsc = r.ReadByte();
            for (int i = 0; i < 5 && i < player.Appearance.HideMisc.Length; i++)
                player.Appearance.HideMisc[i] = (hideMsc & (1 << i)) != 0;
            player.Appearance.SkinVariant = r.ReadByte();

            // === Stats ===
            player.Stats.Health = r.ReadInt32();
            player.Stats.MaxHealth = r.ReadInt32();
            if (player.Stats.MaxHealth > 500) player.Stats.MaxHealth = 500;
            player.Stats.Mana = r.ReadInt32();
            player.Stats.MaxMana = r.ReadInt32();

            // === Upgrades ===
            player.Upgrades.ExtraAccessory = r.ReadBoolean();
            player.Upgrades.UnlockedBiomeTorches = r.ReadBoolean();
            player.Upgrades.UsingBiomeTorches = r.ReadBoolean();
            player.Upgrades.AteArtisanBread = r.ReadBoolean();
            player.Upgrades.UsedAegisCrystal = r.ReadBoolean();
            player.Upgrades.UsedAegisFruit = r.ReadBoolean();
            player.Upgrades.UsedArcaneCrystal = r.ReadBoolean();
            player.Upgrades.UsedGalaxyPearl = r.ReadBoolean();
            player.Upgrades.UsedGummyWorm = r.ReadBoolean();
            player.Upgrades.UsedAmbrosia = r.ReadBoolean();
            r.ReadBoolean(); // downedDD2

            // === Counters ===
            player.TaxMoney = r.ReadInt32();
            player.NumberOfDeathsPvE = r.ReadInt32();
            player.NumberOfDeathsPvP = r.ReadInt32();

            // === Colors (7 × RGB) ===
            player.Appearance.HairColor = ReadColor(r);
            player.Appearance.SkinColor = ReadColor(r);
            player.Appearance.EyeColor = ReadColor(r);
            player.Appearance.ShirtColor = ReadColor(r);
            player.Appearance.UnderShirtColor = ReadColor(r);
            player.Appearance.PantsColor = ReadColor(r);
            player.Appearance.ShoeColor = ReadColor(r);

            // === Armor: 20 slots ===
            var flatArmor = new List<ItemData>(ArmorSlots);
            for (int i = 0; i < ArmorSlots; i++)
            {
                int id = r.ReadInt32();
                byte prefix = r.ReadByte();
                flatArmor.Add(new ItemData { ItemId = id, Prefix = prefix, StackSize = 1 });
            }
            player.Armor = flatArmor.GetRange(0, 3);
            player.VanityArmor = flatArmor.GetRange(3, 3);
            player.Accessories = flatArmor.GetRange(6, 7);
            player.VanityAccessories = flatArmor.GetRange(13, 7);

            // === Dyes: 10 slots ===
            player.ArmorDyes = new List<ItemData>(DyeSlots);
            for (int i = 0; i < DyeSlots; i++)
            {
                int id = r.ReadInt32();
                byte prefix = r.ReadByte();
                player.ArmorDyes.Add(new ItemData { ItemId = id, Prefix = prefix, StackSize = 1 });
            }

            // === Inventory: 58 slots ===
            player.MainInventory = new List<ItemData>(50);
            for (int i = 0; i < ItemSlots; i++)
            {
                int id = r.ReadInt32();
                int stack = r.ReadInt32();
                byte prefix = r.ReadByte();
                bool fav = r.ReadBoolean();
                var item = new ItemData { ItemId = id, StackSize = stack, Prefix = prefix, Favorited = fav };
                if (i < 50) player.MainInventory.Add(item);
                else if (i < 54) player.Coins.Add(item);
                else player.Ammo.Add(item);
            }

            // === Misc equips + dyes: 5 each ===
            player.MiscEquips = new List<ItemData>(MiscEquipCount);
            player.MiscEquipDyes = new List<ItemData>(MiscEquipCount);
            for (int i = 0; i < MiscEquipCount; i++)
            {
                int meId = r.ReadInt32();
                byte mePrefix = r.ReadByte();
                player.MiscEquips.Add(new ItemData { ItemId = meId, Prefix = mePrefix, StackSize = 1 });
                int mdId = r.ReadInt32();
                byte mdPrefix = r.ReadByte();
                player.MiscEquipDyes.Add(new ItemData { ItemId = mdId, Prefix = mdPrefix, StackSize = 1 });
            }

            // === Storage: PiggyBank, Safe, DefenderForge (no favorited byte) ===
            player.PiggyBank = ReadStorageItems(r, StorageSlots, false);
            player.Safe = ReadStorageItems(r, StorageSlots, false);
            player.DefenderForge = ReadStorageItems(r, StorageSlots, false);

            // === Storage: VoidVault (with favorited byte) ===
            player.VoidVault = ReadStorageItems(r, StorageSlots, true);

            // === Void vault info ===
            r.ReadByte();

            // === Buffs: type+time pairs ===
            for (int i = 0; i < BuffCount; i++)
            {
                int bt = r.ReadInt32();
                int btime = r.ReadInt32();
                if (i < player.BuffTypes.Length) player.BuffTypes[i] = bt;
                if (i < player.BuffTimes.Length) player.BuffTimes[i] = btime;
            }

            // === Spawn points ===
            player.SpawnPoints.Clear();
            for (int i = 0; i < 200; i++)
            {
                int sx = r.ReadInt32();
                if (sx == -1) break;
                int sy = r.ReadInt32();
                int swid = r.ReadInt32();
                string swn = r.ReadString();
                player.SpawnPoints.Add(new SpawnPointData { X = sx, Y = sy, WorldId = swid, WorldName = swn });
            }

            // === Flags ===
            player.HotbarLocked = r.ReadBoolean();
            for (int i = 0; i < 13; i++)
                player.HideInfo[i] = r.ReadBoolean();
            player.AnglerQuestsFinished = r.ReadInt32();

            // === DPad / builder ===
            for (int i = 0; i < 4; i++) r.ReadInt32(); // DPad bindings
            for (int i = 0; i < 12; i++) r.ReadInt32(); // builderAccStatus
            r.ReadInt32(); // bartenderQuestLog

            // === Death state ===
            bool dead = r.ReadBoolean();
            if (dead) r.ReadInt32(); // respawnTimer

            // === Timestamp ===
            r.ReadInt64(); // lastTimePlayerWasSaved

            // === Golfer ===
            player.GolferScoreAccumulated = r.ReadInt32();

            // === Creative tracker (research items) ===
            // Uses BinaryWriter strings (7-bit-encoded length), matching the game
            int researchCount = r.ReadInt32();
            player.ResearchedItems.Clear();
            for (int i = 0; i < researchCount; i++)
            {
                string internalName = r.ReadString();
                int count = r.ReadInt32();
                player.ResearchedItems[internalName] = count;
            }

            // === Temporary item slots ===
            byte tempCount = r.ReadByte();
            for (int i = 0; i < tempCount; i++)
            {
                r.ReadInt32(); // type
                r.ReadInt32(); // stack
                r.ReadByte();  // prefix
                r.ReadBoolean(); // favorited
            }

            // === Creative powers ===
            // Sentinel-based: while(true) { bool hasMore; if(!hasMore) break; ushort id; data }
            while (r.ReadBoolean())
            {
                r.ReadUInt16(); // power ID
                // Skip power-specific data — format varies by power type
                // Each power's Load method handles its own data
                // For now, we can't easily skip arbitrary power data
                // Since non-journey chars have 0 powers, this loop won't execute
            }

            // === Super cart bits ===
            byte cartBits = r.ReadByte();
            player.Upgrades.UnlockedSuperCart = (byte)((cartBits & 1) != 0 ? 1 : 0);
            player.Upgrades.EnabledSuperCart = (cartBits & 2) != 0;

            // === Loadouts ===
            if (version >= 262)
            {
                player.CurrentLoadout = r.ReadInt32();
                player.Loadout2 = ReadLoadout(r);
                player.Loadout3 = ReadLoadout(r);
            }

            // === Voice ===
            if (version >= 280)
            {
                r.ReadByte(); // voiceVariant
            }
            if (version >= 281)
            {
                r.ReadSingle(); // voicePitchOffset
            }

            // === Pending refunds ===
            if (version >= 300)
            {
                int refundCount = r.ReadInt32();
                for (int i = 0; i < refundCount; i++)
                {
                    r.ReadInt32(); // type
                    r.ReadInt32(); // stack
                    r.ReadByte();  // prefix
                    r.ReadBoolean(); // favorited
                }
            }

            // === One-time dialogues ===
            if (version >= 310)
            {
                int dialogueCount = r.ReadInt32();
                for (int i = 0; i < dialogueCount; i++)
                    r.ReadString();
            }
        }
        catch (EndOfStreamException)
        {
            // File truncated — return what we've read so far
        }

        return player;
    }

    #region Helpers

    private static byte[] ReadColor(BinaryReader r)
    {
        return [r.ReadByte(), r.ReadByte(), r.ReadByte()];
    }

    private static List<ItemData> ReadStorageItems(BinaryReader r, int count, bool readFavorited)
    {
        var items = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
        {
            int id = r.ReadInt32();
            int stack = r.ReadInt32();
            byte prefix = r.ReadByte();
            bool fav = readFavorited && r.ReadBoolean();
            items.Add(new ItemData { ItemId = id, StackSize = stack, Prefix = prefix, Favorited = fav });
        }
        return items;
    }

    private static PlayerLoadout ReadLoadout(BinaryReader r)
    {
        var lo = new PlayerLoadout();
        // Armor: 20 slots
        for (int i = 0; i < 20; i++)
        {
            int id = r.ReadInt32();
            byte prefix = r.ReadByte();
            var item = new ItemData { ItemId = id, Prefix = prefix, StackSize = 1 };
            if (i < 3) lo.Armor.Add(item);
            else if (i < 6) lo.VanityArmor.Add(item);
            else if (i < 13) lo.Accessories.Add(item);
            else lo.VanityAccessories.Add(item);
        }
        // Dyes: 10 slots
        lo.ArmorDyes = new List<ItemData>(10);
        for (int i = 0; i < 10; i++)
        {
            int id = r.ReadInt32();
            byte prefix = r.ReadByte();
            lo.ArmorDyes.Add(new ItemData { ItemId = id, Prefix = prefix, StackSize = 1 });
        }

        // Misc equips + dyes: 5 each
        for (int i = 0; i < 5; i++)
        {
            int meId = r.ReadInt32();
            byte mePrefix = r.ReadByte();
            lo.MiscEquips.Add(new ItemData { ItemId = meId, Prefix = mePrefix, StackSize = 1 });
            int mdId = r.ReadInt32();
            byte mdPrefix = r.ReadByte();
            lo.MiscEquipDyes.Add(new ItemData { ItemId = mdId, Prefix = mdPrefix, StackSize = 1 });
        }
        return lo;
    }

    #endregion
}
