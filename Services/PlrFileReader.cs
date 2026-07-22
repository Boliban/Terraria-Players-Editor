using Terraria_Players_Editor.Models;
using System.Text;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Reads and parses Terraria .plr player files (1.4.4+ / v319).
///
/// Format reference (from WinTerrEdit analysis and hex verification):
/// - "relogic": fixed 7 bytes with NO length prefix at offset 4
/// - Strings: 1-byte length prefix + UTF-8 bytes
/// - Item IDs / quantities / stats: 2-byte uint16 LE (b1 + 256*b2)
/// - Item slots (v319): [Fav(1)|ID(2)|pad(2)|Qty(2)|pad(2)|Pref(1)] = 10 bytes
/// - Equip slots: [ID(2)|pad(2)|Pref(1)] = 5 bytes
/// - Health/Mana: 4 bytes each [low, high, 0, 0], uint16 encoded
/// </summary>
public static class PlrFileReader
{
    public static PlayerData Read(byte[] data)
    {
        var player = new PlayerData();
        int o = 0;

        try
        {
            // === Header (0-23) ===
            { int v; o = ReadInt32(data, o, out v); player.FileVersion = v; }
            // "relogic" — fixed 7 bytes, no prefix
            string magic = ReadFixedString(data, o, 7); o += 7;
            o++; // fileType (0x03)
            { int v; o = ReadInt32(data, o, out v); player.Revision = v; }
            o += 8; // favorite (int64)

            // === Identity ===
            { string v; o = ReadString(data, o, out v); player.Name = v; }
            int nameEndOffset = o; // position right after name bytes
            { byte v; o = ReadByte(data, o, out v); player.Difficulty = v; }
            { long v; o = ReadInt64(data, o, out v); player.PlayTime = v; }

            // === Stats at nameEndOffset+19 (health) and +27 (mana) ===
            // 4 bytes each, uint16 encoded in first 2 bytes, last 2 are padding
            o = nameEndOffset + 19;
            player.Stats.Health = ReadUInt16(data, o); o += 4;
            player.Stats.MaxHealth = ReadUInt16(data, o); o += 4;

            o = nameEndOffset + 27;
            player.Stats.Mana = ReadUInt16(data, o); o += 4;
            player.Stats.MaxMana = ReadUInt16(data, o); o += 4;

            // === Appearance fields (between playTime and stats) ===
            o = nameEndOffset + 9;
            { byte v; o = ReadByte(data, o, out v); player.Appearance.HairStyle = v; }
            { byte v; o = ReadByte(data, o, out v); player.Appearance.HairDye = v; }

            // hideVisual: 10 bits packed in 2 bytes
            short hideVisBits; o = ReadInt16(data, o, out hideVisBits);
            for (int i = 0; i < 10; i++)
                player.Appearance.HideVisual[i] = ((hideVisBits >> i) & 1) != 0;

            // hideMisc: 5 bits packed in 1 byte
            byte hideMscBits; o = ReadByte(data, o, out hideMscBits);
            for (int i = 0; i < 5; i++)
                player.Appearance.HideMisc[i] = ((hideMscBits >> i) & 1) != 0;

            { byte v; o = ReadByte(data, o, out v); player.Appearance.SkinVariant = v; }

            // selectedItem: 2 bytes
            { short v; o = ReadInt16(data, o, out v); player.SelectedItem = v; }
            o += 2; // 2 bytes padding to reach nameEndOffset+19

            // === Colors at nameEndOffset+57 (21 bytes: 7 × 3 RGB) ===
            o = nameEndOffset + 57;
            player.Appearance.HairColor = ReadBytes(data, o, 3); o += 3;
            player.Appearance.SkinColor = ReadBytes(data, o, 3); o += 3;
            player.Appearance.EyeColor = ReadBytes(data, o, 3); o += 3;
            player.Appearance.ShirtColor = ReadBytes(data, o, 3); o += 3;
            player.Appearance.UnderShirtColor = ReadBytes(data, o, 3); o += 3;
            player.Appearance.PantsColor = ReadBytes(data, o, 3); o += 3;
            player.Appearance.ShoeColor = ReadBytes(data, o, 3); o += 3;

            // === Permanent Upgrades (between mana and colors, at nameEndOffset+35) ===
            o = nameEndOffset + 35;
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.ExtraAccessory = v; }
            o++; // downedDD2
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UnlockedBiomeTorches = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UsingBiomeTorches = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.AteArtisanBread = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UsedAegisCrystal = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UsedAegisFruit = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UsedArcaneCrystal = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UsedGalaxyPearl = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UsedGummyWorm = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.UsedAmbrosia = v; }
            { byte v; o = ReadByte(data, o, out v); player.Upgrades.UnlockedSuperCart = v; }
            { bool v; o = ReadBool(data, o, out v); player.Upgrades.EnabledSuperCart = v; }

