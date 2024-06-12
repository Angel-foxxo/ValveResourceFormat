using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GUI.Controls;
using System.ComponentModel.Design;
using GUI.Types.PackageViewer;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Controls;

namespace DarkModeForms
{
    /// <summary>This tries to automatically apply Windows Dark Mode (if enabled) to a Form.
    /// <para>Author: DarkModeForms (DarkModeForms.play@gmail.com)  2024</para></summary>
    public partial class DarkModeCS
    {
        #region Win32 API Declarations

        public enum DWMWINDOWATTRIBUTE
        {
            /// <summary>
            /// Use with DwmGetWindowAttribute. Discovers whether non-client rendering is enabled. The retrieved value is of type BOOL. TRUE if non-client rendering is enabled; otherwise, FALSE.
            /// </summary>
            DWMWA_NCRENDERING_ENABLED = 1,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Sets the non-client rendering policy. The pvAttribute parameter points to a value from the DWMNCRENDERINGPOLICY enumeration.
            /// </summary>
            DWMWA_NCRENDERING_POLICY,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Enables or forcibly disables DWM transitions. The pvAttribute parameter points to a value of type BOOL. TRUE to disable transitions, or FALSE to enable transitions.
            /// </summary>
            DWMWA_TRANSITIONS_FORCEDISABLED,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Enables content rendered in the non-client area to be visible on the frame drawn by DWM. The pvAttribute parameter points to a value of type BOOL. TRUE to enable content rendered in the non-client area to be visible on the frame; otherwise, FALSE.
            /// </summary>
            DWMWA_ALLOW_NCPAINT,

            /// <summary>
            /// Use with DwmGetWindowAttribute. Retrieves the bounds of the caption button area in the window-relative space. The retrieved value is of type RECT. If the window is minimized or otherwise not visible to the user, then the value of the RECT retrieved is undefined. You should check whether the retrieved RECT contains a boundary that you can work with, and if it doesn't then you can conclude that the window is minimized or otherwise not visible.
            /// </summary>
            DWMWA_CAPTION_BUTTON_BOUNDS,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies whether non-client content is right-to-left (RTL) mirrored. The pvAttribute parameter points to a value of type BOOL. TRUE if the non-client content is right-to-left (RTL) mirrored; otherwise, FALSE.
            /// </summary>
            DWMWA_NONCLIENT_RTL_LAYOUT,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Forces the window to display an iconic thumbnail or peek representation (a static bitmap), even if a live or snapshot representation of the window is available. This value is normally set during a window's creation, and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to require a iconic thumbnail or peek representation; otherwise, FALSE.
            /// </summary>
            DWMWA_FORCE_ICONIC_REPRESENTATION,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Sets how Flip3D treats the window. The pvAttribute parameter points to a value from the DWMFLIP3DWINDOWPOLICY enumeration.
            /// </summary>
            DWMWA_FLIP3D_POLICY,

            /// <summary>
            /// Use with DwmGetWindowAttribute. Retrieves the extended frame bounds rectangle in screen space. The retrieved value is of type RECT.
            /// </summary>
            DWMWA_EXTENDED_FRAME_BOUNDS,

            /// <summary>
            /// Use with DwmSetWindowAttribute. The window will provide a bitmap for use by DWM as an iconic thumbnail or peek representation (a static bitmap) for the window. DWMWA_HAS_ICONIC_BITMAP can be specified with DWMWA_FORCE_ICONIC_REPRESENTATION. DWMWA_HAS_ICONIC_BITMAP normally is set during a window's creation and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to inform DWM that the window will provide an iconic thumbnail or peek representation; otherwise, FALSE. Windows Vista and earlier: This value is not supported.
            /// </summary>
            DWMWA_HAS_ICONIC_BITMAP,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Do not show peek preview for the window. The peek view shows a full-sized preview of the window when the mouse hovers over the window's thumbnail in the taskbar. If this attribute is set, hovering the mouse pointer over the window's thumbnail dismisses peek (in case another window in the group has a peek preview showing). The pvAttribute parameter points to a value of type BOOL. TRUE to prevent peek functionality, or FALSE to allow it. Windows Vista and earlier: This value is not supported.
            /// </summary>
            DWMWA_DISALLOW_PEEK,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Prevents a window from fading to a glass sheet when peek is invoked. The pvAttribute parameter points to a value of type BOOL. TRUE to prevent the window from fading during another window's peek, or FALSE for normal behavior. Windows Vista and earlier: This value is not supported.
            /// </summary>
            DWMWA_EXCLUDED_FROM_PEEK,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Cloaks the window such that it is not visible to the user. The window is still composed by DWM. Using with DirectComposition: Use the DWMWA_CLOAK flag to cloak the layered child window when animating a representation of the window's content via a DirectComposition visual that has been associated with the layered child window. For more details on this usage case, see How to animate the bitmap of a layered child window. Windows 7 and earlier: This value is not supported.
            /// </summary>
            DWMWA_CLOAK,

