using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GUI.Utils;
using Windows.Win32;

namespace GUI;

partial class MainForm
{
    /// <summary>
    /// Equivalent to the LoWord C Macro
    /// </summary>
    /// <param name="dwValue"></param>
    /// <returns></returns>
    public static int LoWord(int dwValue)
    {
        return dwValue & 0xFFFF;
    }

    /// <summary>
    /// Equivalent to the HiWord C Macro
    /// </summary>
    /// <param name="dwValue"></param>
    /// <returns></returns>
    public static int HiWord(int dwValue)
    {
        return (dwValue >> 16) & 0xFFFF;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (menuStrip != null)
        {
            menuStrip.MaximumSize = new Size(Width - 170 - menuStrip.Left, 0);
        }
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        var margins = new Windows.Win32.UI.Controls.MARGINS
        {
            cyTopHeight = 35, // TODO menuStrip.Size.Height is 0 on startup
        };

        _ = PInvoke.DwmExtendFrameIntoClientArea((Windows.Win32.Foundation.HWND)Handle, margins);
    }

    /*
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(Color.Transparent);

        e.Graphics.FillRectangle(SystemBrushes.ButtonFace,
                Rectangle.FromLTRB(
                    dwmMargins.cxLeftWidth - 0,
                    dwmMargins.cyTopHeight - 0,
                    Width - dwmMargins.cxRightWidth - 0,
                    Height - dwmMargins.cyBottomHeight - 0));
    }
    */

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
                if(menuStrip != null)
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

#if false
    private IntPtr HitTestNCA(IntPtr hwnd, IntPtr wparam, IntPtr lparam)
    {
        var HTNOWHERE = 0;
        var HTCLIENT = 1;
        var HTCAPTION = 2;
        var HTGROWBOX = 4;
        var HTSIZE = HTGROWBOX;
        var HTMINBUTTON = 8;
        var HTMAXBUTTON = 9;
        var HTLEFT = 10;
        var HTRIGHT = 11;
        var HTTOP = 12;
        var HTTOPLEFT = 13;
        var HTTOPRIGHT = 14;
        var HTBOTTOM = 15;
        var HTBOTTOMLEFT = 16;
        var HTBOTTOMRIGHT = 17;
        var HTREDUCE = HTMINBUTTON;
        var HTZOOM = HTMAXBUTTON;
        var HTSIZEFIRST = HTLEFT;
        var HTSIZELAST = HTBOTTOMRIGHT;

        var p = new Point(LoWord((int)lparam), HiWord((int)lparam));

        var dwmMargins = new Windows.Win32.UI.Controls.MARGINS();

        var topleft = RectangleToScreen(new Rectangle(0, 0, dwmMargins.cxLeftWidth, dwmMargins.cxLeftWidth));

        if (topleft.Contains(p))
        {
            return new IntPtr(HTTOPLEFT);
        }

        var topright = RectangleToScreen(new Rectangle(Width - dwmMargins.cxRightWidth, 0, dwmMargins.cxRightWidth, dwmMargins.cxRightWidth));

        if (topright.Contains(p))
        {
            return new IntPtr(HTTOPRIGHT);
        }

        var botleft = RectangleToScreen(new Rectangle(0, Height - dwmMargins.cyBottomHeight, dwmMargins.cxLeftWidth, dwmMargins.cyBottomHeight));

        if (botleft.Contains(p))
        {
            return new IntPtr(HTBOTTOMLEFT);
        }

        var botright = RectangleToScreen(new Rectangle(Width - dwmMargins.cxRightWidth, Height - dwmMargins.cyBottomHeight, dwmMargins.cxRightWidth, dwmMargins.cyBottomHeight));

        if (botright.Contains(p))
        {
            return new IntPtr(HTBOTTOMRIGHT);
        }

        var top = RectangleToScreen(new Rectangle(0, 0, Width, dwmMargins.cxLeftWidth));

        if (top.Contains(p))
        {
            return new IntPtr(HTTOP);
        }

        var cap = RectangleToScreen(new Rectangle(0, dwmMargins.cxLeftWidth, Width, dwmMargins.cyTopHeight - dwmMargins.cxLeftWidth));

        if (cap.Contains(p))
        {
            return new IntPtr(HTCAPTION);
        }

        var left = RectangleToScreen(new Rectangle(0, 0, dwmMargins.cxLeftWidth, Height));

        if (left.Contains(p))
        {
            return new IntPtr(HTLEFT);
        }

        var right = RectangleToScreen(new Rectangle(Width - dwmMargins.cxRightWidth, 0, dwmMargins.cxRightWidth, Height));

        if (right.Contains(p))
        {
            return new IntPtr(HTRIGHT);
        }

        var bottom = RectangleToScreen(new Rectangle(0, Height - dwmMargins.cyBottomHeight, Width, dwmMargins.cyBottomHeight));

        if (bottom.Contains(p))
        {
            return new IntPtr(HTBOTTOM);
        }

        return new IntPtr(HTCLIENT);
    }
#endif
}
