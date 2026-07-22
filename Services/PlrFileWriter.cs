using Terraria_Players_Editor.Models;
using System.Text;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Serializes PlayerData to .plr binary format matching 1.4.4+ / v319 layout.
/// </summary>
public static class PlrFileWriter
{
    public static byte[] Write(PlayerData player)
    {
        using var ms = new MemoryStream();

        // === Header ===
        WriteInt32(ms, player.FileVersion);
        ms.Write(Encoding.UTF8.GetBytes("relogic"));
        ms.WriteByte(0x03); // fileType
        WriteInt32(ms, player.Revision);
        WriteInt64(ms, 0);  // favorite

        // === Identity ===
        WriteString(ms, player.Name);
        int nameEndOffset = (int)ms.Position;
        ms.WriteByte(player.Difficulty);
        WriteInt64(ms, player.PlayTime);

        // === Appearance before stats ===
        // hairStyle, hairDye at +9, +10
        ms.WriteByte((byte)player.Appearance.HairStyle);
        ms.WriteByte(player.Appearance.HairDye);

        // hideVisual: 10 bits in 2 bytes
        short hideVis = 0;
        for (int i = 0; i < 10; i++)
            if (i < player.Appearance.HideVisual.Length && player.Appearance.HideVisual[i])
                hideVis |= (short)(1 << i);
        WriteInt16(ms, hideVis);

        // hideMisc: 5 bits in 1 byte
        byte hideMsc = 0;
        for (int i = 0; i < 5; i++)
            if (i < player.Appearance.HideMisc.Length && player.Appearance.HideMisc[i])
                hideMsc |= (byte)(1 << i);
        ms.WriteByte(hideMsc);

        ms.WriteByte(player.Appearance.SkinVariant);

        // selectedItem: 2 bytes
        WriteInt16(ms, (short)player.SelectedItem);

        // 2 bytes padding to align stats at nameEndOffset+19
        WriteInt16(ms, 0);

        // === Stats at nameEndOffset+19 (4 bytes each, uint16 encoded) ===
        WriteEncodedInt32Pad(ms, player.Stats.Health);
        WriteEncodedInt32Pad(ms, player.Stats.MaxHealth);

        // === Mana at nameEndOffset+27 (4 bytes each) ===
        WriteEncodedInt32Pad(ms, player.Stats.Mana);
        WriteEncodedInt32Pad(ms, player.Stats.MaxMana);

        // === Permanent Upgrades at nameEndOffset+35 ===
        ms.WriteByte(player.Upgrades.ExtraAccessory ? (byte)1 : (byte)0);
        ms.WriteByte(0); // downedDD2
        ms.WriteByte(player.Upgrades.UnlockedBiomeTorches ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UsingBiomeTorches ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.AteArtisanBread ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UsedAegisCrystal ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UsedAegisFruit ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UsedArcaneCrystal ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UsedGalaxyPearl ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UsedGummyWorm ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UsedAmbrosia ? (byte)1 : (byte)0);
        ms.WriteByte(player.Upgrades.UnlockedSuperCart);
        ms.WriteByte(player.Upgrades.EnabledSuperCart ? (byte)1 : (byte)0);

        // Pad to colors at nameEndOffset+57
        PadTo(ms, nameEndOffset + 57);

        // === Colors at nameEndOffset+57 (21 bytes) ===
        WriteColor(ms, player.Appearance.HairColor);
        WriteColor(ms, player.Appearance.SkinColor);
        WriteColor(ms, player.Appearance.EyeColor);
        WriteColor(ms, player.Appearance.ShirtColor);
        WriteColor(ms, player.Appearance.UnderShirtColor);
        WriteColor(ms, player.Appearance.PantsColor);
        WriteColor(ms, player.Appearance.ShoeColor);

        // Pad to taxMoney at nameEndOffset+80
        PadTo(ms, nameEndOffset + 80);

        // Tax money (int32)
        WriteInt32(ms, player.TaxMoney);

        // === Equipment at nameEndOffset+84 (5-byte slots) ===
        WriteEquipItems5(ms, player.Armor, 3);
        WriteEquipItems5(ms, player.VanityArmor, 3);
        WriteEquipItems5(ms, player.Accessories, 7);
        WriteEquipItems5(ms, player.VanityAccessories, 7);
        WriteEquipItems5(ms, player.ArmorDyes, 10);
        WriteEquipItems5(ms, player.MiscEquips, 5);
        WriteEquipItems5(ms, player.MiscEquipDyes, 5);

        // Trash item (10-byte format)
        WriteInvItem(ms, player.TrashItem ?? new ItemData());

        // Pad to inventory at nameEndOffset+228
        PadTo(ms, nameEndOffset + 228);

        // === Inventory (50 × 10 bytes) ===
        WriteInvItems10(ms, player.MainInventory, 50);
        WriteInvItems10(ms, player.Coins, 4);
        WriteInvItems10(ms, player.Ammo, 4);

        // === Flags / Toggles ===
        ms.WriteByte(player.HotbarLocked ? (byte)1 : (byte)0);
        for (int i = 0; i < 13; i++)
            ms.WriteByte(i < player.HideInfo.Length && player.HideInfo[i] ? (byte)1 : (byte)0);
        WriteInt32(ms, player.AnglerQuestsFinished);
        WriteInt32(ms, 0); // savedBartender
        WriteInt32(ms, player.GolferScoreAccumulated);

        for (int i = 0; i < 12; i++)
            ms.WriteByte(i < player.BuilderToggles.Length && player.BuilderToggles[i] ? (byte)1 : (byte)0);

        for (int i = 0; i < 4; i++)
            WriteInt32(ms, i < player.DPadRadialBindings.Length ? player.DPadRadialBindings[i] : 0);
        for (int i = 0; i < 4; i++)
            WriteInt32(ms, i < player.BuilderAccStatus.Length ? player.BuilderAccStatus[i] : 0);
        WriteInt32(ms, 0); // bartenderQuestLog

        WriteInt32(ms, player.NumberOfDeathsPvE);
        WriteInt32(ms, player.NumberOfDeathsPvP);
        WriteInt32(ms, player.PotionDelay);
        WriteInt32(ms, player.ManaPotionDelay);
        WriteInt32(ms, player.RestorationPotionCd);

        // Emotes
        if (player.FileVersion >= 220)
        {
            WriteInt32(ms, player.UnlockedEmotes.Count);
            foreach (var e in player.UnlockedEmotes)
                WriteInt32(ms, e);
        }

        // Loadouts
        if (player.FileVersion >= 269)
        {
            WriteInt32(ms, player.CurrentLoadout);
            WriteLoadout(ms, player.Loadout2 ?? new PlayerLoadout());
            WriteLoadout(ms, player.Loadout3 ?? new PlayerLoadout());
        }

        // Journey Research
        if (player.FileVersion >= 230)
        {
            WriteInt32(ms, player.ResearchedItems.Count);
            foreach (var kv in player.ResearchedItems)
            {
                WriteString(ms, kv.Key);
                WriteInt32(ms, kv.Value);
            }
        }

        // Pad to piggy bank at nameEndOffset+1608
        PadTo(ms, nameEndOffset + 1608);

        // === Storage (40 × 10 bytes each) ===
        WriteStorageItems10(ms, player.PiggyBank, 40);
        WriteStorageItems10(ms, player.Safe, 40);
        WriteStorageItems10(ms, player.DefenderForge, 40);
        if (player.FileVersion >= 269)
            WriteStorageItems10(ms, player.VoidVault, 40);

        // === Buffs ===
        int buffCount = player.FileVersion >= 269 ? 44 : 22;
        for (int i = 0; i < buffCount; i++)
            WriteEncodedInt32Pad(ms, i < player.BuffTypes.Length ? player.BuffTypes[i] : 0);
        for (int i = 0; i < buffCount; i++)
        {
            int dur = i < player.BuffTimes.Length ? player.BuffTimes[i] : 0;
            byte[] tmp = BitConverter.GetBytes(dur);
            ms.WriteByte(tmp[0]);
            ms.WriteByte(tmp[1]);
            ms.WriteByte(tmp[2]);
            ms.WriteByte(0);
        }

        // Spawn Points
        WriteInt32(ms, player.SpawnPoints.Count);
        foreach (var sp in player.SpawnPoints)
        {
            WriteInt32(ms, sp.X);
            WriteInt32(ms, sp.Y);
            WriteInt32(ms, sp.WorldId);
            WriteString(ms, sp.WorldName);
        }

        return PlrCrypto.Encrypt(ms.ToArray());
    }

