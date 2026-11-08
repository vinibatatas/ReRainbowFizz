using System.Drawing.Drawing2D;

namespace RedragonFizzMacro.UI.Common;

public static class ApplePalette
{
    // Solid flat Dark HIG surfaces
    public static readonly Color Background = Color.FromArgb(17, 17, 19);          // #111113
    public static readonly Color Surface = Color.FromArgb(26, 26, 29);             // #1A1A1D
    public static readonly Color SurfaceElevated = Color.FromArgb(35, 35, 39);     // #232327
    public static readonly Color SurfaceHover = Color.FromArgb(46, 46, 51);        // #2E2E33
    public static readonly Color Separator = Color.FromArgb(46, 46, 50);           // #2E2E32
    public static readonly Color SeparatorHover = Color.FromArgb(82, 82, 90);      // #52525A

    // Apple System Red priority palette
    public static readonly Color Red = Color.FromArgb(255, 69, 58);                // #FF453A
    public static readonly Color RedHover = Color.FromArgb(255, 92, 82);           // #FF5C52
    public static readonly Color RedPressed = Color.FromArgb(215, 0, 21);          // #D70015
    public static readonly Color RedMutedFill = Color.FromArgb(50, 21, 23);        // #321517
    public static readonly Color RedMutedHover = Color.FromArgb(68, 26, 29);       // #441A1D
    public static readonly Color RedSoftText = Color.FromArgb(255, 115, 107);      // #FF736B

    // Typography
    public static readonly Color TextPrimary = Color.FromArgb(242, 242, 247);      // #F2F2F7
    public static readonly Color TextSecondary = Color.FromArgb(142, 142, 147);    // #8E8E93
    public static readonly Color TextTertiary = Color.FromArgb(95, 95, 100);       // #5F5F64

    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
        var p = new GraphicsPath();
        if (d <= 0.5f)
        {
            p.AddRectangle(r);
            return p;
        }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
