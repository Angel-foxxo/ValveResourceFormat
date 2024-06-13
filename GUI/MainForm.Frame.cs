using System.Drawing;
using System.Reflection.Metadata;
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
            menuStrip.MaximumSize = new Size(Width - 150 - menuStrip.Left, 0);
        }
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
    
        var margins = new Windows.Win32.UI.Controls.MARGINS
        {
            cyTopHeight = 24,// TODO menuStrip.Size.Height is 0 on startup
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

    private bool win32_window_is_maximized(Windows.Win32.Foundation.HWND handle)
    {
        Windows.Win32.UI.WindowsAndMessaging.WINDOWPLACEMENT placement = new();
        unsafe
        {
            placement.length = (uint)sizeof(Windows.Win32.UI.WindowsAndMessaging.WINDOWPLACEMENT);

            if (Windows.Win32.PInvoke.GetWindowPlacement(handle, &placement))
            {
                return placement.showCmd == Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED;
            }
        }
        return false;
    }

    static int win32_dpi_scale(int value, uint dpi)
    {
        return (int)((float)value * dpi / 96);
    }

    static Windows.Win32.Foundation.RECT win32_titlebar_rect(Windows.Win32.Foundation.HWND handle)
    {
        Windows.Win32.Foundation.SIZE title_bar_size = new();
        title_bar_size.cy = 0;
        title_bar_size.cx = 0;

        var WP_CAPTION = 1;
        var CS_ACTIVE = 1;


        const int top_and_bottom_borders = 2;
        Windows.Win32.CloseThemeDataSafeHandle theme = Windows.Win32.PInvoke.OpenThemeData(handle, "WINDOW");
        uint dpi = Windows.Win32.PInvoke.GetDpiForWindow(handle);
        Windows.Win32.PInvoke.GetThemePartSize(theme, new Windows.Win32.Graphics.Gdi.HDC(), WP_CAPTION, CS_ACTIVE, null,
            Windows.Win32.UI.Controls.THEMESIZE.TS_TRUE,
            out title_bar_size);
        Windows.Win32.PInvoke.CloseThemeData((Windows.Win32.UI.Controls.HTHEME)theme.DangerousGetHandle());

        int height = win32_dpi_scale(title_bar_size.cy, dpi) + top_and_bottom_borders;

        Windows.Win32.Foundation.RECT rect;
        Windows.Win32.PInvoke.GetClientRect(handle, out rect);
        rect.bottom = rect.top + height;
        return rect;
    }

    static Windows.Win32.Foundation.LRESULT HTNOWHERE = (Windows.Win32.Foundation.LRESULT)0;
    static Windows.Win32.Foundation.LRESULT HTRIGHT = (Windows.Win32.Foundation.LRESULT)11;
    static Windows.Win32.Foundation.LRESULT HTLEFT = (Windows.Win32.Foundation.LRESULT)10;
    static Windows.Win32.Foundation.LRESULT HTTOPLEFT = (Windows.Win32.Foundation.LRESULT)13;
    static Windows.Win32.Foundation.LRESULT HTTOP = (Windows.Win32.Foundation.LRESULT)12;
    static Windows.Win32.Foundation.LRESULT HTTOPRIGHT = (Windows.Win32.Foundation.LRESULT)14;
    static Windows.Win32.Foundation.LRESULT HTBOTTOMRIGHT = (Windows.Win32.Foundation.LRESULT)17;
    static Windows.Win32.Foundation.LRESULT HTBOTTOM = (Windows.Win32.Foundation.LRESULT)15;
    static Windows.Win32.Foundation.LRESULT HTBOTTOMLEFT = (Windows.Win32.Foundation.LRESULT)16;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == PInvoke.WM_NCCALCSIZE)
        {
            if (m.WParam == 0)
            {
                m.Result = Windows.Win32.PInvoke.DefWindowProc(
                    (Windows.Win32.Foundation.HWND)m.HWnd,
                    (uint)m.Msg,
                    (Windows.Win32.Foundation.WPARAM)(nuint)m.WParam,
                    (Windows.Win32.Foundation.LPARAM)m.LParam);

                return;
            }

            var dpi = Windows.Win32.PInvoke.GetDpiForWindow((Windows.Win32.Foundation.HWND)this.Handle);
            var frameX = Windows.Win32.PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXFRAME, dpi);
            var frameY = Windows.Win32.PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYFRAME, dpi);
            var padding = Windows.Win32.PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi);

            Windows.Win32.UI.WindowsAndMessaging.NCCALCSIZE_PARAMS nccsp = (Windows.Win32.UI.WindowsAndMessaging.NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(Windows.Win32.UI.WindowsAndMessaging.NCCALCSIZE_PARAMS));

            nccsp.rgrc._0.right -= frameX + padding;
            nccsp.rgrc._0.left += frameX + padding;
            nccsp.rgrc._0.bottom -= frameY + padding;

            if (win32_window_is_maximized((Windows.Win32.Foundation.HWND)this.Handle))
            {
                nccsp.rgrc._0.top += padding;
            }
            else
            {
                nccsp.rgrc._0.top += -1;
            }

            Marshal.StructureToPtr(nccsp, m.LParam, false);

            m.Result = 0;
        }
        //else if (m.Msg == PInvoke.WM_CREATE)
        //{
        //    Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS windowFlags =
        //        Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED |
        //        Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
        //        Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
        //
        //    Windows.Win32.Foundation.RECT windowRect;
        //    Windows.Win32.PInvoke.GetWindowRect((Windows.Win32.Foundation.HWND)this.Handle, out windowRect);
        //    Windows.Win32.PInvoke.SetWindowPos((Windows.Win32.Foundation.HWND)this.Handle,
        //        (Windows.Win32.Foundation.HWND)this.Handle,
        //        windowRect.left,
        //        windowRect.top,
        //        windowRect.right - windowRect.left,
        //        windowRect.bottom - windowRect.top,
        //        windowFlags
        //        );
        //
        //    return;
        //}
        //else if (m.Msg == PInvoke.WM_ACTIVATE)
        //{
        //    Windows.Win32.Foundation.RECT title_bar_rect = win32_titlebar_rect((Windows.Win32.Foundation.HWND)this.Handle);
        //    Windows.Win32.PInvoke.InvalidateRect((Windows.Win32.Foundation.HWND)this.Handle, title_bar_rect, false);
        //
        //    m.Result = Windows.Win32.PInvoke.DefWindowProc(
        //            (Windows.Win32.Foundation.HWND)m.HWnd,
        //            (uint)m.Msg,
        //            (Windows.Win32.Foundation.WPARAM)(nuint)m.WParam,
        //            (Windows.Win32.Foundation.LPARAM)m.LParam);
        //
        //    return;
        //}
        else if (m.Msg == PInvoke.WM_NCHITTEST)
        {
            Windows.Win32.Foundation.LRESULT hit;
            hit = Windows.Win32.PInvoke.DefWindowProc(
                    (Windows.Win32.Foundation.HWND)m.HWnd,
                    (uint)m.Msg,
                    (Windows.Win32.Foundation.WPARAM)(nuint)m.WParam,
                    (Windows.Win32.Foundation.LPARAM)m.LParam);

            if (hit == HTNOWHERE || hit == HTRIGHT || hit == HTLEFT || hit == HTTOPLEFT || hit == HTTOP ||
                hit == HTTOPRIGHT || hit == HTBOTTOMRIGHT || hit == HTBOTTOM || hit == HTBOTTOMLEFT)
            {
                m.Result = hit;
                return;
            }

            uint dpi = PInvoke.GetDpiForWindow((Windows.Win32.Foundation.HWND)Handle);
            int frame_y = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYFRAME, dpi);
            int padding = PInvoke.GetSystemMetricsForDpi(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi);

            var point = PointToClient(new Point(LoWord((int)m.LParam), HiWord((int)m.LParam)));

            if (point.Y > 0 && point.Y < frame_y + padding)
            {
                m.Result = 12;
                return;
            }

            m.Result = 1;
            return;
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