            /// <summary>
            /// Use with DwmGetWindowAttribute. If the window is cloaked, provides one of the following values explaining why. DWM_CLOAKED_APP (value 0x0000001). The window was cloaked by its owner application. DWM_CLOAKED_SHELL(value 0x0000002). The window was cloaked by the Shell. DWM_CLOAKED_INHERITED(value 0x0000004). The cloak value was inherited from its owner window. Windows 7 and earlier: This value is not supported.
            /// </summary>
            DWMWA_CLOAKED,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Freeze the window's thumbnail image with its current visuals. Do no further live updates on the thumbnail image to match the window's contents. Windows 7 and earlier: This value is not supported.
            /// </summary>
            DWMWA_FREEZE_REPRESENTATION,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Enables a non-UWP window to use host backdrop brushes. If this flag is set, then a Win32 app that calls Windows::UI::Composition APIs can build transparency effects using the host backdrop brush (see Compositor.CreateHostBackdropBrush). The pvAttribute parameter points to a value of type BOOL. TRUE to enable host backdrop brushes for the window, or FALSE to disable it. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_USE_HOSTBACKDROPBRUSH,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 10 Build 17763.
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the rounded corner preference for a window. The pvAttribute parameter points to a value of type DWM_WINDOW_CORNER_PREFERENCE. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the color of the window border. The pvAttribute parameter points to a value of type COLORREF. The app is responsible for changing the border color according to state changes, such as a change in window activation. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_BORDER_COLOR,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the color of the caption. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_CAPTION_COLOR,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the color of the caption text. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_TEXT_COLOR,

            /// <summary>
            /// Use with DwmGetWindowAttribute. Retrieves the width of the outer border that the DWM would draw around this window. The value can vary depending on the DPI of the window. The pvAttribute parameter points to a value of type UINT. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,

            /// <summary>
            /// The maximum recognized DWMWINDOWATTRIBUTE value, used for validation purposes.
            /// </summary>
            DWMWA_LAST,
        }

        [Flags]
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        public const int EM_SETCUEBANNER = 5377;

        public static IntPtr GetHeaderControl(ListView list)
        {
            const int LVM_GETHEADER = 0x1000 + 31;
            return Windows.Win32.PInvoke.SendMessage((Windows.Win32.Foundation.HWND)list.Handle, LVM_GETHEADER, 0, 0);
        }

        #endregion

        /// <summary>'true' if Dark Mode Color is set in Windows's Settings.</summary>
        public bool IsDarkMode { get; set; }

        /// <summary>Windows Colors. Can be customized.</summary>
        public ThemeColors ThemeColors { get; set; }

        /// <summary>Constructor.</summary>
        public DarkModeCS(bool debugTheme = false)
        {
            DebugTheme = debugTheme;
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(OnUserPreferenceChanged);
        }

        #region Public Methods

        /// <summary>This tries to style and automatically apply Windows Dark Mode (if enabled) to a Form.</summary>
        /// <param name="_Form">The Form to become Dark</param>
        public void Style(Form _Form)
        {
            ApplyTheme(_Form);
        }

