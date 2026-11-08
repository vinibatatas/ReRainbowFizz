using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using RedragonFizzMacro.Core.Models;
using RedragonFizzMacro.UI.Common;

namespace RedragonFizzMacro.UI.Keyboard;

public sealed class FizzKeyboardControl : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AppConfig? Config { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsLayerCurrentlyActive { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LastTriggeredVk { get; set; }

    public event Action<KeycapDef>? OnKeyClicked;
    public event Action<KeycapDef>? OnKeyRightClicked;
    public event Action<KeycapDef?>? OnKeyHovered;

    private KeycapDef? _hoveredKey;
    private readonly List<(KeycapDef Def, GraphicsPath Path, RectangleF Bounds)> _hitRegions = new();

    public FizzKeyboardControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        BackColor = ApplePalette.Background;
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(ApplePalette.Background);

        foreach (var r in _hitRegions)
            r.Path.Dispose();
        _hitRegions.Clear();

        if (Width < 120 || Height < 90) return;

        float padX = 24f;
        float padY = 14f;
        float availW = Width - padX * 2f;
        float availH = Height - padY * 2f;

        const float MaxKeyboardWidth = 1040f;
        float maxUnit = MaxKeyboardWidth / FizzAbnt2Layout.TotalRowUnits;
        float unit = Math.Min(maxUnit, Math.Min(availW / FizzAbnt2Layout.TotalRowUnits, availH / FizzAbnt2Layout.TotalRows));
        float kbW = unit * FizzAbnt2Layout.TotalRowUnits;
        float kbH = unit * FizzAbnt2Layout.TotalRows;
        float startX = (Width - kbW) / 2f;
        float startY = (Height - kbH) / 2f;

        var caseRect = new RectangleF(startX - 10f, startY - 10f, kbW + 20f, kbH + 20f);
        using (var casePath = ApplePalette.RoundedRect(caseRect, 12f))
        using (var caseBorder = new Pen(IsLayerCurrentlyActive ? ApplePalette.Red : ApplePalette.Separator, 1f))
        {
            g.DrawPath(caseBorder, casePath);
        }

        var profile = Config?.GetActiveProfile();
        int holdVk = (Config != null && Config.HoldLayerEnabled) ? Config.HoldLayerVkCode : -1;
        float gap = Math.Max(3.5f, unit * 0.06f);
        float keyRadius = 6f;

        using var keyFont = new Font("Segoe UI", Math.Clamp(unit * 0.145f, 7.5f, 9.0f), FontStyle.Regular);
        using var fnFont = new Font("Segoe UI Semibold", Math.Clamp(unit * 0.145f, 7.5f, 9.0f), FontStyle.Bold);
        using var badgeFont = new Font("Segoe UI", Math.Clamp(unit * 0.13f, 6.8f, 8.2f), FontStyle.Regular);

