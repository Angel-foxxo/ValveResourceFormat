using DarkModeForms;
using System.Drawing;
using System.Windows.Forms;

namespace GUI.Forms
{
    partial class LoadingFile : UserControl
    {
        public LoadingFile()
        {
            InitializeComponent();
        }

        //bit of bullshit in order to hide the border of the loading screen and make it seamless
        private void LoadingFile_Load(object sender, EventArgs e)
        {
            this.Bounds = new Rectangle(this.Parent.Bounds.X - 250, this.Parent.Bounds.Y - 250, this.Parent.Bounds.Width + 500, this.Parent.Bounds.Height + 500);
            this.ForeColor = this.Parent.ForeColor;
            this.BackColor = this.Parent.BackColor;
        }
    }
}
