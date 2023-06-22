using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Threading;


namespace MyFirstNeuralNetwork
{
    public partial class Form1 : Form
    {
        const string path = "..//..//..//picture_of_X";

        Graphics g1, g2, g3, g4;
        Bitmap img;
        int x = -1, y = -1;
        bool moving = false;
        Pen pen;
        NeuralNetwork n;


        public Form1()
        {
            InitializeComponent();

            img = new Bitmap(200, 200);
            g1 = panel1.CreateGraphics();
            g2 = Graphics.FromImage(img);
            g1.Clear(Color.White);
            g2.Clear(Color.White);
            g1.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g2.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            pen = new Pen(Color.Black, 20);
            pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            n = new NeuralNetwork("..//..//..//Neural Network.bin");
            g3 = pictureBox2.CreateGraphics();
            g4 = panel2.CreateGraphics();
        }

        private Bitmap average_img(Bitmap input)
        {
            Point average = new Point(0, 0);
            int count = 0;
            for (int i = 0; i < 200; i++)
                for (int j = 0; j < 200; j++)
                    if (input.GetPixel(i, j).ToArgb() == Color.Black.ToArgb())
                    {
                        average.X += i;
                        average.Y += j;
                        count++;
                    }
            if (count != 0)
            {
                average.X = 100 - average.X / count;
                average.Y = 100 - average.Y / count;
            }
            Bitmap output = new Bitmap(200, 200);
            for (int i = 0; i < 200; i++)
                for (int j = 0; j < 200; j++)
                {
                    if (i - average.X < 200 && 0 <= i - average.X && j - average.Y < 200 && 0 <= j - average.Y)
                        output.SetPixel(i, j, input.GetPixel(i - average.X, j - average.Y));
                    else
                        output.SetPixel(i, j, Color.White);
                }
            return output;
        }

        private Bitmap distortion_img(Bitmap input)
        {
            Random rand = new Random();

            //change position
            Point solt = new Point(rand.Next(-20, 21), rand.Next(-20, 21));
            Bitmap midle_img1 = new Bitmap(200, 200);
            for (int i = 0; i < 200; i++)
                for (int j = 0; j < 200; j++)
                {
                    Color c = Color.White;
                    if (0 <= i + solt.X && i + solt.X < 200 && 0 <= j + solt.Y && j + solt.Y < 200)
                        c = input.GetPixel(i + solt.X, j + solt.Y);
                    midle_img1.SetPixel(i, j, c);
                }

            ///change X-axis ration
            int distortion = rand.Next(7, 15);
            Bitmap midle_img2 = new Bitmap(200, 200);
            for (int i = 0; i < 200; i++)
                for (int j = 0; j < 200; j++)
                    midle_img2.SetPixel(i, j, Color.White);
            int midle_img_index = 0;
            for (int i = 0; i < 200; i += 10)
            {
                Bitmap piece = new Bitmap(10 * distortion, 200);
                for (int i2 = 0; i2 < 10; i2++)
                    for (int j2 = 0; j2 < 200; j2++)
                        for (int k = 0; k < distortion; k++)
                            piece.SetPixel(i2 * distortion + k, j2, midle_img1.GetPixel(i + i2, j2));

                for (int i2 = 0; i2 < distortion; i2++)
                    for (int j2 = 0; j2 < 200; j2++)
                    {
                        if (i2 + midle_img_index >= 200)
                            break;
                        long color = 0;
                        for (int k = 0; k < 10; k++)
                            if (piece.GetPixel(i2 * 10 + k, j2).ToArgb() == Color.White.ToArgb())
                                color++;
                        if (color > 5)
                            color = Color.White.ToArgb();
                        else
                            color = Color.Black.ToArgb();
                        midle_img2.SetPixel(i2 + midle_img_index, j2, Color.FromArgb((int)color));
                    }
                midle_img_index += distortion;
                distortion += rand.Next(-1, 2);
                if (distortion < 7)
                    distortion = 7;
                if (distortion > 14)
                    distortion = 14;
            }

            //switch picturs to zero varubale
            midle_img1 = midle_img2;

            //change Y-axis ration
            distortion = rand.Next(7, 15);
            midle_img2 = new Bitmap(200, 200);
            for (int i = 0; i < 200; i++)
                for (int j = 0; j < 200; j++)
                    midle_img2.SetPixel(i, j, Color.White);
            midle_img_index = 0;
            for (int i = 0; i < 200; i += 10)
            {
                Bitmap piece = new Bitmap(200, 10 * distortion);
                for (int i2 = 0; i2 < 10; i2++)
                    for (int j2 = 0; j2 < 200; j2++)
                        for (int k = 0; k < distortion; k++)
                            piece.SetPixel(j2, i2 * distortion + k, midle_img1.GetPixel(j2, i + i2));

                for (int i2 = 0; i2 < distortion; i2++)
                    for (int j2 = 0; j2 < 200; j2++)
                    {
                        if (i2 + midle_img_index >= 200)
                            break;
                        long color = 0;
                        for (int k = 0; k < 10; k++)
                            if (piece.GetPixel(j2, i2 * 10 + k).ToArgb() == Color.White.ToArgb())
                                color++;
                        if (color > 5)
                            color = Color.White.ToArgb();
                        else
                            color = Color.Black.ToArgb();
                        midle_img2.SetPixel(j2, i2 + midle_img_index, Color.FromArgb((int)color));
                    }
                midle_img_index += distortion;
                distortion += rand.Next(-1, 2);
                if (distortion < 7)
                    distortion = 7;
                if (distortion > 14)
                    distortion = 14;
            }

            return midle_img2;
        }