        foreach (var key in FizzAbnt2Layout.Keys)
        {
            GraphicsPath path;
            RectangleF primaryRect;

            if (key.IsIsoEnter)
            {
                float xTop = startX + key.XUnits * unit + gap / 2f;
                float yTop = startY + 1 * unit + gap / 2f;
                float wTop = 1.5f * unit - gap;
                float xBot = startX + 13.75f * unit + gap / 2f;
                float row1Bottom = startY + 2 * unit + gap / 2f;
                float row2Bottom = startY + 3 * unit - gap / 2f;
                float rightEdge = xTop + wTop;

                path = BuildIsoEnterPath(xTop, yTop, rightEdge, row1Bottom, xBot, row2Bottom, keyRadius);
                primaryRect = new RectangleF(xBot, yTop, rightEdge - xBot, row2Bottom - yTop);
            }
            else
            {
                float x = startX + key.XUnits * unit + gap / 2f;
                float y = startY + key.Row * unit + gap / 2f;
                float w = key.WidthUnits * unit - gap;
                float h = unit - gap;
                primaryRect = new RectangleF(x, y, w, h);
                path = ApplePalette.RoundedRect(primaryRect, keyRadius);
            }

            _hitRegions.Add((key, path, primaryRect));

            bool isHovered = ReferenceEquals(_hoveredKey, key);
            bool isFn2 = key.IsFn2Key;
            bool isHoldKey = key.VkCode != 0 && key.VkCode == holdVk;
            bool isFn1 = key.IsHardwareFn1;
            bool isFlash = key.VkCode != 0 && key.VkCode == LastTriggeredVk;

            MacroBinding? binding = null;
            if (profile != null && key.VkCode != 0)
                profile.Bindings.TryGetValue(key.VkCode, out binding);

            Color fill, border, legendColor;

            if (isFlash)
            {
                fill = ApplePalette.Red;
                border = ApplePalette.Red;
                legendColor = Color.White;
            }
            else if (isFn2)
            {
                fill = IsLayerCurrentlyActive ? ApplePalette.RedHover : ApplePalette.Red;
                border = ApplePalette.Red;
                legendColor = Color.White;
            }
            else if (binding != null)
            {
                fill = isHovered ? ApplePalette.RedMutedHover : ApplePalette.RedMutedFill;
                border = ApplePalette.Red;
                legendColor = ApplePalette.TextPrimary;
            }
            else if (isFn1)
            {
                fill = Color.FromArgb(23, 23, 25);
                border = Color.FromArgb(36, 36, 40);
                legendColor = ApplePalette.TextTertiary;
            }
            else
            {
                fill = isHovered ? ApplePalette.SurfaceHover : ApplePalette.SurfaceElevated;
                border = isHovered ? Color.FromArgb(95, 95, 102) : ApplePalette.Separator;
                legendColor = ApplePalette.TextPrimary;
            }

            using (var fillBrush = new SolidBrush(fill))
            using (var borderPen = new Pen(border, 1f))
            {
                g.FillPath(fillBrush, path);
                g.DrawPath(borderPen, path);
            }

            if (isHoldKey && !isFn2)
            {
                using var dotBrush = new SolidBrush(ApplePalette.Red);
                g.FillEllipse(dotBrush, primaryRect.Right - 9.5f, primaryRect.Y + 5.5f, 4.5f, 4.5f);
            }

            if (isFn2)
            {
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var wBrush = new SolidBrush(Color.White);
                g.DrawString("FN2", fnFont, wBrush, primaryRect, sfCenter);
            }
            else
            {
                if (!string.IsNullOrEmpty(key.TopLegend))
                {
                    using var legBrush = new SolidBrush(legendColor);
                    g.DrawString(key.TopLegend, keyFont, legBrush, primaryRect.X + 5.5f, primaryRect.Y + 4f);
                }

                if (binding != null)
                {
                    Color bColor = isFlash ? Color.White : ApplePalette.RedSoftText;
                    using var bBrush = new SolidBrush(bColor);
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Far,
                        Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    var bRect = new RectangleF(primaryRect.X + 2f, primaryRect.Y + primaryRect.Height * 0.44f, primaryRect.Width - 4f, primaryRect.Height * 0.50f);
                    g.DrawString(binding.GetShortBadge(), badgeFont, bBrush, bRect, sf);
                }
                else if (!string.IsNullOrEmpty(key.SubLegend))
                {
                    using var subBrush = new SolidBrush(ApplePalette.TextTertiary);
                    var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
                    var sRect = new RectangleF(primaryRect.X + 2f, primaryRect.Y + 2f, primaryRect.Width - 5.5f, primaryRect.Height - 4.5f);
                    g.DrawString(key.SubLegend, badgeFont, subBrush, sRect, sf);
                }
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        KeycapDef? hit = HitTest(e.Location);
        if (!ReferenceEquals(hit, _hoveredKey))
        {
            _hoveredKey = hit;
            OnKeyHovered?.Invoke(hit);
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredKey != null)
        {
            _hoveredKey = null;
            OnKeyHovered?.Invoke(null);
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        KeycapDef? hit = HitTest(e.Location);
        if (hit == null) return;

        if (e.Button == MouseButtons.Left)
            OnKeyClicked?.Invoke(hit);
        else if (e.Button == MouseButtons.Right)
            OnKeyRightClicked?.Invoke(hit);
    }

    private KeycapDef? HitTest(Point pt)
    {
        for (int i = _hitRegions.Count - 1; i >= 0; i--)
        {
            if (_hitRegions[i].Path.IsVisible(pt))
                return _hitRegions[i].Def;
        }
        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var r in _hitRegions)
                r.Path.Dispose();
            _hitRegions.Clear();
        }
        base.Dispose(disposing);
    }

    private static GraphicsPath BuildIsoEnterPath(float xTop, float yTop, float xRight, float yStep, float xBot, float yBot, float r)
    {
        var p = new GraphicsPath();
        p.AddLines(new[]
        {
            new PointF(xTop + r, yTop),
            new PointF(xRight - r, yTop),
            new PointF(xRight, yTop + r),
            new PointF(xRight, yBot - r),
            new PointF(xRight - r, yBot),
            new PointF(xBot + r, yBot),
            new PointF(xBot, yBot - r),
            new PointF(xBot, yStep + r),
            new PointF(xTop + r, yStep),
            new PointF(xTop, yStep - r),
            new PointF(xTop, yTop + r)
        });
        p.CloseFigure();
        return p;
    }
}
