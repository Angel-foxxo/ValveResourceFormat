using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GUI.Controls;
using System;
using System.ComponentModel.Design;
using static GUI.Utils.NativeMethods;
using GUI;
using GUI.Types.PackageViewer;

namespace DarkModeForms
{
    /// <summary>This tries to automatically apply Windows Dark Mode (if enabled) to a Form.
    /// <para>Author: DarkModeForms (DarkModeForms.play@gmail.com)  2024</para></summary>
    public partial class DarkModeCS
    {
        #region Win32 API Declarations

        public struct DWMCOLORIZATIONcolors
        {
            internal uint ColorizationColor,
                ColorizationAfterglow,
                ColorizationColorBalance,
                ColorizationAfterglowBalance,
                ColorizationBlurBalance,
                ColorizationGlassReflectionIntensity,
                ColorizationOpaqueBlend;
        }

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


        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangle()
            {
                return Rectangle.FromLTRB(Left, Top, Right, Bottom);
            }
        }

        public const int EM_SETCUEBANNER = 5377;


        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        public static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [LibraryImport("DwmApi")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        public static partial int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        [LibraryImport("dwmapi.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        public static partial int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [LibraryImport("uxtheme.dll", StringMarshalling = StringMarshalling.Utf16)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        private static partial int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [LibraryImport("dwmapi.dll", EntryPoint = "#127")]
#pragma warning disable CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
        public static partial void DwmGetColorizationParameters(ref DWMCOLORIZATIONcolors colors);
#pragma warning restore CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes

        [LibraryImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        private static partial IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
        );

        [LibraryImport("user32")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        private static partial IntPtr GetDC(IntPtr hwnd);

        [LibraryImport("user32")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        private static partial IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        public static IntPtr GetHeaderControl(ListView list)
        {
            const int LVM_GETHEADER = 0x1000 + 31;
            return SendMessage(list.Handle, LVM_GETHEADER, IntPtr.Zero, "");
        }

        #endregion

        /// <summary>'true' if Dark Mode Color is set in Windows's Settings.</summary>
        public bool IsDarkMode { get; set; }

        /// <summary>Windows Colors. Can be customized.</summary>
        public OSThemeColors OScolors { get; set; }

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
            if (!StyledForms.Contains(_Form))
            {
                StyledForms.Add(_Form);
            }
        }

        /// <summary>Recursively apply the Colors from 'OScolors' to the Control and all its childs.</summary>
        /// <param name="control">Can be a Form or any Winforms Control.</param>
        public void ThemeControl(Control control)
        {
            BorderStyle BStyle = BorderStyle.FixedSingle;
            FlatStyle FStyle = FlatStyle.Flat;

            //Change the Colors only if its the default ones, this allows the user to set own colors:
            if (control.BackColor == SystemColors.Control || control.BackColor == SystemColors.Window)
            {
                control.GetType().GetProperty("BackColor")?.SetValue(control, OScolors.Control);
            }
            if (control.ForeColor == SystemColors.ControlText || control.ForeColor == SystemColors.WindowText)
            {
                control.GetType().GetProperty("ForeColor")?.SetValue(control, OScolors.TextActive);
            }

            var borderStyleInfo = control.GetType().GetProperty("BorderStyle");
            if (borderStyleInfo != null)
            {
                BorderStyle borderStyle = (BorderStyle)borderStyleInfo.GetValue(control);
                if ((BorderStyle)borderStyle != BorderStyle.None)
                {
                    borderStyleInfo.SetValue(control, BStyle);
                }
            }

            control.HandleCreated += (object sender, EventArgs e) =>
            {
                ApplySystemTheme(control);
            };


            control.ControlAdded += (object sender, ControlEventArgs e) =>
            {
                ThemeControl(e.Control);
            };

            if (control is Panel panel)
            {
                // Process the panel within the container
                panel.BackColor = OScolors.Surface;
                panel.BorderStyle = BorderStyle.None;
            }
            if (control is GroupBox group)
            {
                group.BackColor = group.Parent.BackColor;
                group.ForeColor = OScolors.TextActive;
            }
            if (control is TableLayoutPanel table)
            {
                // Process the panel within the container
                table.BackColor = table.Parent.BackColor;
                table.BorderStyle = BorderStyle.None;
            }
            if (control is FlatTabControl fTab)
            {
                fTab.BackColor = OScolors.Background;
                fTab.TabColor = OScolors.Surface;
                fTab.SelectTabColor = OScolors.ControlLight;
                fTab.SelectedForeColor = OScolors.TextActive;
                fTab.BorderColor = OScolors.Background;
                fTab.ForeColor = OScolors.TextActive;
                fTab.LineColor = OScolors.Background;
                fTab.Margin = new Padding(-10, 0, 0, 0);
            }
            if (control is PictureBox pic)
            {
                pic.BorderStyle = BorderStyle.None;
                pic.BackColor = pic.Parent.BackColor;
            }
            if (control is ListView lView)
            {
                if (lView.View == View.Details)
                {
                    lView.OwnerDraw = true;
                    lView.DrawColumnHeader += (object sender, DrawListViewColumnHeaderEventArgs e) =>
                    {
                        using (SolidBrush backBrush = new SolidBrush(OScolors.ControlLight))
                        {
                            using (SolidBrush foreBrush = new SolidBrush(OScolors.TextActive))
                            {
                                using (var sf = new StringFormat())
                                {
                                    sf.Alignment = StringAlignment.Center;
                                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                                    e.Graphics.DrawString(e.Header.Text, lView.Font, foreBrush, e.Bounds, sf);
                                }
                            }
                        }

                    };
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
                button.FlatAppearance.CheckedBackColor = OScolors.Control;
                button.BackColor = OScolors.Surface;
                button.FlatAppearance.BorderColor = OScolors.SurfaceDark;
                button.ForeColor = OScolors.TextActive;
            }
            if (control is Label label)
            {
                label.BorderStyle = BorderStyle.None;
                label.ForeColor = OScolors.TextActive;
                label.BackColor = label.Parent.BackColor;
            }
            if (control is LinkLabel link)
            {
                link.LinkColor = OScolors.AccentLight;
                link.VisitedLinkColor = OScolors.Primary;
            }
            if (control is CheckBox chk)
            {
                chk.BackColor = chk.Parent.BackColor;
                chk.ForeColor = OScolors.TextActive;
                chk.UseVisualStyleBackColor = true;
            }
            if (control is RadioButton opt)
            {
                opt.BackColor = opt.Parent.BackColor;
            }
            if (control is ComboBox combo)
            {
                var themeStringCombo = "Explorer";
                var themeStringComboDropdown = "Explorer";

                if (IsDarkMode)
                {
                    themeStringCombo = "DarkMode_CFD";
                    themeStringComboDropdown = "DarkMode_Explorer";
                }
                else
                {
                    themeStringCombo = "Explorer";
                    themeStringComboDropdown = "Explorer";
                }

                SetWindowTheme(control.Handle, themeStringCombo, null);
                COMBOBOXINFO cInfo = default;

                // Style the ComboBox drop-down (including its ScrollBar(s)):
                var result = GetComboBoxInfo(control.Handle, ref cInfo);
                SetWindowTheme(cInfo.hwndList, themeStringComboDropdown, null);

                combo.ForeColor = OScolors.TextActive;
                combo.BackColor = OScolors.Control;
            }
            if (control is MenuStrip menu)
            {
                menu.RenderMode = ToolStripRenderMode.Professional;
                menu.Renderer = new MyRenderer(new CustomColorTable(OScolors), false)
                {
                    MyColors = OScolors
                };
            }
            if (control is ToolStrip toolBar)
            {
                toolBar.GripStyle = ToolStripGripStyle.Hidden;
                toolBar.RenderMode = ToolStripRenderMode.Professional;
                toolBar.Renderer = new MyRenderer(new CustomColorTable(OScolors), false) { MyColors = OScolors };
            }
            if (control is ContextMenuStrip cMenu)
            {
                cMenu.RenderMode = ToolStripRenderMode.Professional;
                cMenu.Renderer = new MyRenderer(new CustomColorTable(OScolors), false) { MyColors = OScolors };
            }
            if (control is DataGridView grid)
            {
                grid.EnableHeadersVisualStyles = false;
                grid.BorderStyle = BorderStyle.FixedSingle;
                grid.BackgroundColor = OScolors.Control;
                grid.GridColor = OScolors.Control;

                grid.DefaultCellStyle.BackColor = OScolors.Surface;
                grid.DefaultCellStyle.ForeColor = OScolors.TextActive;


                grid.ColumnHeadersDefaultCellStyle.BackColor = OScolors.Surface;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = OScolors.TextActive;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = OScolors.AccentOpaque;
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

                grid.RowHeadersDefaultCellStyle.BackColor = OScolors.Surface;
                grid.RowHeadersDefaultCellStyle.ForeColor = OScolors.TextActive;
                grid.RowHeadersDefaultCellStyle.SelectionBackColor = OScolors.AccentOpaque;
                grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            }
            if (control is PropertyGrid pGrid)
            {
                pGrid.BackColor = OScolors.Control;
                pGrid.ViewBackColor = OScolors.Control;
                pGrid.LineColor = OScolors.Surface;
                pGrid.ViewForeColor = OScolors.TextActive;
                pGrid.ViewBorderColor = OScolors.ControlDark;
                pGrid.CategoryForeColor = OScolors.TextActive;
                pGrid.CategorySplitterColor = OScolors.ControlLight;
            }
            if (control is TreeView tree)
            {
                tree.BorderStyle = BorderStyle.None;
                tree.BackColor = OScolors.Surface;
            }
            if (control is TrackBar slider)
            {
                slider.BackColor = control.Parent.BackColor;
            }
            if (control is CodeTextBox console)
            {
                var dimmingFactor = 0.9;
                var dimmedBackground = Color.FromArgb(
                    (int)(control.Parent.BackColor.A * dimmingFactor),
                    (int)(control.Parent.BackColor.R * dimmingFactor),
                    (int)(control.Parent.BackColor.G * dimmingFactor),
                    (int)(control.Parent.BackColor.B * dimmingFactor));

                console.IndentBackColor = control.Parent.BackColor;
                console.ServiceLinesColor = control.Parent.BackColor;
                console.BackColor = dimmedBackground;
                console.FoldingIndicatorColor = control.Parent.BackColor;
                var col = new FastColoredTextBoxNS.ServiceColors();
                col.ExpandMarkerBackColor = control.Parent.BackColor;
                col.ExpandMarkerForeColor = control.Parent.ForeColor;
                col.CollapseMarkerForeColor = control.Parent.ForeColor;
                col.CollapseMarkerBackColor = control.Parent.BackColor;
                col.ExpandMarkerBorderColor = ControlPaint.Dark(control.Parent.ForeColor, 110);
                col.CollapseMarkerBorderColor = ControlPaint.Dark(control.Parent.ForeColor, 90);
                console.ServiceColors = col;
                console.ForeColor = OScolors.TextActive;
            }
            if (control is ByteViewer hexViewer)
            {
                hexViewer.BackColor = ControlPaint.Dark(control.Parent.BackColor, -10);
                hexViewer.ForeColor = OScolors.TextActive;
            }
            if (control.ContextMenuStrip != null)
            {
                ThemeControl(control.ContextMenuStrip);
            }
            if (control is GLViewerMultiSelectionControl multiSelection)
            {
                multiSelection.BackColor = control.Parent.BackColor;
                multiSelection.ForeColor = OScolors.TextActive;
            }
            if (control is ControlPanelView controlPanelView)
            {
                controlPanelView.BackColor = Color.Transparent;
                controlPanelView.Invalidate();
            }
            if (control is ListBox listBox)
            {
                listBox.ForeColor = OScolors.TextActive;
                listBox.BackColor = OScolors.Control;
            }
            if (control is NumericUpDown numeric)
            {
                numeric.ForeColor = OScolors.TextActive;
                numeric.BackColor = OScolors.Control;
            }
            if (control is TextBox textBox)
            {
                textBox.ForeColor = OScolors.TextActive;
                textBox.BackColor = OScolors.SurfaceDark;
                textBox.BorderStyle = BorderStyle.None;
            }
            if (control is BetterListView listView)
            {
                listView.BackColor = OScolors.Control;
                listView.ForeColor = OScolors.TextActive;
            }
            if (control is TreeView treeView)
            {
                treeView.BackColor = OScolors.Control;
                treeView.ForeColor = OScolors.TextActive;
                treeView.LineColor = OScolors.Surface;

                var themeStringCombo = "Explorer";
                var themeStringComboDropdown = "Explorer";

                if (IsDarkMode)
                {
                    themeStringCombo = "DarkMode_CFD";
                    themeStringComboDropdown = "DarkMode_Explorer";
                }
                else
                {
                    themeStringCombo = "Explorer";
                    themeStringComboDropdown = "Explorer";
                }

                SetWindowTheme(control.Handle, themeStringComboDropdown, null);
            }
            if (control is TabPage tabPage)
            {
                tabPage.Padding = new Padding(-10, 0, 0, 0);
            }
            if(control is ProgressBar pgBar)
            {
                pgBar.BackColor = OScolors.Control;
                pgBar.ForeColor = Color.DodgerBlue;
            }

            foreach (Control childControl in control.Controls)
            {
                // Recursively process its children
                ThemeControl(childControl);
            }
        }

        private void Tree_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            throw new NotImplementedException();
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

            return intResult <= 0 ? true : false;
        }

        /// <summary>Returns the Accent Color used by Windows.</summary>
        /// <returns>a Color</returns>
        public static Color GetWindowsAccentColor()
        {
            DWMCOLORIZATIONcolors colors = new DWMCOLORIZATIONcolors();
            DwmGetColorizationParameters(ref colors);

            //get the theme --> only if Windows 10 or newer
            if (IsWindows10orGreater())
            {
                var color = colors.ColorizationColor;

                var colorValue = long.Parse(color.ToString(), System.Globalization.NumberStyles.HexNumber);

                var transparency = (colorValue >> 24) & 0xFF;
                var red = (colorValue >> 16) & 0xFF;
                var green = (colorValue >> 8) & 0xFF;
                var blue = (colorValue >> 0) & 0xFF;

                return Color.FromArgb((int)transparency, (int)red, (int)green, (int)blue);
            }
            else
            {
                return Color.CadetBlue;
            }
        }

        /// <summary>Returns the Accent Color used by Windows.</summary>
        /// <returns>an opaque Color</returns>
        public static Color GetWindowsAccentOpaqueColor()
        {
            DWMCOLORIZATIONcolors colors = new DWMCOLORIZATIONcolors();
            DwmGetColorizationParameters(ref colors);

            //get the theme --> only if Windows 10 or newer
            if (IsWindows10orGreater())
            {
                var color = colors.ColorizationColor;

                var colorValue = long.Parse(color.ToString(), System.Globalization.NumberStyles.HexNumber);

                var red = (colorValue >> 16) & 0xFF;
                var green = (colorValue >> 8) & 0xFF;
                var blue = (colorValue >> 0) & 0xFF;

                return Color.FromArgb(255, (int)red, (int)green, (int)blue);
            }
            else
            {
                return Color.CadetBlue;
            }
        }

        /// <summary>Returns Windows's System Colors for UI components following Google Material Design concepts.</summary>
        /// <param name="Window">[OPTIONAL] Applies DarkMode (if set) to this Window Title and Background.</param>
        /// <returns>List of Colors:  Background, OnBackground, Surface, OnSurface, Primary, OnPrimary, Secondary, OnSecondary</returns>
        public static OSThemeColors GetSystemColors(Form Window = null)
        {
            OSThemeColors _ret = new OSThemeColors();

            bool IsDarkMode = (IsWindowsDarkThemed());

            if (IsDarkMode)
            {
                _ret.Background = Color.FromArgb(32, 32, 32);
                _ret.BackgroundDark = Color.FromArgb(18, 18, 18);
                _ret.BackgroundLight = ControlPaint.Light(_ret.Background);

                _ret.Surface = Color.FromArgb(43, 43, 43);
                _ret.SurfaceLight = Color.FromArgb(50, 50, 50);
                _ret.SurfaceDark = Color.FromArgb(29, 29, 29);

                _ret.TextActive = Color.White;
                _ret.TextInactive = Color.FromArgb(176, 176, 176);
                _ret.TextInAccent = GetReadableColor(_ret.Accent);

                _ret.Control = Color.FromArgb(55, 55, 55);
                _ret.ControlDark = ControlPaint.Dark(_ret.Control);
                _ret.ControlLight = Color.FromArgb(67, 67, 67);

                _ret.Primary = Color.FromArgb(3, 218, 198);
                _ret.Secondary = Color.MediumSlateBlue;
            }

            if (DebugTheme)
            {
                _ret.Background = Color.FromArgb(91, 206, 250);
                _ret.BackgroundDark = Color.FromArgb(21, 136, 180);
                _ret.BackgroundLight = ControlPaint.Light(_ret.Background);

                _ret.Surface = Color.FromArgb(245, 169, 184);
                _ret.SurfaceLight = Color.FromArgb(255, 179, 194);
                _ret.SurfaceDark = Color.FromArgb(185, 109, 124);

                _ret.TextActive = Color.Black;
                _ret.TextInactive = Color.FromArgb(30, 30, 30);
                _ret.TextInAccent = GetReadableColor(_ret.Accent);

                _ret.Control = Color.FromArgb(245, 169, 184);
                _ret.ControlDark = ControlPaint.Dark(_ret.Control);
                _ret.ControlLight = Color.FromArgb(255, 179, 194);

                _ret.Primary = Color.FromArgb(3, 218, 198);
                _ret.Secondary = Color.MediumSlateBlue;
            }

            if (Window != null)
            {
                ApplySystemTheme(Window);

                Window.BackColor = _ret.Background;
                Window.ForeColor = _ret.TextInactive;
            }

            return _ret;
        }

        /// <summary>Recolor image</summary>
        /// <param name="bmp">Image to recolor</param>
        /// <param name="c">Color</param>
        public static Bitmap ChangeToColor(Bitmap bmp, Color c)
        {
            Bitmap bmp2 = new Bitmap(bmp.Width, bmp.Height);
            using (Graphics g = Graphics.FromImage(bmp2))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                float tR = c.R / 255f;
                float tG = c.G / 255f;
                float tB = c.B / 255f;

                System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                new float[] { 1,    0,  0,  0,  0 },
                new float[] { 0,    1,  0,  0,  0 },
                new float[] { 0,    0,  1,  0,  0 },
                new float[] { 0,    0,  0,  1,  0 },  //<- not changing alpha
				new float[] { tR,   tG, tB, 0,  1 }
                });

                System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes();
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

        private static bool DebugTheme;

        private void ApplyTheme(Form _Form)
        {
            IsDarkMode = IsWindowsDarkThemed();
            OScolors = GetSystemColors(_Form);

            if (OScolors != null)
            {
                if (_Form != null && _Form.Controls != null)
                {
                    foreach (Control _control in _Form.Controls)
                    {
                        ThemeControl(_control);
                    }
                    _Form.ControlAdded += (object sender, ControlEventArgs e) =>
                    {
                        ThemeControl(e.Control);
                    };
                }
            }
        }

        // all forms that we are styling, needed for restyling in case theme changes while the app is running
        private static List<Form> StyledForms = new List<Form>();

        private void OnUserPreferenceChanged(object sender, System.EventArgs e)
        {
            var currentTheme = IsWindowsDarkThemed();
            if (IsDarkMode != currentTheme)
            {
                IsDarkMode = IsWindowsDarkThemed();
            }

            //if we close a menu then it gets disposed and needs to be removed from the list
            List<Form> disposedFormsToRemove = new List<Form>();
            foreach (Form form in StyledForms)
            {
                if (!form.IsDisposed)
                {
                    ApplyTheme(form);
                    form.Invalidate();
                }
                else
                {
                    disposedFormsToRemove.Add(form);
                }
            }

            foreach (var item in disposedFormsToRemove)
            {
                StyledForms.Remove(item);
            }
        }

        /// <summary>Attemps to apply Window's Dark Style to the Control and all its childs.</summary>
        /// <param name="control"></param>
        private static void ApplySystemTheme(Control control = null)
        {
            /* 			    
				DWMWA_USE_IMMERSIVE_DARK_MODE:   https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute

				Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. 
				For compatibility reasons, all windows default to light mode regardless of the system setting. 
				The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode.

				This value is supported starting with Windows 11 Build 22000.

				SetWindowTheme:     https://learn.microsoft.com/en-us/windows/win32/api/uxtheme/nf-uxtheme-setwindowtheme
				Causes a window to use a different set of visual style information than its class normally uses.
			 */
            int[] DarkModeOn = new[] { 0 }; //<- 1=True, 0=False

            string windowsTheme = "Explorer";

            if (IsWindowsDarkThemed())
            {
                windowsTheme = "DarkMode_Explorer";
                DarkModeOn = new[] { 1 };
            }
            else
            {
                windowsTheme = "Explorer";
                DarkModeOn = new[] { 0 };
            }

            _ = SetWindowTheme(control.Handle, windowsTheme, null);

            if (DwmSetWindowAttribute(control.Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, DarkModeOn, 4) != 0)
                _ = DwmSetWindowAttribute(control.Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, DarkModeOn, 4);

            foreach (Control child in control.Controls)
            {
                if (child.Controls.Count != 0)
                    ApplySystemTheme(child);
            }
        }

        private static bool IsWindows10orGreater()
        {
            if (WindowsVersion() >= 10)
                return true;
            else
                return false;
        }

        private static int WindowsVersion()
        {
            //for .Net4.8 and Minor
            int result;
            try
            {
                var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                string[] productName = reg.GetValue("ProductName").ToString().Split((char)32);
                if (!(int.TryParse(productName[1], out result)))
                {
                    throw new InvalidOperationException();
                }
            }
            catch (Exception)
            {
                OperatingSystem os = Environment.OSVersion;
                result = os.Version.Major;
            }

            return result;

            //fixed .Net6
            //return System.Environment.OSVersion.Version.Major;
        }

        private static Color GetReadableColor(Color backgroundColor)
        {
            // Calculate the relative luminance of the background color.
            // Normalize values to 0-1 range first.
            double normalizedR = backgroundColor.R / 255.0;
            double normalizedG = backgroundColor.G / 255.0;
            double normalizedB = backgroundColor.B / 255.0;
            double luminance = 0.299 * normalizedR + 0.587 * normalizedG + 0.114 * normalizedB;

            // Choose a contrasting foreground color based on the luminance,
            // with a slight bias towards lighter colors for better readability.
            return luminance < 0.5 ? Color.FromArgb(182, 180, 215) : Color.FromArgb(34, 34, 34); // Dark gray for light backgrounds
        }
        #endregion
    }

    /// <summary>Windows 10+ System Colors for Clear Color Mode.</summary>
    public class OSThemeColors
    {
        public OSThemeColors() { }

        /// <summary>For the very back of the Window</summary>
        public System.Drawing.Color Background { get; set; } = SystemColors.Control;
        /// <summary>For Borders around the Background</summary>
        public System.Drawing.Color BackgroundDark { get; set; } = SystemColors.ControlDark;
        /// <summary>For hightlights over the Background</summary>
        public System.Drawing.Color BackgroundLight { get; set; } = SystemColors.ControlLight;

        /// <summary>For Container above the Background</summary>
        public System.Drawing.Color Surface { get; set; } = SystemColors.ControlLightLight;
        /// <summary>For Borders around the Surface</summary>
        public System.Drawing.Color SurfaceDark { get; set; } = SystemColors.ControlLight;
        /// <summary>For Highligh over the Surface</summary>
        public System.Drawing.Color SurfaceLight { get; set; } = Color.White;

        /// <summary>For Main Texts</summary>
        public System.Drawing.Color TextActive { get; set; } = SystemColors.WindowText;
        /// <summary>For Inactive Texts</summary>
        public System.Drawing.Color TextInactive { get; set; } = SystemColors.MenuText;
        /// <summary>For Hightligh Texts</summary>
        public System.Drawing.Color TextInAccent { get; set; } = SystemColors.HighlightText;

        /// <summary>For the background of any Control</summary>
        public System.Drawing.Color Control { get; set; } = SystemColors.ButtonFace;
        /// <summary>For Bordes of any Control</summary>
        public System.Drawing.Color ControlDark { get; set; } = SystemColors.ButtonShadow;
        /// <summary>For Highlight elements in a Control</summary>
        public System.Drawing.Color ControlLight { get; set; } = SystemColors.ButtonHighlight;

        /// <summary>Windows 10+ Chosen Accent Color</summary>
        public System.Drawing.Color Accent { get; set; } = DarkModeCS.GetWindowsAccentColor();
        public System.Drawing.Color AccentOpaque { get; set; } = DarkModeCS.GetWindowsAccentOpaqueColor();
        public System.Drawing.Color AccentDark { get { return ControlPaint.Dark(Accent); } }
        public System.Drawing.Color AccentLight { get { return ControlPaint.Light(Accent); } }

        /// <summary>the color displayed most frequently across your app's screens and components.</summary>
        public System.Drawing.Color Primary { get; set; } = SystemColors.Highlight;
        public System.Drawing.Color PrimaryDark { get { return ControlPaint.Dark(Primary); } }
        public System.Drawing.Color PrimaryLight { get { return ControlPaint.Light(Primary); } }

        /// <summary>to accent select parts of your UI.</summary>
        public System.Drawing.Color Secondary { get; set; } = SystemColors.HotTrack;
        public System.Drawing.Color SecondaryDark { get { return ControlPaint.Dark(Secondary); } }
        public System.Drawing.Color SecondaryLight { get { return ControlPaint.Light(Secondary); } }
    }

    /* Custom Renderers for Menus and ToolBars */
    public class MyRenderer : ToolStripProfessionalRenderer
    {
        public bool ColorizeIcons { get; set; } = true;
        public OSThemeColors MyColors { get; set; } //<- Your Custom Colors Colection

        public MyRenderer(ProfessionalColorTable table, bool pColorizeIcons = true) : base(table)
        {
            ColorizeIcons = pColorizeIcons;
        }

        // Background of the whole ToolBar Or MenuBar:
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.ToolStrip.BackColor = MyColors.Background;
            base.OnRenderToolStripBackground(e);
        }

        // For Normal Buttons on a ToolBar:
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background;

            Pen BordersPencil = new Pen(MyColors.Background);

            ToolStripButton button = e.Item as ToolStripButton;
            if (button.Pressed || button.Checked)
            {
                gradientBegin = MyColors.Control;
                gradientEnd = MyColors.Control;
            }
            else if (button.Selected)
            {
                gradientBegin = MyColors.Accent;
                gradientEnd = MyColors.Accent;
            }

            using (Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(b, bounds);
            }

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

            ToolStrip toolStrip = button.Owner;

            if (!(button.Owner.GetItemAt(button.Bounds.X, button.Bounds.Bottom + 1) is ToolStripButton nextItem))
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
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background;

            Pen BordersPencil = new Pen(MyColors.Background);

            //1. Determine the colors to use:
            if (e.Item.Pressed)
            {
                gradientBegin = MyColors.Control;
                gradientEnd = MyColors.Control;
            }
            else if (e.Item.Selected)
            {
                gradientBegin = MyColors.Accent;
                gradientEnd = MyColors.Accent;
            }

            //2. Draw the Box around the Control
            using (Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(b, bounds);
            }

            BordersPencil.Dispose();

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
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background;

            //1. Determine the colors to use:
            if (e.Item.Pressed)
            {
                gradientBegin = MyColors.Control;
                gradientEnd = MyColors.Control;
            }
            else if (e.Item.Selected)
            {
                gradientBegin = MyColors.Accent;
                gradientEnd = MyColors.Accent;
            }

            //2. Draw the Box around the Control
            using (Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(b, bounds);
            }

            //3. Draws the Chevron:
            #region Chevron

            int Padding = 2; //<- From the right side
            Size cSize = new Size(8, 4); //<- Size of the Chevron: 8x4 px
            Pen ChevronPen = new Pen(MyColors.TextInactive, 2); //<- Color and Border Width
            Point P1 = new Point(bounds.Width - (cSize.Width + Padding), (bounds.Height / 2) - (cSize.Height / 2));
            Point P2 = new Point(bounds.Width - Padding, (bounds.Height / 2) - (cSize.Height / 2));
            Point P3 = new Point(bounds.Width - (cSize.Width / 2 + Padding), (bounds.Height / 2) + (cSize.Height / 2));

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
                e.TextColor = MyColors.TextActive;
            }
            else
            {
                e.TextColor = MyColors.TextInactive;
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
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background; // Color.FromArgb(125, 165, 224);

            bool DrawIt = false;
            var _menu = e.Item as ToolStripItem;
            if (_menu.Pressed)
            {
                gradientBegin = MyColors.Control; // Color.FromArgb(254, 128, 62);
                gradientEnd = MyColors.Control; // Color.FromArgb(255, 223, 154);
                DrawIt = true;
            }
            else if (_menu.Selected)
            {
                gradientBegin = MyColors.Accent;// Color.FromArgb(255, 255, 222);
                gradientEnd = MyColors.Accent; // Color.FromArgb(255, 203, 136);
                DrawIt = true;
            }

            if (DrawIt)
            {
                using (Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical))
                {
                    g.FillRectangle(b, bounds);
                }
            }
        }

        // Re-Colors the Icon Images to a Clear color:
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (ColorizeIcons && e.Image != null)
            {
                // Get the current icon
                Image image = e.Image;
                Color _ClearColor = e.Item.Enabled ? MyColors.TextInactive : MyColors.SurfaceDark;

                // Create a new image with the desired color adjustments
                using (Image adjustedImage = DarkModeCS.ChangeToColor(image, _ClearColor))
                {
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                    e.Graphics.DrawImage(adjustedImage, e.ImageRectangle);
                }
            }
            else
            {
                base.OnRenderItemImage(e);
            }
        }

    }
    public class CustomColorTable : ProfessionalColorTable
    {
        public OSThemeColors Colors { get; set; }

        public CustomColorTable(OSThemeColors _Colors)
        {
            Colors = _Colors;
            base.UseSystemColors = false;
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Colors.Control; }
        }
        public override Color ImageMarginGradientMiddle
        {
            get { return Colors.Control; }
        }
        public override Color ImageMarginGradientEnd
        {
            get { return Colors.Control; }
        }
    }
}
