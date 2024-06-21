using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GUI.Controls;
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
    public bool IsWindowMaximised()
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
            var controlsBoxPanelPoint = controlsBoxPanel.PointToClient(new Point(LoWord((int)m.LParam), HiWord((int)m.LParam)));

            // Updating here instead of in the ControlsBoxPanel class is better because we can tell when we are outside
            // of the panel here, and corrently set NONE.
            controlsBoxPanel.CheckControlBoxHoverState(controlsBoxPanelPoint);

            if (point.Y - padding <= menuStrip.Top)
            {
                // Manually set none for fix some oddity with hover not updating
                // when moving the cursor outside the window on the top.
                controlsBoxPanel.CurrentHoveredButton = ControlsBoxPanel.CustomTitleBarHoveredButton.None;

                m.Result = new IntPtr(PInvoke.HTTOP);
                return;
            }

            if (point.Y < menuStrip.Height)
            {
                if (controlsBoxPanel.CurrentHoveredButton != ControlsBoxPanel.CustomTitleBarHoveredButton.None)
                {
                    switch (controlsBoxPanel.CurrentHoveredButton)
                    {
                        case ControlsBoxPanel.CustomTitleBarHoveredButton.Maximize:
                            m.Result = new IntPtr(PInvoke.HTMAXBUTTON);
                            return;
                        case ControlsBoxPanel.CustomTitleBarHoveredButton.Minimize:
                            m.Result = new IntPtr(PInvoke.HTMINBUTTON);
                            return;
                        case ControlsBoxPanel.CustomTitleBarHoveredButton.Close:
                            m.Result = new IntPtr(PInvoke.HTCLOSE);
                            return;
                    }
                }

                m.Result = new IntPtr(PInvoke.HTCAPTION);
                return;
            }

            base.WndProc(ref m);
        }
        else if (m.Msg == PInvoke.WM_NCLBUTTONDOWN)
        {
            if (controlsBoxPanel.CurrentHoveredButton != ControlsBoxPanel.CustomTitleBarHoveredButton.None)
            {
                m.Result = 0;
                return;
            }

            base.WndProc(ref m);
        }
        else if (m.Msg == PInvoke.WM_NCLBUTTONUP)
        {
            if (controlsBoxPanel.CurrentHoveredButton == ControlsBoxPanel.CustomTitleBarHoveredButton.Close)
            {
                // Magic number for close message because I don't think the PInvoke source generator offers it?
                PInvoke.PostMessage((HWND)Handle, PInvoke.WM_CLOSE, 0, 0);
                m.Result = 0;
                return;
            }
            else if (controlsBoxPanel.CurrentHoveredButton == ControlsBoxPanel.CustomTitleBarHoveredButton.Minimize)
            {
                PInvoke.ShowWindow((HWND)Handle, SHOW_WINDOW_CMD.SW_MINIMIZE);
                m.Result = 0;
                return;
            }
            else if (controlsBoxPanel.CurrentHoveredButton == ControlsBoxPanel.CustomTitleBarHoveredButton.Maximize)
            {
                var mode = IsWindowMaximised() ? SHOW_WINDOW_CMD.SW_NORMAL : SHOW_WINDOW_CMD.SW_MAXIMIZE;
                PInvoke.ShowWindow((HWND)Handle, mode);
                m.Result = 0;
                return;
            }

            base.WndProc(ref m);
        }
        else if (m.Msg == PInvoke.WM_SIZE)
        {
            // Needed to make sure hover state is updated correctly when the window is maximised.
            // TODO: controlsBoxPanel IS null at start
            if (controlsBoxPanel != null)
            {
                var controlsBoxPanelPoint = controlsBoxPanel.PointToClient(new Point(LoWord((int)m.LParam), HiWord((int)m.LParam)));
                controlsBoxPanel.CheckControlBoxHoverState(controlsBoxPanelPoint);
            }
        }
        else
        {
            base.WndProc(ref m);
        }
    }

    private void logoButton_Click(object sender, EventArgs e)
    {
        PInvoke.PostMessage((HWND)Handle, PInvoke.WM_SYSCOMMAND, 0, 0);
    }
}

