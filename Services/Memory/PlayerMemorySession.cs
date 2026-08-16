using Terraria_Players_Editor.Models;

namespace Terraria_Players_Editor.Services.Memory;

/// <summary>Snapshot of the player's basic fields read from game memory.</summary>
public sealed class MemoryPlayerInfo
{
    public string Name = "";
    public int StatLife;
    public int StatLifeMax;
    public int StatMana;
    public int StatManaMax;
    public int Difficulty;
    public int Hair;
    public int TaxMoney;
    public int DeathsPvE;
}

/// <summary>Which item collection a grid/slot belongs to in the memory editor.</summary>
public enum MemoryItemSection
{
    Inventory,   // Item[59]: 0-49 main, 50-53 coins, 54-57 ammo, 58 trash
    Armor,       // Item[3]
    Dye,         // Item[10]
    MiscEquips,  // Item[5]
    MiscDyes,    // Item[5]
    Bank,        // Chest.item Item[40]
    Bank2,
    Bank3,
    Bank4,
}

/// <summary>
/// Reads and writes a live Terraria Player object through the pointer chain
/// (threadstack0 - 0x3D8 -> 32C -> 4 -> 550 -> 0 -> 0 -> D8). The chain result
/// is the Player object base; all CSX offsets are applied relative to it.
/// </summary>
public sealed class PlayerMemorySession : IDisposable
{
    private readonly MemoryProcess _proc;
    private readonly PlayerMemoryOffsets _o;

    public PlayerMemorySession(MemoryProcess proc)
    {
        _proc = proc;
        _o = MemorySettings.Offsets;
    }

    public MemoryProcess Process => _proc;

    /// <summary>Resolved Player object base address (0 = failed).</summary>
    public uint PlayerBase { get; private set; }

