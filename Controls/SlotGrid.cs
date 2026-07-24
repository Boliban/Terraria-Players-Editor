using System.ComponentModel;
using Terraria_Players_Editor.Models;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// A grid of SlotPanel controls arranged in rows and columns.
/// Supports selection tracking, hotbar color highlighting, and batch item operations.
/// </summary>
public class SlotGrid : UserControl
{
    private readonly TableLayoutPanel _table;
    private readonly Label? _titleLabel;
    private SlotPanel[] _slots;
    private int _columns;
    private int _rows;
    private int _selectedIndex = -1;
    private bool _enableHotbarColor;
    private readonly bool _hasTitle;

    public SlotGrid(int columns, int rows, bool enableHotbarColor = false, string? gridTitle = null)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();

        _columns = columns;
        _rows = rows;
        _enableHotbarColor = enableHotbarColor;
        _hasTitle = gridTitle != null;
        int totalSlots = columns * rows;
        int cellSize = 50; // 48px slot + 2px gap
        int titleH = _hasTitle ? 16 : 0;

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // Title label at top-left (if specified)
        if (_hasTitle)
        {
            _titleLabel = new Label
            {
                Text = gridTitle,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 105),
                AutoSize = true,
                Location = new Point(2, 0)
            };
            Controls.Add(_titleLabel);
        }

        _table = new TableLayoutPanel
        {
            ColumnCount = columns,
            RowCount = rows,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
            Margin = new Padding(0),
            Location = new Point(0, titleH)
        };

        for (int c = 0; c < columns; c++)
            _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, cellSize));
        for (int r = 0; r < rows; r++)
            _table.RowStyles.Add(new RowStyle(SizeType.Absolute, cellSize));

        _slots = new SlotPanel[totalSlots];
        for (int i = 0; i < totalSlots; i++)
        {
            int col = i % columns;
            int row = i / columns;
            bool isHotbar = enableHotbarColor && row == 0;
            var slot = new SlotPanel(i, isHotbar);
            slot.Click += (s, e) => SelectSlot(slot.SlotIndex);
            slot.DoubleClick += (s, e) => SlotDoubleClicked?.Invoke(this, slot.SlotIndex);
            _slots[i] = slot;
            _table.Controls.Add(slot, col, row);
        }

        Controls.Add(_table);
    }

    /// <summary>Number of columns in the grid.</summary>
    public int Columns => _columns;

    /// <summary>Number of rows in the grid.</summary>
    public int Rows => _rows;

    /// <summary>The currently selected slot index, or -1 if none.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SelectSlot(value);
    }

    /// <summary>Whether this grid displays buffs (uses buff icons for all slots).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBuffGrid
    {
        set
        {
            foreach (var slot in _slots)
                slot.IsBuffSlot = value;
        }
    }

    /// <summary>Grid title text (shown at top-left). Set via constructor.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? GridTitle
    {
        get => _titleLabel?.Text;
        set { if (_titleLabel != null) _titleLabel.Text = value; }
    }

    /// <summary>All slot panels in this grid.</summary>
    public SlotPanel[] Slots => _slots;

    /// <summary>Fired when a slot is clicked (single click).</summary>
    public event EventHandler<int>? SlotSelected;

    /// <summary>Fired when a slot is double-clicked.</summary>
    public event EventHandler<int>? SlotDoubleClicked;

    /// <summary>Static event: fired BEFORE any grid selects a slot, so other grids can clear their selection.</summary>
    public static event Action<SlotGrid>? BeforeAnySlotSelected;

    /// <summary>Select a slot by index, deselecting the previous selection.</summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return;

        // Notify all listeners that THIS grid is about to select a slot.
        // Other grids should clear their own selection.
        BeforeAnySlotSelected?.Invoke(this);

        if (_selectedIndex >= 0 && _selectedIndex < _slots.Length)
            _slots[_selectedIndex].Selected = false;

        _selectedIndex = index;
        _slots[index].Selected = true;
        SlotSelected?.Invoke(this, index);
    }

    /// <summary>Clear selection.</summary>
    public void ClearSelection()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _slots.Length)
            _slots[_selectedIndex].Selected = false;
        _selectedIndex = -1;
    }

    /// <summary>Set all slots from a list of ItemData. Pads with empty items if list is shorter than grid.</summary>
    public void SetItems(List<ItemData> items)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            var item = i < items.Count ? items[i] : new ItemData();
            _slots[i].Item = item;
        }
    }

    /// <summary>Set a single slot's item.</summary>
    public void SetSlot(int index, ItemData item)
    {
        if (index >= 0 && index < _slots.Length)
            _slots[index].Item = item;
    }

    /// <summary>Get item data from a specific slot.</summary>
    public ItemData? GetItem(int index)
    {
        return index >= 0 && index < _slots.Length ? _slots[index].Item : null;
    }

    /// <summary>Clear all slots to empty.</summary>
    public void ClearAll()
    {
        foreach (var slot in _slots)
            slot.Clear();
        ClearSelection();
    }

    /// <summary>Refresh all slot displays (e.g., after language change).</summary>
    public void RefreshAll()
    {
        foreach (var slot in _slots)
            slot.RefreshDisplay();
    }
}
