using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
#pragma warning disable CA1416 // Validate platform compatibility

// !!!!! BEWARE !!!!!
// This file contains some *pristine* bullshit, it handles all the windows API messages to extend
// the client area into the title bar, which allows us to have a custom title bar.

// Seriously, Here be dragons! We spent around 2 weeks on this and I still don't understand why half of this
// stuff works so edit with caution.

namespace GUI;

partial class MainForm
{
    // These are DPI compensated.
    private readonly int ControlBoxInconSize = 10;
    private readonly int TitleBarHeight = 35;
    private readonly int TitleBarButtonWidth = 45;

    private enum CustomTitleBarHoveredButton
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

    private bool IsWindowMaximised()
    {
        WINDOWPLACEMENT placement = default;
        PInvoke.GetWindowPlacement((HWND)Handle, ref placement);

        if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED)
        {
            return true;
        }

        return false;
    }

    // Equivalent to the LoWord C Macro.
    public static int LoWord(int dwValue)
    {
        return dwValue & 0xFFFF;
    }

    // Equivalent to the HiWord C Macro.
    public static int HiWord(int dwValue)
    {
        return (dwValue >> 16) & 0xFFFF;
    }

    private CustomTitleBarHoveredButton CurrentHoveredButton;

    protected override void WndProc(ref Message m)
    {
        var padding = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, (uint)DeviceDpi);

        if (m.Msg == PInvoke.WM_NCCALCSIZE && (int)m.WParam == 1)
        {
            var frameX = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXFRAME, (uint)DeviceDpi);
            var frameY = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CYFRAME, (uint)DeviceDpi);

            var nccsp = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
            nccsp.rgrc._0.bottom -= frameY;
            nccsp.rgrc._0.right -= frameX;
            nccsp.rgrc._0.left += frameX;

            if (IsWindowMaximised())
            {
                nccsp.rgrc._0.bottom -= padding;
                nccsp.rgrc._0.right -= padding;
                nccsp.rgrc._0.left += padding;
                nccsp.rgrc._0.top += padding;
            }

            Marshal.StructureToPtr(nccsp, m.LParam, false);

            m.Result = IntPtr.Zero;
        }
        else if (m.Msg == PInvoke.WM_NCHITTEST)
        {
            var dwmHandled = PInvoke.DwmDefWindowProc((HWND)m.HWnd, (uint)m.Msg, new WPARAM((nuint)m.WParam), new LPARAM(m.LParam), out var result);

            if (dwmHandled == 1)
            {
                m.Result = result;
                return;
            }

            // Convert to client coordinates
            // TODO: easier word conversion?
            var point = PointToClient(new Point(LoWord((int)m.LParam), HiWord((int)m.LParam)));

            CheckControlBoxHoverState();

            if (point.Y - padding <= menuStrip.Top)
            {
                m.Result = new IntPtr(PInvoke.HTTOP);
                return;
            }

            if (point.Y < menuStrip.Height)
            {
                m.Result = new IntPtr(PInvoke.HTCAPTION);
                return;
            }

            // Check if hover button is on maximize to support SnapLayout on Windows 11.
            if (CurrentHoveredButton == CustomTitleBarHoveredButton.Close)
            {
                m.Result = new IntPtr(PInvoke.HTMAXBUTTON);
                return;
            }

            base.WndProc(ref m);
        }
        else if (m.Msg == PInvoke.WM_NCLBUTTONDOWN)
        {
            if (CurrentHoveredButton != CustomTitleBarHoveredButton.None)
            {
                m.Result = 0;
                return;
            }

            base.WndProc(ref m);
        }
        else if (m.Msg == PInvoke.WM_NCLBUTTONUP)
        {
            if (CurrentHoveredButton == CustomTitleBarHoveredButton.Close)
            {
                // Magic number for close message because I don't think the PInvoke source generator offers it?
                PInvoke.PostMessage((HWND)Handle, PInvoke.WM_CLOSE, 0, 0);
                m.Result = 0;
                return;
            }
            else if (CurrentHoveredButton == CustomTitleBarHoveredButton.Minimize)
            {
                PInvoke.ShowWindow((HWND)Handle, SHOW_WINDOW_CMD.SW_MINIMIZE);
                m.Result = 0;
                return;
            }
            else if (CurrentHoveredButton == CustomTitleBarHoveredButton.Maximize)
            {
                var mode = IsWindowMaximised() ? SHOW_WINDOW_CMD.SW_NORMAL : SHOW_WINDOW_CMD.SW_MAXIMIZE;
                PInvoke.ShowWindow((HWND)Handle, mode);
                m.Result = 0;
                return;
            }

            base.WndProc(ref m);
        }
        else if (m.Msg == PInvoke.WM_MOUSEMOVE)
        {
            CheckControlBoxHoverState();

            base.WndProc(ref m);
        }
        else if (m.Msg == PInvoke.WM_NCMOUSEMOVE)
        {
            CheckControlBoxHoverState();

            base.WndProc(ref m);
        }
        else
        {
            base.WndProc(ref m);
        }
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

    private void CheckControlBoxHoverState()
    {
        var titleBarButtonRects = GetCustomTitleBarButtonRects();

        var lastHoveredButton = CurrentHoveredButton;

        var point = PointToClient(Cursor.Position);

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

    private void logoButton_Click(object sender, EventArgs e)
    {
        PInvoke.PostMessage((HWND)Handle, PInvoke.WM_SYSCOMMAND, 0, 0);
    }
}

