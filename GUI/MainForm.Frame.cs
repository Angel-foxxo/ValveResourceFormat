using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GUI.Utils;
using Windows.Win32;

namespace GUI;

partial class MainForm
{
    // these are DPI compensated
    private static int CaptionPaddingTop = 5;
    private static int CaptionPaddingBottom = 5;


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

    private void scaleMenuStripHorizontally()
    {
        var caption = SystemInformation.CaptionButtonSize;

        if (menuStrip != null)
        {
            menuStrip.MaximumSize = new Size(Width - AdjustForDPI(170) - menuStrip.Left, 0);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        scaleMenuStripHorizontally();

        //if (menuStrip != null)
        //{
        //    menuStrip.MaximumSize = new Size(Width - AdjustForDPI(170) - menuStrip.Left, 0);
        //}
    }


    // as far as i can tell it doesn't actually matter how much we extend this, it just needs to be big enough
    // setting to 64 here and adjusting for dpi just incase
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        scaleMenuStripHorizontally();

        var margins = new Windows.Win32.UI.Controls.MARGINS
        {
            cyTopHeight = AdjustForDPI(64)
        };

        _ = PInvoke.DwmExtendFrameIntoClientArea((Windows.Win32.Foundation.HWND)Handle, margins);

    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == PInvoke.WM_NCCALCSIZE && (int)m.WParam == 1)
        {
            var frameX = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXFRAME, (uint)DeviceDpi);
            var frameY = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYFRAME, (uint)DeviceDpi);
            var padding = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, (uint)DeviceDpi);

            var nccsp = Marshal.PtrToStructure<Windows.Win32.UI.WindowsAndMessaging.NCCALCSIZE_PARAMS>(m.LParam);
            nccsp.rgrc._0.bottom -= frameY + padding;
            nccsp.rgrc._0.right -= frameX + padding;
            nccsp.rgrc._0.left += frameX + padding;

            Windows.Win32.UI.WindowsAndMessaging.WINDOWPLACEMENT placement = default;
            PInvoke.GetWindowPlacement((Windows.Win32.Foundation.HWND)Handle, ref placement);

            if (placement.showCmd == Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED)
            {
                menuStrip.Padding = new Padding(0, 12, 0, 5);
            }
            else
            {
                if (menuStrip != null)
                {
                    menuStrip.Padding = new Padding(0, 5, 0, 5);
                }
            }

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


            if (point.Y - 6 <= menuStrip.Top)
            {
                m.Result = new IntPtr(PInvoke.HTTOP);
                return;
            }

            // TODO: This is not triggered when hovering over the menu strip except on very left - can't drag the window
            // TODO: Buttons dont work when maximized
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
