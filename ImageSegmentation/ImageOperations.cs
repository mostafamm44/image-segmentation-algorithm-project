using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static ImageTemplate.ImageOperations;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Security.Policy;
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
            public RGBPixel Color;
            public int id;
            public PixelNode(int x, int y, RGBPixel color,int width)
            {
                X = x;
                Y = y;
                Color = color;
                id = y * width + x;
            }
        }
        public struct Edge
        {
            public PixelNode From;
            public PixelNode To;
            public double Weight_red;
            public double Weight_blue;
            public double Weight_green;

            public Edge(PixelNode from, PixelNode to, double w_red, double w_blue, double w_green)
            {
                From = from;
                To = to;
                
               Weight_red = w_red;
                Weight_blue = w_blue;
                Weight_green = w_green;

            }
        }
        //build graph for given matrix and c is the color pixel 1 for red 2 for blue 3 for green
        public static( Dictionary<PixelNode, List<Edge>>,UnionFind) BuildGraph(RGBPixel[,] image)
        {
            int height = GetHeight(image);
            int width = GetWidth(image);
            
            var graph = new Dictionary<PixelNode, List<Edge>>();
            UnionFind uf = new UnionFind(height * width);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    PixelNode  node = new PixelNode(x, y, image[y, x],width); 
                    
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
                            PixelNode next_node = new PixelNode(newX, newY, image[newY, newX], width);
                           
                           

                            Edge ed = new Edge(node,next_node, Math.Abs(node.Color.red - next_node.Color.red),
                                 Math.Abs(node.Color.blue - next_node.Color.blue),
                                  Math.Abs(node.Color.green - next_node.Color.green));
                            graph[node].Add(ed);
                            if (ed.Weight_red <2 && ed.Weight_green <2&& ed.Weight_blue <2)
                            {
                                uf.Union(node.id, next_node.id);
                            }
                        }

                    }
                    

                    
                }
            }

            return (graph,uf);
        }
        
        public static void PrintGraph(Dictionary<PixelNode, List<Edge>> graph, TextBox outputBox)
        {
            outputBox.Clear();  // clear previous content

            foreach (var kvp in graph)
            {
                PixelNode node = kvp.Key;
               // outputBox.AppendText($"Node ({node.X}, {node.Y}) - Color: {node.Color}\r\n");

                foreach (var edge in kvp.Value)
                {
                   // outputBox.AppendText($"\t-> ({edge.To.X}, {edge.To.Y}) - Color: {edge.To.Color}, Weight: {edge.Weight}\r\n");
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
                  //  writer.WriteLine($"Node ({node.X}, {node.Y}) - Color: {node.Color}");

                    foreach (Edge edge in kvp.Value)
                    {
                       // writer.WriteLine($"\t-> ({edge.To.X}, {edge.To.Y}) - Color: {edge.To.Color}, Weight: {edge.Weight}");
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
            return Color.FromArgb(node.Color.red, node.Color.blue, node.Color.green);

            // Alternative if color is stored as a single value:
            // return Color.FromArgb(node.ColorValue);
        }
        // start the union find from here 

        // Finds connected components in a pixel grid using Union-Find
        public static Dictionary<int, List<PixelNode>> FindConnectedComponents(
      Dictionary<PixelNode, List<Edge>> graph,
      UnionFind uf)
        {
            var components = new Dictionary<int, List<PixelNode>>();

            foreach (var node in graph.Keys)
            {
                int root = uf.Find(node.id);
                if (!components.ContainsKey(root))
                {
                    components[root] = new List<PixelNode>();
                }
                components[root].Add(node);
            }

            return components;
        }
        public static Dictionary<int, double> GetInternalMaxEdges(
      Dictionary<PixelNode, List<Edge>> graph,
      UnionFind uf)
        {
            var internalMaxEdges = new Dictionary<int, double>();
            var componentEdges = new Dictionary<int, List<Edge>>();

            // Group edges by their component roots
            foreach (var node in graph.Keys)
            {
                int root = uf.Find(node.id);
                if (!componentEdges.ContainsKey(root))
                    componentEdges[root] = new List<Edge>();

                foreach (Edge edge in graph[node])
                {
                    if (uf.Find(edge.To.id) == root) // Only internal edges
                        componentEdges[root].Add(edge);
                }
            }

            // For each component, find max edge in its MST
            foreach (var component in componentEdges)
            {
                int root = component.Key;
                var edges = component.Value;

                // Kruskal's algorithm to find MST
                edges.Sort((a, b) => (a.Weight_red + a.Weight_green + a.Weight_blue)
                                   .CompareTo(b.Weight_red + b.Weight_green + b.Weight_blue));

                UnionFind tempUF = new UnionFind(uf.parent.Length);
                double maxEdgeWeight = 0;

                foreach (var edge in edges)
                {
                    if (tempUF.Find(edge.From.id) != tempUF.Find(edge.To.id))
                    {
                        double currentWeight = edge.Weight_red + edge.Weight_green + edge.Weight_blue;
                        maxEdgeWeight = Math.Max(maxEdgeWeight, currentWeight);
                        tempUF.Union(edge.From.id, edge.To.id);
                    }
                }

                internalMaxEdges[root] = maxEdgeWeight;
            }

            return internalMaxEdges;
        }
        public static Dictionary<Tuple<int, int>, Tuple<Edge, double>> GetExternalMinEdges(
    Dictionary<PixelNode, List<Edge>> graph,
    UnionFind uf)
        {
            var externalEdges = new Dictionary<Tuple<int, int>, Tuple<Edge, double>>();

            foreach (var node in graph.Keys)
            {
                int rootA = uf.Find(node.id);
                foreach (Edge edge in graph[node])
                {
                    int rootB = uf.Find(edge.To.id);

                    if (rootA != rootB)
                    {
                        var key = Tuple.Create(Math.Min(rootA, rootB), Math.Max(rootA, rootB));
                        double totalWeight = edge.Weight_red + edge.Weight_green + edge.Weight_blue;

                        if (!externalEdges.TryGetValue(key, out var existing) || totalWeight < existing.Item2)
                        {
                            externalEdges[key] = Tuple.Create(edge, totalWeight);
                        }
                    }
                }
            }

            return externalEdges;
        }
        public static UnionFind MergeComponents(
     Dictionary<PixelNode, List<Edge>> graph,
     UnionFind uf,
     Dictionary<int, double> internalMaxEdges,
     Dictionary<Tuple<int, int>, Tuple<Edge, double>> externalMinEdges,
     double baseThreshold = 10)
        {
            // Create a copy of the Union-Find structure
            UnionFind newUF = new UnionFind(uf.parent.Length);
            Array.Copy(uf.parent, newUF.parent, uf.parent.Length);
            Array.Copy(uf.rank, newUF.rank, uf.rank.Length);
            Array.Copy(uf.Size, newUF.Size, uf.Size.Length); // Copy sizes if available

            foreach (var pair in externalMinEdges)
            {
                int rootA = pair.Key.Item1;
                int rootB = pair.Key.Item2;
                double externalWeight = pair.Value.Item2;

                // Get component sizes
                int sizeA = newUF.GetComponentSize(rootA);
                int sizeB = newUF.GetComponentSize(rootB);

                // Get internal max edges
                double maxA = internalMaxEdges.TryGetValue(rootA, out double tempA) ? tempA : 0;
                double maxB = internalMaxEdges.TryGetValue(rootB, out double tempB) ? tempB : 0;

                // Size-dependent threshold adjustment
                double sizeFactorA = baseThreshold / sizeA;
                double sizeFactorB = baseThreshold / sizeB;
                double adaptiveThreshold = Math.Min(maxA + sizeFactorA, maxB + sizeFactorB);

                // Debug output
                Console.WriteLine($"Component A (Root:{rootA}, Size:{sizeA}, MaxInternal:{maxA:0.00})");
                Console.WriteLine($"Component B (Root:{rootB}, Size:{sizeB}, MaxInternal:{maxB:0.00})");
                Console.WriteLine($"External:{externalWeight:0.00} vs Threshold:{adaptiveThreshold:0.00}");

                // Merge condition
                if (externalWeight <= adaptiveThreshold)
                {
                    newUF.Union(rootA, rootB);
                    Console.WriteLine($"Merged! New size: {newUF.GetComponentSize(rootA)}");
                }
            }

            return newUF;
        }
        public static void DisplayComponents(RGBPixel[,] originalImage, PictureBox picBox, UnionFind uf)
        {
            int height = GetHeight(originalImage);
            int width = GetWidth(originalImage);

            Bitmap componentBMP = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Random rand = new Random();

            // Step 1: Assign random colors to each component root
            Dictionary<int, Color> componentColors = new Dictionary<int, Color>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelId = y * width + x;
                    int root = uf.Find(pixelId);

                    if (!componentColors.ContainsKey(root))
                    {
                        // Generate a bright random color
                        componentColors[root] = Color.FromArgb(
                            rand.Next(1, 256),  // Avoid dark colors
                            rand.Next(1, 256),
                            rand.Next(1, 256)
                        );
                    }
                }
            }

            // Step 2: Paint each pixel with its component's color
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelId = y * width + x;
                    int root = uf.Find(pixelId);
                    componentBMP.SetPixel(x, y, componentColors[root]);
                }
            }

            picBox.Image = componentBMP;
        }
    }
  
    // Union-Find (Disjoint Set Union) Data Structure
    public class UnionFind
    {
        public int[] parent;
        public  int[] rank;
        public int[] Size;
        public UnionFind(int count)
        {
            parent = new int[count];
            rank = new int[count];
            Size = new int[count];
            for (int i = 0; i < count; i++)
            {
                parent[i] = i; // Each node is its own parent initially
                Size[i] = 1;
            }
        }

        // Find with path compression
        public int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]); // Path compression
            return parent[x];
        }

        // Union by rank
        public void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);
           
            if (rootX == rootY)
                return; // Already connected

            // Attach smaller tree to larger tree
            if (rank[rootX] > rank[rootY])
            {
                parent[rootY] = rootX;
                Size[rootX] += Size[rootY];
            }
            else if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
                Size[rootY] += Size[rootX];
            }
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;

            }
        }
        public int GetComponentSize(int x)
        {
            return Size[Find(x)];
        }
    }

}
