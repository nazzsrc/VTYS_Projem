using Npgsql;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Windows.Forms;
using VTYSHastaneSistemi;

namespace HastaneSistemi
{
    public partial class FormDoktorIslemleri : Form
    {
        public FormDoktorIslemleri() { InitializeComponent(); }

        SqlBaglantisi bgl = new SqlBaglantisi();

        // Güncelleme işlemi için seçilen doktorun ID'sini tutar
        int secilenDoktorID = 0;




        // ------------------ LİSTELEME ------------------
        void Listele()
        {
            try
            {
                // SQL Sorgusunun sonuna "ORDER BY" ekledik.
                // ASC = Artan (1, 2, 3...)
                // DESC = Azalan (10, 9, 8...) 
                // İstersen "ORDER BY d.ad ASC" yaparak isme göre de sıralayabilirsin.

                string komut = @"SELECT 
                            d.doktorid, 
                            d.ad, 
                            d.soyad, 
                            d.tcno, 
                            d.tel, 
                            d.email, 
                            p.poliklinikad, 
                            d.uzmanlik,
                            d.kangrubu
                         FROM Doktorlar d 
                         JOIN Poliklinikler p ON d.poliklinikid = p.poliklinikid
                         ORDER BY d.doktorid ASC";  // <-- İŞTE SİHİRLİ SATIR BURASI

                DataTable dt = new DataTable();
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(komut, bgl.Baglanti());
                da.Fill(dt);
                dataGridView1.DataSource = dt;

                // Sayacı güncelle
                lblSayac.Text = "Toplam Doktor: " + dt.Rows.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Listeleme Hatası: " + ex.Message);
            }
        }

        void PoliklinikleriYukle()
        {
            try
            {
                cmbPoliklinik.DataSource = null; // Önce temizle

                DataTable dt = new DataTable();
                // SQL sorgusunda ID ve AD sütunlarını çektiğine emin ol
                NpgsqlDataAdapter da = new NpgsqlDataAdapter("SELECT PoliklinikID, PoliklinikAd FROM Poliklinikler", bgl.Baglanti());
                da.Fill(dt);

                // BURASI ÇOK ÖNEMLİ:
                cmbPoliklinik.DisplayMember = "PoliklinikAd"; // Ekranda görünecek isim (Veritabanındaki sütun adı)
                cmbPoliklinik.ValueMember = "PoliklinikID";   // Arka planda tutulacak ID (Veritabanındaki sütun adı)

                cmbPoliklinik.DataSource = dt;

                // İlk açılışta seçili gelmesin (İsteğe bağlı)
                cmbPoliklinik.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Poliklinik Yükleme Hatası: " + ex.Message);
            }
        }

        void SayacGuncelle()
        {
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Doktorlar", bgl.Baglanti());
                object sonuc = cmd.ExecuteScalar();
                if (sonuc != null) lblSayac.Text = "Toplam Doktor: " + sonuc.ToString();
            }
            catch { lblSayac.Text = "-"; }
        }

        private void FormDoktorIslemleri_Load(object sender, EventArgs e)
        {
            Listele();
            PoliklinikleriYukle();
            SayacGuncelle();

            // Comboboxları doldur
            if (cmbCinsiyet.Items.Count == 0) cmbCinsiyet.Items.AddRange(new object[] { "Erkek", "Kadın" });
            if (cmbKan.Items.Count == 0) cmbKan.Items.AddRange(new object[] { "A Rh+", "A Rh-", "B Rh+", "B Rh-", "0 Rh+", "0 Rh-", "AB Rh+", "AB Rh-" });
        }



        // ------------------ EKLEME ------------------
        private void btnDoktorEkle_Click(object sender, EventArgs e)
        {
            // 1. Poliklinik Seçili mi Kontrolü
            if (cmbPoliklinik.SelectedValue == null)
            {
                MessageBox.Show("Lütfen poliklinik seçiniz.");
                return;
            }

            try
            {
                bgl.Baglanti();

                // SQL Sorgusu (@ işaretlerine dikkat)
                string sql = @"INSERT INTO Doktorlar 
                       (Ad, Soyad, TCNo, Tel, Email, Adres, KanGrubu, Uzmanlik, PoliklinikID) 
                       VALUES 
                       (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)";

                NpgsqlCommand komut = new NpgsqlCommand(sql, bgl.Baglanti());

                // Parametreler
                komut.Parameters.AddWithValue("@p1", txtAd.Text);
                komut.Parameters.AddWithValue("@p2", txtSoyad.Text);
                komut.Parameters.AddWithValue("@p3", txtTC.Text);
                komut.Parameters.AddWithValue("@p4", txtTel.Text);
                komut.Parameters.AddWithValue("@p5", txtEmail.Text);
                komut.Parameters.AddWithValue("@p6", txtAdres.Text);
                komut.Parameters.AddWithValue("@p7", cmbKan.Text);

                // Uzmanlık (Eğer formda kutusu yoksa "Uzman" yazıyoruz, varsa txtUzmanlik.Text)
                komut.Parameters.AddWithValue("@p8", "Uzman");

                // HATA ALDIĞIN YER BURASIYDI - ARTIK DEĞİŞKEN YOK, DİREKT ALIYORUZ:
                komut.Parameters.AddWithValue("@p9", Convert.ToInt32(cmbPoliklinik.SelectedValue));

                komut.ExecuteNonQuery();
                bgl.Baglanti().Close();

                MessageBox.Show("Doktor başarıyla eklendi.");

                Listele(); // Listeyi yenile
                Temizle(); // Kutuları temizle
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata Detayı: " + ex.Message);
            }
        }