        private void Save_img(Bitmap input)
        {
            bool stop = false;
            for (int i = 0; i < 200 && !stop; i++)
                for (int j = 0; j < 200 && !stop; j++)
                    if (input.GetPixel(i, j).ToArgb() != Color.White.ToArgb())
                    {
                        int index = 0;
                        do
                        {
                            index++;
                        } while (File.Exists(path + index.ToString() + ".png"));

                        input.Save(path + index.ToString() + ".png");
                        stop = true;
                    }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            moving = true;
            x = e.X;
            y = e.Y;
            panel1.Cursor = Cursors.Cross;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (moving && x != -1 && y != -1)
            {
                g1.DrawLine(pen, new Point(x, y), e.Location);
                g2.DrawLine(pen, new Point(x, y), e.Location);
                x = e.X;
                y = e.Y;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            g1.Clear(Color.White);
            g2.Clear(Color.White);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("?אתה בטוח שברצונך למחוק את הרשת, ליצור אחת חדשה", "הודעה", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            n = new NeuralNetwork(900, new int[] { 20, 20 }, 10);
            n.Save("..//..//..//Neural Network.bin");
            n.SetUpKernel();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            LinkedList<string> input = new LinkedList<string>();
            LinkedList<double[]> result = new LinkedList<double[]>();

            for (int i = 0; i < 10; i++)
                for (int j = 1; j <= 500; j++)
                {
                    input.AddLast("..//..//..//picture_of_" + i.ToString() + "//" + j.ToString() + ".png");
                    result.AddLast(new double[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
                    result.Last.ValueRef[i] = 1;
                }

            int count = n.CountMistakes(input.ToArray(), result.ToArray(), (double[] L1, double[] L2) =>
            {
                int L1MaxIndex = 0;
                for (int i = 1; i < L1.Length; i++)
                    if (L1[L1MaxIndex] < L1[i])
                        L1MaxIndex = i;
                

                return L2[L1MaxIndex] == 1;
            });

            MessageBox.Show((count / 50.0).ToString() + "% :אמינות הרשת המשוערת היא");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("אי אפשר לעשות אימונים עכשיו", "הודעה", MessageBoxButtons.OK);
            //return;

            if (MessageBox.Show("?אתה בטוח שברצונך לבצע אימון נוסף", "הודעה", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            n.SetUpKernel();

            Train(50, 250, 500);
            //MessageBox.Show("End - 50,250,500");
            Train(50, 500, 500);
            //MessageBox.Show("End - 50,500,500");
            Train(50, 1000, 500);
            //MessageBox.Show("End - 50,1000,500");
            Train(50, 2500, 500);
            //MessageBox.Show("End - 50,2500,500");
            Train(50, 5000, 500);
            //MessageBox.Show("End - 50,5000,500");
            MessageBox.Show("End Train");


            n.Save("..//..//..//Neural Network.bin");
        }

        private void Train(int Times, int Parallel, int DataSize)
        {
            int InOrder = DataSize * 10 / Parallel;

            //Random random = new Random(DateTime.Now.Millisecond + DateTime.Now.Second * 1000 + DateTime.Now.Month * 60000 + DateTime.Now.Hour * 3600000);
            Random random = new Random();

            System.Threading.Tasks.Parallel.For(0, InOrder, iteration =>
            {
                List<int[]> l = new List<int[]>();
                for (int i = 0; i < 10; i++)
                    for (int j = 1; j <= DataSize; j++)
                        l.Add(new int[] { i, j });

                System.Threading.Tasks.Parallel.For(0, InOrder, i =>
                {
                    List<string> input = new List<string>();
                    List<double[]> result = new List<double[]>();
                    lock (l) { lock (input) { lock (result)
                            {
                                for (int j = 0; j < Parallel; j++)
                                {
                                    int n = random.Next(0, l.Count);

                                    input.Add("..//..//..//picture_of_" + l[n][0].ToString() + "//" + l[n][1].ToString() + ".png");
                                    result.Add(new double[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
                                    result[^1][l[n][0]] = 1;


                                    l[n] = l[^1];
                                    l.RemoveAt(l.Count - 1);

                                }
                            } } }

                    //if (random.Next(0, 100) == 0)
                    //{
                    //    int number_of_mistake = n.TrainingOnMistake(input.ToArray(), result.ToArray(), n.TrainingGPU);
                    //    if (number_of_mistake <= 300)
                    //    {
                    //        n.Save("..//..//..//Neural Network.bin");
                    //        g4.Clear(Color.White);
                    //        g4.DrawString(number_of_mistake.ToString(), new Font("Calibri", 50), new Pen(Color.Black).Brush, 100, 50);
                    //        return;
                    //    }
                    //}
                    //else
                    n.TrainingGPU(input.ToArray(), result.ToArray());
                });
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            g1.DrawImage(average_img(img), new Point(0, 0));
            g2.DrawImage(average_img(img), new Point(0, 0));

            double[] result = n.Use(img);

            string text = "";
            int number = 0;
            for (int i = 0; i < 10; i++)
            {
                text += i.ToString() + "). \x2192" + result[i].ToString() + "\r\n";
                if (result[i] > result[number])
                    number = i;
                Color color = Color.FromArgb(255 - (int)(255 * result[i]), 255 - (int)(255 * result[i]), 255 - (int)(255 * result[i]));
                g3.FillEllipse(new Pen(Color.FromArgb(255, color.G, color.B)).Brush, 10, 8 + i * 32, 30, 30);
                g3.DrawString(i.ToString(), new Font("Calibri", 20), new Pen(Color.FromArgb(0, 255 - color.G, 255 - color.B)).Brush, 14, 7 + i * 32);
            }
            label2.Text = text;

            Bitmap bi = new Bitmap(600, 170);
            for (int i = 0; i < 600; i++)
            {
                for (int j = 0; j < 170; j++)
                {
                    int r = 0, g = 0, b = 0, a;
                    if (j < 85)
                        a = j;
                    else
                        a = 170 - j;
                    if (i < 200)
                    {
                        r = i;
                        g = i;
                    }
                    if (i >= 200 && i < 400)
                    {
                        r = 400 - i;
                        g = 200;
                        b = i - 200;
                    }
                    if (i >= 400 && i < 600)
                    {
                        b = 600 - i;
                        g = 600 - i;
                    }
                    r = r * a / 85;
                    g = g * a / 85;
                    b = b * a / 85;
                    bi.SetPixel(i, j, Color.FromArgb(240 - r / 2, 240 - g / 2, 240 - b / 2));
                }
            }
            g4.DrawImage(bi, 0, 0);
            g4.DrawString(":התוצאה היא", new Font("Guttman Yad-Brush", 50), new Pen(Color.Black).Brush, 110, 40);
            g4.DrawString(number.ToString(), new Font("Calibri", 80), new Pen(Color.Black).Brush, 20, 15);
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            const int SizeOfImage = 30;
            //Remember to change the number also in the other function (NeuralNetwork.GetData).

            moving = false;
            x = -1;
            y = -1;
            panel1.Cursor = Cursors.Default;

            Bitmap map = new Bitmap(img, SizeOfImage, SizeOfImage);
            pictureBox3.Image = new Bitmap(map, 200, 200);
        }
    }
}