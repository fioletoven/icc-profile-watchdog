using System.Runtime.InteropServices;
using IccProfileWatchdog.Controls.Extensions;

namespace IccProfileWatchdog.Controls;

public class Win11ContextMenuStrip : ContextMenuStrip
{
    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern long DwmSetWindowAttribute(
        nint hwnd, DWMWINDOWATTRIBUTE attribute, ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute, uint cbAttribute);

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref uint pvAttribute, int cbAttribute);

    public Win11ContextMenuStrip()
    {
        var cornerPreference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL;
        DwmSetWindowAttribute(
            Handle, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(uint));

        var borderColor = (uint)0x00e5e5e5;
        DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, ref borderColor, sizeof(uint));
    }

    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_BORDER_COLOR = 34,
    }

    [Flags]
    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWA_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3,
    }
}

public class Win11ColorTable : ProfessionalColorTable
{
    private readonly Color _backgroundColor = Color.FromArgb(249, 249, 249);
    private readonly Color _selectedColor = Color.FromArgb(240, 240, 240);
    private readonly Color _separatorColor = Color.FromArgb(215, 215, 215);

    public Win11ColorTable()
    {
        UseSystemColors = false;
    }

    public override Color MenuBorder => _backgroundColor;
    public override Color MenuItemBorder => _backgroundColor;
    public override Color MenuItemSelected => _selectedColor;
    public override Color MenuItemSelectedGradientBegin => _selectedColor;
    public override Color MenuItemSelectedGradientEnd => _selectedColor;
    public override Color ToolStripBorder => _backgroundColor;
    public override Color StatusStripBorder => _backgroundColor;
    public override Color ToolStripDropDownBackground => _backgroundColor;
    public override Color ImageMarginGradientBegin => _backgroundColor;
    public override Color ImageMarginGradientMiddle => _backgroundColor;
    public override Color ImageMarginGradientEnd => _backgroundColor;
    public override Color SeparatorLight => _separatorColor;
    public override Color SeparatorDark => _separatorColor;
}

public class Win11ToolStripProfessionalRenderer : ToolStripProfessionalRenderer
{
    public Win11ToolStripProfessionalRenderer()
        : base(new Win11ColorTable())
    {
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        var toolStrip = e.ToolStrip;
        var width = toolStrip != null ? toolStrip.Size.Width : item.Size.Width;
        var bounds = new Rectangle(new Point(8, 2), new Size(width - 16, item.Size.Height - 3));

        if (bounds.Width == 0 || bounds.Height == 0)
        {
            return;
        }

        if (item.Selected && item.Enabled)
        {
            using var brush = new Pen(ColorTable.MenuItemSelected, 1).Brush;
            var rectangle = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            e.Graphics.FillRoundedRectangle(brush, rectangle, 3);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var item = e.Item;
        var textColor = e.TextColor;
        var textFont = e.TextFont;
        var text = e.Text;
        var textRect = e.TextRectangle;
        var textFormat = e.TextFormat;

        textColor = item is not null && item.Enabled ? textColor : SystemColors.GrayText;

        if (item != null)
        {
            textRect.Y = (item.Height - textRect.Height) / 2;
        }

        TextRenderer.DrawText(e.Graphics, text, textFont, textRect, textColor, textFormat);

        e.Item.ForeColor = Color.Black;
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        var posY = bounds.Bottom / 2 - 1;

        using var pen = new Pen(ColorTable.SeparatorLight, 1);
        e.Graphics.DrawLine(pen, bounds.Left + 12, posY, bounds.Right - 12, posY);
    }
}