    /// <summary>Anchor address the chain starts from (threadstack0 - subtract).</summary>
    public uint ChainAnchor { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>
    /// Resolve the Player base through the configured pointer chain.
    /// </summary>
    public bool ResolvePlayerBase()
    {
        LastError = null;
        uint stackBase = _proc.MainThreadStackBase;
        if (stackBase == 0)
        {
            LastError = "no thread stack base";
            return false;
        }
        ChainAnchor = stackBase - MemorySettings.ChainStackSubtract;
        if (!_proc.ResolveChain(ChainAnchor, MemorySettings.ChainOffsets, MemorySettings.ChainFinalDeref, out uint baseAddr) || baseAddr == 0)
        {
            LastError = "chain resolve failed";
            return false;
        }
        PlayerBase = baseAddr;
        return true;
    }

    /// <summary>
    /// Locate the live Player object (Main.player[0]) by scanning the target's
    /// memory. Preferred signature: the Main.player array — a 255-length object
    /// whose first element is the local Player. Fallback: the inventory array
    /// (Item[59]) referenced at Player+0xD8. Used when the pointer chain does
    /// not match the game build. The scan covers the lower 1 GB of the 32-bit
    /// address space; modified game builds can place the managed heap higher.
    /// </summary>
    public bool FindPlayerByScan(uint scanEnd = 0x40000000)
    {
        LastError = null;

        // Pass 1: Main.player array (length 255); element[0] is the local player.
        uint x = FindPlayerViaArrayLength(255, scanEnd);
        if (x != 0)
        {
            PlayerBase = x;
            return true;
        }

        // Pass 2 (fallback): inventory array (length 59) referenced at Player+0xD8.
        x = FindPlayerByArrayReferenceForInventory(scanEnd);
        if (x != 0)
        {
            PlayerBase = x;
            return true;
        }

        LastError = "no player object found in memory";
        return false;
    }

    /// <summary>Scan for an array of the given length whose element [0] is a Player object.</summary>
    private uint FindPlayerViaArrayLength(int length, uint scanEnd)
    {
        byte low = (byte)length;
        byte hi = (byte)(length >> 8);
        var buf = new byte[0x10000];
        foreach (var (baseAddr, size) in _proc.EnumerateReadableRegions(0x00400000, scanEnd))
        {
            for (long off = 0; off < size; off += buf.Length)
            {
                int chunk = (int)Math.Min(buf.Length, size - off);
                if (!_proc.ReadBytes(baseAddr + (uint)off, buf.AsSpan(0, chunk)))
                    continue;
                int limit = chunk - 4;
                int idx = 0;
                while (idx < limit)
                {
                    idx = Array.IndexOf(buf, low, idx, limit - idx);
                    if (idx < 0) break;
                    if (buf[idx + 1] == hi && buf[idx + 2] == 0 && buf[idx + 3] == 0)
                    {
                        uint pattern = baseAddr + (uint)off + (uint)idx;
                        if (pattern % 4 != 0) { idx += 4; continue; }
                        uint arr = pattern - 4;
                        uint p0 = _proc.ReadUInt32(arr + PlayerMemoryOffsets.ArrayData);
                        if (p0 >= 0x10000 && p0 <= 0x7FFEFFFF && LooksLikePlayerCore(p0))
                            return p0;
                    }
                    idx += 4;
                }
            }
        }
        return 0;
    }

    /// <summary>
    /// Fallback scan: find a 59-length array whose elements are Item objects
    /// (the inventory), then find the object X that references it at Player+0xD8.
    /// </summary>
    private uint FindPlayerByArrayReferenceForInventory(uint scanEnd)
    {
        var arrays = new List<uint>();
        var buf = new byte[0x10000];
        foreach (var (baseAddr, size) in _proc.EnumerateReadableRegions(0x00400000, scanEnd))
        {
            for (long off = 0; off < size; off += buf.Length)
            {
                int chunk = (int)Math.Min(buf.Length, size - off);
                if (!_proc.ReadBytes(baseAddr + (uint)off, buf.AsSpan(0, chunk)))
                    continue;
                int limit = chunk - 4;
                int idx = 0;
                while (idx < limit)
                {
                    idx = Array.IndexOf(buf, (byte)0x3B /* 59 LE low byte */, idx, limit - idx);
                    if (idx < 0) break;
                    if (buf[idx + 1] == 0 && buf[idx + 2] == 0 && buf[idx + 3] == 0)
                    {
                        // The pattern is the array LENGTH field, which lives at
                        // array base + 4 ([0]=MethodTable, [4]=length).
                        uint patternPos = baseAddr + (uint)off + (uint)idx;
                        uint arr = patternPos - 4;
                        if (arr % 4 == 0 && LooksLikeInventoryArrayFast(arr))
                            arrays.Add(arr);
                    }
                    idx += 4;
                }
            }
        }
        foreach (uint arr in arrays)
        {
            uint x = FindPlayerByArrayReference(arr, scanEnd);
            if (x != 0)
                return x;
        }
        return 0;
    }

    private bool LooksLikeInventoryArrayFast(uint arrAddr)
    {
        int len = _proc.ReadInt32(arrAddr + PlayerMemoryOffsets.ArrayLength);
        if (len != 59) return false;
        for (int i = 0; i < 3; i++)
        {
            uint item = _proc.ReadUInt32(arrAddr + PlayerMemoryOffsets.ArrayData + (uint)(i * 4));
            if (item == 0) continue;
            if (item < 0x10000 || item > 0x7FFEFFFF) return false;
            int type = _proc.ReadInt32(item + _o.ItemType);
            if (type < 0 || type > 6000) return false;
        }
        return true;
    }

    private uint FindPlayerByArrayReference(uint arr, uint scanEnd)
    {
        var buf = new byte[0x10000];
        byte b0 = (byte)arr, b1 = (byte)(arr >> 8), b2 = (byte)(arr >> 16), b3 = (byte)(arr >> 24);
        foreach (var (baseAddr, size) in _proc.EnumerateReadableRegions(0x00400000, scanEnd))
        {
            for (long off = 0; off < size; off += buf.Length)
            {
                int chunk = (int)Math.Min(buf.Length, size - off);
                if (!_proc.ReadBytes(baseAddr + (uint)off, buf.AsSpan(0, chunk)))
                    continue;
                int limit = chunk - 4;
                int idx = 0;
                while (idx < limit)
                {
                    idx = Array.IndexOf(buf, b0, idx, limit - idx);
                    if (idx < 0) break;
                    if (buf[idx + 1] == b1 && buf[idx + 2] == b2 && buf[idx + 3] == b3)
                    {
                        uint pos = baseAddr + (uint)off + (uint)idx;
                        if (pos >= 0xD8)
                        {
                            uint x = pos - 0xD8;
                            if (LooksLikePlayer(x, arr))
                                return x;
                        }
                    }
                    idx += 4;
                }
            }
        }
        return 0;
    }

    /// <summary>Full validation: the object at X is a Player that references the given inventory array.</summary>
    private bool LooksLikePlayer(uint x, uint expectedInvArray)
    {
        if (_proc.ReadUInt32(x + _o.Inventory) != expectedInvArray) return false;
        return LooksLikePlayerCore(x);
    }

    /// <summary>
    /// Core validation: the object at X has plausible Player fields and a
    /// 59-slot inventory array whose elements are Item objects.
    /// </summary>
    private bool LooksLikePlayerCore(uint x)
    {
        int lifeMax = _proc.ReadInt32(x + _o.StatLifeMax);
        int life = _proc.ReadInt32(x + _o.StatLife);
        int mana = _proc.ReadInt32(x + _o.StatMana);
        if (lifeMax < 20 || lifeMax > 1000 || life < 0 || life > 1000 || mana < 0 || mana > 1000)
            return false;
        // Difficulty is a 1-byte field (0=softcore .. 3=journey); read it as a byte.
        if (!_proc.ReadByte(x + _o.Difficulty, out byte difficulty) || difficulty > 3)
            return false;
        // Hair style id must be plausible.
        int hair = _proc.ReadInt32(x + _o.Hair);
        if (hair < 0 || hair > 300)
            return false;
        // The inventory array must have 59 slots whose elements are Item objects.
        uint invPtr = _proc.ReadUInt32(x + _o.Inventory);
        if (invPtr < 0x10000 || invPtr > 0x7FFEFFFF)
            return false;
        if (_proc.ReadInt32(invPtr + PlayerMemoryOffsets.ArrayLength) != 59)
            return false;
        for (int i = 0; i < 12; i++)
        {
            uint item = _proc.ReadUInt32(invPtr + PlayerMemoryOffsets.ArrayData + (uint)(i * 4));
            if (item == 0) continue;
            if (item < 0x10000 || item > 0x7FFEFFFF) return false;
            int type = _proc.ReadInt32(item + _o.ItemType);
            int stack = _proc.ReadInt32(item + _o.ItemStack);
            if (type < 0 || type > 6000 || stack < 0 || stack > 99999) return false;
        }
        uint namePtr = _proc.ReadUInt32(x + _o.Name);
        if (namePtr < 0x10000 || namePtr > 0x7FFEFFFF)
            return false;
        return _proc.ReadDotNetString(x + _o.Name) != null;
    }

    /// <summary>Read the player's basic info for the status bar / verification.</summary>
    public MemoryPlayerInfo? ReadPlayerInfo()
    {
        if (PlayerBase == 0) return null;
        try
        {
            _proc.ReadByte(PlayerBase + _o.Difficulty, out byte difficulty);
            var info = new MemoryPlayerInfo
            {
                Name = _proc.ReadDotNetString(PlayerBase + _o.Name) ?? "?",
                StatLife = _proc.ReadInt32(PlayerBase + _o.StatLife),
                StatLifeMax = _proc.ReadInt32(PlayerBase + _o.StatLifeMax),
                StatMana = _proc.ReadInt32(PlayerBase + _o.StatMana),
                StatManaMax = _proc.ReadInt32(PlayerBase + _o.StatManaMax),
                Difficulty = difficulty,
                Hair = _proc.ReadInt32(PlayerBase + _o.Hair),
                TaxMoney = _proc.ReadInt32(PlayerBase + _o.TaxMoney),
                DeathsPvE = _proc.ReadInt32(PlayerBase + _o.DeathsPvE)
            };
            return info;
        }
        catch
        {
            return null;
        }
    }

    #region Item arrays

    /// <summary>Resolve the address of a single Item object inside a collection.</summary>
    public bool ResolveItemAddress(MemoryItemSection section, int index, out uint itemAddr)
    {
        itemAddr = 0;
        if (PlayerBase == 0 || index < 0) return false;

        uint arrayPtrAddr = section switch
        {
            MemoryItemSection.Inventory => PlayerBase + _o.Inventory,
            MemoryItemSection.Armor => PlayerBase + _o.Armor,
            MemoryItemSection.Dye => PlayerBase + _o.Dye,
            MemoryItemSection.MiscEquips => PlayerBase + _o.MiscEquips,
            MemoryItemSection.MiscDyes => PlayerBase + _o.MiscDyes,
            MemoryItemSection.Bank => PlayerBase + _o.Bank,
            MemoryItemSection.Bank2 => PlayerBase + _o.Bank2,
            MemoryItemSection.Bank3 => PlayerBase + _o.Bank3,
            MemoryItemSection.Bank4 => PlayerBase + _o.Bank4,
            _ => 0
        };
        if (arrayPtrAddr == 0) return false;

        uint arrayObj = _proc.ReadUInt32(arrayPtrAddr);
        if (arrayObj == 0) return false;

        // Chest collections: the Player field holds a Chest object; its item array is at Chest+0x04.
        if (section is MemoryItemSection.Bank or MemoryItemSection.Bank2 or MemoryItemSection.Bank3 or MemoryItemSection.Bank4)
        {
            arrayObj = _proc.ReadUInt32(arrayObj + _o.ChestItemArray);
            if (arrayObj == 0) return false;
        }

        int length = _proc.ReadInt32(arrayObj + PlayerMemoryOffsets.ArrayLength);
        if (length <= 0 || index >= length) return false;

        itemAddr = _proc.ReadUInt32(arrayObj + PlayerMemoryOffsets.ArrayData + (uint)(index * 4));
        return itemAddr != 0;
    }

    /// <summary>Read one item slot's editable fields (type/stack/prefix/favorited).</summary>
    public ItemData? ReadItem(MemoryItemSection section, int index)
    {
        if (!ResolveItemAddress(section, index, out uint itemAddr))
            return null;
        return ReadItemAt(itemAddr);
    }

    public ItemData? ReadItemAt(uint itemAddr)
    {
        if (itemAddr == 0) return null;
        if (!_proc.ReadInt32(itemAddr + _o.ItemType, out int type)) return null;
        _proc.ReadInt32(itemAddr + _o.ItemStack, out int stack);
        _proc.ReadByte(itemAddr + _o.ItemPrefix, out byte prefix);
        _proc.ReadByte(itemAddr + _o.ItemFavorited, out byte fav);
        return new ItemData { ItemId = type, StackSize = stack, Prefix = prefix, Favorited = fav != 0 };
    }

    /// <summary>Read a whole collection into a list (missing/unreadable slots become empty).</summary>
    public List<ItemData> ReadItemSection(MemoryItemSection section, int count, int startIndex = 0)
    {
        var list = new List<ItemData>(count);
        for (int i = 0; i < count; i++)
        {
            var item = ReadItem(section, startIndex + i) ?? new ItemData();
            // The game stores "air" as type 0 / stack 0 — normalize like the file editor.
            if (item.ItemId <= 0 || item.StackSize < 0)
                item = new ItemData();
            list.Add(item);
        }
        return list;
    }

    /// <summary>Write type/stack/prefix/favorited into a live Item object.</summary>
    public bool WriteItem(MemoryItemSection section, int index, ItemData item)
    {
        if (!ResolveItemAddress(section, index, out uint itemAddr))
            return false;
        return WriteItemAt(itemAddr, item);
    }

    public bool WriteItemAt(uint itemAddr, ItemData item)
    {
        if (itemAddr == 0) return false;
        int type = Math.Clamp(item.ItemId, 0, 9999);
        int stack = Math.Clamp(item.StackSize, 0, 99999);
        byte prefix = item.Prefix;
        byte fav = (byte)(item.Favorited ? 1 : 0);
        bool ok = _proc.WriteInt32(itemAddr + _o.ItemType, type);
        ok &= _proc.WriteInt32(itemAddr + _o.ItemStack, stack);
        ok &= _proc.WriteByte(itemAddr + _o.ItemPrefix, prefix);
        ok &= _proc.WriteByte(itemAddr + _o.ItemFavorited, fav);
        return ok;
    }

    #endregion

    #region PlayerData mapping (memory mode)

    /// <summary>
    /// Read the live Player object into the editor's PlayerData model so the
    /// regular tabs can display/edit it. Equipment mapping follows the 1.4.x
    /// layout: armor = Item[20] (0-2 armor, 3-5 vanity, 6-12 accessories,
    /// 13-19 vanity accessories), dye = Item[10], miscEquips/miscDyes = Item[5].
    /// </summary>
    public PlayerData ReadToPlayerData()
    {
        var p = new PlayerData();
        if (PlayerBase == 0) return p;

        var info = ReadPlayerInfo();
        if (info != null)
        {
            p.Name = info.Name;
            p.Difficulty = (byte)info.Difficulty;
            p.Stats.Health = info.StatLife;
            p.Stats.MaxHealth = info.StatLifeMax;
            p.Stats.Mana = info.StatMana;
            p.Stats.MaxMana = info.StatManaMax;
            p.Appearance.HairStyle = info.Hair;
        }
        _proc.ReadByte(PlayerBase + _o.HairDye, out byte hairDye);
        p.Appearance.HairDye = hairDye;
        _proc.ReadByte(PlayerBase + _o.SkinVariant, out byte skinVariant);
        p.Appearance.SkinVariant = skinVariant;
        p.TaxMoney = _proc.ReadInt32(PlayerBase + _o.TaxMoney);
        p.NumberOfDeathsPvE = _proc.ReadInt32(PlayerBase + _o.DeathsPvE);

        // Inventory: 0-49 main, 50-53 coins, 54-57 ammo
        p.MainInventory = ReadItemSection(MemoryItemSection.Inventory, 50);
        p.Coins = ReadItemSection(MemoryItemSection.Inventory, 4, 50);
        p.Ammo = ReadItemSection(MemoryItemSection.Inventory, 4, 54);

        // Trash is a dedicated Item field (Player+0xC4), not an inventory slot.
        uint trashPtr = _proc.ReadUInt32(PlayerBase + _o.TrashItem);
        p.TrashItem = trashPtr != 0 ? (ReadItemAt(trashPtr) ?? new ItemData()) : new ItemData();

        // Equipment (loadout 1 = the in-game active equipment)
        var lo = p.Loadout1 ??= new PlayerLoadout();
        lo.Armor = ReadItemSection(MemoryItemSection.Armor, 3, 0);
        lo.VanityArmor = ReadItemSection(MemoryItemSection.Armor, 3, 3);
        lo.Accessories = ReadItemSection(MemoryItemSection.Armor, 7, 6);
        lo.VanityAccessories = ReadItemSection(MemoryItemSection.Armor, 7, 13);
        lo.ArmorDyes = ReadItemSection(MemoryItemSection.Dye, 10);
        p.MiscEquips = lo.MiscEquips = ReadItemSection(MemoryItemSection.MiscEquips, 5);
        p.MiscEquipDyes = lo.MiscEquipDyes = ReadItemSection(MemoryItemSection.MiscDyes, 5);

        // Storage
        p.PiggyBank = ReadItemSection(MemoryItemSection.Bank, 40);
        p.Safe = ReadItemSection(MemoryItemSection.Bank2, 40);
        p.DefenderForge = ReadItemSection(MemoryItemSection.Bank3, 40);
        p.VoidVault = ReadItemSection(MemoryItemSection.Bank4, 40);

        // Buffs
        if (ReadIntArray(_o.BuffType, 44, out var buffTypes))
            p.BuffTypes = buffTypes;
        if (ReadIntArray(_o.BuffTime, 44, out var buffTimes))
            p.BuffTimes = buffTimes;

        return p;
    }

    /// <summary>
    /// Write the editor's PlayerData back into the live Player object.
    /// Supports: inventory/equipment/storage, buffs, stats, difficulty,
    /// hair/hairDye. Returns the number of failed slot writes (0 = all ok).
    /// </summary>
    public int WriteFromPlayerData(PlayerData p)
    {
        int failures = 0;
        if (PlayerBase == 0) return int.MaxValue;

        bool WriteList(MemoryItemSection section, int start, List<ItemData> items)
        {
            bool ok = true;
            for (int i = 0; i < items.Count; i++)
                ok &= WriteItem(section, start + i, items[i]);
            return ok;
        }

        // Inventory: 0-49 main, 50-53 coins, 54-57 ammo
        failures += WriteList(MemoryItemSection.Inventory, 0, p.MainInventory) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Inventory, 50, p.Coins) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Inventory, 54, p.Ammo) ? 0 : 1;
        // Trash: dedicated Item field (Player+0xC4).
        if (p.TrashItem != null)
        {
            uint trashPtr = _proc.ReadUInt32(PlayerBase + _o.TrashItem);
            failures += trashPtr != 0 && WriteItemAt(trashPtr, p.TrashItem) ? 0 : 1;
        }