            // After colors: skip 2 bytes then read taxMoney at nameEndOffset+80
            o = nameEndOffset + 80;
            { int v; o = ReadInt32(data, o, out v); player.TaxMoney = v; }

            // === Equipment at nameEndOffset+84: 5-byte slots [ID(2)|pad(2)|Pref(1)] ===
            o = nameEndOffset + 84;
            player.Armor = ReadEquipItems5(data, ref o, 3);
            player.VanityArmor = ReadEquipItems5(data, ref o, 3);
            player.Accessories = ReadEquipItems5(data, ref o, 7);
            player.VanityAccessories = ReadEquipItems5(data, ref o, 7);
            player.ArmorDyes = ReadEquipItems5(data, ref o, 10);
            player.MiscEquips = ReadEquipItems5(data, ref o, 5);
            player.MiscEquipDyes = ReadEquipItems5(data, ref o, 5);

            // Trash item (10-byte format)
            player.TrashItem = ReadInvItem(data, ref o);

            // === Jump to Inventory at nameEndOffset+228 ===
            o = nameEndOffset + 228;
            player.MainInventory = ReadInvItems10(data, ref o, 50);
            player.Coins = ReadInvItems10(data, ref o, 4);
            player.Ammo = ReadInvItems10(data, ref o, 4);

            // === Flags / Toggles / Counters ===
            { bool v; o = ReadBool(data, o, out v); player.HotbarLocked = v; }
            for (int i = 0; i < 13; i++)
            { o = ReadBool(data, o, out bool v); player.HideInfo[i] = v; }
            { int v; o = ReadInt32(data, o, out v); player.AnglerQuestsFinished = v; }
            o += 4; // savedBartender
            { int v; o = ReadInt32(data, o, out v); player.GolferScoreAccumulated = v; }

            for (int i = 0; i < 12; i++)
            { o = ReadBool(data, o, out bool v); player.BuilderToggles[i] = v; }

            for (int i = 0; i < 4; i++)
            { o = ReadInt32(data, o, out int v); player.DPadRadialBindings[i] = v; }
            for (int i = 0; i < 4; i++)
            { o = ReadInt32(data, o, out int v); player.BuilderAccStatus[i] = v; }
            o += 4; // bartenderQuestLog

            { int v; o = ReadInt32(data, o, out v); player.NumberOfDeathsPvE = v; }
            { int v; o = ReadInt32(data, o, out v); player.NumberOfDeathsPvP = v; }

            { int v; o = ReadInt32(data, o, out v); player.PotionDelay = v; }
            { int v; o = ReadInt32(data, o, out v); player.ManaPotionDelay = v; }
            { int v; o = ReadInt32(data, o, out v); player.RestorationPotionCd = v; }

            // Emotes
            if (player.FileVersion >= 220)
            {
                int emoteCount; o = ReadInt32(data, o, out emoteCount);
                for (int i = 0; i < Math.Min(emoteCount, 200); i++)
                { o = ReadInt32(data, o, out int e); player.UnlockedEmotes.Add(e); }
            }

            // Loadouts
            if (player.FileVersion >= 269)
            {
                { int v; o = ReadInt32(data, o, out v); player.CurrentLoadout = v; }
                player.Loadout2 = ReadLoadout(data, ref o);
                player.Loadout3 = ReadLoadout(data, ref o);
            }

            // Journey Research
            if (player.FileVersion >= 230)
            {
                int researchCount; o = ReadInt32(data, o, out researchCount);
                for (int i = 0; i < researchCount; i++)
                {
                    string itemName; o = ReadString(data, o, out itemName);
                    int cnt; o = ReadInt32(data, o, out cnt);
                    player.ResearchedItems[itemName] = cnt;
                }
            }

            // === Storage at fixed offsets ===
            o = nameEndOffset + 1608;
            player.PiggyBank = ReadStorageItems10(data, ref o, 40);
            o = nameEndOffset + 2008;
            player.Safe = ReadStorageItems10(data, ref o, 40);
            o = nameEndOffset + 2408;
            player.DefenderForge = ReadStorageItems10(data, ref o, 40);
            if (player.FileVersion >= 269)
            {
                o = nameEndOffset + 2808;
                player.VoidVault = ReadStorageItems10(data, ref o, 40);
            }

            // === Buffs ===
            o = nameEndOffset + 3208;
            int buffCount = player.FileVersion >= 269 ? 44 : 22;
            for (int i = 0; i < buffCount; i++)
            {
                int buffId = ReadUInt16(data, o); o += 4;
                if (i < player.BuffTypes.Length) player.BuffTypes[i] = buffId;
            }
            for (int i = 0; i < buffCount; i++)
            {
                int dur = data[o] + (data[o + 1] << 8) + (data[o + 2] << 16);
                o += 4;
                if (i < player.BuffTimes.Length) player.BuffTimes[i] = dur;
            }

