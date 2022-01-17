using System.Windows.Forms;

namespace HistoricalProcess
{
    public partial class CivRenameDialog : Form
    {
        public CivRenameDialog()
        {
            InitializeComponent();
        }

        private void CivRenameDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.AppForm.Mankind.ContainsKey(textBox.Text) &&
                Program.AppForm.currentCivilization.Name != textBox.Text)
            {
                MessageBox.Show("Такая страна уже существует", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        private void CivRenameDialog_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) buttonOK.PerformClick();
        }
    }
}
