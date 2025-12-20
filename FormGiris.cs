
using System;
using System.Windows.Forms;


namespace HastaneSistemi 
{
    public partial class FormGiris : Form
    {
        public FormGiris() {
            InitializeComponent();
        }

        private void btnHasta_Click(object sender, EventArgs e)
        {
            FormHastaIslemleri fr = new FormHastaIslemleri();
            fr.Show();
            this.Hide();
        }

        private void btnDoktor_Click(object sender, EventArgs e)
        {
            FormDoktorIslemleri fr = new FormDoktorIslemleri();
            fr.Show();
            this.Hide();
        }

        private void btnPersonel_Click(object sender, EventArgs e)
        {
            FormPersonelIslemleri fr = new FormPersonelIslemleri();
            fr.Show();
            this.Hide();
        }

        private void FormGiris_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            FormPersonelIslemleri frm = new FormPersonelIslemleri();
            frm.Show();
        }

    }
}
