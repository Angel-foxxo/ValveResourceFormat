using System.Drawing;
using System.Windows.Forms;

namespace GUI.Controls
{
    internal class TransparentPanel : Panel
    {
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084; // TODO: PInvoke
            const int HTTRANSPARENT = (-1); // TODO: PInvoke

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }

    internal class TransparentMenuStrip : MenuStrip
    {
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084; // TODO: PInvoke
            const int HTTRANSPARENT = (-1); // TODO: PInvoke

            if (m.Msg == WM_NCHITTEST)
            {
                var point = PointToClient(new Point(MainForm.LoWord((int)m.LParam), MainForm.HiWord((int)m.LParam)));

                foreach (ToolStripMenuItem item in Items)
                {
                    if (item.Bounds.Contains(point))
                    {
                        base.WndProc(ref m);
                        return;
                    }
                }

                m.Result = (IntPtr)HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }
}
