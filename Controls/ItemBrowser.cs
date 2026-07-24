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
    private readonly System.Windows.Forms.Timer _searchDebounce;
    private ItemFilterMode _filterMode = ItemFilterMode.All;

    // Buff-granting item categories (exact match on items.json categories)
    private static readonly HashSet<string> BuffCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Potion", "Consumable"
    };

    public ItemBrowser()
    {
        Dock = DockStyle.Fill;

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
        _txtSearch.TextChanged += OnSearchTextChanged;

        // Search debounce timer (300ms delay)
        _searchDebounce = new System.Windows.Forms.Timer { Interval = 400 };
        _searchDebounce.Tick += (s, e) => { _searchDebounce.Stop(); ApplyFilter(); };

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
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            ScrollBars = ScrollBars.Vertical,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = SystemColors.Window,
            RowTemplate = { Height = 34 }
        };
        // Enable double buffering on DataGridView to reduce flicker
        var dgvProp = typeof(DataGridView).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dgvProp?.SetValue(_dgvItems, true);

        var iconCol = new DataGridViewImageColumn
        {
            Name = "Icon",
            Width = 34,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            Resizable = DataGridViewTriState.False,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        var nameCol = new DataGridViewTextBoxColumn
        {
            Name = "Name",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
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

            if (newRow != currentRow)
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
        _dgvItems.Rows.Clear();
        _dgvItems.SuspendLayout();

        if (_filterMode == ItemFilterMode.BuffOnly)
        {
            // Load buff entries, not items
            var buffIds = BuffDatabase.GetAllIds();
            var categories = new HashSet<string> { "All" };
            foreach (var buffId in buffIds)
            {
                if (buffId <= 0) continue; // Skip buff ID 0 (None)
                var name = BuffDatabase.GetName(buffId);
                var type = BuffDatabase.GetType(buffId);
                var icon = IconService.GetBuffIcon(buffId) ?? IconService.DefaultIcon;
                var displayName = $"{name} (ID:{buffId})";
                var rowIndex = _dgvItems.Rows.Add(icon, displayName, buffId);
                _dgvItems.Rows[rowIndex].Tag = buffId;
                // Color-code by buff kind
                var kind = BuffData.GetBuffKind(buffId);
                _dgvItems.Rows[rowIndex].DefaultCellStyle.ForeColor = BuffData.GetColor(kind);
                if (!string.IsNullOrEmpty(type) && type != "Buff")
                    categories.Add(type);
            }
            _dgvItems.ResumeLayout();
            _cmbCategory.Items.Clear();
            _cmbCategory.Items.Add(AppLocale.Get("Browser.All") ?? "All");
            _cmbCategory.SelectedIndex = 0;
            return;
        }

        var itemCategories = new HashSet<string> { "All" };
        var allItems = ItemDatabase.GetAllItems();

        foreach (var item in allItems)
        {
            var cat = ItemDatabase.GetCategory(item.Id);

            // Apply filter mode
            if (_filterMode == ItemFilterMode.DyeOnly)
            {
                if (!cat.Equals("Dye", StringComparison.OrdinalIgnoreCase)) continue;
            }

            var icon = IconService.GetItemIcon(item.Id) ?? IconService.DefaultIcon;
            var rowIndex = _dgvItems.Rows.Add(icon, item.ToString(), item.Id);
            _dgvItems.Rows[rowIndex].Tag = item.Id;

            if (!string.IsNullOrEmpty(cat) && cat != "None")
                itemCategories.Add(cat);
        }

        _dgvItems.ResumeLayout();

        // Populate category filter
        _cmbCategory.Items.Clear();
        _cmbCategory.Items.Add(AppLocale.Get("Browser.All") ?? "All");
        _cmbCategory.SelectedIndex = 0;
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
        foreach (DataGridViewRow row in _dgvItems.Rows)
        {
            if (row.Tag is int itemId)
            {
                row.Cells[1].Value = ItemDatabase.GetName(itemId);
            }
        }
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private void ApplyFilter()
    {
        var query = _txtSearch.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            // Clear filter — show all rows and reset scroll position
            foreach (DataGridViewRow row in _dgvItems.Rows)
                row.Visible = true;
            if (_dgvItems.Rows.Count > 0)
                _dgvItems.FirstDisplayedScrollingRowIndex = 0;
            return;
        }

        bool isNumeric = int.TryParse(query, out int numericQuery);
        _dgvItems.SuspendLayout();
        foreach (DataGridViewRow row in _dgvItems.Rows)
        {
            if (isNumeric)
            {
                // Numeric query: exact ID match first, then fallback to name contains
                if (row.Tag is int rowId && rowId == numericQuery)
                    row.Visible = true;
                else
                {
                    var text = row.Cells[1].Value?.ToString() ?? "";
                    row.Visible = text.Contains(query, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                var text = row.Cells[1].Value?.ToString() ?? "";
                row.Visible = text.Contains(query, StringComparison.OrdinalIgnoreCase);
            }
        }
        _dgvItems.ResumeLayout();
    }
}

/// <summary>Filter mode for item browser.</summary>
public enum ItemFilterMode
{
    All,
    DyeOnly,
    BuffOnly
}
