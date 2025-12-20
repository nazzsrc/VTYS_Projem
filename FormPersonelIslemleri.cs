using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using VTYSHastaneSistemi;

namespace HastaneSistemi
{
    public partial class FormPersonelIslemleri : Form
    {
        public FormPersonelIslemleri() { InitializeComponent(); }

        SqlBaglantisi bgl = new SqlBaglantisi();
        int secilenPersonelID = 0; // Hangi personele tıklandığını tutar

        // ------------------ YÜKLEME ------------------
        private void FormPersonelIslemleri_Load(object sender, EventArgs e)
        {
            GorevleriYukle(); // ComboBox dolsun
            Listele();        // Tablo dolsun
            SayacGuncelle();
        }

        // 1. GÖREVLERİ YÜKLEME (Temizleyip Doldurur)
        void GorevleriYukle()
        {
            try
            {
                // Kutuyu önce temizle
                cmbGorev.DataSource = null;
                cmbGorev.Items.Clear();

                // Veritabanından görevleri çek
                DataTable dt = new DataTable();
                NpgsqlDataAdapter da = new NpgsqlDataAdapter("SELECT gorevid, gorevad FROM gorevler", bgl.Baglanti());
                da.Fill(dt);

                // Eğer veritabanı boşsa program patlamasın diye elle ekleme yapalım (TEST İÇİN)
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Veritabanında hiç görev yok! Lütfen önce veritabanına görev ekleyin.");
                    return;
                }

                // Kutuyu bağla
                cmbGorev.DisplayMember = "gorevad"; // Ekranda görünecek isim
                cmbGorev.ValueMember = "gorevid";   // Arkada çalışacak ID
                cmbGorev.DataSource = dt;

                cmbGorev.SelectedIndex = -1; // Açılışta boş seçili gelsin
            }
            catch (Exception ex)
            {
                MessageBox.Show("Liste Yükleme Hatası: " + ex.Message);
            }
        }

