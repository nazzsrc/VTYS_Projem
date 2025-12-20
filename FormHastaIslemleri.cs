using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using VTYSHastaneSistemi;

namespace HastaneSistemi
{
    public partial class FormHastaIslemleri : Form
    {
        public FormHastaIslemleri() { InitializeComponent(); }

        SqlBaglantisi bgl = new SqlBaglantisi();

        // Seçilen hastanın ID'sini tutar
        int secilenHastaID = 0;

        private void FormHastaIslemleri_Load(object sender, EventArgs e)
        {
            Listele();
            SayacGuncelle();
            ComboDoldur();
        }

        // ---------------- 1. LİSTELEME (DÜZELTİLDİ) ----------------
        void Listele()
        {
            try
            {
                DataTable dt = new DataTable();
                // DÜZELTME BURADA YAPILDI: h.KisiID yerine h.KisilerID yazıldı.
                string sql = @"SELECT h.HastaID, k.TCNo, k.Ad, k.Soyad, k.Cinsiyet, 
                                      k.DogumTarihi, k.Telefon, k.Email, k.Adres, h.KanGrubu
                               FROM Hastalar h
                               JOIN Kisiler k ON h.KisilerID = k.KisiID
                               ORDER BY h.HastaID DESC";

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, bgl.Baglanti());
                da.Fill(dt);
                dataGridView1.DataSource = dt;
            }
            catch (Exception ex) { MessageBox.Show("Listeleme Hatası: " + ex.Message); }
        }

