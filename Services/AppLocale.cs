namespace Terraria_Players_Editor.Services;

/// <summary>
/// Application UI string localization. All UI strings are defined here in EN/ZH pairs.
/// Switch language via <see cref="Current"/> and call RefreshAllUI() on the form.
/// </summary>
public static class AppLocale
{
    public enum Lang { EN, ZH }

    public static Lang Current { get; set; } = Lang.EN;

    /// <summary>Event raised when language changes, so forms can refresh their text.</summary>
    public static event Action? LanguageChanged;

    public static void SetLanguage(Lang lang)
    {
        if (Current != lang)
        {
            Current = lang;
            LanguageChanged?.Invoke();
        }
    }

    public static string Get(string key) =>
        Strings.TryGetValue(key, out var pair) ? pair[(int)Current] : key;

    // ================================================================
    //  ALL UI STRINGS — format: [English, 中文]
    // ================================================================
    private static readonly Dictionary<string, string[]> Strings = new()
    {
        // Menu
        ["Menu.File"] = ["&File", "文件(&F)"],
        ["Menu.Open"] = ["&Open...", "打开(&O)..."],
        ["Menu.Save"] = ["&Save", "保存(&S)"],
        ["Menu.SaveAs"] = ["Save &As...", "另存为(&A)..."],
        ["Menu.Exit"] = ["E&xit", "退出(&X)"],
        ["Menu.Language"] = ["&Language", "语言(&L)"],
        ["Menu.LangEN"] = ["English", "English"],
        ["Menu.LangZH"] = ["中文", "中文"],

        // Tab titles
        ["Tab.PlayerInfo"] = ["Player Info", "玩家信息"],
        ["Tab.Stats"] = ["Stats", "属性"],
        ["Tab.Appearance"] = ["Appearance", "外观"],
        ["Tab.Inventory"] = ["Inventory", "背包"],
        ["Tab.Equipment"] = ["Equipment", "装备"],
        ["Tab.Dyes"] = ["Dyes", "染料"],
        ["Tab.Storage"] = ["Storage", "存储"],
        ["Tab.Buffs"] = ["Buffs", "增益"],
        ["Tab.Upgrades"] = ["Upgrades", "加成"],
        ["Tab.Loadouts"] = ["Loadouts", "负载"],
        ["Tab.SpawnPoints"] = ["Spawn Points", "重生点"],
        ["Tab.Misc"] = ["Misc", "杂项"],

        // Player Info tab
        ["Info.Name"] = ["Name:", "名称:"],
        ["Info.Difficulty"] = ["Difficulty:", "难度:"],
        ["Info.PlayTime"] = ["Play Time:", "游戏时间:"],
        ["Info.FileVersion"] = ["File Version:", "文件版本:"],
        ["Info.Loadout"] = ["Active Loadout:", "当前负载:"],

        // Stats tab
        ["Stats.Health"] = ["Health", "生命"],
        ["Stats.Current"] = ["Current:", "当前:"],
        ["Stats.Max"] = ["Max:", "最大:"],
        ["Stats.Mana"] = ["Mana", "魔力"],
        ["Stats.Counters"] = ["Counters", "计数"],
        ["Stats.DeathsPvE"] = ["Deaths (PvE):", "死亡(PvE):"],
        ["Stats.DeathsPvP"] = ["Deaths (PvP):", "死亡(PvP):"],
        ["Stats.TaxMoney"] = ["Tax Money:", "税收金钱:"],
        ["Stats.AnglerQuests"] = ["Angler Quests:", "渔夫任务:"],
        ["Stats.GolferScore"] = ["Golfer Score:", "高尔夫分数:"],

        // Appearance tab
        ["Appearance.HairStyle"] = ["Hair Style:", "发型:"],
        ["Appearance.HairDye"] = ["Hair Dye:", "发色:"],
        ["Appearance.Skin"] = ["Skin:", "肤色:"],
        ["Appearance.Colors"] = ["Colors", "颜色"],
        ["Appearance.Pick"] = ["Pick...", "选择..."],
        ["Appearance.Visibility"] = ["Visibility Toggles", "可见性开关"],
        ["Appearance.Head"] = ["Head", "头部"],
        ["Appearance.Body"] = ["Body", "身体"],
        ["Appearance.Legs"] = ["Legs", "腿部"],
        ["Appearance.VanityHead"] = ["Vanity Head", "时装头部"],
        ["Appearance.VanityBody"] = ["Vanity Body", "时装身体"],
        ["Appearance.VanityLegs"] = ["Vanity Legs", "时装腿部"],
        ["Appearance.Acc1"] = ["Acc 1", "饰品1"],
        ["Appearance.Acc2"] = ["Acc 2", "饰品2"],
        ["Appearance.Acc3"] = ["Acc 3", "饰品3"],
        ["Appearance.Acc4"] = ["Acc 4", "饰品4"],
        ["Appearance.Pet"] = ["Pet", "宠物"],
        ["Appearance.LightPet"] = ["Light Pet", "照明宠物"],
        ["Appearance.Minecart"] = ["Minecart", "矿车"],
        ["Appearance.Mount"] = ["Mount", "坐骑"],
        ["Appearance.Hook"] = ["Hook", "钩爪"],

        // Color names
        ["Color.Hair"] = ["Hair", "头发"],
        ["Color.Skin"] = ["Skin", "皮肤"],
        ["Color.Eyes"] = ["Eyes", "眼睛"],
        ["Color.Shirt"] = ["Shirt", "上衣"],
        ["Color.UnderShirt"] = ["UnderShirt", "内衣"],
        ["Color.Pants"] = ["Pants", "裤子"],
        ["Color.Shoes"] = ["Shoes", "鞋子"],

        // Inventory tab
        ["Inventory.EditSlot"] = ["Edit Selected Slot", "编辑选中栏位"],
        ["Inventory.Stack"] = ["Stack:", "堆叠:"],
        ["Inventory.Prefix"] = ["Prefix:", "前缀:"],
        ["Inventory.Favorite"] = ["Favorite", "收藏"],
        ["Inventory.SetItem"] = ["Set Item", "设置物品"],
        ["Inventory.ClearSlot"] = ["Clear Slot", "清除栏位"],
        ["Inventory.Coins"] = ["Coins", "金币"],
        ["Inventory.Ammo"] = ["Ammo", "弹药"],

        // Equipment tab
        ["Equip.Armor"] = ["Armor (3)", "盔甲(3)"],
        ["Equip.VanityArmor"] = ["Vanity Armor (3)", "时装盔甲(3)"],
        ["Equip.Accessories"] = ["Accessories (7)", "饰品(7)"],
        ["Equip.VanityAccessories"] = ["Vanity Accessories (7)", "时装饰品(7)"],
        ["Equip.Misc"] = ["Equipment (Pet, Light Pet, Minecart, Mount, Hook)", "装备(宠物、照明宠物、矿车、坐骑、钩爪)"],
        ["Equip.Helmet"] = ["Helmet:", "头盔:"],
        ["Equip.Chestplate"] = ["Chestplate:", "胸甲:"],
        ["Equip.Leggings"] = ["Leggings:", "护腿:"],
        ["Equip.VanityHelmet"] = ["Vanity Helmet:", "时装头盔:"],
        ["Equip.VanityChest"] = ["Vanity Chest:", "时装胸甲:"],
        ["Equip.VanityLegs"] = ["Vanity Legs:", "时装护腿:"],
        ["Equip.Pet"] = ["Pet:", "宠物:"],
        ["Equip.LightPet"] = ["Light Pet:", "照明宠物:"],
        ["Equip.Minecart"] = ["Minecart:", "矿车:"],
        ["Equip.Mount"] = ["Mount:", "坐骑:"],
        ["Equip.Hook"] = ["Hook:", "钩爪:"],

        // Dyes tab
        ["Dyes.Armor"] = ["Armor Dyes (3)", "盔甲染料(3)"],
        ["Dyes.Accessories"] = ["Accessory Dyes (7)", "饰品染料(7)"],
        ["Dyes.Equipment"] = ["Equipment Dyes (5)", "装备染料(5)"],
        ["Dyes.HelmetDye"] = ["Helmet Dye:", "头盔染料:"],
        ["Dyes.ChestDye"] = ["Chestplate Dye:", "胸甲染料:"],
        ["Dyes.LegsDye"] = ["Leggings Dye:", "护腿染料:"],
        ["Dyes.PetDye"] = ["Pet Dye:", "宠物染料:"],
        ["Dyes.LightPetDye"] = ["Light Pet Dye:", "照明宠物染料:"],
        ["Dyes.MinecartDye"] = ["Minecart Dye:", "矿车染料:"],
        ["Dyes.MountDye"] = ["Mount Dye:", "坐骑染料:"],
        ["Dyes.HookDye"] = ["Hook Dye:", "钩爪染料:"],

        // Storage tab
        ["Storage.EditSlot"] = ["Edit Storage Slot", "编辑存储栏位"],
        ["Storage.Set"] = ["Set", "设置"],
        ["Storage.Clear"] = ["Clear", "清除"],
        ["Storage.PiggyBank"] = ["Piggy Bank", "猪猪储蓄罐"],
        ["Storage.Safe"] = ["Safe", "保险箱"],
        ["Storage.DefenderForge"] = ["Defender's Forge", "护卫熔炉"],
        ["Storage.VoidVault"] = ["Void Vault", "虚空宝库"],

        // Buffs tab
        ["Buffs.Title"] = ["Active Buffs (44 slots) — Edit Type ID and Duration in ticks below:", "当前增益(44栏位)—在下方编辑类型ID和持续时间:"],
        ["Buffs.Type"] = ["Buff Type ID", "增益类型ID"],
        ["Buffs.Duration"] = ["Duration (ticks)", "持续时间(刻)"],

        // Upgrades tab
        ["Upgrades.ExtraAccessory"] = ["Extra Accessory Slot (Demon Heart)", "额外饰品栏(恶魔之心)"],
        ["Upgrades.AegisCrystal"] = ["Used Aegis Crystal (+20 HP)", "已使用生命水晶(+20生命)"],
        ["Upgrades.AegisFruit"] = ["Used Aegis Fruit (+20 HP)", "已使用生命果(+20生命)"],
        ["Upgrades.ArcaneCrystal"] = ["Used Arcane Crystal (+20 MP)", "已使用奥术水晶(+20魔力)"],
        ["Upgrades.GalaxyPearl"] = ["Used Galaxy Pearl (+luck)", "已使用银河珍珠(+幸运)"],
        ["Upgrades.GummyWorm"] = ["Used Gummy Worm (+fishing power)", "已使用软糖虫(+钓鱼力)"],
        ["Upgrades.Ambrosia"] = ["Used Ambrosia (+mining speed)", "已使用美味(+挖矿速度)"],
        ["Upgrades.ArtisanBread"] = ["Ate Artisan Bread (+build range)", "已食用工匠面包(+建造范围)"],
        ["Upgrades.BiomeTorches"] = ["Unlocked Biome Torches", "已解锁生物群落火把"],
        ["Upgrades.UsingBiomeTorches"] = ["Using Biome Torches", "使用生物群落火把"],
        ["Upgrades.SuperCart"] = ["Super Cart Level:", "超级矿车等级:"],
        ["Upgrades.SuperCartEnabled"] = ["Enabled", "已启用"],

        // Loadouts tab
        ["Loadouts.Loadout2"] = ["Loadout 2", "负载2"],
        ["Loadouts.Loadout3"] = ["Loadout 3", "负载3"],
        ["Loadouts.Armor3"] = ["Armor (3)", "盔甲(3)"],
        ["Loadouts.VanityArmor3"] = ["Vanity Armor (3)", "时装盔甲(3)"],
        ["Loadouts.Accessories7"] = ["Accessories (7)", "饰品(7)"],
        ["Loadouts.VanityAccessories7"] = ["Vanity Accessories (7)", "时装饰品(7)"],
        ["Loadouts.Equipment5"] = ["Equipment (5)", "装备(5)"],
        ["Loadouts.Helmet"] = ["Helmet:", "头盔:"],
        ["Loadouts.Chestplate"] = ["Chestplate:", "胸甲:"],
        ["Loadouts.Leggings"] = ["Leggings:", "护腿:"],
        ["Loadouts.VanityHelmet"] = ["Vanity Helmet:", "时装头盔:"],
        ["Loadouts.VanityChest"] = ["Vanity Chest:", "时装胸甲:"],
        ["Loadouts.VanityLegs"] = ["Vanity Legs:", "时装护腿:"],

        // Spawn Points tab
        ["Spawn.Add"] = ["Add Spawn Point", "添加重生点"],
        ["Spawn.Remove"] = ["Remove Selected", "删除选中"],
        ["Spawn.WorldId"] = ["World ID", "世界ID"],
        ["Spawn.WorldName"] = ["World Name", "世界名称"],

        // Misc tab
        ["Misc.HotbarLocked"] = ["Hotbar Locked", "快捷栏锁定"],
        ["Misc.HideInfo"] = ["Info Accessory Display", "信息配件显示"],
        ["Misc.Cooldowns"] = ["Cooldowns (ticks)", "冷却时间(刻)"],
        ["Misc.PotionDelay"] = ["Potion Delay:", "药水冷却:"],
        ["Misc.ManaPotionDelay"] = ["Mana Potion Delay:", "魔力药水冷却:"],
        ["Misc.RestorationCd"] = ["Restoration CD:", "恢复药水冷却:"],

        // Info toggles
        ["Info.Watch"] = ["Watch", "表"],
        ["Info.Weather"] = ["Weather", "天气"],
        ["Info.Depth"] = ["Depth", "深度"],
        ["Info.Compass"] = ["Compass", "指南针"],
        ["Info.Sextant"] = ["Sextant", "六分仪"],
        ["Info.Tally"] = ["Tally", "计数"],
        ["Info.Stopwatch"] = ["Stopwatch", "秒表"],
        ["Info.MetalDetector"] = ["Metal Detector", "金属探测器"],
        ["Info.DPS"] = ["DPS", "DPS"],
        ["Info.RareCreature"] = ["Rare Creature", "稀有生物"],
        ["Info.FishingPower"] = ["Fishing Power", "钓鱼力"],
        ["Info.MoonPhase"] = ["Moon Phase", "月相"],
        ["Info.Speed"] = ["Speed", "速度"],

        // Difficulty names
        ["Diff.Softcore"] = ["Softcore (Classic)", "软核(经典)"],
        ["Diff.Mediumcore"] = ["Mediumcore", "中核"],
        ["Diff.Hardcore"] = ["Hardcore", "硬核"],
        ["Diff.Journey"] = ["Journey", "旅途"],

        // Loadout names
        ["Loadout.1"] = ["Loadout 1", "负载1"],
        ["Loadout.2"] = ["Loadout 2", "负载2"],
        ["Loadout.3"] = ["Loadout 3", "负载3"],

        // Gender
        ["Gender.Female"] = ["Female", "女"],
        ["Gender.Male"] = ["Male", "男"],

        // Status bar messages
        ["Status.Ready"] = ["Ready — Open a .plr file to begin.", "就绪 — 打开.plr文件开始。"],
        ["Status.Loaded"] = ["Loaded: {0} — {1} (v{2})", "已加载: {0} — {1} (v{2})"],
        ["Status.Saved"] = ["Saved: {0}", "已保存: {0}"],
        ["Status.Working"] = ["Working...", "处理中..."],
        ["Status.Failed"] = ["Failed to load file.", "加载文件失败。"],
        ["Status.SaveFailed"] = ["Failed to save file.", "保存文件失败。"],

        // Dialog messages
        ["Dialog.NoPlayer"] = ["No player data loaded.", "未加载玩家数据。"],
        ["Dialog.LoadError"] = ["Failed to load file:\n{0}", "加载文件失败:\n{0}"],
        ["Dialog.SaveError"] = ["Failed to save file:\n{0}", "保存文件失败:\n{0}"],
        ["Dialog.FileFilter"] = ["Terraria Player Files (*.plr)|*.plr|All Files (*.*)|*.*", "Terraria玩家文件(*.plr)|*.plr|所有文件(*.*)|*.*"],
        ["Dialog.OpenTitle"] = ["Open Terraria Player File", "打开Terraria玩家文件"],
        ["Dialog.SaveTitle"] = ["Save Terraria Player File", "保存Terraria玩家文件"],

        // DataGridView headers
        ["Grid.Name"] = ["Name", "名称"],
        ["Grid.ID"] = ["ID", "ID"],
        ["Grid.Stack"] = ["Stack", "堆叠"],
        ["Grid.Prefix"] = ["Prefix", "前缀"],

        // Spawn point columns
        ["Spawn.X"] = ["X", "X"],
        ["Spawn.Y"] = ["Y", "Y"],

        // Slot labels
        ["Slot.Generic"] = ["Slot {0}:", "栏位{0}:"],
    };
}
