using System.Drawing.Drawing2D;
using RedragonFizzMacro.UI.Common;

namespace RedragonFizzMacro.UI.Components;

/// <summary>
/// Outline-only container (no background fill, 1px subtle stroke).
/// </summary>
public sealed class AppleCard : Panel
{
    public AppleCard()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        BackColor = ApplePalette.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? ApplePalette.Background);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = ApplePalette.RoundedRect(rect, 9f);
        using var border = new Pen(ApplePalette.Separator, 1f);
        g.DrawPath(border, path);
    }
}