        /// <summary>Recursively apply the Colors from 'ThemeColors' to the Control and all its childs.</summary>
        /// <param name="control">Can be a Form or any Winforms Control.</param>
        public void ThemeControl(Control control)
        {
            var BStyle = BorderStyle.FixedSingle;
            var FStyle = FlatStyle.Flat;

            var borderStyleInfo = control.GetType().GetProperty("BorderStyle");
            if (borderStyleInfo != null)
            {
                var borderStyle = (BorderStyle)borderStyleInfo.GetValue(control);
                if ((BorderStyle)borderStyle != BorderStyle.None)
                {
                    borderStyleInfo.SetValue(control, BStyle);
                }
            }

            if (control is Panel panel)
            {
                // Process the panel within the container
                panel.BackColor = ThemeColors.Container;
                panel.BorderStyle = BorderStyle.None;
            }
            if (control is GroupBox group)
            {
                group.BackColor = ThemeColors.Window;
                group.ForeColor = ThemeColors.Text;
            }
            if (control is TableLayoutPanel table)
            {
                // Process the panel within the container
                table.BackColor = ThemeColors.Window;
                table.BorderStyle = BorderStyle.None;
            }
            if (control is FlatTabControl fTab)
            {
                fTab.BackColor = ThemeColors.Window;
                fTab.TabColor = ThemeColors.Control;
                fTab.SelectTabColor = ThemeColors.ContainerHighlight;
                fTab.SelectedForeColor = ThemeColors.Text;
                fTab.BorderColor = ThemeColors.Window;
                fTab.ForeColor = ThemeColors.TextInactive;
                fTab.LineColor = ThemeColors.Window;
                fTab.HoverColor = ThemeColors.Accent;
                fTab.Margin = new Padding(-10, 0, 0, 0);
            }
            if (control is PictureBox pic)
            {
                pic.BorderStyle = BorderStyle.None;
                pic.BackColor = Color.Transparent;
            }
            if (control is ListView lView)
            {
                if (lView.View == View.Details)
                {
                    lView.OwnerDraw = true;
                    void DrawColumn(object sender, DrawListViewColumnHeaderEventArgs e)
                    {
                        using var backBrush = new SolidBrush(ThemeColors.ContainerHighlight);
                        using var foreBrush = new SolidBrush(ThemeColors.Text);
                        using var sf = new StringFormat();
                        sf.Alignment = StringAlignment.Center;
                        e.Graphics.FillRectangle(backBrush, e.Bounds);
                        e.Graphics.DrawString(e.Header.Text, lView.Font, foreBrush, e.Bounds, sf);

                    };
                    void Dispose(object sender, EventArgs e)
                    {
                        lView.DrawColumnHeader -= DrawColumn;
                        lView.Disposed -= Dispose;
                    }
                    lView.DrawColumnHeader += DrawColumn;
                    lView.Disposed += Dispose;
                    lView.DrawItem += (sender, e) => { e.DrawDefault = true; };
                    lView.DrawSubItem += (sender, e) =>
                    {

                        e.DrawDefault = true;
                    };
                }
            }
            if (control is Button button)
            {
                button.FlatStyle = FStyle;
                button.FlatAppearance.CheckedBackColor = ThemeColors.Control;
                button.BackColor = ThemeColors.Control;
                button.FlatAppearance.BorderColor = ThemeColors.ControlBorder;
                button.ForeColor = ThemeColors.Text;
            }
            if (control is Label label)
            {
                label.BorderStyle = BorderStyle.None;
                label.ForeColor = ThemeColors.Text;
                label.BackColor = Color.Transparent;
            }
            if (control is LinkLabel link)
            {
                link.LinkColor = ThemeColors.Accent;
                link.VisitedLinkColor = ThemeColors.Accent;
            }
            if (control is CheckBox chk)
            {
                chk.BackColor = Color.Transparent;
                chk.ForeColor = ThemeColors.Text;
                chk.UseVisualStyleBackColor = true;
            }
            if (control is RadioButton opt)
            {
                opt.BackColor = ThemeColors.Control;
            }
            if (control is ComboBox combo)
            {
                combo.ForeColor = ThemeColors.Text;
                combo.BackColor = ThemeColors.Window;
            }
            if (control is MenuStrip menu)
            {
                menu.RenderMode = ToolStripRenderMode.Professional;
                menu.Renderer = new MyRenderer(new CustomColorTable(ThemeColors), false)
                {
                    themeColors = ThemeColors
                };
            }
            if (control is ToolStrip toolBar)
            {
                toolBar.GripStyle = ToolStripGripStyle.Hidden;
                toolBar.RenderMode = ToolStripRenderMode.Professional;
                toolBar.Renderer = new MyRenderer(new CustomColorTable(ThemeColors), false) { themeColors = ThemeColors };
            }
            if (control is ContextMenuStrip cMenu)
            {
                cMenu.RenderMode = ToolStripRenderMode.Professional;
                cMenu.Renderer = new MyRenderer(new CustomColorTable(ThemeColors), false) { themeColors = ThemeColors };
            }
            if (control is DataGridView grid)
            {
                grid.EnableHeadersVisualStyles = false;
                grid.BorderStyle = BorderStyle.FixedSingle;
                grid.BackgroundColor = ThemeColors.Window;
                grid.GridColor = ThemeColors.Window;

                grid.DefaultCellStyle.BackColor = ThemeColors.Window;
                grid.DefaultCellStyle.ForeColor = ThemeColors.Text;


                grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeColors.Window;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = ThemeColors.Text;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeColors.Accent;
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

                grid.RowHeadersDefaultCellStyle.BackColor = ThemeColors.Window;
                grid.RowHeadersDefaultCellStyle.ForeColor = ThemeColors.Text;
                grid.RowHeadersDefaultCellStyle.SelectionBackColor = ThemeColors.Accent;
                grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            }
            if (control is PropertyGrid pGrid)
            {
                pGrid.BackColor = ThemeColors.Window;
                pGrid.ViewBackColor = ThemeColors.Window;
                pGrid.LineColor = ThemeColors.Window;
                pGrid.ViewForeColor = ThemeColors.Text;
                pGrid.ViewBorderColor = ThemeColors.ContainerBorder;
                pGrid.CategoryForeColor = ThemeColors.Text;
                pGrid.CategorySplitterColor = ThemeColors.ContainerHighlight;
            }
            if (control is TreeView tree)
            {
                tree.BorderStyle = BorderStyle.None;
                tree.BackColor = ThemeColors.Window;
            }
            if (control is TrackBar slider)
            {
                slider.BackColor = ThemeColors.Control;
            }
            if (control is CodeTextBox console)
            {
                console.IndentBackColor = ThemeColors.Window;
                console.ServiceLinesColor = ThemeColors.Window;
                console.BackColor = ThemeColors.ContainerBorder;
                console.FoldingIndicatorColor = ThemeColors.Control;
                var col = new FastColoredTextBoxNS.ServiceColors
                {
                    ExpandMarkerBackColor = ThemeColors.Control,
                    ExpandMarkerForeColor = ThemeColors.Text,
                    CollapseMarkerForeColor = ThemeColors.Text,
                    CollapseMarkerBackColor = ThemeColors.Control,
                    ExpandMarkerBorderColor = ControlPaint.Dark(ThemeColors.Text, 110),
                    CollapseMarkerBorderColor = ControlPaint.Dark(ThemeColors.Text, 90)
                };
                console.ServiceColors = col;
                console.ForeColor = ThemeColors.Text;
            }
            if (control is ByteViewer hexViewer)
            {
                //hexViewer.BackColor = ControlPaint.Dark(ThemeColors.Control, -10);
                //hexViewer.ForeColor = ThemeColors.TextActive;
            }
            if (control.ContextMenuStrip != null)
            {
                ThemeControl(control.ContextMenuStrip);
            }
            if (control is GLViewerMultiSelectionControl multiSelection)
            {
                multiSelection.BackColor = ThemeColors.Control;
                multiSelection.ForeColor = ThemeColors.Text;
            }
            if (control is ControlPanelView controlPanelView)
            {
                controlPanelView.BackColor = Color.Transparent;
                controlPanelView.Invalidate();
            }
            if (control is ListBox listBox)
            {
                listBox.ForeColor = ThemeColors.Text;
                listBox.BackColor = ThemeColors.Window;
            }
            if (control is NumericUpDown numeric)
            {
                numeric.ForeColor = ThemeColors.Text;
                numeric.BackColor = ThemeColors.Control;
            }
            if (control is TextBox textBox)
            {
                textBox.ForeColor = ThemeColors.Text;
                textBox.BackColor = ThemeColors.ContainerBorder;
                textBox.BorderStyle = BorderStyle.None;
            }
            if (control is BetterListView listView)
            {
                listView.BackColor = ThemeColors.Window;
                listView.ForeColor = ThemeColors.Text;
            }
            if (control is TreeView treeView)
            {
                treeView.BackColor = ThemeColors.Window;
                treeView.ForeColor = ThemeColors.Text;
                treeView.LineColor = ThemeColors.Window;
            }
            if (control is TabPage tabPage)
            {
                tabPage.Padding = new Padding(-10, 0, 0, 0);
                tabPage.BackColor = ThemeColors.Control;
                tabPage.ForeColor = ThemeColors.Text;
            }
            if (control is ProgressBar pgBar)
            {
                pgBar.BackColor = ThemeColors.Window;
                pgBar.ForeColor = ThemeColors.Accent;
            }

            ApplySystemTheme(control);

            foreach (Control childControl in control.Controls)
            {
                // Recursively process its children
                ThemeControl(childControl);
            }
        }

