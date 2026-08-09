using System.ComponentModel;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Left-side panel providing a searchable, filterable list of items with icons.
/// Double-clicking an item fires the ItemSelected event.
/// Supports filtering modes: All, DyeOnly, BuffOnly.
/// Supports two view modes: details rows (icon | name | ID) and a large-icon
/// card grid (icon on top, wrapped name and ID below).
/// </summary>
public class ItemBrowser : UserControl
{
    private readonly TextBox _txtSearch;
    private readonly DataGridView _dgvItems;
    private readonly ComboBox _cmbCategory;
    private ItemFilterMode _filterMode = ItemFilterMode.All;

    // Large-icon mode state
    private BrowserViewMode _viewMode = SettingsManager.BrowserViewMode;
    private int _iconSize = SettingsManager.BrowserIconSize;
    private int _cardColumns;        // number of card columns currently built
    private int _columnsIconSize;    // icon size the current card columns were built with
    private bool _columnsBuffMode;   // whether the current card columns render buffs (IsBuffMode)
    private bool _loading;           // suppresses ApplyFilter reentrancy during LoadItems
    private bool _inLayout;          // suppresses SizeChanged reentrancy during column rebuild
    private List<int> _flatIds = new();
    private List<int> _filteredIds = new();

    // Details-mode columns (instance fields: recreated on every mode switch)
    private DataGridViewImageColumn? _iconCol;
    private DataGridViewTextBoxColumn? _nameCol;
    private DataGridViewTextBoxColumn? _idCol;

    // Item-mode category keys, parallel to _cmbCategory.Items indices
    // (index 0 = "All"). Buff mode keeps its index-based All/Buff/Debuff logic.
    private readonly List<string> _itemCategoryKeys = new();

    // Fixed display order of the curated item taxonomy (categories.json keys)
    private static readonly string[] ItemCategoryOrder =
    {
        "Weapon", "Armor", "Accessory", "Tool", "Ammo", "Potion", "Consumable",
        "Material", "Block", "Wall", "Furniture", "Dye", "Mount", "Vanity", "Misc"
    };

    private const int MaxCards = 32;
    private const int MinColumnWidth = 60;
    private int CardWidth => Math.Max(88, _iconSize + 56);
    private int CardRowHeight => 26 + _iconSize + 3 * _dgvItems.Font.Height;

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

        EnsureDetailsColumns();
        // Startup may land directly in large-icon mode (saved setting): apply the
        // mode-specific grid visuals (headers, selection, row height) right away.
        ApplyModeVisuals();

        // Dynamically resize Name column to fill remaining space (details mode),
        // or recompute the card column layout (large-icon mode).
        // Avoids DataGridViewAutoSizeColumnMode.Fill which causes a
        // layout deadlock with the vertical scrollbar in nested containers.
        _dgvItems.SizeChanged += (s, e) =>
        {
            if (_viewMode == BrowserViewMode.LargeIcons)
            {
                RecomputeGridLayout();
                return;
            }
            ResizeDetailsColumns();
        };

