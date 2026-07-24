using System.ComponentModel;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Right-side panel for editing the selected slot's item properties.
/// Shows item icon, name, stack count, prefix, favorite, and Set/Clear buttons.
/// Visibility of stack/prefix/favorite controls can be configured.
/// </summary>
public class ItemModifier : UserControl
{
    private readonly PictureBox _icon;
    private readonly Label _lblName;
    private readonly Label _lblId;
    private readonly ComboBox _cmbItemSearch;
    private readonly Label _lblStack;
    private readonly NumericUpDown _nudStack;
    private readonly Label _lblPrefix;
    private readonly ComboBox _cmbPrefix;
    private readonly CheckBox _chkFavorite;
    private readonly Button _btnSet;
    private readonly Button _btnClear;
    private int _currentSlotIndex = -1;
    private int _currentItemId;

    // Cached values to avoid WinForms NumericUpDown/ComboBox commit timing issues
    private int _cachedStack = 1;
    private byte _cachedPrefix;

    // Animation support
    private System.Windows.Forms.Timer? _animTimer;
    private Bitmap[]? _animFrames;
    private int _animFrameIdx;

    public ItemModifier()
    {
        Width = 400;
        Height = 130;
        BorderStyle = BorderStyle.FixedSingle;

        // Icon (top-left)
        _icon = new PictureBox
        {
            Size = new Size(32, 32),
            Location = new Point(10, 6),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(40, 35, 45)
        };

        // Name + ID labels
        _lblName = new Label
        {
            Location = new Point(52, 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        _lblId = new Label
        {
            Location = new Point(52, 28),
            AutoSize = true,
            ForeColor = Color.Gray
        };

        // Item search + set button (row 1)
        _cmbItemSearch = new ComboBox
        {
            Location = new Point(8, 50),
            Width = 220,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };

        _btnSet = new Button { Text = AppLocale.Get("Inventory.SetItem"), Location = new Point(234, 49), Width = 75 };
        _btnClear = new Button { Text = AppLocale.Get("Inventory.ClearSlot"), Location = new Point(314, 49), Width = 75 };

        // Stack (row 2)
        _lblStack = new Label { Text = AppLocale.Get("Inventory.Stack"), Location = new Point(8, 78), Width = 42, TextAlign = ContentAlignment.MiddleRight };
        _nudStack = new NumericUpDown { Location = new Point(54, 76), Width = 70, Minimum = 0, Maximum = 9999 };

        // Prefix (row 2)
        _lblPrefix = new Label { Text = AppLocale.Get("Inventory.Prefix"), Location = new Point(134, 78), Width = 42, TextAlign = ContentAlignment.MiddleRight };
        _cmbPrefix = new ComboBox { Location = new Point(180, 76), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };

        // Favorite (row 2)
        _chkFavorite = new CheckBox { Text = AppLocale.Get("Inventory.Favorite"), Location = new Point(330, 78), Width = 65 };

        // Auto-save on Enter or when control loses focus (click away)
        void DoSet()
        {
            if (_currentSlotIndex < 0) return;
            _cachedStack = Math.Max(1, (int)_nudStack.Value);
            _cachedPrefix = (byte)Math.Max(0, _cmbPrefix.SelectedIndex);
            DebugLog.Log($"[ItemMod] AutoSet: slot={_currentSlotIndex}, stack={_cachedStack}, prefix={_cachedPrefix}");
            SetClicked?.Invoke(this, _currentSlotIndex);
        }
        _cmbItemSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; DoSet(); } };
        _nudStack.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; DoSet(); } };
        _cmbPrefix.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; DoSet(); } };
        _nudStack.Leave += (s, e) => DoSet();
        _cmbPrefix.Leave += (s, e) => DoSet();
        _chkFavorite.CheckedChanged += (s, e) => DoSet();

        _btnSet.Click += (s, e) => DoSet();
        _btnClear.Click += (s, e) => ClearClicked?.Invoke(this, _currentSlotIndex);

        Controls.AddRange([
            _icon, _lblName, _lblId,
            _cmbItemSearch, _btnSet, _btnClear,
            _lblStack, _nudStack,
            _lblPrefix, _cmbPrefix,
            _chkFavorite
        ]);

        Disposed += (s, e) => StopAnimation();
    }

    /// <summary>Whether stack controls are visible (hidden for equipment).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowStack
    {
        get => _lblStack.Visible;
        set { _lblStack.Visible = value; _nudStack.Visible = value; }
    }

    /// <summary>Whether prefix controls are visible.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowPrefix
    {
        get => _lblPrefix.Visible;
        set { _lblPrefix.Visible = value; _cmbPrefix.Visible = value; }
    }

    /// <summary>Whether favorite checkbox is visible (inventory only).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowFavorite
    {
        get => _chkFavorite.Visible;
        set => _chkFavorite.Visible = value;
    }

    /// <summary>Fired when Set button is clicked.</summary>
    public event EventHandler<int>? SetClicked;

    /// <summary>Fired when Clear button is clicked.</summary>
    public event EventHandler<int>? ClearClicked;

    /// <summary>Current slot index being edited.</summary>
    public int CurrentSlotIndex => _currentSlotIndex;

    /// <summary>Load item data into the modifier controls.</summary>
    public void LoadFromSlot(int slotIndex, ItemData item)
    {
        StopAnimation();

        _currentSlotIndex = slotIndex;
        _currentItemId = item.ItemId;

        // Check for animation first
        if (!item.IsEmpty && SettingsManager.EnableAnimatedIcons)
        {
            var frames = IconService.GetItemFrames(item.ItemId);
            if (frames != null && frames.Length > 1)
            {
                DebugLog.Log(
                    $"[ItemMod] Anim start ID={item.ItemId}, frames={frames.Length}");
                _animFrames = frames;
                _animFrameIdx = 0;
                _icon.Image = frames[0];
                StartAnimation();
            }
            else
            {
                _icon.Image = IconService.GetItemIcon(item.ItemId) ?? IconService.DefaultIcon;
            }
        }
        else
        {
            _icon.Image = IconService.GetItemIcon(item.ItemId) ?? IconService.DefaultIcon;
        }

        _lblName.Text = item.ItemName;
        _lblId.Text = $"ID: {item.ItemId}";
        _nudStack.Value = item.StackSize > 0 ? item.StackSize : 1;
        _cachedStack = (int)_nudStack.Value;
        _chkFavorite.Checked = item.Favorited;

        // Update search combo text to match current item so Set doesn't clobber it
        _cmbItemSearch.Text = item.IsEmpty ? "" : $"{item.ItemName} (ID:{item.ItemId})";

        // Set prefix combo
        if (_cmbPrefix.Items.Count > 0 && item.Prefix < _cmbPrefix.Items.Count)
            _cmbPrefix.SelectedIndex = item.Prefix;
        _cachedPrefix = (byte)_cmbPrefix.SelectedIndex;
    }

    private void StartAnimation()
    {
        if (_animTimer != null) return;
        _animTimer = new System.Windows.Forms.Timer { Interval = 150 };
        _animTimer.Tick += AnimTick;
        _animTimer.Start();
    }

    private void StopAnimation()
    {
        if (_animTimer != null)
        {
            _animTimer.Stop();
            _animTimer.Dispose();
            _animTimer = null;
        }
        _animFrames = null;
        _animFrameIdx = 0;
    }

    private void AnimTick(object? sender, EventArgs e)
    {
        if (_animFrames == null || _animFrames.Length == 0) return;
        _animFrameIdx = (_animFrameIdx + 1) % _animFrames.Length;
        _icon.Image = _animFrames[_animFrameIdx];
    }

    /// <summary>Build an ItemData from the current control values.</summary>
    public ItemData BuildItemData()
    {
        // Try to parse item ID from search combo text; fall back to current item
        var searchText = _cmbItemSearch.Text;
        int itemId;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            itemId = ItemDatabase.FindIdByPartialName(searchText);
            if (itemId < 0) itemId = _currentItemId;
        }
        else
        {
            itemId = _currentItemId;
        }

        DebugLog.Log($"[ItemMod] BuildItemData: slot={_currentSlotIndex}, itemId={itemId}, stack={_cachedStack}, prefix={_cachedPrefix}, favorited={_chkFavorite.Checked}");

        return new ItemData
        {
            ItemId = itemId,
            StackSize = _cachedStack,
            Prefix = _cachedPrefix,
            Favorited = _chkFavorite.Checked
        };
    }

    /// <summary>Populate the item search combo with all items.</summary>
    public void PopulateItems()
    {
        _cmbItemSearch.Items.Clear();
        foreach (var item in ItemDatabase.GetAllItems())
            _cmbItemSearch.Items.Add(item.ToString());
    }

    /// <summary>Populate the prefix combo with localized prefix names.</summary>
    public void PopulatePrefixes()
    {
        _cmbPrefix.Items.Clear();
        foreach (var kv in PrefixData.All)
            _cmbPrefix.Items.Add(PrefixData.GetName(kv.Key));
    }

    /// <summary>Refresh display text (for language switching).</summary>
    public void RefreshLocale()
    {
        _lblStack.Text = AppLocale.Get("Inventory.Stack");
        _lblPrefix.Text = AppLocale.Get("Inventory.Prefix");
        _chkFavorite.Text = AppLocale.Get("Inventory.Favorite");
        _btnSet.Text = AppLocale.Get("Inventory.SetItem");
        _btnClear.Text = AppLocale.Get("Inventory.ClearSlot");
    }
}
