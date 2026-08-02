using System.ComponentModel;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Left-side panel providing a searchable, filterable list of items with icons.
/// Double-clicking an item fires the ItemSelected event.
/// Supports filtering modes: All, DyeOnly, BuffOnly.
/// </summary>
public class ItemBrowser : UserControl
{
    private readonly TextBox _txtSearch;
    private readonly DataGridView _dgvItems;
    private readonly ComboBox _cmbCategory;
    private ItemFilterMode _filterMode = ItemFilterMode.All;

    // Buff-granting item categories (exact match on items.json categories)
    private static readonly HashSet<string> BuffCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Potion", "Consumable"
    };

    public ItemBrowser()
    {
        Dock = DockStyle.Fill;
        // Leave 1px for the theme-aware border painted in OnPaint
        // (DataGridView BorderStyle.FixedSingle is system-drawn and
        // stays white in dark mode regardless of the app theme).
        Padding = new Padding(1);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // Search
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // Category
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Grid

        // Search box
        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search items..."
        };
        _txtSearch.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilter();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        // Category filter
        _cmbCategory = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbCategory.SelectedIndexChanged += (s, e) => ApplyFilter();

        // Item list grid
        _dgvItems = new DataGridView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None, // themed border is painted by the parent ItemBrowser
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            ScrollBars = ScrollBars.Vertical,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = ThemeManager.SurfaceCard,
            RowTemplate = { Height = 32 }
        };
        // Enable double buffering on DataGridView to reduce flicker
        var dgvProp = typeof(DataGridView).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dgvProp?.SetValue(_dgvItems, true);

        var iconCol = new DataGridViewImageColumn
        {
            Name = "Icon",
            Width = 32,
            ImageLayout = DataGridViewImageCellLayout.Normal,
            Resizable = DataGridViewTriState.False,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        var nameCol = new DataGridViewTextBoxColumn
        {
            Name = "Name",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 200
        };
        var idCol = new DataGridViewTextBoxColumn
        {
            Name = "ID",
            Width = 55,
            HeaderText = "ID",
            Resizable = DataGridViewTriState.False,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        _dgvItems.Columns.Add(iconCol);
        _dgvItems.Columns.Add(nameCol);
        _dgvItems.Columns.Add(idCol);

        // Dynamically resize Name column to fill remaining space.
        // Avoids DataGridViewAutoSizeColumnMode.Fill which causes a
        // layout deadlock with the vertical scrollbar in nested containers.
        _dgvItems.SizeChanged += (s, e) =>
        {
            int fixedWidth = iconCol.Width + idCol.Width;
            // Check if vertical scrollbar is needed by comparing row count against visible rows
            int visibleRows = _dgvItems.ClientSize.Height /
                (_dgvItems.RowTemplate.Height > 0 ? _dgvItems.RowTemplate.Height : 32);
            bool needsScrollbar = _dgvItems.RowCount > visibleRows;
            int scrollbarW = needsScrollbar ? SystemInformation.VerticalScrollBarWidth : 0;
            int newNameWidth = _dgvItems.ClientSize.Width - fixedWidth - scrollbarW - 2;
            if (newNameWidth > 50)
                nameCol.Width = newNameWidth;
        };

        _dgvItems.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _dgvItems.Rows.Count)
            {
                var row = _dgvItems.Rows[e.RowIndex];
                if (row.Tag is int itemId)
                    ItemSelected?.Invoke(this, itemId);
            }
        };

        // Forward mouse wheel from this UserControl to the DataGridView.
        // WinForms UserControl does not natively pass WM_MOUSEWHEEL to child controls.
        MouseWheel += (s, e) =>
        {
            if (_dgvItems.RowCount == 0 || _dgvItems.Rows.Count == 0) return;

            int scrollLinesPerDetent = SystemInformation.MouseWheelScrollLines;
            int detents = e.Delta / 120;
            int currentRow = _dgvItems.FirstDisplayedScrollingRowIndex;
            if (currentRow < 0) currentRow = 0;
            int newRow = currentRow - (detents * scrollLinesPerDetent);
            newRow = Math.Max(0, Math.Min(newRow, _dgvItems.Rows.Count - 1));

            // Skip past invisible rows (filtered out)
            while (newRow > 0 && !_dgvItems.Rows[newRow].Visible)
                newRow--;
            while (newRow < _dgvItems.Rows.Count - 1 && !_dgvItems.Rows[newRow].Visible)
                newRow++;

            if (newRow != currentRow && _dgvItems.Rows[newRow].Visible)
            {
                _dgvItems.FirstDisplayedScrollingRowIndex = newRow;
                ((HandledMouseEventArgs)e).Handled = true;
            }
        };

        // Ensure the DataGridView gets focus when the mouse enters its area,
        // so that native scrolling and keyboard navigation work intuitively.
        _dgvItems.MouseEnter += (s, e) => _dgvItems.Focus();

        layout.Controls.Add(_txtSearch, 0, 0);
        layout.Controls.Add(_cmbCategory, 0, 1);
        layout.Controls.Add(_dgvItems, 0, 2);
        Controls.Add(layout);

        // Apply theme colors to DataGridView
        ApplyDgvTheme();
        _dgvItems.EnableHeadersVisualStyles = false;
        ThemeManager.ThemeChanged += () => { ApplyDgvTheme(); RefreshBuffRowColors(); _dgvItems.Invalidate(); Invalidate(); };

        // Refresh scrollbar when this control becomes visible (tab switch, etc.)
        VisibleChanged += (s, e) =>
        {
            if (Visible && _dgvItems.RowCount > 0)
            {
                _dgvItems.ScrollBars = ScrollBars.None;
                _dgvItems.ScrollBars = ScrollBars.Vertical;
            }
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Win11Renderer.DrawThemedBorder(e.Graphics, this);
    }

    /// <summary>Filter mode: show all items, dyes only, or buffs only.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ItemFilterMode FilterMode
    {
        get => _filterMode;
        set
        {
            _filterMode = value;
            LoadItems();
        }
    }

    /// <summary>Fired when the user double-clicks an item in the list.</summary>
    public event EventHandler<int>? ItemSelected;

    /// <summary>Load all items from ItemDatabase into the list.</summary>
    public void LoadItems()
    {
        // Collect data first, then populate in one batch to ensure scrollbar is correct.
        // Using RowCount pre-allocation avoids SuspendLayout/ResumeLayout which can
        // prevent the DataGridView from properly calculating its scrollbar visibility.

        if (_filterMode == ItemFilterMode.BuffOnly)
        {
            var buffIds = BuffDatabase.GetAllIds();
            var buffData = new List<(int id, string name, string type, Bitmap icon)>();
            foreach (var buffId in buffIds)
            {
                if (buffId <= 0) continue;
                buffData.Add((buffId, BuffDatabase.GetName(buffId),
                    BuffDatabase.GetType(buffId),
                    IconService.GetBuffIcon(buffId) ?? IconService.DefaultIcon));
            }

            _dgvItems.Rows.Clear();
            _dgvItems.RowCount = buffData.Count;

            for (int i = 0; i < buffData.Count; i++)
            {
                var d = buffData[i];
                var row = _dgvItems.Rows[i];
                row.Cells[0].Value = d.icon;
                row.Cells[1].Value = d.name;
                row.Cells[2].Value = d.id;
                row.Tag = d.id;
                row.DefaultCellStyle.ForeColor = d.type.Equals("Debuff", StringComparison.OrdinalIgnoreCase)
                    ? ThemeManager.DebuffText
                    : ThemeManager.BuffText;
            }

            // Populate category filter: All, Buff, Debuff
            _cmbCategory.Items.Clear();
            _cmbCategory.Items.Add(AppLocale.Get("Browser.All") ?? "All");
            _cmbCategory.Items.Add(AppLocale.Get("Browser.Buff") ?? "Buff");
            _cmbCategory.Items.Add(AppLocale.Get("Browser.Debuff") ?? "Debuff");
            _cmbCategory.SelectedIndex = 0;
            // Force scrollbar recalculation after bulk row loading
            _dgvItems.ScrollBars = ScrollBars.None;
            _dgvItems.ScrollBars = ScrollBars.Vertical;
            return;
        }

        var itemCategories = new HashSet<string> { "All" };
        var allItems = ItemDatabase.GetAllItems();
        var itemData = new List<(int id, string name, string cat, Bitmap icon)>();

        foreach (var item in allItems)
        {
            var cat = ItemDatabase.GetCategory(item.Id);
            if (_filterMode == ItemFilterMode.DyeOnly)
            {
                if (!cat.Equals("Dye", StringComparison.OrdinalIgnoreCase)) continue;
            }
            itemData.Add((item.Id, item.ToString(), cat,
                IconService.GetItemIcon(item.Id) ?? IconService.DefaultIcon));
            if (!string.IsNullOrEmpty(cat) && cat != "None")
                itemCategories.Add(cat);
        }

        _dgvItems.Rows.Clear();
        _dgvItems.RowCount = itemData.Count;

        for (int i = 0; i < itemData.Count; i++)
        {
            var d = itemData[i];
            var row = _dgvItems.Rows[i];
            row.Cells[0].Value = d.icon;
            row.Cells[1].Value = d.name;
            row.Cells[2].Value = d.id;
            row.Tag = d.id;
        }

        // Populate category filter
        _cmbCategory.Items.Clear();
        _cmbCategory.Items.Add(AppLocale.Get("Browser.All") ?? "All");
        _cmbCategory.SelectedIndex = 0;

        // Force layout update so scrollbar appears immediately
        // Force scrollbar recalculation after bulk row loading
            _dgvItems.ScrollBars = ScrollBars.None;
            _dgvItems.ScrollBars = ScrollBars.Vertical;
    }

    /// <summary>Reload items and apply current text filter.</summary>
    public void RefreshItems()
    {
        LoadItems();
        ApplyFilter();
    }

    /// <summary>Refresh only display text (for language switching) without rebuilding rows.</summary>
    public void RefreshDisplayText()
    {
        // Refresh category dropdown locale text
        if (_filterMode == ItemFilterMode.BuffOnly && _cmbCategory.Items.Count >= 3)
        {
            int sel = _cmbCategory.SelectedIndex;
            _cmbCategory.Items[0] = AppLocale.Get("Browser.All") ?? "All";
            _cmbCategory.Items[1] = AppLocale.Get("Browser.Buff") ?? "Buff";
            _cmbCategory.Items[2] = AppLocale.Get("Browser.Debuff") ?? "Debuff";
            _cmbCategory.SelectedIndex = sel;
        }

        // Refresh row display text
        foreach (DataGridViewRow row in _dgvItems.Rows)
        {
            if (row.Tag is int itemId)
            {
                if (_filterMode == ItemFilterMode.BuffOnly)
                    row.Cells[1].Value = BuffDatabase.GetName(itemId);
                else
                    row.Cells[1].Value = ItemDatabase.GetName(itemId);
            }
        }
    }

    /// <summary>Apply theme colors to the DataGridView.</summary>
    private void ApplyDgvTheme()
    {
        _dgvItems.BackgroundColor = ThemeManager.SurfaceCard;
        _dgvItems.DefaultCellStyle.BackColor = ThemeManager.SurfaceCard;
        _dgvItems.DefaultCellStyle.ForeColor = ThemeManager.TextPrimary;
        _dgvItems.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.SurfaceBackground;
        _dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.TextPrimary;
        _dgvItems.GridColor = ThemeManager.ControlInputBorder;
    }

    /// <summary>Refresh per-row buff/debuff colors when theme changes.</summary>
    private void RefreshBuffRowColors()
    {
        if (_filterMode != ItemFilterMode.BuffOnly) return;
        foreach (DataGridViewRow row in _dgvItems.Rows)
        {
            if (row.Tag is int buffId)
            {
                var type = BuffDatabase.GetType(buffId);
                row.DefaultCellStyle.ForeColor = type.Equals("Debuff", StringComparison.OrdinalIgnoreCase)
                    ? ThemeManager.DebuffText
                    : ThemeManager.BuffText;
            }
        }
    }

    private void ApplyFilter()
    {
        var query = _txtSearch.Text.Trim();

        // Category filter: 0 = All, 1 = Buff, 2 = Debuff (uses index to avoid locale text mismatch)
        int categoryFilter = _filterMode == ItemFilterMode.BuffOnly ? _cmbCategory.SelectedIndex : 0;

        bool hasTextFilter = !string.IsNullOrEmpty(query);
        bool hasCategoryFilter = categoryFilter > 0;

        if (!hasTextFilter && !hasCategoryFilter)
        {
            // Clear all filters — show all rows and reset scroll position
            foreach (DataGridViewRow row in _dgvItems.Rows)
                row.Visible = true;
            if (_dgvItems.Rows.Count > 0)
                _dgvItems.FirstDisplayedScrollingRowIndex = 0;
            return;
        }

        bool isNumeric = int.TryParse(query, out int numericQuery);
        foreach (DataGridViewRow row in _dgvItems.Rows)
        {
            bool visible = true;

            // Category filter: compare against English type names ("Buff"/"Debuff") from database
            if (hasCategoryFilter && row.Tag is int rowId)
            {
                var rowType = BuffDatabase.GetType(rowId);
                if (categoryFilter == 1)
                    visible = rowType.Equals("Buff", StringComparison.OrdinalIgnoreCase);
                else if (categoryFilter == 2)
                    visible = rowType.Equals("Debuff", StringComparison.OrdinalIgnoreCase);
            }

            // Text filter
            if (visible && hasTextFilter)
            {
                if (isNumeric)
                {
                    if (row.Tag is int id && id == numericQuery)
                        visible = true;
                    else
                    {
                        var text = row.Cells[1].Value?.ToString() ?? "";
                        visible = text.Contains(query, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    var text = row.Cells[1].Value?.ToString() ?? "";
                    visible = text.Contains(query, StringComparison.OrdinalIgnoreCase);
                }
            }

            row.Visible = visible;
        }
    }
}

/// <summary>Filter mode for item browser.</summary>
public enum ItemFilterMode
{
    All,
    DyeOnly,
    BuffOnly
}
