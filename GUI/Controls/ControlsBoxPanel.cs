using System.Drawing;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace GUI.Controls;

class ControlsBoxPanel : Panel
{
    // These are DPI compensated.
    private readonly int ControlBoxInconSize = 10;
    private readonly int TitleBarHeight = 35;
    private readonly int TitleBarButtonWidth = 45;

    public enum CustomTitleBarHoveredButton
    {
        None,
        Minimize,
        Maximize,
        Close,
    }

    private struct CustomTitleBarButtonRects
    {
        internal Rectangle Close;
        internal Rectangle Maximize;
        internal Rectangle Minimize;
    }

    public CustomTitleBarHoveredButton CurrentHoveredButton { get; private set; }

    private Rectangle GetTitleBarRect()
    {
        var titleBarRect = ClientRectangle;
        titleBarRect.Height = titleBarRect.Top + AdjustForDPI(TitleBarHeight);

        return titleBarRect;
    }

    private CustomTitleBarButtonRects GetCustomTitleBarButtonRects()
    {
        CustomTitleBarButtonRects titleBarButtonRects;

        var titleBarButtonWidth = AdjustForDPI(TitleBarButtonWidth);

        // Calculate the size of the title bar buttons
        titleBarButtonRects.Close = GetTitleBarRect();
        titleBarButtonRects.Close.X = titleBarButtonRects.Close.Width - titleBarButtonWidth;
        titleBarButtonRects.Close.Width = titleBarButtonWidth;

        titleBarButtonRects.Maximize = titleBarButtonRects.Close;
        titleBarButtonRects.Maximize.X -= titleBarButtonWidth;

        titleBarButtonRects.Minimize = titleBarButtonRects.Maximize;
        titleBarButtonRects.Minimize.X -= titleBarButtonWidth;

        return titleBarButtonRects;
    }

