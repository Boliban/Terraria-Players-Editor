using Terraria_Players_Editor.Models;
using System.Text;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Serializes PlayerData to .plr binary format using sequential BinaryWriter,
/// matching Terraria's actual Player.Serialize method for v319.
/// </summary>
public static class PlrFileWriter
{
    private const int ItemSlots = 58;
    private const int ArmorSlots = 20;
    private const int DyeSlots = 10;
    private const int MiscEquipCount = 5;
    private const int StorageSlots = 40;
    private const int BuffCount = 44;

    public static byte[] Write(PlayerData player)
    {
        DebugLog.Log($"Writer: Health={player.Stats.Health}/{player.Stats.MaxHealth}, Mana={player.Stats.Mana}/{player.Stats.MaxMana}, HairStyle={player.Appearance.HairStyle}, Name='{player.Name}'");
        DebugLog.Log($"Writer: Inventory={player.MainInventory.Count(x=>x.ItemId>0)} non-empty, BuilderAccStatus[0]={player.BuilderAccStatus[0]}");
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        // === Original PLR header: version + "relogic" magic + fileType + revision + favorites ===
        w.Write(player.FileVersion);
        w.Write(Encoding.UTF8.GetBytes("relogic")); // 7 raw bytes, no length prefix
        w.Write((byte)0x03); // fileType
        w.Write(player.Revision);
        w.Write(0L); // favorites (int64)

        // === Core identity ===
        w.Write(player.Name);
        w.Write(player.Difficulty);
        w.Write(player.PlayTime);
        w.Write((int)player.Appearance.HairStyle);
        w.Write(player.Appearance.HairDye);
        w.Write((byte)0); // team

        // === Hide visual accessory: 10 bits in 2 bytes ===
        byte hideVa1 = 0;
        for (int i = 0; i < 8 && i < player.Appearance.HideVisual.Length; i++)
            if (player.Appearance.HideVisual[i]) hideVa1 |= (byte)(1 << i);
        w.Write(hideVa1);
        byte hideVa2 = 0;
        for (int i = 0; i < 2 && i + 8 < player.Appearance.HideVisual.Length; i++)
            if (player.Appearance.HideVisual[i + 8]) hideVa2 |= (byte)(1 << i);
        w.Write(hideVa2);

        // hideMisc: pack 5 bools into 1 byte
        byte hideMsc = 0;
        for (int i = 0; i < 5 && i < player.Appearance.HideMisc.Length; i++)
            if (player.Appearance.HideMisc[i]) hideMsc |= (byte)(1 << i);
        w.Write(hideMsc);
        w.Write(player.Appearance.SkinVariant);

        // === Stats ===
        w.Write(player.Stats.Health);
        w.Write(player.Stats.MaxHealth);
        w.Write(player.Stats.Mana);
        w.Write(player.Stats.MaxMana);

        // === Upgrades ===
        w.Write(player.Upgrades.ExtraAccessory);
        w.Write(player.Upgrades.UnlockedBiomeTorches);
        w.Write(player.Upgrades.UsingBiomeTorches);
        w.Write(player.Upgrades.AteArtisanBread);
        w.Write(player.Upgrades.UsedAegisCrystal);
        w.Write(player.Upgrades.UsedAegisFruit);
        w.Write(player.Upgrades.UsedArcaneCrystal);
        w.Write(player.Upgrades.UsedGalaxyPearl);
        w.Write(player.Upgrades.UsedGummyWorm);
        w.Write(player.Upgrades.UsedAmbrosia);
        w.Write(false); // downedDD2

        // === Counters ===
        w.Write(player.TaxMoney);
        w.Write(player.NumberOfDeathsPvE);
        w.Write(player.NumberOfDeathsPvP);

        // === Colors (7 × RGB) ===
        WriteColor(w, player.Appearance.HairColor);
        WriteColor(w, player.Appearance.SkinColor);
        WriteColor(w, player.Appearance.EyeColor);
        WriteColor(w, player.Appearance.ShirtColor);
        WriteColor(w, player.Appearance.UnderShirtColor);
        WriteColor(w, player.Appearance.PantsColor);
        WriteColor(w, player.Appearance.ShoeColor);

        // === Armor: 20 slots (armor 3 + vanity 3 + accessories 7 + vanity acc 7) ===
        var flatArmor = new List<ItemData>(ArmorSlots);
        flatArmor.AddRange(SafeTake(player.Armor, 3));
        flatArmor.AddRange(SafeTake(player.VanityArmor, 3));
        flatArmor.AddRange(SafeTake(player.Accessories, 7));
        flatArmor.AddRange(SafeTake(player.VanityAccessories, 7));
        for (int i = 0; i < ArmorSlots; i++)
        {
            var a = i < flatArmor.Count ? flatArmor[i] : new ItemData();
            w.Write(a.ItemId);
            w.Write(a.Prefix);
        }

        // === Dyes: 10 slots ===
        for (int i = 0; i < DyeSlots; i++)
        {
            var d = i < player.ArmorDyes.Count ? player.ArmorDyes[i] : new ItemData();
            w.Write(d.ItemId);
            w.Write(d.Prefix);
        }

        // === Inventory: 58 slots (main 50 + coins 4 + ammo 4) ===
        var flatInv = new List<ItemData>(ItemSlots);
        flatInv.AddRange(SafeTake(player.MainInventory, 50));
        flatInv.AddRange(SafeTake(player.Coins, 4));
        flatInv.AddRange(SafeTake(player.Ammo, 4));
        // Log first 10 inventory slots for debugging
        var invHex = new System.Text.StringBuilder("Inventory slots: ");
        int invOffset = (int)ms.Position;
        for (int i = 0; i < ItemSlots; i++)
        {
            var inv = i < flatInv.Count ? flatInv[i] : new ItemData();
            if (i < 10 && inv.ItemId > 0)
                invHex.Append($"[{i}: ID={inv.ItemId} stack={inv.StackSize} pref={inv.Prefix}] ");
            w.Write(inv.ItemId);
            w.Write(inv.StackSize);
            w.Write(inv.Prefix);
            w.Write(inv.Favorited);
        }
        DebugLog.Log($"Inventory starts at offset {invOffset}, first non-empty: {invHex}");

        // === Misc equips + dyes: 5 each ===
        for (int i = 0; i < MiscEquipCount; i++)
        {
            var me = i < player.MiscEquips.Count ? player.MiscEquips[i] : new ItemData();
            w.Write(me.ItemId);
            w.Write(me.Prefix);
            var md = i < player.MiscEquipDyes.Count ? player.MiscEquipDyes[i] : new ItemData();
            w.Write(md.ItemId);
            w.Write(md.Prefix);
        }

        // === Storage: PiggyBank (40) + Safe (40) + DefenderForge (40) ===
        WriteStorage(w, player.PiggyBank, StorageSlots, false);
        WriteStorage(w, player.Safe, StorageSlots, false);
        WriteStorage(w, player.DefenderForge, StorageSlots, false);

        // === Storage: VoidVault (40, with favorited) ===
        WriteStorage(w, player.VoidVault, StorageSlots, true);

        // === Void vault info flag ===
        w.Write((byte)0); // voidVaultInfo

        // === Buffs: 44 pairs (type + time) ===
        for (int i = 0; i < BuffCount; i++)
        {
            w.Write(i < player.BuffTypes.Length ? player.BuffTypes[i] : 0);
            w.Write(i < player.BuffTimes.Length ? player.BuffTimes[i] : 0);
        }

        // === Spawn points ===
        for (int i = 0; i < 200; i++)
        {
            if (i >= player.SpawnPoints.Count)
            {
                w.Write(-1);
                break;
            }
            w.Write(player.SpawnPoints[i].X);
            w.Write(player.SpawnPoints[i].Y);
            w.Write(player.SpawnPoints[i].WorldId);
            w.Write(Encoding.UTF8.GetByteCount(player.SpawnPoints[i].WorldName));
            w.Write(Encoding.UTF8.GetBytes(player.SpawnPoints[i].WorldName));
        }

        // === Flags ===
        w.Write(player.HotbarLocked);
        for (int i = 0; i < 13; i++)
            w.Write(i < player.HideInfo.Length ? player.HideInfo[i] : false);
        w.Write(player.AnglerQuestsFinished);

        // === DPad bindings ===
        for (int i = 0; i < 4; i++)
            w.Write(0); // DPad bindings
        for (int i = 0; i < 12; i++)
            w.Write(i < player.BuilderAccStatus.Length ? player.BuilderAccStatus[i] : 0);

        w.Write(0); // bartenderQuestLog

        // === Death state ===
        w.Write(false); // dead

        // === Timestamp ===
        w.Write(DateTime.UtcNow.ToBinary());

        // === Golfer score ===
        w.Write(player.GolferScoreAccumulated);

        // === Creative tracker (research items) ===
        // Format: int32 count + for each: string(internalName) + int32(researchCount)
        WriteResearchItems(w, player);

        // === Temporary item slots ===
        // Format: byte count (0-1) + optionally: int32 type + int32 stack + byte prefix + bool favorited
        w.Write((byte)0);

        // === Creative powers ===
        // Format: sentinel-based — while(true) { bool hasMore; if(!hasMore) break; ushort id; data }
        // For non-journey characters: just write false (1 byte) — no powers persisted
        w.Write(false);

        // === Super cart bits ===
        byte cartBits = 0;
        if (player.Upgrades.UnlockedSuperCart != 0) cartBits |= 1;
        if (player.Upgrades.EnabledSuperCart) cartBits |= 2;
        w.Write(cartBits);

        // === Loadouts === (game expects 3: main + loadout2 + loadout3)
        w.Write(player.CurrentLoadout);
        WriteLoadout(w, BuildMainLoadout(player));    // loadout[0]: current equipment
        WriteLoadout(w, player.Loadout2 ?? new PlayerLoadout());  // loadout[1]
        WriteLoadout(w, player.Loadout3 ?? new PlayerLoadout());  // loadout[2]

        // === Voice ===
        w.Write((byte)0); // voiceVariant
        w.Write(0f); // voicePitchOffset

        // === Pending refunds (empty) ===
        w.Write(0);

        // === One-time dialogues (empty) ===
        w.Write(0);

        w.Flush();
        byte[] plain = ms.ToArray();
        DebugLog.Log($"Writer: plaintext size = {plain.Length} bytes");
        DebugLog.LogHex("Writer plaintext output", plain);
        byte[] encrypted = PlrCrypto.Encrypt(plain);
        DebugLog.Log($"Writer: encrypted size = {encrypted.Length} bytes");
        return encrypted;
    }

