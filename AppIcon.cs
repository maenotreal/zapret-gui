using System.Drawing.Drawing2D;

namespace ZapretGUI;

public static class AppIcon
{
    public static Icon Create()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        // Background — rounded rectangle, deep blue
        using var bgBrush = new SolidBrush(Color.FromArgb(0, 96, 185));
        using var path = RoundedRect(new Rectangle(1, 1, 30, 30), 6);
        g.FillPath(bgBrush, path);

        // Letter "Z"
        using var font = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var fBrush = new SolidBrush(Color.White);
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString("Z", font, fBrush, new RectangleF(0, 0, 32, 32), sf);

        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
