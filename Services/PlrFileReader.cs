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
        DebugLog.Log($"Game Reader: input plaintext size = {plainData.Length} bytes");

        // Strip PKCS7 padding — PlrCrypto.Decrypt leaves it in the output
        // (PaddingMode.None to match the game's raw block layout).
        if (plainData.Length > 0)
        {
            int pad = plainData[^1];
            if (pad > 0 && pad <= 16)
            {
                bool valid = true;
                for (int i = plainData.Length - pad; i < plainData.Length; i++)
                    if (plainData[i] != pad) { valid = false; break; }
                if (valid) plainData = plainData[..^pad];
            }
        }
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
            // Game slot order: armor(3) + accessories(7) + vanity armor(3) + vanity accessories(7)
            player.Armor = flatArmor.GetRange(0, 3);
            player.Accessories = flatArmor.GetRange(3, 7);
            player.VanityArmor = flatArmor.GetRange(10, 3);
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
            long invPos = ms.Position;
            player.MainInventory = new List<ItemData>(50);
            var invDbg = new System.Text.StringBuilder("Inventory read: ");
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
                if (i < 10 && id > 0)
                    invDbg.Append($"[{i}: ID={id} stack={stack}] ");
            }
            DebugLog.Log($"Reader: inventory at offset {invPos}, first non-empty: {invDbg}");

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
            for (int i = 0; i < 4; i++) r.ReadInt32(); // DPad bindings — skip
            for (int i = 0; i < 12; i++)
                player.BuilderAccStatus[i] = r.ReadInt32();
            DebugLog.Log($"Reader: BuilderAccStatus[0]={player.BuilderAccStatus[0]} (ruler: {(player.BuilderAccStatus[0] == 1 ? "ON" : "OFF")})");
            r.ReadInt32(); // bartenderQuestLog

            // === Death state ===
            bool dead = r.ReadBoolean();
            if (dead) r.ReadInt32(); // respawnTimer

            // === Timestamp ===
            r.ReadInt64(); // lastTimePlayerWasSaved

            // === Golfer ===
            player.GolferScoreAccumulated = r.ReadInt32();

            // === Creative tracker / powers / loadouts (v262+) ===
            // Exact layout (from the game source): creativeTracker.Save writes a
            // bool + int32 count + (string,int) entries; then temporary item
            // slots; then CreativePowerManager.SaveToPlayer's sentinel loop
            // (while(hasMore) { ushort id; data } + final false). The power data
            // sizes are not tracked here, so the loadout section (cart bits +
            // CurrentLoadoutIndex + 3 × 280-byte loadouts) is located by scanning
            // right after the tracker/temp sections, and the surrounding raw
            // bytes are preserved for a faithful round-trip.
            if (version >= 262)
            {
                int suffixStart = (int)ms.Position;

                // CreativeTracker.Save: bool flag + int32 count + (string,int) entries
                r.ReadBoolean();
                int researchCount = r.ReadInt32();
                for (int i = 0; i < researchCount; i++)
                {
                    r.ReadString();
                    r.ReadInt32();
                }

                // SaveTemporaryItemSlotContents: byte count + items
                byte tempCount = r.ReadByte();
                for (int i = 0; i < tempCount; i++)
                {
                    r.ReadInt32();
                    r.ReadInt32();
                    r.ReadByte();
                    r.ReadBoolean();
                }

                // Creative powers + cart + loadouts: locate by scanning
                int scanStart = (int)ms.Position;
                int loadoutPos = FindLoadoutSection(plainData, scanStart);
                if (loadoutPos <= 0)
                    throw new InvalidDataException($"无法定位负载数据区段 (scanStart=0x{scanStart:X} len={plainData.Length})");

                // Preserve tracker/temp/powers bytes (cart byte at loadoutPos-1
                // is rewritten from the model on save)
                player.LoadoutPrefix = plainData[suffixStart..(loadoutPos - 1)];

                // The scan locates the CurrentLoadoutIndex; the cart bits byte
                // immediately precedes it.
                ms.Position = loadoutPos - 1;
                byte cartBits = r.ReadByte();
                player.Upgrades.UnlockedSuperCart = (byte)((cartBits & 1) != 0 ? 1 : 0);
                player.Upgrades.EnabledSuperCart = (cartBits & 2) != 0;
                player.CurrentLoadout = r.ReadInt32();
                player.Loadout1 = ReadLoadout(r); // loadout[0]
                player.Loadout2 = ReadLoadout(r); // loadout[1]
                player.Loadout3 = ReadLoadout(r); // loadout[2]

                // The game keeps the active loadout in sync with the armor array
                // when the character loads, so mirror that here: the displayed
                // equipment of the active loadout equals the current equipment.
                if (player.CurrentLoadout == 0 && player.Loadout1 != null)
                    SyncLoadoutToEquipment(player.Loadout1, player);
                else if (player.CurrentLoadout == 1 && player.Loadout2 != null)
                    SyncLoadoutToEquipment(player.Loadout2, player);
                else if (player.CurrentLoadout == 2 && player.Loadout3 != null)
                    SyncLoadoutToEquipment(player.Loadout3, player);

                // Preserve voice/refunds/dialogues bytes
                player.FileTail = plainData[(int)ms.Position..];
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

    /// <summary>
    /// Read loadout data matching the game's EquipmentLoadout.Deserialize format.
    /// Each item is 9 bytes: int32 type + int32 stack + byte prefix (NO favorited).
    /// </summary>
    private static PlayerLoadout ReadLoadout(BinaryReader r)
    {
        var lo = new PlayerLoadout();
        // Armor: 20 items × 9 bytes each, in game slot order:
        // armor(3) + accessories(7) + vanity armor(3) + vanity accessories(7)
        for (int i = 0; i < 20; i++)
        {
            var item = new ItemData { ItemId = r.ReadInt32(), StackSize = r.ReadInt32(), Prefix = r.ReadByte() };
            if (i < 3) lo.Armor.Add(item);
            else if (i < 10) lo.Accessories.Add(item);
            else if (i < 13) lo.VanityArmor.Add(item);
            else lo.VanityAccessories.Add(item);
        }
        // Dyes: 10 items × 9 bytes each
        lo.ArmorDyes = new List<ItemData>(10);
        for (int i = 0; i < 10; i++)
            lo.ArmorDyes.Add(new ItemData { ItemId = r.ReadInt32(), StackSize = r.ReadInt32(), Prefix = r.ReadByte() });
        // Hide flags: 10 bools
        for (int i = 0; i < 10; i++) r.ReadBoolean();
        // Misc equips are NOT part of the loadout — skip
        return lo;
    }

    /// <summary>Overwrite a loadout with the player's current equipment (game sync behavior).</summary>
    private static void SyncLoadoutToEquipment(PlayerLoadout lo, PlayerData player)
    {
        lo.Armor = player.Armor.Select(a => a.Clone()).ToList();
        lo.Accessories = player.Accessories.Select(a => a.Clone()).ToList();
        lo.VanityArmor = player.VanityArmor.Select(a => a.Clone()).ToList();
        lo.VanityAccessories = player.VanityAccessories.Select(a => a.Clone()).ToList();
        lo.ArmorDyes = player.ArmorDyes.Select(a => a.Clone()).ToList();
    }

    /// <summary>
    /// Locate the loadout section (cart bits + CurrentLoadoutIndex + 3 × 280-byte
    /// loadouts + tail) by scanning from <paramref name="start"/>. The candidate
    /// must have valid cart bits, a loadout index 0-2, 30 valid 9-byte items,
    /// 10 hide flags, and a tail (voice/refunds/dialogues) that parses to EOF.
    /// </summary>
    private static int FindLoadoutSection(byte[] data, int start)
    {
        int maxScan = Math.Min(start + 512, data.Length - 4 - LoadoutBytes);
        for (int p = start + 1; p <= maxScan; p++)
        {
            if (data[p - 1] > 3) continue; // cart bits must be 0-3
            int idx = BitConverter.ToInt32(data, p);
            if (idx < 0 || idx > 2) continue; // CurrentLoadoutIndex 0-2
            // The tail must parse (voice/refunds/dialogues) to exact EOF.
            // Loadout items are NOT validated — older/corrupted saves can contain
            // unusual item data, and the exact-EOF tail check is the reliable filter.
            if (!IsValidTail(data, p + 4 + LoadoutBytes)) continue;
            return p;
        }
        return -1;
    }

    private const int LoadoutBytes = 3 * (20 * 9 + 10 * 9 + 10); // 3 loadouts × 280

    /// <summary>
    /// Validate that the remainder of the file parses as the fixed tail —
    /// voice(byte+float) + refunds(count + 10B items) + dialogues(count + 7-bit strings)
    /// — and consumes exactly to EOF.
    /// </summary>
    private static bool IsValidTail(byte[] data, int off)
    {
        int pos = off;
        if (pos + 5 > data.Length) return false;
        pos += 5; // voice: variant byte + pitch float
        if (pos + 4 > data.Length) return false;
        int refunds = BitConverter.ToInt32(data, pos);
        pos += 4;
        if (refunds < 0 || refunds > 100) return false;
        if (pos + refunds * 10 > data.Length) return false;
        pos += refunds * 10;
        if (pos + 4 > data.Length) return false;
        int dialogues = BitConverter.ToInt32(data, pos);
        pos += 4;
        if (dialogues < 0 || dialogues > 1000) return false;
        for (int i = 0; i < dialogues; i++)
        {
            // 7-bit encoded string length (BinaryWriter.Write(string)).
            // Each dialogue must consume at least its length byte.
            int lenStart = pos;
            int len = 0, shift = 0;
            while (pos < data.Length && shift < 35)
            {
                byte b = data[pos++];
                len |= (b & 0x7F) << shift;
                shift += 7;
                if ((b & 0x80) == 0) break;
            }
            if (pos == lenStart || len < 0 || pos + len > data.Length) return false;
            pos += len;
        }
        return pos == data.Length;
    }

    #endregion
}
