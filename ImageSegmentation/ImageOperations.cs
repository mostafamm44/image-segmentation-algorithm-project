using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageTemplate
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }
    
  
    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }
        
        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


       /// <summary>
       /// Apply Gaussian smoothing filter to enhance the edge detection 
       /// </summary>
       /// <param name="ImageMatrix">Colored image matrix</param>
       /// <param name="filterSize">Gaussian mask size</param>
       /// <param name="sigma">Gaussian sigma</param>
       /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];

           
            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return  Filtered;
        }


        // start code graph
        public struct PixelNode
        {
            public int X, Y;
            public byte Color;

            public PixelNode(int x, int y, byte color)
            {
                X = x;
                Y = y;
                Color = color;
            }
        }
        public struct Edge
        {
            public PixelNode From;
            public PixelNode To;
            public double Weight;

            public Edge(PixelNode from, PixelNode to, double weight)
            {
                From = from;
                To = to;
                Weight = weight;
            }
        }
        //build graph for given matrix and c is the color pixel 1 for red 2 for blue 3 for green
        public static Dictionary<PixelNode, List<Edge>> BuildGraph(RGBPixel[,] image,int c)
        {
            int height = GetHeight(image);
            int width = GetWidth(image);
            
            var graph = new Dictionary<PixelNode, List<Edge>>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    PixelNode node;
                    if (c == 1) {
                        node = new PixelNode(x, y, image[y, x].red); 
                    }
                    else if (c == 2)
                    {
                         node = new PixelNode(x, y, image[y, x].blue);
                    }
                    else //if (c == 3)
                    {
                       node = new PixelNode(x, y, image[y, x].green);
                    }
                    graph[node] = new List<Edge>();
                    //calc the 8 edges
                   
                    int[,] directions = {
                        {1,0 },
                        {1,1},
                        {0,1},
                        {-1, 1},
                        {-1,0 },
                        {-1,-1},
                        {0, -1},
                        {1,-1 }  
                    };

                    int l = directions.GetLength(0);
                    for (int i = 0; i < l; i++)
                    {
                        int newY = y + directions[i, 0];
                        int newX = x + directions[i, 1];
                        if (newY >= 0 && newY < height && newX >= 0 && newX < width)
                        {
                            PixelNode next_node;
                            if (c == 1)
                            {
                                next_node = new PixelNode(newX, newY, image[newY, newX].red);
                            }
                            else if (c == 2)
                            {
                                next_node = new PixelNode(newX, newY, image[newY, newX].blue);
                            }
                            else //if (c == 3)
                            {
                                next_node = new PixelNode(newX, newY, image[newY, newX].green);
                            }
                           

                            Edge ed = new Edge(node,next_node, Math.Abs(node.Color - next_node.Color));
                            graph[node].Add(ed);
                        }

                    }
                    

                    
                }
            }

            return graph;
        }
        
        public static void PrintGraph(Dictionary<PixelNode, List<Edge>> graph, TextBox outputBox)
        {
            outputBox.Clear();  // clear previous content

            foreach (var kvp in graph)
            {
                PixelNode node = kvp.Key;
                outputBox.AppendText($"Node ({node.X}, {node.Y}) - Color: {node.Color}\r\n");

                foreach (var edge in kvp.Value)
                {
                    outputBox.AppendText($"\t-> ({edge.To.X}, {edge.To.Y}) - Color: {edge.To.Color}, Weight: {edge.Weight}\r\n");
                }
            }
        }
        // save the graph to textfile
        public static void SaveGraphToFile(Dictionary<PixelNode, List<Edge>> graph, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var kvp in graph)
                {
                    PixelNode node = kvp.Key;
                    writer.WriteLine($"Node ({node.X}, {node.Y}) - Color: {node.Color}");

                    foreach (Edge edge in kvp.Value)
                    {
                        writer.WriteLine($"\t-> ({edge.To.X}, {edge.To.Y}) - Color: {edge.To.Color}, Weight: {edge.Weight}");
                    }
                }
            }
        }
        //display the graph in the picBox 
        public static void DisplayGraph(Dictionary<PixelNode, List<Edge>> graph, PictureBox PicBox, int width, int height)
        {
            // Create a blank bitmap with the specified dimensions
            Bitmap graphBMP = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            // Use Graphics object to draw on the bitmap
            using (Graphics g = Graphics.FromImage(graphBMP))
            {
                // Fill the background with white (or any other color)
                g.Clear(Color.White);

                // Draw edges first (so nodes appear on top)
                foreach (var kvp in graph)
                {
                    PixelNode fromNode = kvp.Key;
                    foreach (Edge edge in kvp.Value)
                    {
                        PixelNode toNode = edge.To;
                        // Draw edge as a line (light gray)
                        g.DrawLine(Pens.LightGray, fromNode.X, fromNode.Y, toNode.X, toNode.Y);
                    }
                }

                // Then draw nodes with their respective colors
                foreach (PixelNode node in graph.Keys)
                {
                    // Convert node's color to System.Drawing.Color
                    Color nodeColor = GetColorFromNode(node);

                    // Create a brush with the node's color
                    using (Brush nodeBrush = new SolidBrush(nodeColor))
                    {
                        // Draw node as a small filled circle
                        int nodeSize = 3; // Adjust size as needed
                        g.FillEllipse(nodeBrush, node.X - nodeSize / 2, node.Y - nodeSize / 2, nodeSize, nodeSize);
                    }
                }
            }

            // Display the resulting bitmap in the PictureBox
            PicBox.Image = graphBMP;
        }

        // Helper method to convert PixelNode's color to System.Drawing.Color
        private static Color GetColorFromNode(PixelNode node)
        {
            // Assuming PixelNode has Red, Green, Blue properties (or similar)
            // Adjust this based on your actual PixelNode structure
            return Color.FromArgb(node.Color, 0, 0);

            // Alternative if color is stored as a single value:
            // return Color.FromArgb(node.ColorValue);
        }
    }
}
