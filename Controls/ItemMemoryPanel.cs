using Terraria_Players_Editor.Services;
using Terraria_Players_Editor.Services.Memory;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// In-memory item attribute editor: shows every editable field of the selected
/// live Item object (offsets from the CSX Item structure) and writes changes
/// straight into the game's memory as the user edits. Only used in memory mode.
/// </summary>
public sealed class ItemMemoryPanel : UserControl
{
    private enum FieldKind { Int, Short, Float, Bool }

    private sealed record FieldDef(string Name, uint Offset, FieldKind Kind, decimal Min, decimal Max, decimal Increment, int Decimals = 0);

    // Field name -> localized display name (zh) / fallback to the raw field name.
    private static string DisplayName(string field)
    {
        var zh = field switch
        {
            "type" => "类型", "stack" => "数量", "maxStack" => "最大堆叠", "prefix" => "前缀", "favorited" => "已收藏",
            "damage" => "伤害", "crit" => "暴击", "knockBack" => "击退", "useTime" => "使用时间", "useAnimation" => "使用动画",
            "useStyle" => "使用样式", "holdStyle" => "握持样式", "reuseDelay" => "复用延迟", "autoReuse" => "自动使用",
            "useTurn" => "使用转身", "shoot" => "发射物", "shootSpeed" => "发射速度", "ammo" => "弹药类型", "useAmmo" => "消耗弹药",
            "notAmmo" => "非弹药", "melee" => "近战", "magic" => "魔法", "ranged" => "远程", "summon" => "召唤",
            "sentry" => "哨兵", "noMelee" => "非近战", "noUseGraphic" => "无使用图像", "armorPenetration" => "护甲穿透",
            "bonusTagDamage" => "标签伤害加成",
            "pick" => "镐力", "axe" => "斧力", "hammer" => "锤力", "tileBoost" => "放置范围", "createTile" => "放置方块",
            "createWall" => "放置墙", "placeStyle" => "放置样式", "tileWand" => "方块法杖",
            "healLife" => "治疗生命", "healMana" => "治疗魔力", "mana" => "消耗魔力", "manaIncrease" => "魔力上限",
            "lifeRegen" => "生命回复", "defense" => "防御", "consumable" => "消耗品", "potion" => "药水",
            "buffType" => "增益类型", "buffTime" => "增益时间", "rare" => "稀有度", "value" => "价值",
            "shopCustomPrice" => "商店自定义价格", "color" => "颜色",
            "headSlot" => "头盔槽", "bodySlot" => "上衣槽", "legSlot" => "裤子槽", "accessory" => "饰品",
            "vanity" => "时装", "social" => "社交", "dye" => "染料", "wornArmor" => "已穿戴护甲", "expertOnly" => "仅专家",
            "expert" => "专家", "material" => "材料", "questItem" => "任务物品", "uniqueStack" => "唯一堆叠",
            "makeNPC" => "生成NPC", "hairDye" => "发型染料", "glowMask" => "发光遮罩",
            "width" => "宽度", "height" => "高度", "alpha" => "透明度", "scale" => "缩放", "useSoundPitch" => "音调",
            "fishingPole" => "鱼竿", "bait" => "鱼饵", "mountType" => "坐骑类型", "stringColor" => "线颜色",
            _ => field
        };
        return AppLocale.Current == AppLocale.Lang.ZH ? $"{zh} ({field})" : $"{field} ({zh})";
    }