    #region Writer helpers

    private static void WriteInt32(MemoryStream ms, int v) => ms.Write(BitConverter.GetBytes(v));
    private static void WriteInt16(MemoryStream ms, short v) => ms.Write(BitConverter.GetBytes(v));
    private static void WriteInt64(MemoryStream ms, long v) => ms.Write(BitConverter.GetBytes(v));

    private static void WriteString(MemoryStream ms, string v)
    {
        var b = Encoding.UTF8.GetBytes(v);
        ms.WriteByte((byte)b.Length);
        ms.Write(b);
    }

    private static void WriteColor(MemoryStream ms, byte[] c)
    {
        for (int i = 0; i < 3; i++)
            ms.WriteByte(i < c.Length ? c[i] : (byte)0);
    }

    private static void WriteEncodedInt32Pad(MemoryStream ms, int v)
    {
        byte[] tmp = BitConverter.GetBytes(v);
        ms.WriteByte(tmp[0]);
        ms.WriteByte(tmp[1]);
        ms.WriteByte(0);
        ms.WriteByte(0);
    }

    private static void WriteEquipItems5(MemoryStream ms, List<ItemData> items, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i < items.Count)
            {
                byte[] idb = BitConverter.GetBytes(items[i].ItemId);
                ms.WriteByte(idb[0]); ms.WriteByte(idb[1]);
                WriteInt16(ms, 0); // padding
                ms.WriteByte(items[i].Prefix);
            }
            else
            {
                ms.WriteByte(0); ms.WriteByte(0);
                WriteInt16(ms, 0);
                ms.WriteByte(0);
            }
        }
    }

    private static void WriteInvItem(MemoryStream ms, ItemData item)
    {
        ms.WriteByte(item.Favorited ? (byte)1 : (byte)0);
        byte[] idb = BitConverter.GetBytes(item.ItemId);
        ms.WriteByte(idb[0]); ms.WriteByte(idb[1]);
        WriteInt16(ms, 0);
        byte[] stb = BitConverter.GetBytes(item.StackSize);
        ms.WriteByte(stb[0]); ms.WriteByte(stb[1]);
        WriteInt16(ms, 0);
        ms.WriteByte(item.Prefix);
    }

    private static void WriteInvItems10(MemoryStream ms, List<ItemData> items, int count)
    {
        for (int i = 0; i < count; i++)
            WriteInvItem(ms, i < items.Count ? items[i] : new ItemData());
    }

    private static void WriteStorageItems10(MemoryStream ms, List<ItemData> items, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i < items.Count)
            {
                byte[] idb = BitConverter.GetBytes(items[i].ItemId);
                ms.WriteByte(idb[0]); ms.WriteByte(idb[1]);
                WriteInt16(ms, 0);
                byte[] stb = BitConverter.GetBytes(items[i].StackSize);
                ms.WriteByte(stb[0]); ms.WriteByte(stb[1]);
                WriteInt16(ms, 0);
                ms.WriteByte(items[i].Prefix);
            }
            else
            {
                for (int j = 0; j < 9; j++) ms.WriteByte(0);
            }
        }
    }

    private static void WriteLoadout(MemoryStream ms, PlayerLoadout lo)
    {
        WriteEquipItems5(ms, lo.Armor, 3);
        WriteEquipItems5(ms, lo.VanityArmor, 3);
        WriteEquipItems5(ms, lo.Accessories, 7);
        WriteEquipItems5(ms, lo.VanityAccessories, 7);
        WriteEquipItems5(ms, lo.ArmorDyes, 10);
        WriteEquipItems5(ms, lo.MiscEquips, 5);
        WriteEquipItems5(ms, lo.MiscEquipDyes, 5);
    }

    private static void PadTo(MemoryStream ms, int target)
    {
        while (ms.Position < target)
            ms.WriteByte(0);
    }

    #endregion
}