    #region Helpers

    private static void WriteColor(BinaryWriter w, byte[] c)
    {
        w.Write(c.Length > 0 ? c[0] : (byte)0);
        w.Write(c.Length > 1 ? c[1] : (byte)0);
        w.Write(c.Length > 2 ? c[2] : (byte)0);
    }

    private static void WriteResearchItems(BinaryWriter w, PlayerData player)
    {
        // Format: int32 count + foreach: string(internalName) + int32(researchCount)
        // BinaryWriter.Write(string) uses 7-bit-encoded length prefix, matching the game
        if (player.ResearchedItems.Count > 0)
        {
            w.Write(player.ResearchedItems.Count);
            foreach (var kv in player.ResearchedItems)
            {
                w.Write(kv.Key);
                w.Write(kv.Value);
            }
        }
        else
        {
            w.Write(0);
        }
    }

    private static void WriteStorage(BinaryWriter w, List<ItemData> items, int count, bool writeFavorited)
    {
        for (int i = 0; i < count; i++)
        {
            var it = i < items.Count ? items[i] : new ItemData();
            w.Write(it.ItemId);
            w.Write(it.StackSize);
            w.Write(it.Prefix);
            if (writeFavorited)
                w.Write(it.Favorited);
        }
    }

    /// <summary>Build a PlayerLoadout from the player's current equipment (used as loadout[0]).</summary>
    private static PlayerLoadout BuildMainLoadout(PlayerData player)
    {
        return new PlayerLoadout
        {
            Armor = SafeList(player.Armor, 3),
            VanityArmor = SafeList(player.VanityArmor, 3),
            Accessories = SafeList(player.Accessories, 7),
            VanityAccessories = SafeList(player.VanityAccessories, 7),
            ArmorDyes = SafeList(player.ArmorDyes, 10),
            MiscEquips = SafeList(player.MiscEquips, 5),
            MiscEquipDyes = SafeList(player.MiscEquipDyes, 5),
        };
    }

