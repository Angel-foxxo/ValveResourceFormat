using BlueMystic;
using System.Windows.Forms;

namespace GUI.Forms
{
    public partial class PromptForm : Form
    {
        public string ResultText => inputTextBox.Text;

        public PromptForm(string title)
        {
            InitializeComponent();

            _ = new DarkModeCS(this, false, false);

            Text = title;
            textLabel.Text = string.Concat(title, ":");
        }

        private void PromptForm_Load(object sender, EventArgs e)
        {
            ActiveControl = inputTextBox;
        }
    }
}