        // ---------------- 2. SAYAÇ ----------------
        void SayacGuncelle()
        {
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Hastalar", bgl.Baglanti());
                object sonuc = cmd.ExecuteScalar();
                lblSayac.Text = "Toplam Hasta: " + (sonuc != null ? sonuc.ToString() : "0");
            }
            catch { lblSayac.Text = "-"; }
        }

        // ---------------- 3. TIKLAYINCA KUTULARI DOLDUR ----------------
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                // ID'yi al
                secilenHastaID = int.Parse(dataGridView1.Rows[e.RowIndex].Cells["HastaID"].Value.ToString());

                // Kutuları doldur
                txtTC.Text = dataGridView1.Rows[e.RowIndex].Cells["TCNo"].Value.ToString();
                txtAd.Text = dataGridView1.Rows[e.RowIndex].Cells["Ad"].Value.ToString();
                txtSoyad.Text = dataGridView1.Rows[e.RowIndex].Cells["Soyad"].Value.ToString();
                txtTel.Text = dataGridView1.Rows[e.RowIndex].Cells["Telefon"].Value.ToString();
                txtEmail.Text = dataGridView1.Rows[e.RowIndex].Cells["Email"].Value.ToString();
                txtAdres.Text = dataGridView1.Rows[e.RowIndex].Cells["Adres"].Value.ToString();

                object cinsiyet = dataGridView1.Rows[e.RowIndex].Cells["Cinsiyet"].Value;
                if (cinsiyet != null) cmbCinsiyet.Text = cinsiyet.ToString();

                object kan = dataGridView1.Rows[e.RowIndex].Cells["KanGrubu"].Value;
                if (kan != null) cmbKan.Text = kan.ToString();

                string tarihStr = dataGridView1.Rows[e.RowIndex].Cells["DogumTarihi"].Value.ToString();
                if (DateTime.TryParse(tarihStr, out DateTime tarih))
                {
                    dtpDogum.Value = tarih;
                }
            }
            catch { }
        }

        // ---------------- 4. EKLEME ----------------
        private void btnEkle_Click(object sender, EventArgs e)
        {
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand("SELECT sp_HastaEkle(@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)", bgl.Baglanti());
                cmd.Parameters.AddWithValue("@p1", txtAd.Text);
                cmd.Parameters.AddWithValue("@p2", txtSoyad.Text);
                cmd.Parameters.AddWithValue("@p3", txtTC.Text);
                cmd.Parameters.AddWithValue("@p4", cmbCinsiyet.Text);
                cmd.Parameters.AddWithValue("@p5", dtpDogum.Value.Date);
                cmd.Parameters.AddWithValue("@p6", txtTel.Text);
                cmd.Parameters.AddWithValue("@p7", txtAdres.Text);
                cmd.Parameters.AddWithValue("@p8", txtEmail.Text);
                cmd.Parameters.AddWithValue("@p9", cmbKan.Text);

                cmd.ExecuteNonQuery();
                MessageBox.Show("Hasta Eklendi!");

                Listele();
                SayacGuncelle();
                Temizle();
            }
            catch (Exception ex) { MessageBox.Show("Ekleme Hatası: " + ex.Message); }
        }

        // ---------------- 5. SİLME ----------------
        private void btnSil_Click(object sender, EventArgs e)
        {
            if (secilenHastaID == 0)
            {
                MessageBox.Show("Lütfen önce listeden silinecek hastayı seçin.");
                return;
            }

            if (MessageBox.Show("Bu hastayı silmek istiyor musunuz?", "Onay", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM Hastalar WHERE HastaID=@p1", bgl.Baglanti());
                    cmd.Parameters.AddWithValue("@p1", secilenHastaID);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Hasta Silindi.");
                    Listele();
                    SayacGuncelle();
                    Temizle();
                }
                catch (Exception ex) { MessageBox.Show("Silme Hatası: " + ex.Message); }
            }
        }

        // ---------------- 6. GÜNCELLEME ----------------
        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            if (secilenHastaID == 0)
            {
                MessageBox.Show("Lütfen güncellenecek hastayı seçin.");
                return;
            }

            try
            {
                NpgsqlConnection conn = bgl.Baglanti();

                // Kişiler tablosunu güncelle
                string sqlKisi = @"UPDATE Kisiler SET 
                                   Ad=@p1, Soyad=@p2, Telefon=@p3, Email=@p4, Adres=@p5, Cinsiyet=@p6, DogumTarihi=@p7
                                   WHERE TCNo=@p8";

                NpgsqlCommand cmdKisi = new NpgsqlCommand(sqlKisi, conn);
                cmdKisi.Parameters.AddWithValue("@p1", txtAd.Text);
                cmdKisi.Parameters.AddWithValue("@p2", txtSoyad.Text);
                cmdKisi.Parameters.AddWithValue("@p3", txtTel.Text);
                cmdKisi.Parameters.AddWithValue("@p4", txtEmail.Text);
                cmdKisi.Parameters.AddWithValue("@p5", txtAdres.Text);
                cmdKisi.Parameters.AddWithValue("@p6", cmbCinsiyet.Text);
                cmdKisi.Parameters.AddWithValue("@p7", dtpDogum.Value.Date);
                cmdKisi.Parameters.AddWithValue("@p8", txtTC.Text);
                cmdKisi.ExecuteNonQuery();

                // Hastalar tablosunu güncelle
                string sqlHasta = "UPDATE Hastalar SET KanGrubu=@k1 WHERE HastaID=@h1";
                NpgsqlCommand cmdHasta = new NpgsqlCommand(sqlHasta, conn);
                cmdHasta.Parameters.AddWithValue("@k1", cmbKan.Text);
                cmdHasta.Parameters.AddWithValue("@h1", secilenHastaID);
                cmdHasta.ExecuteNonQuery();

                MessageBox.Show("Bilgiler Güncellendi.");
                Listele();
                Temizle();
            }
            catch (Exception ex) { MessageBox.Show("Güncelleme Hatası: " + ex.Message); }
        }

        // ---------------- 7. ANALİZ (GÜNCELLENMİŞ) ----------------
        private void btnAnaliz_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. YAŞ HESAPLAMA
                string yas = "Bilinmiyor";
                try
                {
                    NpgsqlCommand cmdYas = new NpgsqlCommand("SELECT fn_YasHesapla(@p1)", bgl.Baglanti());
                    cmdYas.Parameters.AddWithValue("@p1", txtTC.Text);
                    object yasObj = cmdYas.ExecuteScalar();
                    if (yasObj != null) yas = yasObj.ToString();
                }
                catch { } // Fonksiyon yoksa yaş 'Bilinmiyor' kalır, hata vermez.

                // 2. REÇETE SAYISI (YENİ EKLENEN KISIM)
                string receteSayisi = "0";
                try
                {
                    // Veritabanındaki reçete sayan fonksiyonu çağırıyoruz
                    NpgsqlCommand cmdRecete = new NpgsqlCommand("SELECT fn_HastaYillikRecete(@p1)", bgl.Baglanti());
                    cmdRecete.Parameters.AddWithValue("@p1", txtTC.Text);
                    object recObj = cmdRecete.ExecuteScalar();

                    if (recObj != null) receteSayisi = recObj.ToString();
                }
                catch
                {
                    // Eğer fonksiyon yoksa hata vermek yerine tire koyar
                    receteSayisi = "-";
                }

                // 3. MESAJ KUTUSU (SONUÇ)
                MessageBox.Show($"HASTA ANALİZ RAPORU\n----------------------\n" +
                                $"Yaş: {yas}\n" +
                                $"Kan Grubu: {cmbKan.Text}\n" +
                                $"Toplam Reçete: {receteSayisi}",
                                "Analiz Sonucu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Analiz işleminde hata oluştu: " + ex.Message); }
        }

        void Temizle()
        {
            txtAd.Text = ""; txtSoyad.Text = ""; txtTC.Text = "";
            txtTel.Text = ""; txtAdres.Text = ""; txtEmail.Text = "";
            cmbCinsiyet.Text = ""; cmbKan.Text = "";
            secilenHastaID = 0;
        }

        void ComboDoldur()
        {
            if (cmbCinsiyet.Items.Count == 0) cmbCinsiyet.Items.AddRange(new object[] { "Erkek", "Kadın" });
            if (cmbKan.Items.Count == 0) cmbKan.Items.AddRange(new object[] { "A Rh+", "A Rh-", "B Rh+", "B Rh-", "0 Rh+", "0 Rh-", "AB Rh+", "AB Rh-" });
        }

        private void btnGeri_Click(object sender, EventArgs e)
        {
            FormGiris fr = new FormGiris(); fr.Show(); this.Hide();
        }

        private void txtAd_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
