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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
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
            public int From;
            public int To;
            public byte weight;
       
            public Edge(int  from, int to,byte weight)
            {
                From = from;
                To = to;

                this.weight = weight;
            }
        }
        //build graph for given matrix and c is the color pixel 1 for red 2 for blue 3 for green
        public static (DisjointSet,int[,], List<Edge>, List<Edge>, List<Edge>, DisjointSet , DisjointSet, DisjointSet) BuildGraph(RGBPixel[,] image)
        {
            int height = GetHeight(image);
            int width = GetWidth(image);
            int [,] nodeMap = new int[height, width];
            //var graph = new Dictionary<PixelNode, List<Edge>>();
            //  List<Edge> Alledges = new List<Edge>();
            List<Edge> RAlledges = new List<Edge>();
            List<Edge> GAlledges = new List<Edge>();
            List<Edge> BAlledges = new List<Edge>();
            // int[,] check = new int [height,width]; 
            DisjointSet Rset = new DisjointSet(width * height);
            DisjointSet Gset = new DisjointSet(width * height);
            DisjointSet Bset = new DisjointSet(width * height);
            DisjointSet regionSet = new DisjointSet(width * height);
            int id = 1;
           
            nodeMap[0, 0] = 0;
           
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Rset.make_set(nodeMap[y, x], y, x);
                    Gset.make_set(nodeMap[y,x], y, x);
                    Bset.make_set(nodeMap[y, x], y, x);
                    regionSet.make_set(nodeMap[y, x], y, x);
                    int[,] directions = {
                                    {0, 1},
                                    {1, 1},
                                    {1, 0},
                                    {1, -1},
                                  
                                };
                    PixelNode current = new PixelNode(x,y, image[y,x], nodeMap[y,x]);
                   // nodeMap[y, x] = current;
                    for (int i = 0; i < directions.GetLength(0); i++)
                    {
                        int dx = directions[i, 1];
                        int dy = directions[i, 0];

                        int nx = x + dx;
                        int ny = y + dy;
                        //int id_d = id;
                        if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                        {
                            PixelNode neighbor;
                            if (nodeMap[ny, nx] != 0)
                            {
                                neighbor = new PixelNode(nx, ny, image[ny, nx], nodeMap[ny, nx]);
                                

                            }
                            else
                            {
                                
                                  neighbor = new PixelNode(nx, ny, image[ny, nx], id);
                                    nodeMap[ny, nx] = neighbor.id;

                                    id++;
                                
                            }
                           byte wr = (byte)Math.Abs(current.color.red - neighbor.color.red);
                           byte wg = (byte)Math.Abs(current.color.green - neighbor.color.green);
                           byte wb = (byte)Math.Abs(current.color.blue - neighbor.color.blue);
                            Edge redge = new Edge(current.id, neighbor.id, wr);
                            Edge gedge = new Edge(current.id, neighbor.id,  wg);
                            Edge bedge = new Edge(current.id, neighbor.id,  wb);

                            RAlledges.Add(redge);
                            GAlledges.Add(gedge);
                            BAlledges.Add(bedge);



                        }
                }
           } 
            }
                        return (regionSet,nodeMap, RAlledges, GAlledges, BAlledges, Rset,  Gset, Bset);
        }

        public static DisjointSet  components(DisjointSet regionSet,int[,] nodeMap, int height, int width ,int K, List<Edge>  Rdges, List<Edge> Gdges, List<Edge> Bdges, DisjointSet Rset, DisjointSet Gset, DisjointSet Bset)
        {
         
           Rdges.Sort((a, b) => a.weight.CompareTo(b.weight));
           Gdges.Sort((a, b) => a.weight.CompareTo(b.weight));
           Bdges.Sort((a, b) => a.weight.CompareTo(b.weight));
            // Array.Sort(Rdges, (a, b) => a.Red_Weight.CompareTo(b.Red_Weight));
            foreach (Edge e in Rdges )
            { int rootF = Rset.Find(e.From);
                int rootT = Rset.Find(e.To);

                  //  Rset.Add((e.From.id, e.To.id), e.Weight);
                if (rootF != rootT)
                {
                    double itF = Rset.it[rootF] +
                        (double)((double)K / (double)Rset.GetSize(e.From));
                    double itT = Rset.it[rootT] +
                        (double)((double)K / (double)Rset.GetSize(e.To));
                    double Mint = (double)Math.Min(itT, itF);
                    
                    if (Mint >=e.weight)
                    {
                        //Console.WriteLine("here " + K);
                        Rset.Union(e.From, e.To);
                        Rset.it[Rset.Find(rootF)] = e.weight;
                     
                    }
                       
                }
            }

            //Array.Sort(Gdges, (a, b) => a.green_Weight.CompareTo(b.green_Weight));
            foreach (Edge e in Gdges)
            {
                //  Gset.Add((e.From.id, e.To.id), e.Weight);
                int rootF = Gset.Find(e.From);
                int rootT = Gset.Find(e.To);
                if (rootF != Gset.Find(e.To))
                {
                    double itF = Gset.it[rootF] + 
                        (double)((double)K / (double)Gset.GetSize(e.From));
                    double itT = Gset.it[rootT] + 
                        (double)((double)K / (double)Gset.GetSize(e.To));
                    double Mint = Math.Min(itT, itF);
                       // if(Rset.Find(e.From.id) != Rset.Find(e.To.id))redges.Add(e);
                    if (Mint >=e.weight)
                    {
                        Gset.Union(e.From, e.To);

                        Gset.it[Gset.Find(rootF)] =  e.weight;
                        //Math.Max(Gset.it[Gset.Find(e.From.id)] , e.Weight);
                    }

                }
            }
           // Array.Sort(Bdges, (a, b) => a.blue_Weight.CompareTo(b.blue_Weight));
            foreach (Edge e in Bdges)
            {
                int rootF = Bset.Find(e.From);
                int rootT = Bset.Find(e.To);

                if (rootF != rootT) //&& Rset.Find(e.From.id) == Rset.Find(e.To.id))
                {
                    double itF = Bset.it[rootF]
                        + (double)((double)K / (double)Bset.GetSize(e.From));
                    double itT = Bset.it[rootT] +
                        (double)((double)K / (double)Bset.GetSize(e.To));
                    double Mint = Math.Min(itT, itF);
                    if (Mint >= e.weight)
                    {
                        Bset.Union(e.From, e.To);
                        Bset.it[Bset.Find(rootF)] =  e.weight;

                    }

                }
            }
            foreach (Edge e in Rdges)
            {
              //int x=  e.From.X;
              //  int y = e.From.Y;
              //  int nx = e.To.X;
              //  int ny = e.To.Y;
                int idF = e.From;// nodeMap[y, x].id;
                int idT = e.To;// nodeMap[ny,nx].id;
              
                    if (Rset.Find(idF) == Rset.Find(idT)
                        && Gset.Find(idF) == Gset.Find(idT)
                        && Bset.Find(idF) == Bset.Find(idT))
                    {

                        regionSet.Union(idF, idT);
                    }
                
            }
            //Console.WriteLine("here "+c);
            return regionSet;
                 }


        public static void WriteDisjointSetsToDesktop(int[,] nodeMap,List<Edge> edges, DisjointSet set, int width, int height)
        {
            string fileName = "segmentation_results.txt";
            // Get desktop path that works on Windows, Mac, and Linux
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, fileName); 
         
          
                // Delete the file if it exists
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Create a new file and immediately close it to avoid locking issues
                using (FileStream fs = File.Create(filePath))
                {
                    // Optionally write initial content here, or just leave it empty
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
                        int root = set.Find(nodeMap[y, x]);
                        if (!components.ContainsKey(root)){
                            components.Add(root, set.GetSize(root)) ;
                        }
                            // components.TryGetValue(root, out var count) ? count + 1 : 1;
                    }
                }

                // Write statistics
            //    writer.WriteLine("\nComponent Statistics:");
                writer.WriteLine( components.Count);
              //  writer.WriteLine("Top  largest components:");
                foreach (var kvp in components.OrderByDescending(x => x.Value))
                {
                    writer.WriteLine(kvp.Value);
                }
            }

            Console.WriteLine($"Results saved to: {filePath}");
        }
        public static void DisplayDisjointSets(int[,] nodeMap, int width, int height, DisjointSet set, PictureBox PicBox)
        {
            Dictionary<int, RGBPixel> regionColors = new Dictionary<int, RGBPixel>();
            Random rand = new Random();
            RGBPixel[,] segmented = new RGBPixel[height, width];
            Bitmap bmp = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int root = set.Find(nodeMap[y, x]); // or Gset/Bset
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
                    RGBPixel px = segmented[y, x];
                    Color color = Color.FromArgb(px.red, px.green, px.blue);
                    bmp.SetPixel(x, y, color);
                }
            }
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "output.png");
            bmp.Save(path);
            DisplayImage(segmented, PicBox);

        }


     public static void DisplayMergedComponent(int[,] nodeMap, int width, int height,int component, DisjointSet set,RGBPixel[,] image, PictureBox PicBox)
        {
            RGBPixel[,] segmented = new RGBPixel[height, width];
            Bitmap bmp = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int root = set.Find(nodeMap[y, x]); // or Gset/Bset
                    if (root == component)
                    {
                        segmented[y, x] = image[y, x];
                    }
                    else
                    {
                        segmented[y, x].blue = 255;
                        segmented[y, x].red = 255;
                        segmented[y, x].green = 255;
                    }
                    RGBPixel px =segmented[y, x];
                    Color color = Color.FromArgb(px.red, px.green, px.blue);
                    bmp.SetPixel(x, y, color);
                  
                }
            }
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "output.png");
            bmp.Save(path);

            DisplayImage(segmented, PicBox);
        }


    }
    public class DisjointSet
    {
        private int[] parent;
       // private int[] rank;
        public double [] it ;
        private int[] count;
        //public int[,] exdiff;
       public Dictionary<int, List<(int ,int) >> P_id;
       // private List<((int, int) pair, int value)> _data;
        public DisjointSet(int size)
        {
            parent = new int[size];
           // rank = new int[size];
            count = new int[size];
            it = new double[size];
            //_data  = new List<((int, int), int)>();
            //   exdiff = new int[size + 1, size + 1];
           // P_id = new Dictionary<int, List<(int,int)>>();
        }


        public void make_set(int v,int y,int x)
        {
            it[v]= 0;
            parent[v] = v;
         //   rank[v] = 0;
            count[v] = 1;
           //if (!P_id.ContainsKey(v))
           // {
           //     P_id[v]= new List<(int ,int)>();
                
           // }
           // P_id[v].Add((y,x));
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
       
            public void Union(int a, int b)
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
             //   P_id[rootB].AddRange(P_id[rootA]);
                }
               else
            {
                parent[rootB] = rootA;
                count[rootA]+= count[rootB];
               // P_id[rootA].AddRange(P_id[rootB]);
            }
          
            }
        public int GetSize(int v)
        {
            return count[Find(v)];
        }
        // Optional: Print sets
       
    }
}

    