        /// <summary>Returns Windows Color Mode for Applications.
        /// <para>true=dark theme, false=light theme</para>
        /// </summary>
        public static bool IsWindowsDarkThemed(bool GetSystemColorModeInstead = false)
        {
            var intResult = 1;

            try
            {
                intResult = (int)Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    GetSystemColorModeInstead ? "SystemUsesLightTheme" : "AppsUseLightTheme",
                    -1);
            }
            catch
            {
                intResult = 1;
            }

            return intResult <= 0;
        }

        public static ThemeColors GetAppTheme(Form Window = null)
        {
            var themeColors = new ThemeColors();

            var IsDarkMode = (IsWindowsDarkThemed());

            if (IsDarkMode)
            {
                themeColors.Window = Color.FromArgb(42, 42, 42);
                themeColors.WindowBorder = Color.FromArgb(18, 18, 18);
                themeColors.WindowHighlight = Color.FromArgb(57, 57, 57);

                themeColors.Container = Color.FromArgb(43, 43, 43);
                themeColors.ContainerBorder = Color.FromArgb(30, 30, 30);
                themeColors.ContainerHighlight = Color.FromArgb(59, 59, 59);

                themeColors.Text = Color.White;
                themeColors.TextInactive = Color.FromArgb(176, 176, 176);
                themeColors.TextHighlight = Color.DodgerBlue;

                themeColors.Control = Color.FromArgb(55, 55, 55);
                themeColors.ControlBorder = Color.FromArgb(28, 28, 28);
                themeColors.ControlHighlight = Color.FromArgb(67, 67, 67);

                themeColors.Accent = Color.DodgerBlue;
            }
            else
            {
                themeColors.Window = Color.FromArgb(240, 240, 240);
                themeColors.WindowBorder = Color.FromArgb(200, 200, 200);
                themeColors.WindowHighlight = Color.FromArgb(255, 255, 255);

                themeColors.Container = Color.FromArgb(245, 245, 245);
                themeColors.ContainerBorder = Color.FromArgb(215, 215, 215);
                themeColors.ContainerHighlight = Color.FromArgb(255, 255, 255);

                themeColors.Text = Color.Black;
                themeColors.TextInactive = Color.FromArgb(100, 100, 100);
                themeColors.TextHighlight = Color.DodgerBlue;

                themeColors.Control = Color.FromArgb(220, 220, 220);
                themeColors.ControlBorder = Color.FromArgb(190, 190, 190);
                themeColors.ControlHighlight = Color.FromArgb(235, 235, 235);

                themeColors.Accent = Color.DodgerBlue;
            }

            if (DebugTheme)
            {
                themeColors.Window = Color.FromArgb(91, 206, 250);
                themeColors.WindowBorder = Color.FromArgb(21, 136, 180);
                themeColors.WindowHighlight = Color.FromArgb(131, 236, 255);

                themeColors.Container = Color.FromArgb(245, 169, 184);
                themeColors.ContainerBorder = Color.FromArgb(255, 179, 194);
                themeColors.ContainerHighlight = Color.FromArgb(185, 109, 124);

                themeColors.Text = Color.White;
                themeColors.TextInactive = Color.FromArgb(30, 30, 30);
                themeColors.TextHighlight = Color.DodgerBlue;

                themeColors.Control = Color.FromArgb(245, 169, 184);
                themeColors.ControlBorder = Color.FromArgb(245, 169, 184);
                themeColors.ControlHighlight = Color.FromArgb(255, 179, 194);

                themeColors.Accent = Color.DodgerBlue;
            }

            if (Window != null)
            {
                ApplySystemTheme(Window);

                Window.BackColor = themeColors.Window;
                Window.ForeColor = themeColors.Text;
            }

            return themeColors;
        }