    private int AdjustForDPI(float value)
    {
        return (int)(value * DeviceDpi / 96f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var controlBoxPen = new Pen(MainForm.DarkModeCS.ThemeColors.Text);
        controlBoxPen.Width = AdjustForDPI(1);

        // This needs to always be white to contrast with the red.
        using var controlBoxPenCloseButtonHighlighted = new Pen(Color.White);
        controlBoxPen.Width = AdjustForDPI(1);

        // Setting all the drawing settings to high in order to get nice looking caption buttons.
        // Not setting SmoothingMode to AntiAlias because it makes the X button appear darker.
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
        // High quality here seems to make the lines oddly thick and non-crisp? idk it's just weird
        // But that can be an advantage for stuff like the X button which otherwise seems thinner
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        var titleBarButtonRects = GetCustomTitleBarButtonRects();

        var controlBoxIconSize = AdjustForDPI(ControlBoxInconSize);

        var closeIconRect = new Rectangle
        {
            Width = controlBoxIconSize,
            Height = controlBoxIconSize
        };
        closeIconRect.X = titleBarButtonRects.Close.X + ((titleBarButtonRects.Close.Width - closeIconRect.Width) / 2);
        closeIconRect.Y = titleBarButtonRects.Close.Y + ((titleBarButtonRects.Close.Height - closeIconRect.Height) / 2);

        var maximiseIconRect = new Rectangle
        {
            Width = controlBoxIconSize,
            Height = controlBoxIconSize
        };
        maximiseIconRect.X = titleBarButtonRects.Maximize.X + ((titleBarButtonRects.Maximize.Width - maximiseIconRect.Width) / 2);
        maximiseIconRect.Y = titleBarButtonRects.Maximize.Y + ((titleBarButtonRects.Maximize.Height - maximiseIconRect.Height) / 2);

        var minimiseIconRect3 = new Rectangle
        {
            Width = controlBoxIconSize,
            Height = controlBoxIconSize
        };
        minimiseIconRect3.X = titleBarButtonRects.Minimize.X + ((titleBarButtonRects.Minimize.Width - minimiseIconRect3.Width) / 2);
        minimiseIconRect3.Y = titleBarButtonRects.Minimize.Y + ((titleBarButtonRects.Minimize.Height) / 2);

        // Draw the button rectangle if the mouse is hovering over the button,
        using var controlBoxButtonBrush = new SolidBrush(MainForm.DarkModeCS.ThemeColors.ControlBoxHighlightCloseButton);
        using var closeButtonBrush = new SolidBrush(MainForm.DarkModeCS.ThemeColors.ControlBoxHighlight);

        if (CurrentHoveredButton == CustomTitleBarHoveredButton.Close)
        {
            e.Graphics.FillRectangle(controlBoxButtonBrush, titleBarButtonRects.Close);
        }
        else if (CurrentHoveredButton == CustomTitleBarHoveredButton.Maximize)
        {
            e.Graphics.FillRectangle(closeButtonBrush, titleBarButtonRects.Maximize);
        }
        else if (CurrentHoveredButton == CustomTitleBarHoveredButton.Minimize)
        {
            e.Graphics.FillRectangle(closeButtonBrush, titleBarButtonRects.Minimize);
        }

        // Draws the horizontal line for the minimise icon.
        e.Graphics.DrawLine(controlBoxPen, minimiseIconRect3.X, minimiseIconRect3.Y, minimiseIconRect3.X + minimiseIconRect3.Width, minimiseIconRect3.Y);

        // Draws the square for the maximise icon.
        e.Graphics.DrawRectangle(controlBoxPen, maximiseIconRect);

        // Draws the X for the close icon.
        // Drawing this last so it can use high quality PixelOffsetMode which makes the line have a
        // more consistent thickness in relation to the other caption buttons
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        if (CurrentHoveredButton == CustomTitleBarHoveredButton.Close)
        {
            e.Graphics.DrawLine(controlBoxPenCloseButtonHighlighted, closeIconRect.X, closeIconRect.Y, closeIconRect.Right, closeIconRect.Bottom);
            e.Graphics.DrawLine(controlBoxPenCloseButtonHighlighted, closeIconRect.X, closeIconRect.Bottom, closeIconRect.Right, closeIconRect.Top);
        }
        else
        {
            e.Graphics.DrawLine(controlBoxPen, closeIconRect.X, closeIconRect.Y, closeIconRect.Right, closeIconRect.Bottom);
            e.Graphics.DrawLine(controlBoxPen, closeIconRect.X, closeIconRect.Bottom, closeIconRect.Right, closeIconRect.Top);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == PInvoke.WM_NCHITTEST)
        {
            // Convert to client coordinates
            var point = PointToClient(new Point(MainForm.LoWord((int)m.LParam), MainForm.HiWord((int)m.LParam)));

            CheckControlBoxHoverState(point);

            m.Result = PInvoke.HTTRANSPARENT;
        }
        else if (m.Msg == PInvoke.WM_MOUSELEAVE)
        {
            var lastHoveredButton = CurrentHoveredButton;
            CurrentHoveredButton = CustomTitleBarHoveredButton.None;

            if (lastHoveredButton != CurrentHoveredButton)
            {
                Invalidate();
            }
        }
        else
        {
            base.WndProc(ref m);
        }
    }

    private void CheckControlBoxHoverState(Point point)
    {
        var titleBarButtonRects = GetCustomTitleBarButtonRects();

        var lastHoveredButton = CurrentHoveredButton;

        if (titleBarButtonRects.Close.Contains(point))
        {
            CurrentHoveredButton = CustomTitleBarHoveredButton.Close;
        }
        else if (titleBarButtonRects.Maximize.Contains(point))
        {
            CurrentHoveredButton = CustomTitleBarHoveredButton.Maximize;
        }
        else if (titleBarButtonRects.Minimize.Contains(point))
        {
            CurrentHoveredButton = CustomTitleBarHoveredButton.Minimize;
        }
        else
        {
            CurrentHoveredButton = CustomTitleBarHoveredButton.None;
        }

        if (lastHoveredButton != CurrentHoveredButton)
        {
            Invalidate();
        }
    }
}
