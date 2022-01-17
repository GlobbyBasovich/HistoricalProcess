using System.Windows.Forms;

namespace HistoricalProcess
{
    public partial class CreateMapDialog : Form
    {
        public CreateMapDialog()
        {
            InitializeComponent();
        }

        private void CreateMapDialog_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) buttonOK.PerformClick();
        }
    }
}
