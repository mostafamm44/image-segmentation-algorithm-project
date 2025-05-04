using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
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

        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value ;
            ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            //ImageOperations.DisplayImage(ImageMatrix, pictureBox2);

            var (graph, uf) = ImageOperations.BuildGraph(ImageMatrix);
            var internalMaxEdges = ImageOperations.GetInternalMaxEdges(graph, uf);

            // 3. Find external min edges
            var externalMinEdges =ImageOperations.GetExternalMinEdges(graph, uf);

            // 4. Merge components adaptively
            UnionFind mergedUF = ImageOperations.MergeComponents(graph, uf, internalMaxEdges, externalMinEdges,300);

            ImageOperations.DisplayComponents(ImageMatrix, pictureBox2, mergedUF);
            //  var graph = ImageOperations.BuildGraph(ImageMatrix, 1);
            //  ImageOperations.DisplayGraph(graph, pictureBox2, ImageMatrix.GetLength(1), ImageMatrix.GetLength(0));
            //  string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "graph_output.txt");

            //  ImageOperations.SaveGraphToFile(graph, path);
            //   MessageBox.Show($"Graph saved to:\n{path}");



        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}