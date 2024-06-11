using GUI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkModeForms
{
    public partial class FlatTabControl : TabControl
    {
        #region Public Properties

        [Description("Color for a decorative line"), Category("Appearance")]
        public Color LineColor { get; set; } = SystemColors.Highlight;

        [Description("Color for all Borders"), Category("Appearance")]
        public Color BorderColor { get; set; } = SystemColors.ControlDark;

        [Description("Back color for selected Tab"), Category("Appearance")]
        public Color SelectTabColor { get; set; } = SystemColors.ControlLight;

        [Description("Fore Color for Selected Tab"), Category("Appearance")]
        public Color SelectedForeColor { get; set; } = SystemColors.HighlightText;

        [Description("Back Color for un-selected tabs"), Category("Appearance")]
        public Color TabColor { get; set; } = SystemColors.ControlLight;

        [Description("Background color for the whole control"), Category("Appearance"), Browsable(true)]
        public override Color BackColor { get; set; } = SystemColors.Control;

        [Description("Fore Color for all Texts"), Category("Appearance")]
        public override Color ForeColor { get; set; } = SystemColors.ControlText;

        [Description("Hover Color for the tab"), Category("Appearance")]
        public Color HoverColor { get; set; } = SystemColors.Highlight;

        #endregion

        public FlatTabControl() : base()
        {
            Appearance = TabAppearance.Buttons;
            DrawMode = TabDrawMode.Normal;
            SizeMode = TabSizeMode.Normal;
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

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Refresh();  // Forces the control to be redrawn
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.Refresh();  // Forces the control to be redrawn
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawControl(e.Graphics);
        }

        public void DrawControl(Graphics g)
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

        protected override void OnCreateControl()
        {
            // Necessary to give tabs the correct width
            base.OnCreateControl();
            OnFontChanged(EventArgs.Empty);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            // Necessary to give tabs the correct width
            base.OnFontChanged(e);
            IntPtr hFont = Font.ToHfont();

            UpdateStyles();
        }

        public void DrawTab(Graphics g, TabPage customTabPage, int nIndex)
        {
            bool isHovered = false;
            bool isSelected = (SelectedIndex == nIndex);

            Rectangle tabRect = GetTabRect(nIndex);
            if (tabRect.Contains(this.PointToClient(Cursor.Position)))
            {
                isHovered = true;
            }

            // Draws the Tab Header:
            Color HeaderColor = isSelected ? SelectTabColor : isHovered ? HoverColor : BackColor;
            using (Brush brush = new SolidBrush(HeaderColor))
            {
                using (var headerPen = new Pen(HeaderColor))
                {
                    using (var headerUnderlinePen = new Pen(Color.DodgerBlue, tabRect.Height / 11))
                    {
                        g.FillRectangle(brush, tabRect);

                        if (isSelected)
                        {
                            g.DrawLine(headerUnderlinePen,
                                new Point(tabRect.Left, tabRect.Bottom), new Point(tabRect.Right, tabRect.Bottom));
                        }
                    }
                }
            }

            var imageScaleFactor = 0.7;
            var imageSize = (int)(tabRect.Height * imageScaleFactor);
            var imageHorizontalPadding = (int)(imageSize * 0.2);

            //center image by adding half of the difference between the tab height and the image height
            var imageCenteringOffset = (tabRect.Height - imageSize) / 2;
            var imageHorizontalPositioning = (int)(tabRect.X + imageCenteringOffset) + imageHorizontalPadding;
            var imageVerticalPositioning = (int)(tabRect.Y + imageCenteringOffset);
            Rectangle imageRect = new Rectangle(imageHorizontalPositioning, imageVerticalPositioning, imageSize, imageSize);

            var textFlags = new TextFormatFlags() | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
            if (customTabPage.ImageIndex >= 0 && ImageList != null && ImageList.Images.Count > customTabPage.ImageIndex)
            {
                Rectangle textRect = new Rectangle(imageHorizontalPositioning + imageSize, tabRect.Y, (tabRect.Width - (imageCenteringOffset + imageSize) - imageHorizontalPadding), tabRect.Height);

                var image = ImageList.Images[customTabPage.ImageIndex];
                g.DrawImage(image, imageRect.Left, imageRect.Top, imageRect.Height, imageRect.Height);

                TextRenderer.DrawText(g, customTabPage.Text, Font, textRect, isSelected ? SelectedForeColor : ForeColor, textFlags);
            }
            else
            {
                TextRenderer.DrawText(g, customTabPage.Text, Font, tabRect, isSelected ? SelectedForeColor : ForeColor, textFlags);
            }
        }
    }
}
