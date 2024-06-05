using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueMystic
{
    public class FlatTabControl : TabControl
    {
        #region Public Properties

        static private DarkModeCS darkModeCS = new DarkModeCS(null, false, false);

        [Description("Color for a decorative line"), Category("Appearance")]
        public Color LineColor { get; set; } = darkModeCS.OScolors.Accent;

        [Description("Color for all Borders"), Category("Appearance")]
        public Color BorderColor { get; set; } = darkModeCS.OScolors.TextInactive;

        [Description("Back color for selected Tab"), Category("Appearance")]
        public Color SelectTabColor { get; set; } = darkModeCS.OScolors.Surface;

        [Description("Fore Color for Selected Tab"), Category("Appearance")]
        public Color SelectedForeColor { get; set; } = darkModeCS.OScolors.TextActive;

        [Description("Back Color for un-selected tabs"), Category("Appearance")]
        public Color TabColor { get; set; } = darkModeCS.OScolors.Control;

        [Description("Background color for the whole control"), Category("Appearance"), Browsable(true)]
        public override Color BackColor { get; set; } = darkModeCS.OScolors.Control;

        [Description("Fore Color for all Texts"), Category("Appearance")]
        public override Color ForeColor { get; set; } = darkModeCS.OScolors.TextInactive;

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

                // a decorative line on top of pages:
                //using (Brush bLineColor = new SolidBrush(LineColor))
                //{
                //	Rectangle rectangle = ClientRectangle;
                //	rectangle.Height = 1;
                //	rectangle.Y = 25;
                //	g.FillRectangle(bLineColor, rectangle);

                //	rectangle = ClientRectangle;
                //	rectangle.Height = 1;
                //	rectangle.Y = 26;
                //	g.FillRectangle(bLineColor, rectangle);
                //}

            }
            catch { }
        }

        internal void DrawTab(Graphics g, TabPage customTabPage, int nIndex)
        {
            Rectangle tabRect = GetTabRect(nIndex);
            bool isSelected = (SelectedIndex == nIndex);
            Point[] points;

            customTabPage.Padding = new Padding(0, 0, 0, 0);

            if (Alignment == TabAlignment.Top)
            {
                points = new[]
                {
                    new Point(tabRect.Left, tabRect.Bottom),
                    new Point(tabRect.Left, tabRect.Top + 0),
                    new Point(tabRect.Left + 0, tabRect.Top),
                    new Point(tabRect.Right - 0, tabRect.Top),
                    new Point(tabRect.Right, tabRect.Top + 0),
                    new Point(tabRect.Right, tabRect.Bottom),
                    new Point(tabRect.Left, tabRect.Bottom)
                };
            }
            else
            {
                points = new[]
                {
                    new Point(tabRect.Left, tabRect.Top),
                    new Point(tabRect.Right, tabRect.Top),
                    new Point(tabRect.Right, tabRect.Bottom - 0),
                    new Point(tabRect.Right - 0, tabRect.Bottom),
                    new Point(tabRect.Left + 0, tabRect.Bottom),
                    new Point(tabRect.Left, tabRect.Bottom - 0),
                    new Point(tabRect.Left, tabRect.Top)
                };
            }

            // Draws the Tab Header:
            Color HeaderColor = isSelected ? SelectTabColor : BackColor;
            using (Brush brush = new SolidBrush(HeaderColor))
            {
                g.FillPolygon(brush, points);
                g.DrawPolygon(new Pen(HeaderColor), points);

                if (isSelected)
                {
                    g.DrawLine(new Pen(BackColor),
                        new Point(tabRect.Left, tabRect.Top), new Point(tabRect.Left, tabRect.Top));
                    g.DrawLine(new Pen(Color.DodgerBlue, 2),
                        new Point(tabRect.Left, tabRect.Bottom), new Point(tabRect.Left + tabRect.Width + 1, tabRect.Bottom));
                }
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