        _dgvItems.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _dgvItems.Rows.Count)
            {
                if (_viewMode == BrowserViewMode.LargeIcons)
                {
                    // Each cell of a card row is one item; empty slots have no Tag.
                    var cell = _dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    if (cell.Tag is int itemId)
                        ItemSelected?.Invoke(this, itemId);
                }
                else
                {
                    var row = _dgvItems.Rows[e.RowIndex];
                    if (row.Tag is int itemId)
                        ItemSelected?.Invoke(this, itemId);
                }
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

            // Skip past invisible rows (filtered out); no-ops in large-icon mode
            // where filtering rebuilds rows instead of toggling visibility.
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

    /// <summary>
    /// Switch between details rows and the large-icon card grid, and set the card size.
    /// No-op when neither changed.
    /// </summary>
    public void SetView(BrowserViewMode mode, int iconSize)
    {
        if (_viewMode == mode && _iconSize == iconSize) return;
        _viewMode = mode;
        _iconSize = iconSize;
        ApplyModeVisuals();
        RefreshItems();
    }

    /// <summary>Apply grid-level visuals for the current view mode.</summary>
    private void ApplyModeVisuals()
    {
        if (_viewMode == BrowserViewMode.LargeIcons)
        {
            _dgvItems.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _dgvItems.ColumnHeadersVisible = false;
            _dgvItems.RowTemplate.Height = CardRowHeight;
        }
        else
        {
            _dgvItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvItems.ColumnHeadersVisible = true;
            _dgvItems.RowTemplate.Height = 32;
        }
    }

    /// <summary>Load all items from ItemDatabase into the list.</summary>
    public void LoadItems()
    {
        // Collect data first, then populate in one batch to ensure scrollbar is correct.
        // Using RowCount pre-allocation avoids SuspendLayout/ResumeLayout which can
        // prevent the DataGridView from properly calculating its scrollbar visibility.
        _loading = true;
        try
        {
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

                // Populate category filter: All, Buff, Debuff (index-based)
                _cmbCategory.Items.Clear();
                _itemCategoryKeys.Clear();
                _cmbCategory.Items.Add(AppLocale.Get("Browser.All") ?? "All");
                _cmbCategory.Items.Add(AppLocale.Get("Browser.Buff") ?? "Buff");
                _cmbCategory.Items.Add(AppLocale.Get("Browser.Debuff") ?? "Debuff");
                _cmbCategory.SelectedIndex = 0;

                if (_viewMode == BrowserViewMode.LargeIcons)
                {
                    _flatIds.Clear();
                    foreach (var d in buffData) _flatIds.Add(d.id);
                    _filteredIds = new List<int>(_flatIds);
                    EnsureLargeColumns();
                    RebuildChunkedRows();
                    return;
                }

                EnsureDetailsColumns();
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

                // Force scrollbar recalculation after bulk row loading
                _dgvItems.ScrollBars = ScrollBars.None;
                _dgvItems.ScrollBars = ScrollBars.Vertical;
                ResizeDetailsColumns();
                return;
            }

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
            }

            // Populate category filter: All + the curated taxonomy, localized,
            // in a fixed order. Keys live in _itemCategoryKeys (parallel to indices).
            _cmbCategory.Items.Clear();
            _itemCategoryKeys.Clear();
            _cmbCategory.Items.Add(AppLocale.Get("Browser.All") ?? "All");
            _itemCategoryKeys.Add("All");
            foreach (var key in ItemCategoryOrder)
            {
                _cmbCategory.Items.Add(AppLocale.Get("Browser.Cat." + key) ?? key);
                _itemCategoryKeys.Add(key);
            }
            _cmbCategory.SelectedIndex = 0;

            if (_viewMode == BrowserViewMode.LargeIcons)
            {
                _flatIds.Clear();
                foreach (var d in itemData) _flatIds.Add(d.id);
                _filteredIds = new List<int>(_flatIds);
                EnsureLargeColumns();
                RebuildChunkedRows();
                return;
            }

            EnsureDetailsColumns();
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

            // Force layout update so scrollbar appears immediately
            // Force scrollbar recalculation after bulk row loading
            _dgvItems.ScrollBars = ScrollBars.None;
            _dgvItems.ScrollBars = ScrollBars.Vertical;
            ResizeDetailsColumns();
        }
        finally
        {
            _loading = false;
        }

        // Preserve the original behavior where the category combo's SelectedIndex=0
        // fired ApplyFilter after a reload: re-apply the active search/filter now.
        ApplyFilter();
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
        else if (_filterMode != ItemFilterMode.BuffOnly && _cmbCategory.Items.Count == _itemCategoryKeys.Count)
        {
            int sel = _cmbCategory.SelectedIndex;
            for (int i = 0; i < _itemCategoryKeys.Count; i++)
            {
                var key = _itemCategoryKeys[i];
                _cmbCategory.Items[i] = key == "All"
                    ? (AppLocale.Get("Browser.All") ?? "All")
                    : (AppLocale.Get("Browser.Cat." + key) ?? key);
            }
            _cmbCategory.SelectedIndex = sel;
        }

        if (_viewMode == BrowserViewMode.LargeIcons)
        {
            // Each card cell holds one item id in its Tag
            foreach (DataGridViewRow row in _dgvItems.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Tag is int itemId)
                        cell.Value = GetDisplayName(itemId);
                }
            }
            return;
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
        // In large-icon mode colors are resolved at paint time from each card's id
        if (_viewMode == BrowserViewMode.LargeIcons) return;
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

    /// <summary>Build the three details-mode columns (recreated whenever the grid holds card columns).</summary>
    private void EnsureDetailsColumns()
    {
        if (_dgvItems.Columns.Count > 0 && _dgvItems.Columns[0] is DataGridViewImageColumn)
            return;

        // The grid now holds card columns (or none): invalidate the large-mode
        // column state so the next EnsureLargeColumns() forces a rebuild.
        _cardColumns = 0;
        _columnsIconSize = 0;
        _columnsBuffMode = false;

        _iconCol = new DataGridViewImageColumn
        {
            Name = "Icon",
            Width = 32,
            ImageLayout = DataGridViewImageCellLayout.Normal,
            Resizable = DataGridViewTriState.False,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        _nameCol = new DataGridViewTextBoxColumn
        {
            Name = "Name",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 200
        };
        _idCol = new DataGridViewTextBoxColumn
        {
            Name = "ID",
            Width = 55,
            HeaderText = "ID",
            Resizable = DataGridViewTriState.False,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        _dgvItems.Columns.Clear();
        _dgvItems.Columns.Add(_iconCol);
        _dgvItems.Columns.Add(_nameCol);
        _dgvItems.Columns.Add(_idCol);
    }

    /// <summary>
    /// Resize the details Name column to fill the remaining grid width.
    /// Called from SizeChanged and after every details-mode rebuild so the
    /// column width follows the current panel width immediately.
    /// </summary>
    private void ResizeDetailsColumns()
    {
        if (_iconCol == null || _nameCol == null || _idCol == null) return;
        int fixedWidth = _iconCol.Width + _idCol.Width;
        // Check if vertical scrollbar is needed by comparing row count against visible rows
        int visibleRows = _dgvItems.ClientSize.Height /
            (_dgvItems.RowTemplate.Height > 0 ? _dgvItems.RowTemplate.Height : 32);
        bool needsScrollbar = _dgvItems.RowCount > visibleRows;
        int scrollbarW = needsScrollbar ? SystemInformation.VerticalScrollBarWidth : 0;
        int newNameWidth = _dgvItems.ClientSize.Width - fixedWidth - scrollbarW - 2;
        if (newNameWidth > 50)
            _nameCol.Width = newNameWidth;
    }

    /// <summary>Ensure the card columns exist for the current width, icon size, and buff mode.</summary>
    private void EnsureLargeColumns()
    {
        if (_cardColumns == 0 || _columnsIconSize != _iconSize
            || _columnsBuffMode != (_filterMode == ItemFilterMode.BuffOnly))
            RecomputeGridLayout();
    }

    /// <summary>
    /// Recompute the card grid layout (column count and widths) for the current grid width.
    /// Rebuilds rows only when the column count or icon size actually changed.
    /// </summary>
    private void RecomputeGridLayout()
    {
        if (_inLayout || _viewMode != BrowserViewMode.LargeIcons) return;

        // Always reserve the scrollbar width: a heuristic here could oscillate
        // (scrollbar appears -> width shrinks -> fewer cards fit -> ... -> repeat)
        int avail = _dgvItems.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2;
        int k = Math.Clamp(avail / CardWidth, 1, MaxCards);
        int colW = Math.Max(MinColumnWidth, avail / k);
        int remainder = Math.Max(0, avail - colW * k);

        bool buffMode = _filterMode == ItemFilterMode.BuffOnly;

        // Width-only change: adjust existing columns and keep rows untouched.
        // A buff-mode mismatch (e.g. columns built by an early SizeChanged before
        // FilterMode was set) forces a full rebuild so cards render buff icons.
        if (_cardColumns == k && _columnsIconSize == _iconSize
            && _dgvItems.Columns.Count == k && _columnsBuffMode == buffMode)
        {
            for (int c = 0; c < k; c++)
                _dgvItems.Columns[c].Width = colW + (c == k - 1 ? remainder : 0);
            return;
        }

        // Anchor the top-left card's flat index so resize drags don't jump.
        // Only anchor when the icon size is unchanged (a size change resets to top).
        int anchor = _cardColumns > 0 && _columnsIconSize == _iconSize && _dgvItems.RowCount > 0
            ? Math.Max(0, _dgvItems.FirstDisplayedScrollingRowIndex) * _cardColumns
            : 0;

        _inLayout = true;
        try
        {
            _cardColumns = k;
            _columnsIconSize = _iconSize;
            _columnsBuffMode = buffMode;
            _dgvItems.Columns.Clear();
            for (int c = 0; c < k; c++)
            {
                _dgvItems.Columns.Add(new CardColumn(_iconSize, buffMode)
                {
                    Width = colW + (c == k - 1 ? remainder : 0)
                });
            }
            RebuildChunkedRows();
        }
        finally
        {
            _inLayout = false;
        }

        // Restore scroll position so the anchored card stays in view
        if (anchor > 0 && _dgvItems.RowCount > 0)
        {
            int newRow = Math.Min(anchor / k, _dgvItems.RowCount - 1);
            _dgvItems.FirstDisplayedScrollingRowIndex = newRow;
        }
    }

    /// <summary>Fill the grid rows from _filteredIds, chunking _cardColumns cards per row.</summary>
    private void RebuildChunkedRows()
    {
        if (_cardColumns <= 0) return;

        int n = _filteredIds.Count;
        int rows = (n + _cardColumns - 1) / _cardColumns;
        _dgvItems.Rows.Clear();
        _dgvItems.RowCount = rows;

        for (int i = 0; i < n; i++)
        {
            var cell = _dgvItems.Rows[i / _cardColumns].Cells[i % _cardColumns];
            cell.Value = GetDisplayName(_filteredIds[i]);
            cell.Tag = _filteredIds[i];
        }
        // Trailing cells of a partial last row keep Value/Tag null (empty slots).

        // Force scrollbar recalculation after bulk row loading
        _dgvItems.ScrollBars = ScrollBars.None;
        _dgvItems.ScrollBars = ScrollBars.Vertical;

        if (rows > 0)
        {
            _dgvItems.CurrentCell = _dgvItems.Rows[0].Cells[0];
            _dgvItems.FirstDisplayedScrollingRowIndex = 0;
        }
    }

    /// <summary>Locale-aware display name for an item or buff id.</summary>
    private string GetDisplayName(int id) =>
        _filterMode == ItemFilterMode.BuffOnly ? BuffDatabase.GetName(id) : ItemDatabase.GetName(id);

    private void ApplyFilter()
    {
        if (_loading) return;

        var query = _txtSearch.Text.Trim();

        // Buff mode: 0 = All, 1 = Buff, 2 = Debuff (index-based).
        // Item mode: the selected category key from the parallel list.
        int categoryFilter = _filterMode == ItemFilterMode.BuffOnly ? _cmbCategory.SelectedIndex : 0;
        string? itemCategoryKey = null;
        if (_filterMode != ItemFilterMode.BuffOnly)
        {
            int idx = _cmbCategory.SelectedIndex;
            itemCategoryKey = idx > 0 && idx < _itemCategoryKeys.Count ? _itemCategoryKeys[idx] : null;
        }

        bool hasTextFilter = !string.IsNullOrEmpty(query);
        bool hasCategoryFilter = categoryFilter > 0 || itemCategoryKey != null;

        if (_viewMode == BrowserViewMode.LargeIcons)
        {
            ApplyFilterLarge(query, categoryFilter, itemCategoryKey, hasTextFilter, hasCategoryFilter);
            return;
        }

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

            // Category filter (buff mode: index-based Buff/Debuff;
            // item mode: category key compared against the curated taxonomy)
            if (hasCategoryFilter && row.Tag is int rowId)
            {
                if (_filterMode == ItemFilterMode.BuffOnly)
                {
                    var rowType = BuffDatabase.GetType(rowId);
                    if (categoryFilter == 1)
                        visible = rowType.Equals("Buff", StringComparison.OrdinalIgnoreCase);
                    else if (categoryFilter == 2)
                        visible = rowType.Equals("Debuff", StringComparison.OrdinalIgnoreCase);
                }
                else if (itemCategoryKey != null)
                {
                    visible = ItemDatabase.GetCategory(rowId)
                        .Equals(itemCategoryKey, StringComparison.OrdinalIgnoreCase);
                }
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

    /// <summary>
    /// Large-icon mode filter: filter the flat item list, then re-chunk the matches
    /// into card rows (a single row holds several items, so row.Visible can't express
    /// per-card visibility).
    /// </summary>
    private void ApplyFilterLarge(string query, int categoryFilter, string? itemCategoryKey,
        bool hasTextFilter, bool hasCategoryFilter)
    {
        if (!hasTextFilter && !hasCategoryFilter)
        {
            _filteredIds = new List<int>(_flatIds);
            RebuildChunkedRows();
            return;
        }

        bool isNumeric = int.TryParse(query, out int numericQuery);
        _filteredIds = new List<int>(_flatIds.Count);
        foreach (int id in _flatIds)
        {
            if (PassesFilter(id, GetDisplayName(id), query, isNumeric, numericQuery, categoryFilter, itemCategoryKey))
                _filteredIds.Add(id);
        }
        RebuildChunkedRows();
    }

    /// <summary>Shared per-item filter semantics: category (buffs) plus numeric-ID / name substring.</summary>
    private bool PassesFilter(int id, string name, string query, bool isNumeric, int numericQuery,
        int categoryFilter, string? itemCategoryKey)
    {
        if (categoryFilter > 0 && _filterMode == ItemFilterMode.BuffOnly)
        {
            var rowType = BuffDatabase.GetType(id);
            if (categoryFilter == 1 && !rowType.Equals("Buff", StringComparison.OrdinalIgnoreCase)) return false;
            if (categoryFilter == 2 && !rowType.Equals("Debuff", StringComparison.OrdinalIgnoreCase)) return false;
        }
        if (itemCategoryKey != null && _filterMode != ItemFilterMode.BuffOnly)
        {
            if (!ItemDatabase.GetCategory(id).Equals(itemCategoryKey, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (string.IsNullOrEmpty(query)) return true;
        if (isNumeric && id == numericQuery) return true;
        return name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Filter mode for item browser.</summary>
public enum ItemFilterMode
{
    All,
    DyeOnly,
    BuffOnly
}

/// <summary>
/// DataGridView column whose cells render a large-icon card:
/// icon on top, wrapped name (by word / CJK character) and ID below.
/// </summary>
public sealed class CardColumn : DataGridViewTextBoxColumn
{
    public CardColumn(int iconSize, bool isBuffMode)
    {
        CellTemplate = new CardCell { IconSize = iconSize, IsBuffMode = isBuffMode };
        Resizable = DataGridViewTriState.False;
        AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        SortMode = DataGridViewColumnSortMode.NotSortable;
    }
}

/// <summary>Owner-drawn card cell for the large-icon browser mode.</summary>
public sealed class CardCell : DataGridViewTextBoxCell
{
    /// <summary>Icon display size in pixels (32, 48, or 64).</summary>
    public int IconSize { get; set; } = 48;

    /// <summary>Whether this card renders a buff (buff/debuff text colors).</summary>
    public bool IsBuffMode { get; set; }

    private int _memoId = -1;
    private Bitmap? _memoIcon;
    private bool _memoOwned;

    public override object Clone()
    {
        var clone = (CardCell)base.Clone();
        clone.IconSize = IconSize;
        clone.IsBuffMode = IsBuffMode;
        return clone;
    }

    private Bitmap GetScaledIcon(int id)
    {
        if (_memoId == id && _memoIcon != null) return _memoIcon;

        var src = IsBuffMode ? IconService.GetBuffIcon(id) : IconService.GetItemIcon(id);
        src ??= IconService.DefaultIcon;

        // Only dispose bitmaps we allocated — the 32px exact match is IconService's
        // shared cache bitmap and must not be disposed.
        if (_memoOwned)
            _memoIcon?.Dispose();
        _memoId = id;
        if (src.Width == IconSize && src.Height == IconSize)
        {
            _memoIcon = src;
            _memoOwned = false;
        }
        else
        {
            _memoIcon = IconService.ScaleContain(src, IconSize, IconSize);
            _memoOwned = true;
        }
        return _memoIcon;
    }

    protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
        int rowIndex, DataGridViewElementStates cellState, object? value, object? formattedValue,
        string? errorText, DataGridViewCellStyle cellStyle,
        DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
    {
        // The grid does not guarantee a per-cell clip region
        if (!cellBounds.IntersectsWith(clipBounds)) return;

        bool selected = (cellState & DataGridViewElementStates.Selected) != 0;
        bool focused = DataGridView?.Focused == true && DataGridView.CurrentCell == this;

        // Card background
        graphics.FillRectangle(new SolidBrush(ThemeManager.SurfaceCard), cellBounds);

        var card = Rectangle.Inflate(cellBounds, -2, -2);
        var bg = selected ? cellStyle.SelectionBackColor : ThemeManager.SurfaceContainer;
        using (var fill = new SolidBrush(bg))
            graphics.FillRectangle(fill, card);
        if (focused)
        {
            using var pen = new Pen(ThemeManager.AccentPrimary);
            graphics.DrawRectangle(pen, card.X, card.Y, card.Width - 1, card.Height - 1);
        }

        // Empty slot in a partial last row: background only
        if (Tag is not int id) return;

        // Icon, centered at the top of the card (crisp NearestNeighbor pixel art)
        var icon = GetScaledIcon(id);
        int iconX = card.X + (card.Width - icon.Width) / 2;
        int iconY = card.Y + 6;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        graphics.DrawImage(icon, iconX, iconY, icon.Width, icon.Height);

        // Name: wrapped (Latin by word, CJK by character), centered, max 2 lines
        var nameColor = selected ? cellStyle.SelectionForeColor
            : IsBuffMode
                ? (BuffDatabase.GetType(id).Equals("Debuff", StringComparison.OrdinalIgnoreCase)
                    ? ThemeManager.DebuffText
                    : ThemeManager.BuffText)
                : ThemeManager.TextPrimary;

        var font = cellStyle.Font ?? DataGridView?.Font ?? SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        int textX = card.X + 4;
        int textW = card.Width - 8;
        int nameTop = iconY + icon.Height + 4;
        var nameRect = new Rectangle(textX, nameTop, textW, 2 * font.Height);
        DrawCardName(graphics, value?.ToString() ?? "", nameRect, nameColor, bg, font);

        // ID below the name
        var idRect = new Rectangle(textX, nameTop + 2 * font.Height, textW, font.Height);
        var idColor = selected ? cellStyle.SelectionForeColor : ThemeManager.TextSecondary;
        TextRenderer.DrawText(graphics, id.ToString(), font, idRect, idColor, bg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix |
            TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
    }

    /// <summary>
    /// Draw the name with WordBreak wrapping (Latin by word, CJK per character) and a
    /// 2-line budget. Overflow is truncated with the localized ellipsis — the GDI
    /// WordBreak|EndEllipsis combination is unreliable, so we measure-then-draw.
    /// </summary>
    private void DrawCardName(Graphics g, string name, Rectangle rect, Color color, Color backColor, Font font)
    {
        var flags = TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        var maxSize = new Size(rect.Width, 10000);
        var m = TextRenderer.MeasureText(g, name, font, maxSize, flags);

        string text = name;
        if (m.Height > rect.Height)
        {
            var ellipsis = AppLocale.Get("UI.Ellipsis") ?? "…";
            string prefix = "";
            foreach (char ch in name)
            {
                string test = prefix + ch + ellipsis;
                if (TextRenderer.MeasureText(g, test, font, maxSize, flags).Height > rect.Height)
                    break;
                prefix += ch;
            }
            text = prefix + ellipsis;
            m = TextRenderer.MeasureText(g, text, font, maxSize, flags);
        }

        // Center the wrapped block vertically within the 2-line budget
        var drawRect = new Rectangle(rect.X, rect.Y + Math.Max(0, (rect.Height - m.Height) / 2),
            rect.Width, m.Height);
        TextRenderer.DrawText(g, text, font, drawRect, color, backColor, flags);
    }
}