        /// <summary>Recolor image</summary>
        /// <param name="bmp">Image to recolor</param>
        /// <param name="c">Color</param>
        public static Bitmap ChangeToColor(Bitmap bmp, Color c)
        {
            var bmp2 = new Bitmap(bmp.Width, bmp.Height);
            using (var g = Graphics.FromImage(bmp2))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                var tR = c.R / 255f;
                var tG = c.G / 255f;
                var tB = c.B / 255f;

                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                [
                [1,    0,  0,  0,  0],
                [0,    1,  0,  0,  0],
                [0,    0,  1,  0,  0],
                [0,    0,  0,  1,  0],  //<- not changing alpha
				[tR,   tG, tB, 0,  1]
                ]);

                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
                    0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);

                attributes.Dispose();
            }
            return bmp2;
        }
        public static Image ChangeToColor(Image bmp, Color c) => (Image)ChangeToColor((Bitmap)bmp, c);

        #endregion

        #region Private Methods

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMBOBOXINFO
        {
            internal int cbSize;
            internal Win32Rect rcItem;
            internal Win32Rect rcButton;
            internal int stateButton;
            internal IntPtr hwndCombo;
            internal IntPtr hwndItem;
            internal IntPtr hwndList;

            internal COMBOBOXINFO(int size)
            {
                cbSize = size;
                rcItem = Win32Rect.Empty;
                rcButton = Win32Rect.Empty;
                stateButton = 0;
                hwndCombo = IntPtr.Zero;
                hwndItem = IntPtr.Zero;
                hwndList = IntPtr.Zero;
            }
        };
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Rect
        {
            internal int left;
            internal int top;
            internal int right;
            internal int bottom;
            static internal Win32Rect Empty
            {
                get
                {
                    return new Win32Rect(0, 0, 0, 0);
                }
            }

            internal Win32Rect(int _left, int _top, int _right, int _bottom)
            {
                left = _left; top = _top; right = _right; bottom = _bottom;
            }
        }

        private static bool DebugTheme;

        private void ApplyTheme(Form _Form)
        {
            IsDarkMode = IsWindowsDarkThemed();
            ThemeColors = GetAppTheme(_Form);

            if (ThemeColors != null)
            {
                if (_Form != null && _Form.Controls != null)
                {
                    foreach (Control _control in _Form.Controls)
                    {
                        ThemeControl(_control);
                    }

                    void ControlAdded(object sender, ControlEventArgs e)
                    {
                        ThemeControl(e.Control);
                    };
                    void ControlDisposed(object sender, EventArgs e)
                    {
                        _Form.ControlAdded -= ControlAdded;
                        _Form.Disposed -= ControlDisposed;
                    };
                    _Form.ControlAdded += ControlAdded;
                    _Form.Disposed += ControlDisposed;
                }
            }
        }

        private void OnUserPreferenceChanged(object sender, System.EventArgs e)
        {
            var currentTheme = IsWindowsDarkThemed();
            if (IsDarkMode != currentTheme)
            {
                IsDarkMode = IsWindowsDarkThemed();

                foreach (Form form in Application.OpenForms)
                {
                    ApplyTheme(form);
                    form.Invalidate();
                }
            }
        }

        /// <summary>Attemps to apply Window's Dark Style to the Control and all its childs.</summary>
        /// <param name="control"></param>
        private static void ApplySystemTheme(Control control = null)
        {
            if (control is ByteViewer)
            {
                return;
            }
            /* 			    
				DWMWA_USE_IMMERSIVE_DARK_MODE:   https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute

				Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. 
				For compatibility reasons, all windows default to light mode regardless of the system setting. 
				The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode.

				This value is supported starting with Windows 11 Build 22000.

				SetWindowTheme:     https://learn.microsoft.com/en-us/windows/win32/api/uxtheme/nf-uxtheme-setwindowtheme
				Causes a window to use a different set of visual style information than its class normally uses.
			 */
            int DarkModeOn = 0; //<- 1=True, 0=False

            var windowsTheme = "Explorer";
            var windowsThemeCombo = "Explorer";

            if (IsWindowsDarkThemed())
            {
                windowsTheme = "DarkMode_Explorer";
                windowsThemeCombo = "DarkMode_CFD";
                DarkModeOn = 1;
            }
            else
            {
                DarkModeOn = 0;
            }


            if (control is System.Windows.Forms.ComboBox comboBox)
            {
                _ = Windows.Win32.PInvoke.SetWindowTheme((Windows.Win32.Foundation.HWND)comboBox.Handle, windowsThemeCombo, null);

                // Style the ComboBox drop-down (including its ScrollBar(s)):
                Windows.Win32.UI.Controls.COMBOBOXINFO cInfo = default;
                var result = Windows.Win32.PInvoke.GetComboBoxInfo((Windows.Win32.Foundation.HWND)comboBox.Handle, ref cInfo);
                _ = Windows.Win32.PInvoke.SetWindowTheme(cInfo.hwndList, windowsThemeCombo, null);
            }
            else
            {
                _ = Windows.Win32.PInvoke.SetWindowTheme((Windows.Win32.Foundation.HWND)control.Handle, windowsTheme, null);
            }
            unsafe
            {
                if (Windows.Win32.PInvoke.DwmSetWindowAttribute((Windows.Win32.Foundation.HWND)control.Handle, (Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, &DarkModeOn, sizeof(bool)) != 0)
                {
                    _ = Windows.Win32.PInvoke.DwmSetWindowAttribute((Windows.Win32.Foundation.HWND)control.Handle, (Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &DarkModeOn, sizeof(bool));
                }
            }

            foreach (Control child in control.Controls)
            {
                if (child.Controls.Count != 0)
                {
                    ApplySystemTheme(child);
                }
            }
        }
        #endregion
    }

    public class ThemeColors
    {
        public ThemeColors() { }

        /// <summary>For the very back of the Window</summary>
        public System.Drawing.Color Window { get; set; }
        /// <summary>For Borders around the Background</summary>
        public System.Drawing.Color WindowBorder { get; set; }
        /// <summary>For hightlights over the Background</summary>
        public System.Drawing.Color WindowHighlight { get; set; }

        /// <summary>For Container above the Background</summary>
        public System.Drawing.Color Container { get; set; }
        /// <summary>For Borders around the Surface</summary>
        public System.Drawing.Color ContainerBorder { get; set; }
        /// <summary>For Highligh over the Surface</summary>
        public System.Drawing.Color ContainerHighlight { get; set; }

        /// <summary>For Main Texts</summary>
        public System.Drawing.Color Text { get; set; }
        /// <summary>For Inactive Texts</summary>
        public System.Drawing.Color TextInactive { get; set; }
        /// <summary>For Hightligh Texts</summary>
        public System.Drawing.Color TextHighlight { get; set; }

        /// <summary>For the background of any Control</summary>
        public System.Drawing.Color Control { get; set; }
        /// <summary>For Borders of any Control</summary>
        public System.Drawing.Color ControlBorder { get; set; }
        /// <summary>For Highlight elements in a Control</summary>
        public System.Drawing.Color ControlHighlight { get; set; }

        /// <summary>For anything that accented like hovering over a tab</summary>
        public System.Drawing.Color Accent { get; set; }
    }

    /* Custom Renderers for Menus and ToolBars */
    public class MyRenderer : ToolStripProfessionalRenderer
    {
        public bool ColorizeIcons { get; set; } = true;
        public ThemeColors themeColors { get; set; } //<- Your Custom Colors Colection

        public MyRenderer(ProfessionalColorTable table, bool pColorizeIcons = true) : base(table)
        {
            ColorizeIcons = pColorizeIcons;
        }

        // Background of the whole ToolBar Or MenuBar:
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.ToolStrip.BackColor = themeColors.Window;
            base.OnRenderToolStripBackground(e);
        }

        // For Normal Buttons on a ToolBar:
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            var bounds = new Rectangle(Point.Empty, e.Item.Size);

            var gradientBegin = themeColors.Container;
            var gradientEnd = themeColors.Container;

            var BordersPencil = new Pen(themeColors.Container);

            var button = e.Item as ToolStripButton;
            if (button.Pressed || button.Checked)
            {
                gradientBegin = themeColors.Control;
                gradientEnd = themeColors.Control;
            }
            else if (button.Selected)
            {
                gradientBegin = themeColors.Accent;
                gradientEnd = themeColors.Accent;
            }

            using Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical);

            g.FillRectangle(b, bounds);

            e.Graphics.DrawRectangle(
                BordersPencil,
                bounds);

            g.DrawLine(
                BordersPencil,
                bounds.X,
                bounds.Y,
                bounds.Width - 1,
                bounds.Y);

            g.DrawLine(
                BordersPencil,
                bounds.X,
                bounds.Y,
                bounds.X,
                bounds.Height - 1);

            var toolStrip = button.Owner;

            if (button.Owner.GetItemAt(button.Bounds.X, button.Bounds.Bottom + 1) is not ToolStripButton nextItem)
            {
                g.DrawLine(
                    BordersPencil,
                    bounds.X,
                    bounds.Height - 1,
                    bounds.X + bounds.Width - 1,
                    bounds.Height - 1);
            }

            BordersPencil.Dispose();
        }

        // For DropDown Buttons on a ToolBar:
        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            var gradientBegin = themeColors.Container; // Color.FromArgb(203, 225, 252);
            var gradientEnd = themeColors.Container;

            using var BordersPencil = new Pen(themeColors.Container);

            //1. Determine the colors to use:
            if (e.Item.Pressed)
            {
                gradientBegin = themeColors.Control;
                gradientEnd = themeColors.Control;
            }
            else if (e.Item.Selected)
            {
                gradientBegin = themeColors.Accent;
                gradientEnd = themeColors.Accent;
            }

            //2. Draw the Box around the Control
            using Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(b, bounds);

            //3. Draws the Chevron:
            #region Chevron

            //int Padding = 2; //<- From the right side
            //Size cSize = new Size(8, 4); //<- Size of the Chevron: 8x4 px
            //Pen ChevronPen = new Pen(MyColors.TextInactive, 2); //<- Color and Border Width
            //Point P1 = new Point(bounds.Width - (cSize.Width + Padding), (bounds.Height / 2) - (cSize.Height / 2));
            //Point P2 = new Point(bounds.Width - Padding, (bounds.Height / 2) - (cSize.Height / 2));
            //Point P3 = new Point(bounds.Width - (cSize.Width / 2 + Padding), (bounds.Height / 2) + (cSize.Height / 2));

            //e.Graphics.DrawLine(ChevronPen, P1, P3);
            //e.Graphics.DrawLine(ChevronPen, P2, P3);

            #endregion
        }

        // For SplitButtons on a ToolBar:
        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            var gradientBegin = themeColors.Container; // Color.FromArgb(203, 225, 252);
            var gradientEnd = themeColors.Container;

            //1. Determine the colors to use:
            if (e.Item.Pressed)
            {
                gradientBegin = themeColors.Control;
                gradientEnd = themeColors.Control;
            }
            else if (e.Item.Selected)
            {
                gradientBegin = themeColors.Accent;
                gradientEnd = themeColors.Accent;
            }

            //2. Draw the Box around the Control
            using Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(b, bounds);

            //3. Draws the Chevron:
            #region Chevron

            var Padding = 2; //<- From the right side
            var cSize = new Size(8, 4); //<- Size of the Chevron: 8x4 px
            var ChevronPen = new Pen(themeColors.TextInactive, 2); //<- Color and Border Width
            var P1 = new Point(bounds.Width - (cSize.Width + Padding), (bounds.Height / 2) - (cSize.Height / 2));
            var P2 = new Point(bounds.Width - Padding, (bounds.Height / 2) - (cSize.Height / 2));
            var P3 = new Point(bounds.Width - (cSize.Width / 2 + Padding), (bounds.Height / 2) + (cSize.Height / 2));

            e.Graphics.DrawLine(ChevronPen, P1, P3);
            e.Graphics.DrawLine(ChevronPen, P2, P3);

            ChevronPen.Dispose();

            #endregion
        }

        // For the Text Color of all Items:
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Enabled)
            {
                e.TextColor = themeColors.Text;
            }
            else
            {
                e.TextColor = themeColors.TextInactive;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
        {
            base.OnRenderItemBackground(e);

            //// Only draw border for ComboBox items
            //if (e.Item is ComboBox)
            //{
            //    Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
            //    e.Graphics.DrawRectangle(new Pen(MyColors.ControlLight, 1), rect);
            //}
        }

        // For Menu Items BackColor:
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            var bounds = new Rectangle(Point.Empty, e.Item.Size);

            var gradientBegin = themeColors.Container;
            var gradientEnd = themeColors.Container;

            var DrawIt = false;
            var _menu = e.Item as ToolStripItem;
            if (_menu.Pressed)
            {
                gradientBegin = themeColors.Control;
                gradientEnd = themeColors.Control;
                DrawIt = true;
            }
            else if (_menu.Selected)
            {
                gradientBegin = themeColors.Accent;
                gradientEnd = themeColors.Accent;
                DrawIt = true;
            }

            if (DrawIt)
            {
                using Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical);
                g.FillRectangle(b, bounds);
            }
        }

        // Re-Colors the Icon Images to a Clear color:
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (ColorizeIcons && e.Image != null)
            {
                // Get the current icon
                var image = e.Image;
                var _ClearColor = e.Item.Enabled ? themeColors.Text : themeColors.WindowBorder;

                // Create a new image with the desired color adjustments
                using var adjustedImage = DarkModeCS.ChangeToColor(image, _ClearColor);
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.DrawImage(adjustedImage, e.ImageRectangle);
            }
            else
            {
                base.OnRenderItemImage(e);
            }
        }

    }
    public class CustomColorTable : ProfessionalColorTable
    {
        public ThemeColors Colors { get; set; }

        public CustomColorTable(ThemeColors _Colors)
        {
            Colors = _Colors;
            base.UseSystemColors = false;
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Colors.Container; }
        }
        public override Color ImageMarginGradientMiddle
        {
            get { return Colors.Container; }
        }
        public override Color ImageMarginGradientEnd
        {
            get { return Colors.Container; }
        }
    }
}
