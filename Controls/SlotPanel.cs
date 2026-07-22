using System.ComponentModel;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// A single inventory-style slot displaying an item icon, stack count, and selection state.
/// Mimics Terraria's slot appearance with dark backgrounds and color-coded borders.
/// </summary>
public class SlotPanel : UserControl
{
    private readonly PictureBox _icon;
    private readonly Label _stackLabel;
    private bool _selected;
    private int _slotIndex;
    private ItemData? _item;
    private Color _normalBackColor;

    public SlotPanel(int slotIndex = 0, bool isHotbar = false)
    {
        _slotIndex = slotIndex;
        _normalBackColor = isHotbar
            ? Color.FromArgb(80, 60, 40)   // Warm brown - hotbar
            : Color.FromArgb(40, 35, 45);  // Dark purple-gray - normal

        Size = new Size(48, 48);
        BackColor = _normalBackColor;
        BorderStyle = BorderStyle.FixedSingle;
        Cursor = Cursors.Hand;
        Margin = new Padding(1);

        _icon = new PictureBox
        {
            Size = new Size(36, 36),
            Location = new Point(5, 3),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Enabled = false
        };

        _stackLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 6.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomRight,
            Visible = false
        };

        Controls.Add(_icon);
        Controls.Add(_stackLabel);

        _icon.MouseClick += (s, e) => OnMouseClick(e);
        _icon.MouseDoubleClick += (s, e) => OnMouseDoubleClick(e);
        _stackLabel.MouseClick += (s, e) => OnMouseClick(e);
        _stackLabel.MouseDoubleClick += (s, e) => OnMouseDoubleClick(e);
    }

    /// <summary>Index of this slot within its parent grid.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SlotIndex
    {
        get => _slotIndex;
        set => _slotIndex = value;
    }

    /// <summary>Whether this slot is currently selected (gold border).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            BackColor = value ? Color.Gold : _normalBackColor;
        }
    }

    /// <summary>The item data displayed in this slot.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ItemData? Item
    {
        get => _item;
        set
        {
            _item = value;
            RefreshDisplay();
        }
    }

    /// <summary>Whether this is a hotbar slot (special background color).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsHotbar
    {
        set
        {
            _normalBackColor = value
                ? Color.FromArgb(80, 60, 40)
                : Color.FromArgb(40, 35, 45);
            if (!_selected) BackColor = _normalBackColor;
        }
    }

    /// <summary>Update the icon and stack label from current Item data.</summary>
    public void RefreshDisplay()
    {
        if (_item == null || _item.IsEmpty)
        {
            _icon.Image = IconService.DefaultIcon;
            _stackLabel.Visible = false;
            BackColor = _selected ? Color.Gold : Color.FromArgb(60, 50, 60); // Empty slot color
            return;
        }

        _icon.Image = IconService.GetItemIcon(_item.ItemId) ?? IconService.DefaultIcon;
        _stackLabel.Visible = _item.StackSize > 1;
        _stackLabel.Text = _item.StackSize > 999 ? "999+" : _item.StackSize.ToString();
        _stackLabel.Location = new Point(Width - _stackLabel.PreferredWidth - 2,
            Height - _stackLabel.PreferredHeight);
        _normalBackColor = _normalBackColor == Color.FromArgb(80, 60, 40)
            ? Color.FromArgb(80, 60, 40)
            : Color.FromArgb(40, 35, 45);
        if (!_selected) BackColor = _normalBackColor;
    }

    /// <summary>Clear this slot to empty state.</summary>
    public void Clear()
    {
        _item = null;
        RefreshDisplay();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!_selected)
            BackColor = Color.FromArgb(60, 80, 100); // Blue hover
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_selected)
            BackColor = _item != null && !_item.IsEmpty ? _normalBackColor : Color.FromArgb(60, 50, 60);
    }
}
