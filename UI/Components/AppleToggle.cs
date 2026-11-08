using System.ComponentModel;
using System.Drawing.Drawing2D;
using RedragonFizzMacro.UI.Common;

namespace RedragonFizzMacro.UI.Components;

public sealed class AppleToggle : Control
{
    private bool _checked;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CheckedChanged;

    public AppleToggle()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Size = new Size(32, 18);
        BackColor = ApplePalette.Background;
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? ApplePalette.Background);

        var trackRect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var trackPath = ApplePalette.RoundedRect(trackRect, trackRect.Height / 2f);

        if (_checked)
        {
            using var trackBrush = new SolidBrush(ApplePalette.Red);
            g.FillPath(trackBrush, trackPath);
        }
        else
        {
            using var borderPen = new Pen(ApplePalette.SeparatorHover, 1f);
            g.DrawPath(borderPen, trackPath);
        }

        float pad = 2.5f;
        float knobSize = trackRect.Height - pad * 2f;
        float knobX = _checked ? (trackRect.Right - knobSize - pad) : (trackRect.X + pad);
        using var knobBrush = new SolidBrush(_checked ? Color.White : ApplePalette.TextSecondary);
        g.FillEllipse(knobBrush, knobX, trackRect.Y + pad, knobSize, knobSize);
    }
}