    private static readonly FieldDef[] Fields =
    {
        // === Identity / stack ===
        new("type", 0x50, FieldKind.Int, 0, 9999, 1),
        new("stack", 0x64, FieldKind.Int, 0, 99999, 1),
        new("maxStack", 0x68, FieldKind.Int, 0, 99999, 1),
        new("prefix", 0x12E, FieldKind.Int, 0, 89, 1),
        new("favorited", 0x10C, FieldKind.Bool, 0, 1, 1),

        // === Combat ===
        new("damage", 0x88, FieldKind.Int, 0, 100000, 1),
        new("crit", 0xEC, FieldKind.Int, 0, 10000, 1),
        new("knockBack", 0x8C, FieldKind.Float, 0, 100, 0.01m, 2),
        new("useTime", 0x60, FieldKind.Int, 0, 1000, 1),
        new("useAnimation", 0x5C, FieldKind.Int, 0, 1000, 1),
        new("useStyle", 0x58, FieldKind.Int, 0, 100, 1),
        new("holdStyle", 0x54, FieldKind.Int, 0, 100, 1),
        new("reuseDelay", 0xF8, FieldKind.Int, 0, 10000, 1),
        new("autoReuse", 0x111, FieldKind.Bool, 0, 1, 1),
        new("useTurn", 0x112, FieldKind.Bool, 0, 1, 1),
        new("shoot", 0xBC, FieldKind.Int, 0, 10000, 1),
        new("shootSpeed", 0xC0, FieldKind.Float, 0, 100, 0.01m, 2),
        new("ammo", 0xC4, FieldKind.Int, 0, 9999, 1),
        new("useAmmo", 0xC8, FieldKind.Int, 0, 9999, 1),
        new("notAmmo", 0x120, FieldKind.Bool, 0, 1, 1),
        new("melee", 0x12F, FieldKind.Bool, 0, 1, 1),
        new("magic", 0x130, FieldKind.Bool, 0, 1, 1),
        new("ranged", 0x131, FieldKind.Bool, 0, 1, 1),
        new("summon", 0x132, FieldKind.Bool, 0, 1, 1),
        new("sentry", 0x133, FieldKind.Bool, 0, 1, 1),
        new("noMelee", 0x123, FieldKind.Bool, 0, 1, 1),
        new("noUseGraphic", 0x122, FieldKind.Bool, 0, 1, 1),
        new("armorPenetration", 0xF0, FieldKind.Int, 0, 10000, 1),
        new("bonusTagDamage", 0xF4, FieldKind.Int, 0, 10000, 1),

        // === Tools / building ===
        new("pick", 0x6C, FieldKind.Int, 0, 500, 1),
        new("axe", 0x70, FieldKind.Int, 0, 500, 1),
        new("hammer", 0x74, FieldKind.Int, 0, 500, 1),
        new("tileBoost", 0x78, FieldKind.Int, -10, 50, 1),
        new("createTile", 0x7C, FieldKind.Int, 0, 9999, 1),
        new("createWall", 0x80, FieldKind.Int, 0, 9999, 1),
        new("placeStyle", 0x84, FieldKind.Int, 0, 100, 1),
        new("tileWand", 0x3C, FieldKind.Int, 0, 9999, 1),

        // === Stats / consumables ===
        new("healLife", 0x90, FieldKind.Int, 0, 10000, 1),
        new("healMana", 0x94, FieldKind.Int, 0, 10000, 1),
        new("mana", 0xD4, FieldKind.Int, 0, 10000, 1),
        new("manaIncrease", 0xD0, FieldKind.Int, 0, 10000, 1),
        new("lifeRegen", 0xCC, FieldKind.Int, 0, 10000, 1),
        new("defense", 0xA4, FieldKind.Int, 0, 10000, 1),
        new("consumable", 0x110, FieldKind.Bool, 0, 1, 1),
        new("potion", 0x10F, FieldKind.Bool, 0, 1, 1),
        new("buffType", 0xDC, FieldKind.Int, 0, 9999, 1),
        new("buffTime", 0xE0, FieldKind.Int, 0, 100000, 1),
        new("rare", 0xB8, FieldKind.Int, -50, 50, 1),
        new("value", 0xD8, FieldKind.Int, 0, 10000000, 1),
        new("shopCustomPrice", 0x13C, FieldKind.Int, 0, 100000000, 1),
        new("color", 0x138, FieldKind.Int, 0, 0xFFFFFF, 1),

        // === Vanity / equipment slots ===
        new("headSlot", 0xA8, FieldKind.Int, 0, 500, 1),
        new("bodySlot", 0xAC, FieldKind.Int, 0, 500, 1),
        new("legSlot", 0xB0, FieldKind.Int, 0, 500, 1),
        new("accessory", 0x10E, FieldKind.Bool, 0, 1, 1),
        new("vanity", 0x126, FieldKind.Bool, 0, 1, 1),
        new("social", 0x125, FieldKind.Bool, 0, 1, 1),
        new("dye", 0x106, FieldKind.Bool, 0, 1, 1),
        new("wornArmor", 0x105, FieldKind.Bool, 0, 1, 1),
        new("expertOnly", 0x107, FieldKind.Bool, 0, 1, 1),
        new("expert", 0x108, FieldKind.Bool, 0, 1, 1),
        new("material", 0x127, FieldKind.Bool, 0, 1, 1),
        new("questItem", 0x102, FieldKind.Bool, 0, 1, 1),
        new("uniqueStack", 0x12A, FieldKind.Bool, 0, 1, 1),
        new("makeNPC", 0xFC, FieldKind.Short, 0, 65535, 1),
        new("hairDye", 0xFE, FieldKind.Short, 0, 65535, 1),
        new("glowMask", 0x100, FieldKind.Short, 0, 65535, 1),
        new("width", 0x18, FieldKind.Int, 0, 500, 1),
        new("height", 0x1C, FieldKind.Int, 0, 500, 1),
        new("alpha", 0x98, FieldKind.Int, 0, 255, 1),
        new("scale", 0x9C, FieldKind.Float, 0, 100, 0.01m, 2),
        new("useSoundPitch", 0xA0, FieldKind.Float, -2, 2, 0.01m, 2),
        new("fishingPole", 0x48, FieldKind.Int, 0, 100, 1),
        new("bait", 0x4C, FieldKind.Int, 0, 100, 1),
        new("mountType", 0xE4, FieldKind.Int, 0, 9999, 1),
        new("stringColor", 0xB4, FieldKind.Int, 0, 31, 1),
    };