        // Equipment: armor[20] = 3 armor + 3 vanity + 7 acc + 7 vanity acc
        var lo = p.Loadout1 ?? new PlayerLoadout();
        failures += WriteList(MemoryItemSection.Armor, 0, lo.Armor) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Armor, 3, lo.VanityArmor) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Armor, 6, lo.Accessories) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Armor, 13, lo.VanityAccessories) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Dye, 0, lo.ArmorDyes) ? 0 : 1;
        failures += WriteList(MemoryItemSection.MiscEquips, 0, p.MiscEquips) ? 0 : 1;
        failures += WriteList(MemoryItemSection.MiscDyes, 0, p.MiscEquipDyes) ? 0 : 1;

        // Storage
        failures += WriteList(MemoryItemSection.Bank, 0, p.PiggyBank) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Bank2, 0, p.Safe) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Bank3, 0, p.DefenderForge) ? 0 : 1;
        failures += WriteList(MemoryItemSection.Bank4, 0, p.VoidVault) ? 0 : 1;

        // Buffs
        if (!WriteIntArray(_o.BuffType, p.BuffTypes)) failures++;
        if (!WriteIntArray(_o.BuffTime, p.BuffTimes)) failures++;

        // Stats / identity
        if (!_proc.WriteInt32(PlayerBase + _o.StatLife, p.Stats.Health)) failures++;
        if (!_proc.WriteInt32(PlayerBase + _o.StatLifeMax, p.Stats.MaxHealth)) failures++;
        if (!_proc.WriteInt32(PlayerBase + _o.StatMana, p.Stats.Mana)) failures++;
        if (!_proc.WriteInt32(PlayerBase + _o.StatManaMax, p.Stats.MaxMana)) failures++;
        if (!_proc.WriteByte(PlayerBase + _o.Difficulty, p.Difficulty)) failures++;
        if (!_proc.WriteInt32(PlayerBase + _o.Hair, p.Appearance.HairStyle)) failures++;
        if (!_proc.WriteByte(PlayerBase + _o.HairDye, (byte)p.Appearance.HairDye)) failures++;
        if (!_proc.WriteInt32(PlayerBase + _o.SkinVariant, p.Appearance.SkinVariant)) failures++;
        if (!_proc.WriteInt32(PlayerBase + _o.TaxMoney, p.TaxMoney)) failures++;
        if (!_proc.WriteInt32(PlayerBase + _o.DeathsPvE, p.NumberOfDeathsPvE)) failures++;

        return failures;
    }

    /// <summary>Read an int array field (e.g. buffType) from the Player object.</summary>
    public bool ReadIntArray(uint fieldOffset, int count, out int[] values)
    {
        values = new int[count];
        uint arrPtr = _proc.ReadUInt32(PlayerBase + fieldOffset);
        if (arrPtr == 0) return false;
        if (_proc.ReadInt32(arrPtr + PlayerMemoryOffsets.ArrayLength) < count) return false;
        var buf = new byte[count * 4];
        if (!_proc.ReadBytes(arrPtr + PlayerMemoryOffsets.ArrayData, buf.AsSpan(0, buf.Length)))
            return false;
        for (int i = 0; i < count; i++)
            values[i] = BitConverter.ToInt32(buf, i * 4);
        return true;
    }

    /// <summary>Write an int array field (e.g. buffType) into the Player object.</summary>
    public bool WriteIntArray(uint fieldOffset, int[] values)
    {
        uint arrPtr = _proc.ReadUInt32(PlayerBase + fieldOffset);
        if (arrPtr == 0) return false;
        if (_proc.ReadInt32(arrPtr + PlayerMemoryOffsets.ArrayLength) < values.Length) return false;
        var buf = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++)
            BitConverter.GetBytes(values[i]).CopyTo(buf, i * 4);
        return _proc.WriteBytes(arrPtr + PlayerMemoryOffsets.ArrayData, buf);
    }

    #region Immediate single-field writes (memory mode)

    public bool WriteStatLife(int value) => _proc.WriteInt32(PlayerBase + _o.StatLife, value);
    public bool WriteStatLifeMax(int value) => _proc.WriteInt32(PlayerBase + _o.StatLifeMax, value);
    public bool WriteStatMana(int value) => _proc.WriteInt32(PlayerBase + _o.StatMana, value);
    public bool WriteStatManaMax(int value) => _proc.WriteInt32(PlayerBase + _o.StatManaMax, value);
    public bool WriteDifficulty(byte value) => _proc.WriteByte(PlayerBase + _o.Difficulty, value);
    public bool WriteHair(int value) => _proc.WriteInt32(PlayerBase + _o.Hair, value);
    public bool WriteHairDye(byte value) => _proc.WriteByte(PlayerBase + _o.HairDye, value);
    public bool WriteSkinVariant(int value) => _proc.WriteInt32(PlayerBase + _o.SkinVariant, value);
    public bool WriteTaxMoney(int value) => _proc.WriteInt32(PlayerBase + _o.TaxMoney, value);
    public bool WriteDeathsPvE(int value) => _proc.WriteInt32(PlayerBase + _o.DeathsPvE, value);

    /// <summary>Immediately write one buff slot (type + remaining time).</summary>
    public bool WriteBuffSlot(int index, int type, int time)
    {
        if (index < 0 || index >= 44) return false;
        if (!ReadIntArray(_o.BuffType, 44, out var types) || !ReadIntArray(_o.BuffTime, 44, out var times))
            return false;
        types[index] = type;
        times[index] = time;
        return WriteIntArray(_o.BuffType, types) & WriteIntArray(_o.BuffTime, times);
    }

    #endregion

    #endregion

    public void Dispose()
    {
        _proc.Dispose();
    }
}
