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

    public ItemBrowser()
    {
        Dock = DockStyle.Fill;

        // Search box
        _txtSearch = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 24,
            PlaceholderText = "Search items..."
        };
        _txtSearch.TextChanged += OnSearchTextChanged;

        // Category filter
        _cmbCategory = new ComboBox
        {
            Dock = DockStyle.Top,
            Height = 24,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbCategory.SelectedIndexChanged += (s, e) => ApplyFilter();

        // Item list grid
        _dgvItems = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = SystemColors.Window,
            RowTemplate = { Height = 34 }
        };

        var iconCol = new DataGridViewImageColumn
        {
            Name = "Icon",
            Width = 34,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            Resizable = DataGridViewTriState.False
        };
        var nameCol = new DataGridViewTextBoxColumn
        {
            Name = "Name",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };
        _dgvItems.Columns.Add(iconCol);
        _dgvItems.Columns.Add(nameCol);
        _dgvItems.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _dgvItems.Rows.Count)
            {
                var row = _dgvItems.Rows[e.RowIndex];
                if (row.Tag is int itemId)
                    ItemSelected?.Invoke(this, itemId);
            }
        };

        Controls.Add(_dgvItems);
        Controls.Add(_cmbCategory);
        Controls.Add(_txtSearch);

        _txtSearch.BringToFront();
        _cmbCategory.BringToFront();
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

        var categories = new HashSet<string> { "All" };
        var allItems = ItemDatabase.GetAllItems();

        foreach (var item in allItems)
        {
            // Apply filter mode
            if (_filterMode == ItemFilterMode.DyeOnly)
            {
                var itemCat = ItemDatabase.GetCategory(item.Id);
                if (!itemCat.Equals("Dye", StringComparison.OrdinalIgnoreCase)) continue;
            }
            // BuffOnly is handled separately by BuffBrowser

            var icon = IconService.GetItemIcon(item.Id) ?? IconService.DefaultIcon;
            var rowIndex = _dgvItems.Rows.Add(icon, item.ToString());
            _dgvItems.Rows[rowIndex].Tag = item.Id;

            var cat = ItemDatabase.GetCategory(item.Id);
            if (!string.IsNullOrEmpty(cat) && cat != "None")
                categories.Add(cat);
        }

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

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = _txtSearch.Text.Trim().ToLowerInvariant();
        foreach (DataGridViewRow row in _dgvItems.Rows)
        {
            if (string.IsNullOrEmpty(query))
            {
                row.Visible = true;
            }
            else
            {
                var text = row.Cells[1].Value?.ToString()?.ToLowerInvariant() ?? "";
                row.Visible = text.Contains(query, StringComparison.OrdinalIgnoreCase);
            }
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
