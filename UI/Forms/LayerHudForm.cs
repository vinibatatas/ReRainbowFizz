using System.Drawing.Drawing2D;
using System.Drawing.Text;
using RedragonFizzMacro.UI.Common;

namespace RedragonFizzMacro.UI.Forms;

public sealed class LayerHudForm : Form
{
    private string _layerLabel = "FN2";
    private long _startTicks;
    private int _durationMs;
    private readonly System.Windows.Forms.Timer _animTimer;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            const int WS_EX_TOPMOST = 0x00000008;
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public LayerHudForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(190, 32);
        BackColor = ApplePalette.Surface;
        Opacity = 0.94;
        DoubleBuffered = true;

        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 16, 16));

        _animTimer = new System.Windows.Forms.Timer { Interval = 33 };
        _animTimer.Tick += (_, _) => Invalidate();
    }

    public void ShowHud(string layerLabel, int durationMs)
    {
        _layerLabel = layerLabel;
        _durationMs = durationMs;
        _startTicks = Environment.TickCount64;

        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(
            screen.Left + (screen.Width - Width) / 2,
            screen.Bottom - Height - 24);

        if (!Visible)
            Show();

        if (durationMs > 0)
            _animTimer.Start();
        else
            _animTimer.Stop();

        Invalidate();
    }

    public void HideHud()
    {
        _animTimer.Stop();
        if (Visible)
            Hide();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using (var dotBrush = new SolidBrush(ApplePalette.Red))
            g.FillEllipse(dotBrush, 14, 11, 7, 7);

        using var font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var textBrush = new SolidBrush(ApplePalette.TextPrimary);
        var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(_layerLabel, font, textBrush, new RectangleF(28, 0, Width - 40, Height - 3), sf);

        if (_durationMs > 0)
        {
            long elapsed = Environment.TickCount64 - _startTicks;
            float ratio = Math.Clamp(1f - (float)elapsed / _durationMs, 0f, 1f);
            float barW = (Width - 28) * ratio;
            using var barBrush = new SolidBrush(ApplePalette.Red);
            g.FillRectangle(barBrush, 14, Height - 3, barW, 2);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animTimer.Stop();
            _animTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);
}
