using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GUI.Controls;
using GUI.Types.PackageViewer;
using Microsoft.Win32;
using Windows.Win32;

namespace DarkModeForms
{
    /// <summary>This tries to automatically apply Windows Dark Mode (if enabled) to a Form.
    /// <para>Author: DarkModeForms (DarkModeForms.play@gmail.com)  2024</para></summary>
    public partial class DarkModeCS
    {
        /// <summary>'true' if Dark Mode Color is set in Windows's Settings.</summary>
        public bool IsDarkMode { get; set; }

        /// <summary>Windows Colors. Can be customized.</summary>
        public ThemeColors ThemeColors { get; set; }

        private static bool DebugTheme;

        /// <summary>Constructor.</summary>
        public DarkModeCS(bool debugTheme = false)
        {
            DebugTheme = debugTheme;
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(OnUserPreferenceChanged);
            IsDarkMode = IsWindowsDarkThemed();
            ThemeColors = GetAppTheme();
        }

        /// <summary>This tries to style and automatically apply Windows Dark Mode (if enabled) to a Form.</summary>
        /// <param name="_Form">The Form to become Dark</param>
        public void Style(Form _Form)
        {
            ApplyTheme(_Form);
            ApplySystemTheme(_Form);
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
            if (control is TransparentMenuStrip menu)
            {
                menu.RenderMode = ToolStripRenderMode.Professional;
                menu.Renderer = new DarkToolStripRenderer(new CustomColorTable(ThemeColors), false)
                {
                    themeColors = ThemeColors
                };
            }
            if (control is ToolStrip toolBar)
            {
                toolBar.GripStyle = ToolStripGripStyle.Hidden;
                toolBar.RenderMode = ToolStripRenderMode.Professional;
                toolBar.Renderer = new DarkToolStripRenderer(new CustomColorTable(ThemeColors), false) { themeColors = ThemeColors };
            }
            if (control is ContextMenuStrip cMenu)
            {
                cMenu.RenderMode = ToolStripRenderMode.Professional;
                cMenu.Renderer = new DarkToolStripRenderer(new CustomColorTable(ThemeColors), false) { themeColors = ThemeColors };
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
                intResult = (int)Registry.GetValue(
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

        public ThemeColors GetAppTheme()
        {
            var themeColors = new ThemeColors();

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

                themeColors.ControlBoxHighlight = Color.FromArgb(67, 67, 67);
                themeColors.ControlBoxHighlightCloseButton = Color.FromArgb(240, 20, 20);


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

                themeColors.ControlBoxHighlight = Color.FromArgb(190, 190, 190);
                themeColors.ControlBoxHighlightCloseButton = Color.FromArgb(240, 20, 20);

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

                themeColors.ControlBoxHighlight = Color.FromArgb(255, 179, 194);
                themeColors.ControlBoxHighlightCloseButton = Color.FromArgb(240, 20, 20);

                themeColors.Accent = Color.DodgerBlue;
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
                [1, 0, 0, 0, 0],
                    [0, 1, 0, 0, 0],
                    [0, 0, 1, 0, 0],
                    [0, 0, 0, 1, 0],  //<- not changing alpha
                    [tR, tG, tB, 0, 1]
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

        private void ApplyTheme(Form _Form)
        {
            if (ThemeColors != null)
            {
                if (_Form != null && _Form.Controls != null)
                {
                    _Form.BackColor = ThemeColors.Window;
                    _Form.ForeColor = ThemeColors.Text;

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

        private void OnUserPreferenceChanged(object sender, EventArgs e)
        {
            var currentTheme = IsWindowsDarkThemed();

            if (IsDarkMode != currentTheme)
            {
                IsDarkMode = currentTheme;

                foreach (Form form in Application.OpenForms)
                {
                    ThemeColors = GetAppTheme();
                    ApplyTheme(form);
                    ApplySystemTheme(form);
                    form.Invalidate();
                }
            }
        }

        /// <summary>Attemps to apply Window's Dark Style to the Control and all its childs.</summary>
        /// <param name="control"></param>
        private void ApplySystemTheme(Control control = null)
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
            IntPtr DarkModeOn = 0; //<- 1=True, 0=False

            var windowsTheme = "Explorer";
            var windowsThemeCombo = "Explorer";

            if (IsDarkMode)
            {
                windowsTheme = "DarkMode_Explorer";
                windowsThemeCombo = "DarkMode_CFD";
                DarkModeOn = 1;
            }
            else
            {
                DarkModeOn = 0;
            }

            if (control is ComboBox comboBox)
            {
                _ = PInvoke.SetWindowTheme((Windows.Win32.Foundation.HWND)comboBox.Handle, windowsThemeCombo, null);

                // Style the ComboBox drop-down (including its ScrollBar(s)):
                Windows.Win32.UI.Controls.COMBOBOXINFO cInfo = default;
                var result = PInvoke.GetComboBoxInfo((Windows.Win32.Foundation.HWND)comboBox.Handle, ref cInfo);
                _ = PInvoke.SetWindowTheme(cInfo.hwndList, windowsThemeCombo, null);
            }
            else
            {
                _ = PInvoke.SetWindowTheme((Windows.Win32.Foundation.HWND)control.Handle, windowsTheme, null);
            }
            unsafe
            {
                const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

                if (PInvoke.DwmSetWindowAttribute((Windows.Win32.Foundation.HWND)control.Handle, (Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE)DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, &DarkModeOn, sizeof(int)) != 0)
                {
                    _ = PInvoke.DwmSetWindowAttribute((Windows.Win32.Foundation.HWND)control.Handle, Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &DarkModeOn, sizeof(int));
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
    }

    public class ThemeColors
    {
        public ThemeColors() { }

        /// <summary>For the very back of the Window</summary>
        public Color Window { get; set; }
        /// <summary>For Borders around the Background</summary>
        public Color WindowBorder { get; set; }
        /// <summary>For hightlights over the Background</summary>
        public Color WindowHighlight { get; set; }

        /// <summary>For Container above the Background</summary>
        public Color Container { get; set; }
        /// <summary>For Borders around the Surface</summary>
        public Color ContainerBorder { get; set; }
        /// <summary>For Highligh over the Surface</summary>
        public Color ContainerHighlight { get; set; }

        /// <summary>For Main Texts</summary>
        public Color Text { get; set; }
        /// <summary>For Inactive Texts</summary>
        public Color TextInactive { get; set; }
        /// <summary>For Hightligh Texts</summary>
        public Color TextHighlight { get; set; }

        /// <summary>For the background of any Control</summary>
        public Color Control { get; set; }
        /// <summary>For Borders of any Control</summary>
        public Color ControlBorder { get; set; }
        /// <summary>For Highlight elements in a Control</summary>
        public Color ControlHighlight { get; set; }

        /// <summary>For the control box</summary>
        public Color ControlBoxHighlight { get; set; }
        public Color ControlBoxHighlightCloseButton { get; set; }

        /// <summary>For anything that accented like hovering over a tab</summary>
        public Color Accent { get; set; }
    }

    // Custom Renderers for Menus and ToolBars
    public class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        public bool ColorizeIcons { get; set; } = true;
        public ThemeColors themeColors { get; set; }

        public DarkToolStripRenderer(ProfessionalColorTable table, bool pColorizeIcons = true) : base(table)
        {
            ColorizeIcons = pColorizeIcons;
        }

        // Background of the whole ToolBar Or MenuBar:
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.ToolStrip.BackColor = themeColors.Window;
            base.OnRenderToolStripBackground(e);
            //e.Graphics.FillRectangle(new SolidBrush(themeColors.Window), e.AffectedBounds);
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
            //int Padding = 2; //<- From the right side
            //Size cSize = new Size(8, 4); //<- Size of the Chevron: 8x4 px
            //Pen ChevronPen = new Pen(MyColors.TextInactive, 2); //<- Color and Border Width
            //Point P1 = new Point(bounds.Width - (cSize.Width + Padding), (bounds.Height / 2) - (cSize.Height / 2));
            //Point P2 = new Point(bounds.Width - Padding, (bounds.Height / 2) - (cSize.Height / 2));
            //Point P3 = new Point(bounds.Width - (cSize.Width / 2 + Padding), (bounds.Height / 2) + (cSize.Height / 2));

            //e.Graphics.DrawLine(ChevronPen, P1, P3);
            //e.Graphics.DrawLine(ChevronPen, P2, P3);
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
            var Padding = 2; //<- From the right side
            var cSize = new Size(8, 4); //<- Size of the Chevron: 8x4 px
            var ChevronPen = new Pen(themeColors.TextInactive, 2); //<- Color and Border Width
            var P1 = new Point(bounds.Width - (cSize.Width + Padding), (bounds.Height / 2) - (cSize.Height / 2));
            var P2 = new Point(bounds.Width - Padding, (bounds.Height / 2) - (cSize.Height / 2));
            var P3 = new Point(bounds.Width - (cSize.Width / 2 + Padding), (bounds.Height / 2) + (cSize.Height / 2));

            e.Graphics.DrawLine(ChevronPen, P1, P3);
            e.Graphics.DrawLine(ChevronPen, P2, P3);

            ChevronPen.Dispose();
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

            var text = e.Text.Replace("&", "", StringComparison.Ordinal);

            using var textBrush = new SolidBrush(e.TextColor);
            //e.Graphics.DrawString(text, e.TextFont, textBrush, e.TextRectangle);

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
            //base.OnRenderToolStripBackground(e);
            //e.Graphics.FillRectangle(new SolidBrush(themeColors.Window), e.Item.Bounds);
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
            UseSystemColors = false;
        }

        public override Color ImageMarginGradientBegin => Colors.Container;
        public override Color ImageMarginGradientMiddle => Colors.Container;
        public override Color ImageMarginGradientEnd => Colors.Container;
    }
}