            // === Spawn Points ===
            if (o < data.Length)
            {
                try
                {
                    int spawnCount; o = ReadInt32(data, o, out spawnCount);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        int sx; o = ReadInt32(data, o, out sx);
                        int sy; o = ReadInt32(data, o, out sy);
                        int swid; o = ReadInt32(data, o, out swid);
                        string swn; o = ReadString(data, o, out swn);
                        player.SpawnPoints.Add(new SpawnPointData { X = sx, Y = sy, WorldId = swid, WorldName = swn });
                    }
                }
                catch { /* Spawn points format may vary */ }
            }
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentOutOfRangeException or ArgumentException)
        {
            // File truncated or malformed — return what we've read so far
        }

        player.RawData = data;
        return player;
    }

    #region Low-level helpers

    private static int ReadInt32(byte[] data, int o, out int value)
    {
        if (o + 4 > data.Length) throw new IndexOutOfRangeException();
        value = BitConverter.ToInt32(data, o);
        return o + 4;
    }

    private static int ReadInt64(byte[] data, int o, out long value)
    {
        if (o + 8 > data.Length) throw new IndexOutOfRangeException();
        value = BitConverter.ToInt64(data, o);
        return o + 8;
    }

    private static int ReadByte(byte[] data, int o, out byte value)
    {
        if (o >= data.Length) throw new IndexOutOfRangeException();
        value = data[o];
        return o + 1;
    }

    private static int ReadBool(byte[] data, int o, out bool value)
    {
        if (o >= data.Length) throw new IndexOutOfRangeException();
        value = data[o] != 0;
        return o + 1;
    }

    private static int ReadInt16(byte[] data, int o, out short value)
    {
        if (o + 2 > data.Length) throw new IndexOutOfRangeException();
        value = BitConverter.ToInt16(data, o);
        return o + 2;
    }

    private static int ReadUInt16(byte[] data, int o)
    {
        if (o + 2 > data.Length) throw new IndexOutOfRangeException();
        return data[o] + (256 * data[o + 1]);
    }

    private static int ReadString(byte[] data, int o, out string value)
    {
        if (o >= data.Length) throw new IndexOutOfRangeException();
        int length = data[o]; o++;
        if (o + length > data.Length) throw new IndexOutOfRangeException();
        value = Encoding.UTF8.GetString(data, o, length);
        return o + length;
    }

    private static string ReadFixedString(byte[] data, int o, int length)
    {
        if (o + length > data.Length) throw new IndexOutOfRangeException();
        return Encoding.UTF8.GetString(data, o, length);
    }

    private static byte[] ReadBytes(byte[] data, int o, int count)
    {
        if (o + count > data.Length) throw new IndexOutOfRangeException();
        var result = new byte[count];
        Array.Copy(data, o, result, 0, count);
        return result;
    }

    #endregion

    #region Item reading

    private static List<ItemData> ReadEquipItems5(byte[] data, ref int o, int count)
    {
        var items = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
        {
            int id = ReadUInt16(data, o); o += 2;
            o += 2; // padding
            byte prefix = data[o]; o += 1;
            items.Add(new ItemData { ItemId = id, Prefix = prefix, StackSize = 1 });
        }
        return items;
    }

    private static ItemData ReadInvItem(byte[] data, ref int o)
    {
        bool fav = data[o] != 0; o++;
        int id = ReadUInt16(data, o); o += 2;
        o += 2; // padding
        int stack = ReadUInt16(data, o); o += 2;
        o += 2; // padding
        byte prefix = data[o]; o++;
        return new ItemData { ItemId = id, StackSize = stack, Prefix = prefix, Favorited = fav };
    }

    private static List<ItemData> ReadInvItems10(byte[] data, ref int o, int count)
    {
        var items = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
            items.Add(ReadInvItem(data, ref o));
        return items;
    }

    private static List<ItemData> ReadStorageItems10(byte[] data, ref int o, int count)
    {
        var items = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
        {
            int id = ReadUInt16(data, o); o += 2;
            o += 2; // padding
            int stack = ReadUInt16(data, o); o += 2;
            o += 2; // padding
            byte prefix = data[o]; o++;
            items.Add(new ItemData { ItemId = id, StackSize = stack, Prefix = prefix });
        }
        return items;
    }

    private static PlayerLoadout ReadLoadout(byte[] data, ref int o)
    {
        var lo = new PlayerLoadout();
        lo.Armor = ReadEquipItems5(data, ref o, 3);
        lo.VanityArmor = ReadEquipItems5(data, ref o, 3);
        lo.Accessories = ReadEquipItems5(data, ref o, 7);
        lo.VanityAccessories = ReadEquipItems5(data, ref o, 7);
        lo.ArmorDyes = ReadEquipItems5(data, ref o, 10);
        lo.MiscEquips = ReadEquipItems5(data, ref o, 5);
        lo.MiscEquipDyes = ReadEquipItems5(data, ref o, 5);
        return lo;
    }

    #endregion
}
