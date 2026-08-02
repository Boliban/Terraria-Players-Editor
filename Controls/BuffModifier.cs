using System.ComponentModel;
using Terraria_Players_Editor.Models;
using Terraria_Players_Editor.Services;

namespace Terraria_Players_Editor.Controls;

/// <summary>
/// Right-side panel for editing the selected buff slot's properties.
/// Mirrors ItemModifier layout: icon, name, ID, search combo, duration, Set/Clear buttons.
/// </summary>
public class BuffModifier : UserControl
{
    private readonly FlatPictureBox _icon;
    private readonly Label _lblName;
    private readonly Label _lblId;
    private readonly ComboBox _cmbBuffSearch;
    private readonly Label _lblDuration;
    private readonly NumericUpDown _nudDuration;
    private readonly Label _lblTimeUnit;
    private readonly Button _btnSet;
    private readonly Button _btnClear;
    private int _currentSlotIndex = -1;
    private int _currentBuffId;

    // Cached values to avoid WinForms NumericUpDown commit timing issues
    private int _cachedDuration = 0;

    public BuffModifier()
    {
        Width = 400;
        Height = 110;
        // FixedSingle is system-drawn and ignores the app theme (black in light mode),
        // so the border is painted in OnPaint with ThemeManager.ControlInputBorder.
        BorderStyle = BorderStyle.None;

        // Icon (top-left)
        _icon = new FlatPictureBox
        {
            Size = new Size(32, 32),
            Location = new Point(10, 6),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = ThemeManager.IconModifierBg
        };

        // Name + ID labels
        _lblName = new Label
        {
            Location = new Point(52, 8),
            AutoSize = true,
            Font = ThemeManager.Typography.BodyBold
        };
        _lblId = new Label
        {
            Location = new Point(52, 28),
            AutoSize = true,
            ForeColor = ThemeManager.TextSecondary,
            Tag = "secondary"
        };

        // Buff search + Set/Clear buttons (row 1)
        _cmbBuffSearch = new ComboBox
        {
            Location = new Point(8, 48),
            Width = 220,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };

        _btnSet = new Button { Text = AppLocale.Get("Storage.Set"), Location = new Point(234, 47), Width = 75 };
        _btnClear = new Button { Text = AppLocale.Get("Storage.Clear"), Location = new Point(314, 47), Width = 75 };

        // Duration (row 2)
        _lblDuration = new Label { Text = AppLocale.Get("Buffs.Duration"), Location = new Point(8, 78), Width = 84, TextAlign = ContentAlignment.MiddleRight };
        _nudDuration = new NumericUpDown { Location = new Point(96, 76), Width = 100, Minimum = 0, Maximum = int.MaxValue };
        _lblTimeUnit = new Label { Text = "ticks", Location = new Point(200, 78), Width = 40, ForeColor = ThemeManager.TextSecondary, Tag = "secondary" };

        // Auto-save on Enter or when control loses focus
        void DoSet()
        {
            if (_currentSlotIndex < 0) return;
            _cachedDuration = (int)_nudDuration.Value;
            // Search combo: find buff by name
            var searchText = _cmbBuffSearch.Text;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                int foundId = FindBuffByName(searchText);
                if (foundId >= 0) _currentBuffId = foundId;
            }
            DebugLog.Log($"[BuffMod] AutoSet: slot={_currentSlotIndex}, buffId={_currentBuffId}, dur={_cachedDuration}");
            SetClicked?.Invoke(this, _currentSlotIndex);
        }
        _cmbBuffSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; DoSet(); } };
        _nudDuration.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; DoSet(); } };
        _cmbBuffSearch.Leave += (s, e) => DoSet();
        _nudDuration.Leave += (s, e) => DoSet();

        _btnSet.Click += (s, e) => DoSet();
        _btnClear.Click += (s, e) => ClearClicked?.Invoke(this, _currentSlotIndex);

        Controls.AddRange([
            _icon, _lblName, _lblId,
            _cmbBuffSearch, _btnSet, _btnClear,
            _lblDuration, _nudDuration, _lblTimeUnit
        ]);

        ThemeManager.ThemeChanged += () => ApplyTheme();
    }

    /// <summary>Re-apply theme colors and refresh name color.</summary>
    public void ApplyTheme()
    {
        _icon.BackColor = ThemeManager.IconModifierBg;
        _lblId.ForeColor = ThemeManager.TextSecondary;
        _lblTimeUnit.ForeColor = ThemeManager.TextSecondary;
        RefreshNameColor();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Win11Renderer.DrawThemedBorder(e.Graphics, this);
    }

    /// <summary>Fired when Set button is clicked.</summary>
    public event EventHandler<int>? SetClicked;

    /// <summary>Fired when Clear button is clicked.</summary>
    public event EventHandler<int>? ClearClicked;

    /// <summary>Current slot index being edited.</summary>
    public int CurrentSlotIndex => _currentSlotIndex;

    /// <summary>Current buff ID.</summary>
    public int CurrentBuffId => _currentBuffId;

    /// <summary>Current buff duration (ticks).</summary>
    public int CurrentDuration => _cachedDuration;

    /// <summary>Load buff data into the modifier controls.</summary>
    public void LoadFromSlot(int slotIndex, int buffId, int duration)
    {
        _currentSlotIndex = slotIndex;
        _currentBuffId = buffId;
        _cachedDuration = duration;

        // Icon
        _icon.Image = buffId > 0
            ? (IconService.GetBuffIcon(buffId) ?? IconService.DefaultIcon)
            : IconService.DefaultIcon;

        // Name with color
        var name = buffId > 0 ? BuffDatabase.GetName(buffId) : "";
        _lblName.Text = string.IsNullOrEmpty(name) ? "" : name;
        RefreshNameColor();

        // ID
        _lblId.Text = buffId > 0 ? $"ID: {buffId}" : "";

        // Search combo
        _cmbBuffSearch.Text = buffId > 0 ? BuffDatabase.GetName(buffId) : "";

        // Duration
        _nudDuration.Value = duration;
    }

    /// <summary>Refresh the name label color based on buff type.</summary>
    private void RefreshNameColor()
    {
        if (_currentBuffId > 0)
        {
            var type = BuffDatabase.GetType(_currentBuffId);
            _lblName.ForeColor = type.Equals("Debuff", StringComparison.OrdinalIgnoreCase)
                ? ThemeManager.DebuffText
                : ThemeManager.BuffText;
        }
        else
        {
            _lblName.ForeColor = ThemeManager.TextPrimary;
        }
    }

    /// <summary>Populate the buff search combo with all buffs.</summary>
    public void PopulateBuffs()
    {
        _cmbBuffSearch.Items.Clear();
        foreach (var buffId in BuffDatabase.GetAllIds())
        {
            if (buffId <= 0) continue;
            var name = BuffDatabase.GetName(buffId);
            if (!string.IsNullOrEmpty(name))
                _cmbBuffSearch.Items.Add(name);
        }
    }

    /// <summary>Find buff ID by partial name match.</summary>
    private static int FindBuffByName(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return -1;
        foreach (var buffId in BuffDatabase.GetAllIds())
        {
            if (buffId <= 0) continue;
            var name = BuffDatabase.GetName(buffId);
            if (!string.IsNullOrEmpty(name) && name.Equals(query, StringComparison.OrdinalIgnoreCase))
                return buffId;
        }
        // Partial match fallback
        foreach (var buffId in BuffDatabase.GetAllIds())
        {
            if (buffId <= 0) continue;
            var name = BuffDatabase.GetName(buffId);
            if (!string.IsNullOrEmpty(name) && name.Contains(query, StringComparison.OrdinalIgnoreCase))
                return buffId;
        }
        return -1;
    }

    /// <summary>Refresh display text (for language switching).</summary>
    public void RefreshLocale()
    {
        _lblDuration.Text = AppLocale.Get("Buffs.Duration");
        _btnSet.Text = AppLocale.Get("Storage.Set");
        _btnClear.Text = AppLocale.Get("Storage.Clear");
    }
}