    private readonly Panel _scroll;
    private readonly TableLayoutPanel _table;
    private readonly List<(FieldDef def, NumericUpDown nud, CheckBox chk)> _rows = new();
    private MemoryProcess? _proc;
    private uint _itemAddr;
    private bool _suppress;

    public ItemMemoryPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
        Padding = new Padding(1); // room for the border

        var title = new Label
        {
            Text = AppLocale.Get("MemEdit.ItemAttrTitle"),
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = ThemeManager.Typography.Caption,
            ForeColor = ThemeManager.TextPrimary,
            Padding = new Padding(6, 0, 0, 0)
        };

        _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Top,
            Padding = new Padding(4)
        };
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < Fields.Length; i++)
        {
            var def = Fields[i];
            var lbl = new Label
            {
                Text = DisplayName(def.Name),
                AutoSize = true,
                ForeColor = ThemeManager.TextSecondary,
                Margin = new Padding(0, 5, 4, 0),
                Tag = "secondary"
            };
            if (def.Kind == FieldKind.Bool)
            {
                var chk = new CheckBox { AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
                chk.CheckedChanged += (_, _) => OnChanged(def, chk.Checked ? 1 : 0);
                _table.Controls.Add(lbl, 0, i);
                _table.Controls.Add(chk, 1, i);
                _rows.Add((def, null!, chk));
            }
            else
            {
                var nud = new NumericUpDown
                {
                    Minimum = def.Min,
                    Maximum = def.Max,
                    Increment = def.Increment,
                    DecimalPlaces = def.Decimals,
                    Width = 110,
                    Margin = new Padding(0, 1, 0, 1)
                };
                nud.ValueChanged += (_, _) => OnChanged(def, (double)nud.Value);
                _table.Controls.Add(lbl, 0, i);
                _table.Controls.Add(nud, 1, i);
                _rows.Add((def, nud, null!));
            }
        }
        _scroll.Controls.Add(_table);

        Controls.Add(_scroll);
        Controls.Add(title);
    }

    /// <summary>Point the editor at a live Item object (memory mode).</summary>
    public void SetItem(MemoryProcess? proc, uint itemAddr)
    {
        _proc = proc;
        _itemAddr = itemAddr;
        Reload();
    }

    public void ClearItem() => SetItem(null, 0);

    private void Reload()
    {
        _suppress = true;
        try
        {
            foreach (var (def, nud, chk) in _rows)
            {
                if (def.Kind == FieldKind.Bool)
                {
                    chk.Checked = _itemAddr != 0 && _proc != null && _proc.ReadByte(_itemAddr + def.Offset, out byte b) && b != 0;
                }
                else
                {
                    double value = 0;
                    if (_itemAddr != 0 && _proc != null)
                    {
                        switch (def.Kind)
                        {
                            case FieldKind.Int: value = _proc.ReadInt32(_itemAddr + def.Offset); break;
                            case FieldKind.Short: value = _proc.ReadInt32(_itemAddr + def.Offset) & 0xFFFF; break;
                            case FieldKind.Float: value = _proc.ReadFloat(_itemAddr + def.Offset, out float f) ? f : 0; break;
                        }
                    }
                    nud.Value = Math.Clamp((decimal)value, nud.Minimum, nud.Maximum);
                }
            }
            Enabled = _itemAddr != 0;
        }
        finally
        {
            _suppress = false;
        }
    }

    private void OnChanged(FieldDef def, double value)
    {
        if (_suppress || _proc == null || _itemAddr == 0) return;
        bool ok = def.Kind switch
        {
            FieldKind.Int => _proc.WriteInt32(_itemAddr + def.Offset, (int)Math.Round(value)),
            FieldKind.Short => _proc.WriteInt32(_itemAddr + def.Offset, (int)Math.Round(value) & 0xFFFF),
            FieldKind.Float => _proc.WriteFloat(_itemAddr + def.Offset, (float)value),
            FieldKind.Bool => _proc.WriteByte(_itemAddr + def.Offset, (byte)(value != 0 ? 1 : 0)),
            _ => false
        };
        if (!ok)
        {
            DebugLog.Log($"[ItemAttr] write failed {def.Name}@{def.Offset:X}");
        }
    }

    /// <summary>Theme-aware 1px border, same style as the item browser.</summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Win11Renderer.DrawThemedBorder(e.Graphics, this);
    }

    public void RefreshLocale()
    {
        var title = Controls.OfType<Label>().FirstOrDefault();
        if (title != null) title.Text = AppLocale.Get("MemEdit.ItemAttrTitle");
        // Each row added a Label then an editor; labels sit at even indices.
        for (int i = 0; i < _rows.Count; i++)
        {
            int idx = i * 2;
            if (idx < _table.Controls.Count && _table.Controls[idx] is Label lbl)
                lbl.Text = DisplayName(_rows[i].def.Name);
        }
    }
}
