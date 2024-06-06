using GUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DarkModeForms
{
    public class FlatTabControl : TabControl
    {
        #region Public Properties

        [Description("Color for a decorative line"), Category("Appearance")]
        public Color LineColor { get; set; } = MainForm.DarkModeCS.OScolors.Accent;

        [Description("Color for all Borders"), Category("Appearance")]
        public Color BorderColor { get; set; } = MainForm.DarkModeCS.OScolors.TextInactive;

        [Description("Back color for selected Tab"), Category("Appearance")]
        public Color SelectTabColor { get; set; } = MainForm.DarkModeCS.OScolors.Surface;

        [Description("Fore Color for Selected Tab"), Category("Appearance")]
        public Color SelectedForeColor { get; set; } = MainForm.DarkModeCS.OScolors.TextActive;

        [Description("Back Color for un-selected tabs"), Category("Appearance")]
        public Color TabColor { get; set; } = MainForm.DarkModeCS.OScolors.Control;

        [Description("Background color for the whole control"), Category("Appearance"), Browsable(true)]
        public override Color BackColor { get; set; } = MainForm.DarkModeCS.OScolors.Control;

        [Description("Fore Color for all Texts"), Category("Appearance")]
        public override Color ForeColor { get; set; } = MainForm.DarkModeCS.OScolors.TextInactive;

        #endregion

        public FlatTabControl()
        {
            try
            {
                Appearance = TabAppearance.Buttons;
                DrawMode = TabDrawMode.Normal;
                SizeMode = TabSizeMode.Normal;
            }
            catch { }
        }

        protected override void InitLayout()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);
            base.InitLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawControl(e.Graphics);
        }

        internal void DrawControl(Graphics g)
        {
            try
            {
                if (!Visible)
                {
                    return;
                }

                Rectangle clientRectangle = ClientRectangle;
                clientRectangle.Inflate(2, 2);

                // Whole Control Background:
                using (Brush bBackColor = new SolidBrush(BackColor))
                {
                    g.FillRectangle(bBackColor, ClientRectangle);
                }

                Region region = g.Clip;

                for (int i = 0; i < TabCount; i++)
                {
                    DrawTab(g, TabPages[i], i);
                    TabPages[i].BackColor = TabColor;
                }

                g.Clip = region;

                using (Pen border = new Pen(BorderColor))
                {
                    g.DrawRectangle(border, clientRectangle);

                    if (SelectedTab != null)
                    {
                        clientRectangle.Offset(1, 1);
                        clientRectangle.Width -= 2;
                        clientRectangle.Height -= 2;
                        g.DrawRectangle(border, clientRectangle);
                        clientRectangle.Width -= 1;
                        clientRectangle.Height -= 1;
                        g.DrawRectangle(border, clientRectangle);
                    }
                }
            }
            catch { }
        }

        internal void DrawTab(Graphics g, TabPage customTabPage, int nIndex)
        {
            Rectangle tabRect = GetTabRect(nIndex);
            bool isSelected = (SelectedIndex == nIndex);

            // Draws the Tab Header:
            Color HeaderColor = isSelected ? SelectTabColor : BackColor;
            using (Brush brush = new SolidBrush(HeaderColor))
            {
                var headerPen = new Pen(HeaderColor);
                var headerUnderlinePen1 = new Pen(BackColor);
                var headerUnderlinePen2 = new Pen(Color.DodgerBlue, 2);

                g.DrawRectangle(headerPen, tabRect);

                if (isSelected)
                {
                    g.DrawLine(headerUnderlinePen1,
                        new Point(tabRect.Left, tabRect.Top), new Point(tabRect.Left, tabRect.Top));
                    g.DrawLine(headerUnderlinePen2,
                        new Point(tabRect.Left, tabRect.Bottom), new Point(tabRect.Left + tabRect.Width + 1, tabRect.Bottom));
                }

                headerPen.Dispose();
                headerUnderlinePen1.Dispose();
                headerUnderlinePen2.Dispose();
            }

            Rectangle imageRect = tabRect;
            imageRect.Height = (int)(imageRect.Height * 0.7);
            var imagePadding = 7;
            var textPadding = 3;

            Rectangle textRect = tabRect;

            if (customTabPage.ImageIndex >= 0 && ImageList != null && ImageList.Images.Count > customTabPage.ImageIndex)
            {
                var image = ImageList.Images[customTabPage.ImageIndex];
                g.DrawImage(image, imageRect.Left + imagePadding, imageRect.Top + 4, imageRect.Height, imageRect.Height);

                textRect.Offset(image.Width + imagePadding + textPadding, 0);

                var flags = new TextFormatFlags() | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
                TextRenderer.DrawText(g, customTabPage.Text, Font, textRect,
                     isSelected ? SelectedForeColor : ForeColor, flags);
            }
            else
            {
                TextRenderer.DrawText(g, customTabPage.Text, Font, textRect,
                     isSelected ? SelectedForeColor : ForeColor);
            }
        }
    }
}