    private static List<ItemData> SafeList(List<ItemData> source, int count)
    {
        var result = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
            result.Add(i < source.Count ? source[i] : new ItemData());
        return result;
    }

    /// <summary>
    /// Write loadout data matching the game's EquipmentLoadout.Serialize format.
    /// Each item uses Item.Serialize (10 bytes: type+stack+prefix+fav), NOT 5 bytes.
    /// </summary>
    private static void WriteLoadout(BinaryWriter w, PlayerLoadout lo)
    {
        // Armor: 20 items, each using full Item.Serialize (int32 type + int32 stack + byte prefix + bool fav)
        var la = new List<ItemData>(20);
        la.AddRange(SafeTake(lo.Armor, 3));
        la.AddRange(SafeTake(lo.VanityArmor, 3));
        la.AddRange(SafeTake(lo.Accessories, 7));
        la.AddRange(SafeTake(lo.VanityAccessories, 7));
        for (int i = 0; i < 20; i++) WriteItemFull(w, i < la.Count ? la[i] : new ItemData());

        // Dyes: 10 items, full Item.Serialize
        for (int i = 0; i < 10; i++) WriteItemFull(w, i < lo.ArmorDyes.Count ? lo.ArmorDyes[i] : new ItemData());

        // Hide flags: 10 bools (hideVisual for loadout)
        for (int i = 0; i < 10; i++) w.Write(false);
    }

    private static void WriteItemFull(BinaryWriter w, ItemData item)
    {
        w.Write(item.ItemId);
        w.Write(item.StackSize);
        w.Write(item.Prefix);
        w.Write(item.Favorited);
    }

    private static List<ItemData> SafeTake(List<ItemData> source, int count)
    {
        var result = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
            result.Add(i < source.Count ? source[i] : new ItemData());
        return result;
    }

    #endregion
}