        // ------------------ 2. LİSTELEME ------------------
        void Listele()
        {
            try
            {
                DataTable dt = new DataTable();
                // Personel + Kişiler + Görevler tablosunu birleştir
                string sql = @"SELECT p.PersonelID, k.TCNo, k.Ad, k.Soyad, 
                                      g.GorevAdi, p.GorevID, 
                                      k.Telefon, k.Email, k.Adres
                               FROM Personel p
                               JOIN Kisiler k ON p.KisiID = k.KisiID
                               LEFT JOIN Gorevler g ON p.GorevID = g.GorevID
                               ORDER BY p.PersonelID DESC";

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, bgl.Baglanti());
                da.Fill(dt);
                dataGridView1.DataSource = dt;

                // ID sütununu gizle (Kullanıcı görmesin)
                if (dataGridView1.Columns.Contains("GorevID"))
                    dataGridView1.Columns["GorevID"].Visible = false;
            }
            catch (Exception ex) { MessageBox.Show("Listeleme Hatası: " + ex.Message); }
        }

        // ------------------ 3. TIKLAYINCA DOLDUR ------------------
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                secilenPersonelID = int.Parse(dataGridView1.Rows[e.RowIndex].Cells["PersonelID"].Value.ToString());

                txtTC.Text = dataGridView1.Rows[e.RowIndex].Cells["TCNo"].Value.ToString();
                txtAd.Text = dataGridView1.Rows[e.RowIndex].Cells["Ad"].Value.ToString();
                txtSoyad.Text = dataGridView1.Rows[e.RowIndex].Cells["Soyad"].Value.ToString();
                txtTel.Text = dataGridView1.Rows[e.RowIndex].Cells["Telefon"].Value.ToString();
                txtEmail.Text = dataGridView1.Rows[e.RowIndex].Cells["Email"].Value.ToString();
                txtAdres.Text = dataGridView1.Rows[e.RowIndex].Cells["Adres"].Value.ToString();

                // Görev ComboBox'ını ayarla
                if (dataGridView1.Rows[e.RowIndex].Cells["GorevID"].Value != DBNull.Value)
                {
                    int gID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["GorevID"].Value);
                    cmbGorev.SelectedValue = gID;
                }
            }
            catch { }
        }

        // ------------------ 4. EKLEME (GÖREV SEÇİMLİ) ------------------

        // 2. PERSONEL EKLEME (Standart Yöntem)
        private void btnPersonelEkle_Click(object sender, EventArgs e)
        {
            // Kutu boş mu diye kontrol et
            if (cmbGorev.SelectedIndex == -1 || cmbGorev.SelectedValue == null)
            {
                MessageBox.Show("Lütfen listeden bir Görev seçiniz.");
                return;
            }

            try
            {
                bgl.Baglanti();

                // SQL sorgusu (: parametreleri ile)
                string sql = "INSERT INTO Personel (Ad, Soyad, TCNo, Tel, Email, Adres, Cinsiyet, DogumTarihi, GorevID) VALUES (:p1, :p2, :p3, :p4, :p5, :p6, :p7, :p8, :p9)";

                NpgsqlCommand komut = new NpgsqlCommand(sql, bgl.Baglanti());

                komut.Parameters.AddWithValue(":p1", txtAd.Text);
                komut.Parameters.AddWithValue(":p2", txtSoyad.Text);
                komut.Parameters.AddWithValue(":p3", txtTC.Text);
                komut.Parameters.AddWithValue(":p4", txtTel.Text);
                komut.Parameters.AddWithValue(":p5", txtEmail.Text);
                komut.Parameters.AddWithValue(":p6", txtAdres.Text);
                komut.Parameters.AddWithValue(":p7", cmbCinsiyet.Text);
                komut.Parameters.AddWithValue(":p8", dtpDogum.Value.Date);

                // ARTIK BURASI ÇALIŞACAK:
                // Çünkü Items kısmını temizlediğin için artık String değil, ID gelecek.
                komut.Parameters.AddWithValue(":p9", Convert.ToInt32(cmbGorev.SelectedValue));

                komut.ExecuteNonQuery();
                bgl.Baglanti().Close();

                MessageBox.Show("BAŞARILI: Personel Eklendi.");
                Listele();
                Temizle();
            }
            catch (Exception ex)
            {
                MessageBox.Show("KAYIT HATASI: " + ex.Message);
            }
        }

        // ------------------ 5. ANALİZ (YAŞ HESAPLAMALI) ------------------
        private void btnAnaliz_Click(object sender, EventArgs e)
        {
            // Eğer kimse seçili değilse uyar
            if (secilenPersonelID == 0)
            {
                MessageBox.Show("Lütfen önce listeden analiz edilecek personeli seçin.");
                return;
            }

            try
            {
                // 1. YAŞ HESAPLAMA (Veritabanındaki Fonksiyonu Kullanır)
                string yas = "Bilinmiyor";
                try
                {
                    NpgsqlCommand cmdYas = new NpgsqlCommand("SELECT fn_YasHesapla(@p1)", bgl.Baglanti());
                    cmdYas.Parameters.AddWithValue("@p1", txtTC.Text);
                    object sonuc = cmdYas.ExecuteScalar();
                    if (sonuc != null) yas = sonuc.ToString();
                }
                catch { yas = "-"; } // Fonksiyon yoksa tire koy

                // 2. GÖREV BİLGİSİ
                string gorevAdi = cmbGorev.Text;
                if (string.IsNullOrEmpty(gorevAdi)) gorevAdi = "Belirtilmemiş";

                // 3. RAPORU GÖSTER
                MessageBox.Show($"PERSONEL KARTVİZİTİ\n--------------------------\n" +
                                $"Ad Soyad : {txtAd.Text} {txtSoyad.Text}\n" +
                                $"TC No    : {txtTC.Text}\n" +
                                $"Yaş      : {yas}\n" +
                                $"Görevi   : {gorevAdi}\n" +
                                $"Telefon  : {txtTel.Text}",
                                "Personel Detay Analizi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Analiz Hatası: " + ex.Message);
            }
        }

        // ------------------ 6. SİLME ------------------
        private void btnSil_Click(object sender, EventArgs e)
        {
            if (secilenPersonelID == 0) return;

            if (MessageBox.Show("Personeli silmek istiyor musunuz?", "Onay", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM Personel WHERE PersonelID=@p1", bgl.Baglanti());
                    cmd.Parameters.AddWithValue("@p1", secilenPersonelID);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Silindi.");
                    Listele(); SayacGuncelle(); Temizle();
                }
                catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
            }
        }

        // ------------------ 7. GÜNCELLEME ------------------
        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            if (secilenPersonelID == 0) return;

            try
            {
                NpgsqlConnection conn = bgl.Baglanti();

                // Kişiler tablosunu güncelle
                NpgsqlCommand cmdKisi = new NpgsqlCommand("UPDATE Kisiler SET Ad=@p1, Soyad=@p2, Telefon=@p3, Email=@p4, Adres=@p5 WHERE TCNo=@p6", conn);
                cmdKisi.Parameters.AddWithValue("@p1", txtAd.Text);
                cmdKisi.Parameters.AddWithValue("@p2", txtSoyad.Text);
                cmdKisi.Parameters.AddWithValue("@p3", txtTel.Text);
                cmdKisi.Parameters.AddWithValue("@p4", txtEmail.Text);
                cmdKisi.Parameters.AddWithValue("@p5", txtAdres.Text);
                cmdKisi.Parameters.AddWithValue("@p6", txtTC.Text);
                cmdKisi.ExecuteNonQuery();

                // Personel tablosunu (Görevini) güncelle
                NpgsqlCommand cmdPer = new NpgsqlCommand("UPDATE Personel SET GorevID=@g1 WHERE PersonelID=@pid", conn);
                cmdPer.Parameters.AddWithValue("@g1", Convert.ToInt32(cmbGorev.SelectedValue));
                cmdPer.Parameters.AddWithValue("@pid", secilenPersonelID);
                cmdPer.ExecuteNonQuery();

                MessageBox.Show("Güncellendi.");
                Listele(); 
                Temizle();
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        void SayacGuncelle() { /* Sayaç kodun varsa buraya */ }

        void Temizle()
        {
            txtAd.Text = ""; txtSoyad.Text = ""; txtTC.Text = ""; txtTel.Text = "";
            txtEmail.Text = ""; txtAdres.Text = ""; cmbGorev.SelectedIndex = -1;
            secilenPersonelID = 0;
        }

        private void btnGeri_Click(object sender, EventArgs e)
        {
            FormGiris fr = new FormGiris(); fr.Show(); this.Hide();
        }

        private void btnEkle_Click(object sender, EventArgs e)
        {

        }
    }
}
