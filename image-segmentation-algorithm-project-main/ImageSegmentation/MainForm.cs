using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static ImageTemplate.ImageOperations;
using static System.Net.Mime.MediaTypeNames;

namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
        }
        ImageOperations.PixelNode[,] NodeMap;
        DisjointSet regoins;
        int w, h;
        RGBPixel[,] allimage;
        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            allimage = ImageMatrix;
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;
            //ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            //ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
            int width = ImageOperations.GetWidth(ImageMatrix);
            int height = ImageOperations.GetHeight(ImageMatrix);
            h = height;
            w = width;
            var (regionSet, nodeMap, Rdges, /*Gdges, Bdges,*/ RAlledges, GAlledges, BAlledges) = ImageOperations.BuildGraph(ImageMatrix);
            NodeMap = new PixelNode[height, w];
            regoins = new DisjointSet(width * height);
            regoins = regionSet;
            NodeMap = nodeMap;
            //var Rset = ImageOperations.components(regionSet, nodeMap, height, width, 30000, Rdges, /*Gdges, Bdges,*/ RAlledges, GAlledges, BAlledges);            // 3. Find external min edges
            //ImageOperations.WriteDisjointSetsToDesktop(nodeMap, Rset, width, height);
            //ImageOperations.DisplayDisjointSets(nodeMap, width, height, Rset, pictureBox2);




        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
        List<(int x, int y)> selected = new List<(int, int)>();

        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {


            selected.Add((e.X, e.Y));

        }
        private void button1_Click(object sender, EventArgs e)
        {
            RGBPixel[,] merged = new RGBPixel[h, w];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    merged[i, j].red = 255;
                    merged[i, j].green = 255;
                    merged[i, j].blue = 255;
                }
            }
            //int u;
            //  DisjointSet merged = new DisjointSet(h * w);
            foreach (var t in selected)
            {
                int component = regoins.Find(NodeMap[t.y, t.x].id);

                foreach (var c in regoins.P_id[component])
                {
                    merged[c.Item1, c.Item2] = allimage[c.Item1, c.Item2];
                }
            }
            ImageOperations.DisplayImage(merged, pictureBox2);
        }
    }
}