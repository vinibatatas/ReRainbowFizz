using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using RedragonFizzMacro.UI.Common;

namespace RedragonFizzMacro.UI.Components;

public enum AppleButtonStyle
{
    Outline,
    PrimaryRed,
    OutlineRed,
    Ghost
}

public sealed class AppleButton : Control
{
    private AppleButtonStyle _style = AppleButtonStyle.Outline;
    private bool _hovered;
    private bool _pressed;
    private bool _selected;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AppleButtonStyle ButtonStyle
    {
        get => _style;
        set { _style = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public AppleButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Size = new Size(80, 24);
        BackColor = ApplePalette.Background;
        Font = new Font("Segoe UI", 8.0f, FontStyle.Regular);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        Color parentBg = Parent?.BackColor ?? ApplePalette.Background;
        g.Clear(parentBg);

        Color fill = Color.Transparent;
        Color fg;
        Color border;

        if (_selected)
        {
            fill = ApplePalette.RedMutedFill;
            fg = ApplePalette.TextPrimary;
            border = ApplePalette.Red;
        }
        else
        {
            switch (_style)
            {
                case AppleButtonStyle.PrimaryRed:
                    fill = _pressed ? ApplePalette.RedPressed : (_hovered ? ApplePalette.RedHover : ApplePalette.Red);
                    fg = Color.White;
                    border = fill;
                    break;
                case AppleButtonStyle.OutlineRed:
                    fg = _hovered ? Color.White : ApplePalette.RedSoftText;
                    border = _hovered ? ApplePalette.Red : Color.FromArgb(140, ApplePalette.Red);
                    if (_hovered) fill = ApplePalette.RedMutedFill;
                    break;
                case AppleButtonStyle.Ghost:
                    fg = _hovered ? ApplePalette.TextPrimary : ApplePalette.TextSecondary;
                    border = _hovered ? ApplePalette.Separator : Color.Transparent;
                    break;
                default:
                    fg = _hovered ? ApplePalette.TextPrimary : ApplePalette.TextSecondary;
                    border = _hovered ? ApplePalette.SeparatorHover : ApplePalette.Separator;
                    break;
            }
        }

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = ApplePalette.RoundedRect(rect, 6f);

        if (fill.A > 0)
        {
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);
        }

        if (border.A > 0)
        {
            using var pen = new Pen(border, 1f);
            g.DrawPath(pen, path);
        }

        using var textBrush = new SolidBrush(fg);
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(Text, Font, textBrush, rect, sf);
    }
}
