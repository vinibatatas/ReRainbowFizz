using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using RedragonFizzMacro.UI.Common;

namespace RedragonFizzMacro.UI.Components;

public sealed class AppleSegmentedControl : Control
{
    private string[] _items = Array.Empty<string>();
    private int _selectedIndex;
    private int _hoverIndex = -1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] Items
    {
        get => _items;
        set { _items = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = _items.Length == 0 ? 0 : Math.Clamp(value, 0, _items.Length - 1);
            if (_selectedIndex == clamped) return;
            _selectedIndex = clamped;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectedIndexChanged;
    public event Action<int>? SegmentRightClicked;

    public AppleSegmentedControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Size = new Size(160, 24);
        BackColor = ApplePalette.Background;
        Font = new Font("Segoe UI", 8.0f, FontStyle.Regular);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    public int CalculateResponsiveWidth(int minPerSegment = 44)
    {
        int count = Math.Max(1, _items.Length);
        return count * minPerSegment + 4;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int idx = HitSegment(e.X);
        if (idx != _hoverIndex)
        {
            _hoverIndex = idx;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        int idx = HitSegment(e.X);
        if (idx < 0) return;

        if (e.Button == MouseButtons.Left)
            SelectedIndex = idx;
        else if (e.Button == MouseButtons.Right)
            SegmentRightClicked?.Invoke(idx);
    }

    private int HitSegment(int x)
    {
        if (_items.Length == 0 || Width <= 0) return -1;
        float segW = (float)Width / _items.Length;
        return Math.Clamp((int)(x / segW), 0, _items.Length - 1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Parent?.BackColor ?? ApplePalette.Background);

        var outer = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var outerPath = ApplePalette.RoundedRect(outer, 6f);
        using var outerPen = new Pen(ApplePalette.Separator, 1f);
        g.DrawPath(outerPen, outerPath);

        if (_items.Length == 0) return;

        float segW = (Width - 4f) / _items.Length;
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

        for (int i = 0; i < _items.Length; i++)
        {
            var segRect = new RectangleF(2f + i * segW, 2f, segW, Height - 4.5f);
            bool active = i == _selectedIndex;

            if (active)
            {
                using var activePath = ApplePalette.RoundedRect(segRect, 4.5f);
                using var activeFill = new SolidBrush(ApplePalette.Red);
                g.FillPath(activeFill, activePath);
            }
            else if (i == _hoverIndex)
            {
                using var hoverPath = ApplePalette.RoundedRect(segRect, 4.5f);
                using var hoverBorder = new Pen(ApplePalette.SeparatorHover, 1f);
                g.DrawPath(hoverBorder, hoverPath);
            }

            using var textBrush = new SolidBrush(active ? Color.White : ApplePalette.TextSecondary);
            g.DrawString(_items[i], Font, textBrush, segRect, sf);
        }
    }
}
