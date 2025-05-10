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
using System.Windows.Forms.VisualStyles;
using System.Linq;
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

            return Filtered;
        }


        // start code graph
        public struct PixelNode
        {
            public int X;// Column position
            public int Y;// Row position
            public RGBPixel color;
            public int id;
            public PixelNode(int x, int y, RGBPixel co, int i)

            {
                color = co;
                X = x;
                Y = y;
                id = i;
            }


        }

        public struct Edge
        {
            public PixelNode From;
            public PixelNode To;
            public int Weight;

            public Edge(PixelNode from, PixelNode to,int w)
            {
                From = from;
                To = to;

                Weight = w;//Math.Abs(from.color.blue - to.color.blue);
            }
        }
        //build graph for given matrix and c is the color pixel 1 for red 2 for blue 3 for green
        public static (PixelNode[,], List<Edge> , List<Edge> ,List<Edge> , DisjointSet , DisjointSet, DisjointSet) BuildGraph(RGBPixel[,] image)
        {
            int height = GetHeight(image);
            int width = GetWidth(image);
            var nodeMap = new PixelNode[height, width];
            //var graph = new Dictionary<PixelNode, List<Edge>>();
            List<Edge> RAlledges = new List<Edge>();
            List<Edge> GAlledges = new List<Edge>();
            List<Edge> BAlledges = new List<Edge>();
            DisjointSet Rset = new DisjointSet(width * height);
            DisjointSet Gset = new DisjointSet(width * height);
            DisjointSet Bset = new DisjointSet(width * height);
            int id = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Rset.make_set(id);
                    Gset.make_set(id);
                    Bset.make_set(id);
                    PixelNode node = new PixelNode(x, y, image[y, x], id) ;
                    id++;
                    nodeMap[y, x] = node;
                   // graph[node] = new List<Edge>();           
                }
            }
            int[,] directions = {
                             {1, 0},
                               {1, 1},
                                    {0, 1},
                                    {-1, 1},
                                    {-1, 0},
                                    {-1, -1},
                                    {0, -1},
                                    {1, -1}
                                };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    PixelNode current = nodeMap[y, x];

                    for (int i = 0; i < directions.GetLength(0); i++)
                    {
                        int dx = directions[i, 1];
                        int dy = directions[i, 0];

                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                        {
                            PixelNode neighbor = nodeMap[ny, nx];

                            int wr = Math.Abs(current.color.red - neighbor.color.red);
                            int wg = Math.Abs(current.color.green - neighbor.color.green);
                            int wb = Math.Abs(current.color.blue - neighbor.color.blue);
                            Edge Redge = new Edge(current, neighbor, wr);
                            Edge Gedge = new Edge(current, neighbor, wg);
                            Edge Bedge = new Edge(current, neighbor, wb);
                            // Add edge
                          //  graph[current].Add(Redge);
                            RAlledges.Add(Redge);
                          //  Rset.edges[Rset.Find(Redge.From.id)].Add(Redge);
                           // Rset.edges[Redge.To.id].Add(Redge);
                            GAlledges.Add(Gedge);
                          //  Gset.edges[Gset.Find(Gedge.From.id)].Add(Gedge);
                           // Gset.edges[Gedge.To.id].Add(Gedge);
                            BAlledges.Add(Bedge);
                           // Bset.edges[Bset.Find(Bedge.From.id)].Add(Bedge);
                           // Bset.edges[Bedge.To.id].Add(Bedge);
                        }
                }
           } 
            }
                        return (nodeMap,RAlledges,GAlledges, BAlledges, Rset,  Gset, Bset);
        }

        public static DisjointSet  components(int K,List<Edge> Rdges,List<Edge>Gdges,List<Edge>Bdges,DisjointSet Rset, DisjointSet Gset, DisjointSet Bset)
        {
           Rdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            Gdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            Bdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            foreach (Edge e in Rdges )
            {
                  //  Rset.Add((e.From.id, e.To.id), e.Weight);
                if (Rset.Find(e.From.id) != Rset.Find(e.To.id))
                {
                    if (Rset.it[Bset.Find(e.From.id)] +(K/Rset.GetSize(e.From.id)) >=e.Weight)//Rset.FindValueOptimized((Rset.Find(e.From.id), Rset.Find(e.To.id))))//Rset.diff(e.From.id,e.To.id))
                    {
                        //Console.WriteLine("here " + K);
                        Rset.Union(e.From.id, e.To.id, e);
                        Rset.it[Bset.Find(e.From.id)] = e.Weight;
                    }
                       
                }
            }
            foreach (Edge e in Gdges)
            {
                  //  Gset.Add((e.From.id, e.To.id), e.Weight);
                if (Gset.Find(e.From.id) != Gset.Find(e.To.id)&&Rset.Find(e.From.id)==Rset.Find(e.To.id))
                {
                    if (Gset.it[Bset.Find(e.From.id)] + (K / Gset.GetSize(e.From.id)) >=e.Weight)//Gset.FindValueOptimized((Gset.Find(e.From.id),Gset.Find(e.To.id))))// Gset.diff(e.From.id, e.To.id))
                    {
                        Gset.Union(e.From.id, e.To.id,e);
                        Gset.it[Bset.Find(e.From.id)] = e.Weight;
                    }

                }
            }
            foreach (Edge e in Bdges)
            {
                    //Bset.Add((e.From.id, e.To.id), e.Weight);
                if (Bset.Find(e.From.id) != Bset.Find(e.To.id)&& Gset.Find(e.From.id) == Gset.Find(e.To.id) && Rset.Find(e.From.id) == Rset.Find(e.To.id))
                {
                    if (Bset.it[Bset.Find(e.From.id)] + (K / Bset.GetSize(e.From.id)) >=e.Weight)//Bset.FindValueOptimized((Bset.Find(e.From.id),Bset.Find(e.To.id)))) // Bset.diff(e.From.id, e.To.id))
                    {
                        Bset.Union(e.From.id, e.To.id,e);
                        Bset.it[Bset.Find(e.From.id)]= e.Weight;
                    }

                }
            }

            return Bset;
        }


        public static void WriteDisjointSetsToDesktop(PixelNode[,] nodeMap,List<Edge>edges, DisjointSet set, int width, int height)
        {
            string fileName = "segmentation_results.txt";
            // Get desktop path that works on Windows, Mac, and Linux
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, fileName);
             if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write component matrix
                writer.WriteLine($"Component IDs for {width}x{height} image:");


               

                // Calculate component statistics
                var components = new Dictionary<int, int>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int root = set.Find(nodeMap[y, x].id);
                        components[root] = components.TryGetValue(root, out var count) ? count + 1 : 1;
                    }
                }

                // Write statistics
                writer.WriteLine("\nComponent Statistics:");
                writer.WriteLine($"Total components: {components.Count}");
                writer.WriteLine("Top 10 largest components:");
                foreach (var kvp in components.OrderByDescending(x => x.Value))
                {
                    writer.WriteLine($"Component {kvp.Key}: {kvp.Value} pixels");
                }
            }

            Console.WriteLine($"Results saved to: {filePath}");
        }
        public static void DisplayDisjointSets(PixelNode[,] nodeMap, int width, int height, DisjointSet set, PictureBox PicBox)
        {
            Dictionary<int, RGBPixel> regionColors = new Dictionary<int, RGBPixel>();
            Random rand = new Random();
            RGBPixel[,] segmented = new RGBPixel[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int root = set.Find(nodeMap[y, x].id); // or Gset/Bset
                    if (!regionColors.ContainsKey(root))
                    {
                        regionColors[root] = new RGBPixel
                        {
                            red = (byte)rand.Next(256),
                            green = (byte)rand.Next(256),
                            blue = (byte)rand.Next(256)
                        };
                    }
                    segmented[y, x] = regionColors[root];
                }
            }
            DisplayImage(segmented, PicBox);

        }



    }

    public class DisjointSet
    {
        private int[] parent;
       // private int[] rank;
        public int [] it ;
        private int[] count;
        //public int[,] exdiff;
       //public Dictionary<int, List<Edge>> edges;
       // private List<((int, int) pair, int value)> _data;
        public DisjointSet(int size)
        {
            parent = new int[size];
           // rank = new int[size];
            count = new int[size];
            it = new int[size];
        //_data  = new List<((int, int), int)>();
         //   exdiff = new int[size + 1, size + 1];
        }

        // Add a pair with its value
        //public void Add((int, int) pair, int value)
        //{
        //    _data.Add((pair, value));
        //}
        //public int FindValueOptimized((int, int) searchPair)
        //{
        //    foreach (var item in _data)
        //    {
        //        if (item.pair.Item1 == searchPair.Item1 &&
        //            item.pair.Item2 == searchPair.Item2)
        //        {
        //            return item.value;
        //        }
        //    }
        //    return int.MaxValue;
        //}
        // Initially, each node is its own parent


        public void make_set(int v)
        {
            it[v]= 0;
            parent[v] = v;
         //   rank[v] = 0;
            count[v] = 1;
           // edges[v] = new List<Edge>();
        }
        // Find with path compression
        public int Find(int x)
        {
            if (parent[x] != x)
            {
                parent[x] = Find(parent[x]); // Path compression
            }
            return parent[x];
        }

        // Union by rank
       

        //private bool AreEdgesEqual(Edge a, Edge b)
        //{
        //    // Check if edges are equal (undirected)
        //    return (a.From.id == b.From.id && a.To.id == b.To.id) ||
        //           (a.From.id == b.To.id && a.To.id == b.From.id);
        //}
       
            public void Union(int a, int b,Edge e)
        {
                int rootA = Find(a);
                int rootB = Find(b);
            //exdiff[rootA, rootB] = e.Weight;

           // exdiff[rootB, rootA] = e.Weight;
                if (rootA == rootB) return;

                // Union by size: attach smaller to larger
                if (count[rootA] < count[rootB])
                {
                    parent[rootA] = rootB;
                    count[rootB] += count[rootA];
              //  edges[rootB].AddRange(edges[rootA]);
               //edges.Remove(rootA);
            }
            else
                {
                    parent[rootB] = rootA;
                    count[rootA] += count[rootB];
              // edges[rootA].AddRange(edges[rootB]);
               // edges.Remove(rootB);
            }
            }
        public int GetSize(int v)
        {
            return count[Find(v)];
        }
        // Optional: Print sets
       
    }
}

    