        // ------------------ SİLME ------------------
        private void btnSil_Click(object sender, EventArgs e)
        {
            if (secilenDoktorID == 0)
            {
                MessageBox.Show("Lütfen silinecek doktoru listeden seçin.");
                return;
            }

            try
            {
                // Onay isteyelim
                DialogResult secim = MessageBox.Show("Bu doktoru silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (secim == DialogResult.No) return;

                bgl.Baglanti();

                // Sadece ID'ye göre silmek yeterli
                NpgsqlCommand komut = new NpgsqlCommand("DELETE FROM Doktorlar WHERE DoktorID=@p1", bgl.Baglanti());
                komut.Parameters.AddWithValue("@p1", secilenDoktorID);

                komut.ExecuteNonQuery();
                bgl.Baglanti().Close();

                MessageBox.Show("Doktor silindi. (Trigger çalıştı)");

                Listele();
                secilenDoktorID = 0;
                Temizle();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Silme Hatası: " + ex.Message);
            }
        }




        // ---------------- 7. ANALİZ (GÜNCELLENMİŞ) ----------------
        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            // Önce bir doktor seçilmiş mi kontrol et
            if (secilenDoktorID == 0)
            {
                MessageBox.Show("Lütfen güncellenecek doktoru seçin.");
                return;
            }

            try
            {
                bgl.Baglanti();

                // SQL Sorgusu: Hem bilgileri hem de PoliklinikID'yi güncelliyoruz
                // DİKKAT: Veritabanındaki sütun adlarınla birebir aynı olmalı (ad, soyad, poliklinikid vs.)
                string sql = @"UPDATE Doktorlar SET 
                        Ad=@p1, 
                        Soyad=@p2, 
                        TCNo=@p3, 
                        Tel=@p4, 
                        Email=@p5, 
                        PoliklinikID=@p6,
                        KanGrubu=@p7
                       WHERE DoktorID=@p8";

                NpgsqlCommand komut = new NpgsqlCommand(sql, bgl.Baglanti());

                komut.Parameters.AddWithValue("@p1", txtAd.Text);
                komut.Parameters.AddWithValue("@p2", txtSoyad.Text);
                komut.Parameters.AddWithValue("@p3", txtTC.Text);
                komut.Parameters.AddWithValue("@p4", txtTel.Text);
                komut.Parameters.AddWithValue("@p5", txtEmail.Text); // Email kutun varsa

                // ÖNEMLİ: ComboBox'tan Görünen yazıyı değil, arka plandaki ID'yi alıyoruz (SelectedValue)
                komut.Parameters.AddWithValue("@p6", Convert.ToInt32(cmbPoliklinik.SelectedValue));

                komut.Parameters.AddWithValue("@p7", cmbKan.Text); // Kan grubunu da güncelle
                komut.Parameters.AddWithValue("@p8", secilenDoktorID);  // WHERE koşulu (Hangi doktor)

                komut.ExecuteNonQuery();
                bgl.Baglanti().Close();

                MessageBox.Show("Doktor bilgileri ve Polikliniği başarıyla güncellendi.");

                // Listeyi yenile ve seçimi sıfırla
                Listele();
                secilenDoktorID = 0;
                Temizle(); // Kutuları temizle
            }
            catch (Exception ex)
            {
                MessageBox.Show("Güncelleme Hatası: " + ex.Message);
            }
        }


        void Temizle()
        {
            txtAd.Text = ""; txtSoyad.Text = ""; txtTC.Text = ""; txtTel.Text = "";
            txtAdres.Text = ""; txtEmail.Text = ""; cmbCinsiyet.Text = "";
            cmbPoliklinik.SelectedIndex = -1;
            secilenDoktorID = 0;
        }

        private void btnGeri_Click(object sender, EventArgs e)
        {
            FormGiris fr = new FormGiris(); // Giriş formunun adı FormGiris ise
            fr.Show();  // Giriş formunu aç
            this.Hide(); // Şu anki (Doktor) formunu gizle

            // NOT: Eğer tamamen kapatmak istersen this.Close(); kullanabilirsin.
            // Ama Hide() daha güvenlidir, programı komple kapatmaz.
        }


        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // Başlığa tıklanırsa hata vermesin
                if (e.RowIndex >= 0)
                {
                    DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                    // 1. ÖNEMLİ: Doktorun ID'sini hafızaya alıyoruz
                    secilenDoktorID = Convert.ToInt32(row.Cells[0].Value);

                    // 2. Kutuları dolduruyoruz
                    txtAd.Text = row.Cells[1].Value.ToString();
                    txtSoyad.Text = row.Cells[2].Value.ToString();
                    txtTC.Text = row.Cells[3].Value.ToString();
                    // Tarih formatı hatası olmaması için kontrol
                    // dtDogumTarihi.Value = Convert.ToDateTime(row.Cells[4].Value); 
                    txtTel.Text = row.Cells[5].Value.ToString();
                    txtEmail.Text = row.Cells[6].Value.ToString();

                    // Kan Grubu ve Cinsiyet (Eğer tablonda varsa indexleri düzelt)
                    // cmbKanGrubu.Text = row.Cells[...].Value.ToString(); 

                    // 3. ÖNEMLİ: Polikliniği ComboBox'ta seçili hale getiriyoruz
                    // Tablodaki "Poliklinik Adı"nı alıp ComboBox'ta bulup seçiyoruz.
                    cmbPoliklinik.Text = row.Cells["poliklinikad"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                // Hata vermesin, bazen boş yere tıklanınca patlayabilir
            }
        }
        private void cmbKan_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnGeri_Click_1(object sender, EventArgs e)
        {
            FormGiris fr = new FormGiris(); fr.Show(); this.Hide();
        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
