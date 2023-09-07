using System.Windows.Forms;

namespace GUI.Forms
{
    partial class EntityInfoForm : Form
    {
        public EntityInfoForm()
        {
            InitializeComponent();

            Icon = Program.Instance.Icon;
        }

        public void AddColumn(string name, string value)
        {
            dataGrid.Rows.Add(new string[] { name, value });
        }
    }
}
