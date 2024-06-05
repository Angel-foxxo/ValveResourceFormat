using System.Drawing;
using System.Windows.Forms;

namespace DarkModeForms
{
    using System.ComponentModel;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    [
        DefaultEvent("CheckedChanged"),
    ]

    public class FlatCheckbox : Control
    {
        private bool _isChecked;
        private static readonly object EVENT_CHECKEDCHANGED = new object();
        public bool Checked
        {
            get { return _isChecked; }
            set { _isChecked = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw the box
            e.Graphics.FillRectangle(Checked ? Brushes.DarkSlateGray : Brushes.White, 0, 0, 16, 16);
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, 16, 16);

            // If checked, draw a checkmark
            if (Checked)
            {
                e.Graphics.DrawLine(Pens.White, 2, 8, 6, 12);
                e.Graphics.DrawLine(Pens.White, 6, 12, 14, 2);
            }
        }

        public virtual void CheckedChanged(EventArgs e)
        {
            EventHandler handler = (EventHandler)Events[EVENT_CHECKEDCHANGED];
            if (handler != null) handler(this, e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }
    }
}
