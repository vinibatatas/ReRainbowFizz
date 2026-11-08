using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using RedragonFizzMacro.UI.Common;

namespace RedragonFizzMacro.UI.Components;

public sealed class AppleStepper : Control
{
    private int _value = 1;
    private int _min = 1;
    private int _max = 10;
    private int _step = 1;
    private Func<int, string>? _formatter;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum { get => _min; set { _min = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum { get => _max; set { _max = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Increment { get => _step; set => _step = Math.Max(1, value); }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<int, string>? Formatter { get => _formatter; set { _formatter = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            int c = Math.Clamp(value, _min, _max);
            if (_value == c) return;
            _value = c;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ValueChanged;

    public AppleStepper()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Size = new Size(80, 21);
        BackColor = ApplePalette.Background;
        Font = new Font("Segoe UI", 7.8f, FontStyle.Regular);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        if (e.X < 22)
            Value -= _step;
        else if (e.X > Width - 22)
            Value += _step;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Parent?.BackColor ?? ApplePalette.Background);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = ApplePalette.RoundedRect(rect, 5f);
        using var border = new Pen(ApplePalette.Separator, 1f);
        g.DrawPath(border, path);

        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var minusBrush = new SolidBrush(_value > _min ? ApplePalette.RedSoftText : ApplePalette.TextTertiary);
        using var plusBrush = new SolidBrush(_value < _max ? ApplePalette.RedSoftText : ApplePalette.TextTertiary);
        using var valBrush = new SolidBrush(ApplePalette.TextPrimary);

        g.DrawString("−", Font, minusBrush, new RectangleF(0, 0, 20, Height), sf);
        g.DrawString("+", Font, plusBrush, new RectangleF(Width - 20, 0, 20, Height), sf);

        string txt = _formatter != null ? _formatter(_value) : _value.ToString();
        g.DrawString(txt, Font, valBrush, new RectangleF(20, 0, Width - 40, Height), sf);
    }
}
