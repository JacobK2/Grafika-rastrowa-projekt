using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ObrazyRastrowe
{
    public partial class FormatkaGłówna : Form
    {
        private const string filtrObrazu = "JPEG|*.jpg|BMP|*.bmp|PNG|*.png|TIFF|*.tif|Format kompresji wysoko stratnej|*.fks|Wszystkie pliki|*.*";
        Bitmap bmp = null;
        Bitmap bmpBezZmian = null;

        public FormatkaGłówna()
        {
            InitializeComponent();
        }

        private void OtwórzToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Filter = filtrObrazu;
                openFile.FileName = "";
                openFile.Title = "Wybierz obraz, który chcesz otworzyć";
                if (openFile.ShowDialog() != DialogResult.OK)
                    return;
                string ext = openFile.FileName.Substring(openFile.FileName.Length - 3, 3).ToLower();
                if (ext == "fks")
                {
                    OtworzMetodaKompresji(openFile.FileName);
                    pictureBox1.Image = bmp;
                    pictureBox1.Size = bmp.Size;
                    pictureBox1.Refresh();
                    return;
                }
                bmp = new Bitmap(openFile.FileName);
                bmpBezZmian = new Bitmap(bmp);
                pictureBox1.Image = bmp;
                pictureBox1.Size = bmp.Size;
                pictureBox1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się wczytać obrazu.\n" + ex.Message);
            }
        }

        private void ZapiszToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
            {
                MessageBox.Show("Brak pliku do zapisania");
                return;
            }
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = filtrObrazu;
            if (saveFile.ShowDialog() != DialogResult.OK)
                return;
            string ext = saveFile.FileName.Substring(saveFile.FileName.Length - 3, 3).ToLower();
            ImageFormat imageFormat = ImageFormat.Emf;

            if (ext == "png")
                imageFormat = ImageFormat.Png;
            if (ext == "tif")
                imageFormat = ImageFormat.Tiff;
            if (ext == "jpg")
                imageFormat = ImageFormat.Jpeg;
            if (ext == "bmp")
                imageFormat = ImageFormat.Bmp;
            if (ext == "fks")
            {
                ZapiszMetodaKompresji(saveFile.FileName);
                return;
            }

            bmp.Save(saveFile.FileName, imageFormat);
        }

        void OtworzMetodaKompresji(string fn)
        {
            FileStream fs = new FileStream(fn, FileMode.Open, FileAccess.Read);
            byte[] tbl = new byte[fs.Length - 4];
            int bmp_width = 256 * fs.ReadByte() + fs.ReadByte();
            int bmp_height = 256 * fs.ReadByte() + fs.ReadByte();
            fs.Read(tbl, 0, tbl.Length);
            fs.Close();

            int[,,] bmp_wyn = new int[bmp_width, bmp_height, 3];

            int pixels = bmp_width * bmp_height;
            for (int i = 0; i < bmp_width; i++)
                for (int j = 0; j < bmp_height; j++)
                    for (int s = 0; s < 3; s++)
                        bmp_wyn[i, j, s] = 0;
    

            for (int y = 0; y < bmp_height; y++)
            {
                progressBar1.Value = 10 + y * 20 / bmp_height;

                for (int x = 0; x < bmp_width; x++)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        int bit_nr = y * bmp_width * 3 + x * 3 + s;
                        int byte_nr = bit_nr / 8;
                        int bit_in_byte_nr = bit_nr % 8;

                        if (tbl[byte_nr] / ((byte)Math.Pow(2, bit_in_byte_nr)) % 2 == 1)
                            bmp_wyn[x, y, s] = 255;
                    }
                }
            }
            int blur = 2;
            for (int y = 0; y < bmp_height; y++)
            {
                progressBar1.Value = 30 + y * 50 / bmp_height;

                for (int x = 0; x < bmp_width; x++)
                {
                    int[] suma = { 0, 0, 0 };
                    int i = 0;
                    for (int x1 = x - blur; x1 <= x + blur; x1++)
                    {
                        for (int y1 = y - blur; y1 <= y + blur; y1++)
                        {
                            if (x1 < 0 || x1 >= bmp_width || y1 < 0 || y1 >= bmp_height)
                                continue;
                            i++;
                            for (int s = 0; s < 3; s++)
                                suma[s] += bmp_wyn[x1, y1, s];
                        }
                    }

                    for (int s = 0; s < 3; s++)
                        bmp_wyn[x, y, s] = suma[s] / i;

                }
            }
            bmp = new Bitmap(bmp_width, bmp_height);
            for (int y = 0; y < bmp_height; y++)
            {
                progressBar1.Value = 80 + y * 20 / bmp_height;

                for (int x = 0; x < bmp_width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb(255,bmp_wyn[x, y, 0], bmp_wyn[x, y, 1], bmp_wyn[x, y, 2]));
            }


        }
        void ZapiszMetodaKompresji(string fn)
        {

            {
                if (bmp == null)
                    return;

                int bmp_width = bmp.Width, bmp_height = bmp.Height;

                int[,,] bmp_tbl = new int[bmp_width, bmp_height, 3];

                Color c;
                int err;
                progressBar1.Visible = true;
                for (int y = 0; y < bmp_height; y++)
                {
                    progressBar1.Value = y * 15 / bmp_height;
                    for (int x = 0; x < bmp_width; x++)
                    {
                        c = bmp.GetPixel(x, y);
                        bmp_tbl[x, y, 0] = c.R;
                        bmp_tbl[x, y, 1] = c.G;
                        bmp_tbl[x, y, 2] = c.B;
                    }
                }

                int[,,] bmp_wyn = new int[bmp_width, bmp_height, 3];

                for (int y = 0; y < bmp_height; y++)
                {
                    progressBar1.Value = 15 + y * 15 / bmp_height;
                    for (int x = 0; x < bmp_width; x++)
                    {
                        for (int s = 0; s < 3; s++)
                        {
                            if (bmp_tbl[x, y, s] < 128)
                                bmp_wyn[x, y, s] = 0;
                            else
                                bmp_wyn[x, y, s] = 255;

                            err = bmp_tbl[x, y, s] - bmp_wyn[x, y, s];

                            if (x < bmp_width - 1)
                                bmp_tbl[x + 1, y, s] += err / 4;
                            if (y < bmp_height - 1)
                            {
                                if (x > 0)
                                    bmp_tbl[x - 1, y + 1, s] += err / 4;
                                bmp_tbl[x, y + 1, s] += err / 4;
                                if (x < bmp.Width - 1)
                                    bmp_tbl[x + 1, y + 1, s] += err / 4;
                            }
                        }
                    }
                }
                int pixels = bmp_width * bmp_height;
                int bytes = pixels * 3 / 8;
                if (bytes * 8 < pixels * 3)
                    bytes++;
                byte[] tbl = new byte[bytes];
                for (int i = 0; i < tbl.Length; i++)
                    tbl[i] = 0;

                for (int y = 0; y < bmp_height; y++)
                {
                    progressBar1.Value = 30 + y * 15 / bmp_height;

                    for (int x = 0; x < bmp_width; x++)
                    {
                        for (int s = 0; s < 3; s++)
                        {
                            int bit_nr = y * bmp_width * 3 + x * 3 + s;
                            int byte_nr = bit_nr / 8;
                            int bit_in_byte_nr = bit_nr % 8;

                            if (bmp_wyn[x, y, s] > 128)
                                tbl[byte_nr] += (byte)Math.Pow(2, bit_in_byte_nr);
                        }
                    }
                }
                FileStream fs = new FileStream(fn, FileMode.Create, FileAccess.ReadWrite);
                fs.WriteByte((byte)(bmp_width / 256));
                fs.WriteByte((byte)(bmp_width % 256));
                fs.WriteByte((byte)(bmp_height / 256));
                fs.WriteByte((byte)(bmp_height % 256));

                fs.Write(tbl, 0, tbl.Length);

                fs.Close();
                int blur = 2;
                for (int y = 0; y < bmp.Height; y++)
                {
                    progressBar1.Value = 45 + y * 35 / bmp.Height;

                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int[] suma = { 0, 0, 0 };
                        int i = 0;
                        for (int x1 = x - blur; x1 <= x + blur; x1++)
                        {
                            for (int y1 = y - blur; y1 <= y + blur; y1++)
                            {
                                if (x1 < 0 || x1 >= bmp_width || y1 < 0 || y1 >= bmp_height)
                                    continue;
                                i++;
                                for (int s = 0; s < 3; s++)
                                    suma[s] += bmp_wyn[x1, y1, s];
                            }
                        }

                        for (int s = 0; s < 3; s++)
                            bmp_wyn[x, y, s] = suma[s] / i;

                    }
                }
                for (int y = 0; y < bmp.Height; y++)
                {
                    progressBar1.Value = 80 + y * 20 / bmp.Height;

                    for (int x = 0; x < bmp.Width; x++)
                        bmp.SetPixel(x, y, Color.FromArgb(bmp_wyn[x, y, 0], bmp_wyn[x, y, 1], bmp_wyn[x, y, 2]));
                }

                pictureBox1.Refresh();
                progressBar1.Visible = false;
            }

        }

        private void ToolStripMenuItem5_Click(object sender, EventArgs e)
        {

        }

        private void XToolStripMenuItem3_Click(object sender, EventArgs e)
        {

        }

        private void ToolStripTextBox1_Click(object sender, EventArgs e)
        {

        }

        private void ToolStripTextBox2_Click(object sender, EventArgs e)
        {

        }

        private void ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width / 2, bmp.Height / 2);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void OryginalnyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = bmp.Size;
            pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
            pictureBox1.Refresh();
        }

        private void DopasujToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = panel1.ClientSize;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width / 4, bmp.Height / 4);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width / 8, bmp.Height / 8);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void ToolStripMenuItem5_Click_1(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width / 16, bmp.Height / 16);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width * 2, bmp.Height * 2);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void XToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width * 4, bmp.Height * 4);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void XToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width * 8, bmp.Height * 8);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void XToolStripMenuItem3_Click_1(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            pictureBox1.Size = new Size(bmp.Width * 16, bmp.Height * 16);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Refresh();
        }

        private void ŚciemnijToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = y * 100 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(c.R / 2, c.G / 2, c.B / 2));
                }
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void RozjaśnijToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = y * 100 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(255 - (255 - c.R) / 2, 255 - (255 - c.G) / 2, 255 - (255 - c.B) / 2));
                }
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void SuwakDoJasnościToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
            {
                MessageBox.Show("Proszę wybrać obraz do obróbki");
                return;
            }

            SuwakJasności suwakJasności;
            suwakJasności = new SuwakJasności();
            if (suwakJasności.ShowDialog() != DialogResult.OK)
                return;

            int rś = suwakJasności.Wartość;
            if (rś == 0)
                return;

            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = y * 100 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (rś < 0)
                        bmp.SetPixel(x, y, Color.FromArgb(c.R * (rś + 100) / 100, c.G * (rś + 100) / 100, c.B * (rś + 100) / 100));
                    if (rś > 0)
                        bmp.SetPixel(x, y, Color.FromArgb(255 - (255 - c.R) * (100 - rś) / 100, 255 - (255 - c.G) * (100 - rś) / 100, 255 - (255 - c.B) * (100 - rś) / 100));
                }
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void KoloryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;

            int[,,] bmp_tbl = new int[bmp.Width, bmp.Height, 3];

            Color c;
            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = y * 20 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    c = bmp.GetPixel(x, y);
                    bmp_tbl[x, y, 0] = c.R;
                    bmp_tbl[x, y, 1] = c.G;
                    bmp_tbl[x, y, 2] = c.B;
                }
            }

            int[,,] bmp_wyn = new int[bmp.Width, bmp.Height, 3];

            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = 20 + y * 60 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        if (bmp_tbl[x, y, s] < 128)
                            bmp_wyn[x, y, s] = 0;
                        else
                            bmp_wyn[x, y, s] = 255;
                    }


                }
            }

            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = 80 + y * 20 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb(bmp_wyn[x, y, 0], bmp_wyn[x, y, 1], bmp_wyn[x, y, 2]));
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void KoloryFSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;

            int[,,] bmp_tbl = new int[bmp.Width, bmp.Height, 3];

            Color c;
            int err;
            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = y * 20 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    c = bmp.GetPixel(x, y);
                    bmp_tbl[x, y, 0] = c.R;
                    bmp_tbl[x, y, 1] = c.G;
                    bmp_tbl[x, y, 2] = c.B;
                }
            }

            int[,,] bmp_wyn = new int[bmp.Width, bmp.Height, 3];

            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = 20 + y * 60 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        if (bmp_tbl[x, y, s] < 128)
                            bmp_wyn[x, y, s] = 0;
                        else
                            bmp_wyn[x, y, s] = 255;

                        err = bmp_tbl[x, y, s] - bmp_wyn[x, y, s];

                        if (x < bmp.Width - 1)
                            bmp_tbl[x + 1, y, s] += err / 4;
                        if (y < bmp.Height - 1)
                        {
                            if (x > 0)
                                bmp_tbl[x - 1, y + 1, s] += err / 4;

                            bmp_tbl[x, y + 1, s] += err / 4;

                            if (x < bmp.Width - 1)
                                bmp_tbl[x + 1, y + 1, s] += err / 4;


                        }
                    }


                }
            }

            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = 80 + y * 20 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb(bmp_wyn[x, y, 0], bmp_wyn[x, y, 1], bmp_wyn[x, y, 2]));
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void KoloryFSBlurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;

            int bmp_width = bmp.Width, bmp_hieght = bmp.Height;

            int[,,] bmp_tbl = new int[bmp_width, bmp_hieght, 3];

            Color c;
            int err;
            progressBar1.Visible = true;
            for (int y = 0; y < bmp_hieght; y++)
            {
                progressBar1.Value = y * 20 / bmp_hieght;

                for (int x = 0; x < bmp_width; x++)
                {
                    c = bmp.GetPixel(x, y);
                    bmp_tbl[x, y, 0] = c.R;
                    bmp_tbl[x, y, 1] = c.G;
                    bmp_tbl[x, y, 2] = c.B;
                }
            }

            int[,,] bmp_wyn = new int[bmp_width, bmp_hieght, 3];


            for (int y = 0; y < bmp_hieght; y++)
            {
                progressBar1.Value = 20 + y * 40 / bmp_hieght;

                for (int x = 0; x < bmp_width; x++)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        if (bmp_tbl[x, y, s] < 128)
                            bmp_wyn[x, y, s] = 0;
                        else
                            bmp_wyn[x, y, s] = 255;

                        err = bmp_tbl[x, y, s] - bmp_wyn[x, y, s];

                        if (x < bmp_width - 1)
                            bmp_tbl[x + 1, y, s] += err / 4;
                        if (y < bmp_hieght - 1)
                        {
                            if (x > 0)
                                bmp_tbl[x - 1, y + 1, s] += err / 4;

                            bmp_tbl[x, y + 1, s] += err / 4;

                            if (x < bmp.Width - 1)
                                bmp_tbl[x + 1, y + 1, s] += err / 4;


                        }
                    }


                }
            }

            int blur = 1;


            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = 60 + y * 20 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    int[] suma = { 0, 0, 0 };
                    int i = 0;
                    for (int x1 = x - blur; x1 <= x + blur; x1++)
                    {
                        for (int y1 = y - blur; y1 <= y + blur; y1++)
                        {
                            if (x1 < 0 || x1 >= bmp_width || y1 < 0 || y1 >= bmp_hieght)
                                continue;
                            i++;
                            for (int s = 0; s < 3; s++)
                                suma[s] += bmp_wyn[x1, y1, s];
                        }
                    }

                    for (int s = 0; s < 3; s++)
                        bmp_wyn[x, y, s] = suma[s] / i;

                }
            }


            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = 80 + y * 20 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb(bmp_wyn[x, y, 0], bmp_wyn[x, y, 1], bmp_wyn[x, y, 2]));
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void NegatywToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;

            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = y * 100 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
                }
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void CofnijZmianyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmpBezZmian == null)
                return;
            bmp = new Bitmap(bmpBezZmian);
            pictureBox1.Image = bmp;
            pictureBox1.Size = bmp.Size;
            pictureBox1.Refresh();
        }

        private void BlurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;


            progressBar1.Visible = true;
            for (int y = 0; y < bmp.Height; y++)
            {
                progressBar1.Value = y * 100 / bmp.Height;

                for (int x = 0; x < bmp.Width; x++)
                {
                    int sr = 0, sg = 0, sb = 0, i = 0;

                    for (int x1 = x - 1; x1 <= x + 1; x1++)
                        for (int y1 = y - 1; y1 <= y + 1; y1++)
                        {
                            if (x1 >= bmp.Width || x1 < 0 || y1 < 0 || y1 >= bmp.Height)
                                continue;
                            Color c = bmp.GetPixel(x1, y1);
                            sr += c.R;
                            sg += c.G;
                            sb += c.B;
                            i++;
                        }

                    bmp.SetPixel(x, y, Color.FromArgb(sr / i, sg / i, sb / i));


                }
            }

            pictureBox1.Refresh();
            progressBar1.Visible = false;
        }

        private void FormatkaGłówna_Load(object sender, EventArgs e)
        {

        }

        private void SepiaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    int red = (int)(0.393 * c.R + 0.769 * c.G + 0.189 * c.B);
                    int green = (int)(0.349 * c.R + 0.686 * c.G + 0.168 * c.B);
                    int blue = (int)(0.272 * c.R + 0.534 * c.G + 0.131 * c.B);
                    if (red > 255) red = 255;
                    if (red < 0) red = 0;
                    if (green > 255) green = 255;
                    if (green < 0) green = 0;
                    if (blue > 255) blue = 255;
                    if (blue < 0) blue = 0;
                    bmp.SetPixel(x, y, Color.FromArgb(red, green, blue));
                }
            }
            pictureBox1.Refresh();
        }

        private void OdcieńSzarościToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bmp == null)
                return;
            for (int y = 1; y < bmp.Height; y++)
            {
                for (int x = 1; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    int sz = (int)Math.Round(.299 * c.R + .587 * c.G + .114 * c.B);
                    bmp.SetPixel(x, y, Color.FromArgb(sz, sz, sz));
                }
            }
            pictureBox1.Refresh();
        }
    }
}
