using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;

// !!!!! BEWARE !!!!!
// This file contains some *pristine* bullshit, it handles all the windows API messages to extend
// the client area into the title bar, which allows us to have a custom title bar.

// Seriously, Here be dragons! We spent around 2 weeks on this and I still don't understand why half of this
// stuff works so edit with caution.

namespace GUI;

partial class MainForm
{
    // these are DPI compensated
    private static int BaseTitleBarHeight = 32;
    private static int BaseMenuStripHeight = 24;
    private static int GapBetweenMenuStripAndControlBox = 8;

    /// Equivalent to the LoWord C Macro
    public static int LoWord(int dwValue)
    {
        return dwValue & 0xFFFF;
    }

    /// Equivalent to the HiWord C Macro
    public static int HiWord(int dwValue)
    {
        return (dwValue >> 16) & 0xFFFF;
    }

    private bool IsWindowMaximised()
    {
        Windows.Win32.UI.WindowsAndMessaging.WINDOWPLACEMENT placement = default;
        PInvoke.GetWindowPlacement((Windows.Win32.Foundation.HWND)Handle, ref placement);

        if (placement.showCmd == Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED)
        {
            return true;
        }

        return false;
    }

    private int GetAdjustedTitleBarHeight()
    {
        if (IsWindowMaximised())
        {
            // Padding is the border around the window where you can resize the window.
            // Need to add this to the top when the window is maximised because when windows maximises a window it tries to hide its borders beyond
            // the edge of the screen, but since our borders are inside the frame and not outsidem it means it hides the top of the window
            // so extending by the amount of the border will make it remain visible.
            var padding = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, (uint)DeviceDpi);

            return AdjustForDPI(BaseTitleBarHeight) + padding;
        }

        return AdjustForDPI(BaseTitleBarHeight);
    }

    private void ExtendFrameIntoClientArea()
    {
        var margins = new Windows.Win32.UI.Controls.MARGINS
        {
            cyTopHeight = GetAdjustedTitleBarHeight()
        };

        _ = PInvoke.DwmExtendFrameIntoClientArea((Windows.Win32.Foundation.HWND)Handle, margins);
    }

    private void ScaleMenuStrip()
    {
        if (menuStrip != null)
        {
            RECT controlBoxSize;
            unsafe
            {
                _ = PInvoke.DwmGetWindowAttribute((HWND)Handle, Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_CAPTION_BUTTON_BOUNDS, &controlBoxSize, (uint)Marshal.SizeOf(typeof(RECT)));
            }

            // Scale the length of the menuStrip so it doesn't crash into the control box buttons.
            menuStrip.MaximumSize = new Size(controlBoxSize.left - AdjustForDPI(GapBetweenMenuStripAndControlBox), 0);

            var sizeDifference = GetAdjustedTitleBarHeight() - AdjustForDPI(BaseMenuStripHeight);

            // No particular reason for using 11 here, it's just what makes it looks good when maximised
            // this should be fine since it's DPI compensated but might need to be tweaked when layout changes happen
            if(IsWindowMaximised())
            {
                sizeDifference += AdjustForDPI(11);
            }

            // This tries to center the menuStrip to the control box
            menuStrip.Padding = new Padding(0, sizeDifference / 2, 0, sizeDifference / 2);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        ExtendFrameIntoClientArea();
        ScaleMenuStrip();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        ExtendFrameIntoClientArea();
        ScaleMenuStrip();
    }

    protected override void WndProc(ref Message m)
    {
        var padding = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, (uint)DeviceDpi);

        if (m.Msg == PInvoke.WM_NCCALCSIZE && (int)m.WParam == 1)
        {
            var frameX = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXFRAME, (uint)DeviceDpi);
            var frameY = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYFRAME, (uint)DeviceDpi);

            var nccsp = Marshal.PtrToStructure<Windows.Win32.UI.WindowsAndMessaging.NCCALCSIZE_PARAMS>(m.LParam);
            nccsp.rgrc._0.bottom -= frameY + padding;
            nccsp.rgrc._0.right -= frameX + padding;
            nccsp.rgrc._0.left += frameX + padding;

            Marshal.StructureToPtr(nccsp, m.LParam, false);

            m.Result = IntPtr.Zero;
        }
        else if (m.Msg == PInvoke.WM_NCHITTEST)
        {
            var dwmHandled = PInvoke.DwmDefWindowProc((Windows.Win32.Foundation.HWND)m.HWnd, (uint)m.Msg, new Windows.Win32.Foundation.WPARAM((nuint)m.WParam), new Windows.Win32.Foundation.LPARAM(m.LParam), out var result);

            if (dwmHandled == 1)
            {
                m.Result = result;
                return;
            }

            // Convert to client coordinates
            // TODO: easier word conversion?
            var point = PointToClient(new Point(LoWord((int)m.LParam), HiWord((int)m.LParam)));

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

            //m.Result = HitTestNCA(m.HWnd, m.WParam, m.LParam);
            base.WndProc(ref m);
        }
        else
        {
            base.WndProc(ref m);
        }
    }
}
