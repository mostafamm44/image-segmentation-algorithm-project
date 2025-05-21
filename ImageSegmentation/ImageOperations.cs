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

using System.Threading.Tasks;
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
        public static (DisjointSet,int[,], List<Edge>, List<Edge>, List<Edge>, DisjointSet , DisjointSet, DisjointSet) BuildGraph(RGBPixel[,] image) // Exact O(n^2)
        {
            int height = GetHeight(image); //O(1)
            int width = GetWidth(image); //O(1)
            int [,] nodeMap = new int[height, width]; //O(N^2)

            List<Edge> RAlledges = new List<Edge>();//O(1)
            List<Edge> GAlledges = new List<Edge>();//O(1)
            List<Edge> BAlledges = new List<Edge>();//O(1)
            DisjointSet Rset = new DisjointSet(width * height);//O(N^2)
            DisjointSet Gset = new DisjointSet(width * height);//O(N^2)
            DisjointSet Bset = new DisjointSet(width * height);//O(N^2)
            DisjointSet regionSet = new DisjointSet(width * height);//O(N^2)
            int id = 1;//O(1)

            nodeMap[0, 0] = 0;//O(1)

            for (int y = 0; y < height; y++) //O(N^2)
            {
                for (int x = 0; x < width; x++)//O(N)
                {
                    Rset.make_set(nodeMap[y, x]);//O(1)
                    Gset.make_set(nodeMap[y, x]);//O(1)
                    Bset.make_set(nodeMap[y, x]);//O(1)
                    regionSet.make_set(nodeMap[y, x]);//O(1)
                    int[,] directions = { //O(1)
                                    {0, 1},
                                    {1, 1},
                                    {1, 0},
                                    {1, -1},
                                  
                                };
                    PixelNode current = new PixelNode(x,y, image[y,x], nodeMap[y,x]);//O(1)

                    for (int i = 0; i < directions.GetLength(0); i++) //O(1)
                    {
                        int dx = directions[i, 1];//O(1)
                        int dy = directions[i, 0];//O(1)

                        int nx = x + dx;//O(1)
                        int ny = y + dy;//O(1)

                        if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                        {
                            PixelNode neighbor;//O(1)
                            if (nodeMap[ny, nx] != 0)//O(1)
                            {
                                neighbor = new PixelNode(nx, ny, image[ny, nx], nodeMap[ny, nx]);//O(1)

                            }
                            else
                            {
                                
                                  neighbor = new PixelNode(nx, ny, image[ny, nx], id);//O(1)
                                  nodeMap[ny, nx] = neighbor.id;//O(1)
                                  id++;//O(1)
                            }
                           byte wr = (byte)Math.Abs(current.color.red - neighbor.color.red);//O(1)
                            byte wg = (byte)Math.Abs(current.color.green - neighbor.color.green);//O(1)
                            byte wb = (byte)Math.Abs(current.color.blue - neighbor.color.blue);//O(1)
                            Edge redge = new Edge(current.id, neighbor.id, wr);//O(1)
                            Edge gedge = new Edge(current.id, neighbor.id,  wg);//O(1)
                            Edge bedge = new Edge(current.id, neighbor.id,  wb);//O(1)

                            RAlledges.Add(redge);//O(1)
                            GAlledges.Add(gedge);//O(1)
                            BAlledges.Add(bedge);//O(1)



                        }
                }
           } 
            }
                        return (regionSet,nodeMap, RAlledges, GAlledges, BAlledges, Rset,  Gset, Bset); //O(1)
        }

        public static DisjointSet components(DisjointSet regionSet, int[,] nodeMap, int height, int width, int K, List<Edge> Rdges, List<Edge> Gdges, List<Edge> Bdges, DisjointSet Rset, DisjointSet Gset, DisjointSet Bset) //O(mlog m)
        {

            Rdges.Sort((a, b) => a.weight.CompareTo(b.weight)); //O(m log m)
            Gdges.Sort((a, b) => a.weight.CompareTo(b.weight));//O(m log m)
            Bdges.Sort((a, b) => a.weight.CompareTo(b.weight));//O(m log m)
            Parallel.Invoke(
            // Array.Sort(Rdges, (a, b) => a.Red_Weight.CompareTo(b.Red_Weight));
            () =>
            {
                foreach (Edge e in Rdges) //Best O(m)  Worst O(mlog m)
                {
                    int rootF = Rset.Find(e.From); // Best Case O(1) , Worst Case //O(log m)
                    int rootT = Rset.Find(e.To); // Best Case O(1) , Worst Case //O(log m)

                    if (rootF != rootT) //O(1)
                    {
                        double itF = Rset.it[rootF] + (K / Rset.GetSize(e.From)); // Best Case O(1) , Worst Case //O(log m)
                        double itT = Rset.it[rootT] +(K / Rset.GetSize(e.To));// Best Case O(1) , Worst Case //O(log m)
                        double Mint = Math.Min(itT, itF);  //O(1)

                        if (Mint >= e.weight) //O(1)
                        {
                            //Console.WriteLine("here " + K);
                            Rset.Union(e.From, e.To); // Best Case O(1) , Worst Case //O(log m)
                            Rset.it[Rset.Find(rootF)] = e.weight; // Best Case O(1) , Worst Case //O(log m)

                        }

                    }
                }
            },

            //Array.Sort(Gdges, (a, b) => a.green_Weight.CompareTo(b.green_Weight));
            () =>
            {
                foreach (Edge e in Gdges) //Best O(m)  Worst O(mlog m)
                {
                    //  Gset.Add((e.From.id, e.To.id), e.Weight);
                    int rootF = Gset.Find(e.From);
                    int rootT = Gset.Find(e.To);
                    if (rootF != Gset.Find(e.To))
                    {
                        double itF = Gset.it[rootF] + (K / Gset.GetSize(e.From));
                        double itT = Gset.it[rootT] + (K / Gset.GetSize(e.To));
                        double Mint = Math.Min(itT, itF);
                        
                        if (Mint >= e.weight)
                        {
                            Gset.Union(e.From, e.To);

                            Gset.it[Gset.Find(rootF)] = e.weight;
                        }

                    }
                }
            },


            // Array.Sort(Bdges, (a, b) => a.blue_Weight.CompareTo(b.blue_Weight));
             () =>
             {
                 foreach (Edge e in Bdges) //Best O(m)  Worst O(mlog m)
                 {
                     int rootF = Bset.Find(e.From);
                     int rootT = Bset.Find(e.To);

                     if (rootF != rootT) //&& Rset.Find(e.From.id) == Rset.Find(e.To.id))
                     {
                         double itF = Bset.it[rootF] + (K / Bset.GetSize(e.From));
                         double itT = Bset.it[rootT] + (K / Bset.GetSize(e.To));
                         double Mint = Math.Min(itT, itF);
                         if (Mint >= e.weight)
                         {
                             Bset.Union(e.From, e.To);
                             Bset.it[Bset.Find(rootF)] = e.weight;

                         }

                     }
                 }
             }
            );
            
            foreach (Edge e in Rdges) //Best O(m)  Worst O(mlog m)
            {
                int idF = e.From;//O(1)
                int idT = e.To; //O(1)

                if (Rset.Find(idF) == Rset.Find(idT) && Gset.Find(idF) == Gset.Find(idT) && Bset.Find(idF) == Bset.Find(idT))//Best O(1)  Worst O(log m)
                {

                        regionSet.Union(idF, idT); //Best O(1)  Worst O(log m)
                }
                
            }
            //Console.WriteLine("here "+c);
            return regionSet; //O(1)
        }


        public static void WriteDisjointSetsToDesktopWithPreCalculatedData(List<KeyValuePair<int, int>> sortedComponents)

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

                writer.WriteLine(sortedComponents.Count);

                foreach (var kvp in sortedComponents)
                {
                    writer.WriteLine(kvp.Value);
                }
            }

            Console.WriteLine($"Results saved to: {filePath}");
        }
        public static void DisplayDisjointSetsWithPreCalculatedColors(int[,] nodeMap, int width, int height, DisjointSet set, Dictionary<int, RGBPixel> regionColors, PictureBox PicBox)
        {

            RGBPixel[,] segmented = new RGBPixel[height, width];
            Bitmap bmp = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int root = set.Find(nodeMap[y, x]);
                    segmented[y, x] = regionColors[root];
                    RGBPixel px = segmented[y, x];
                    Color color = Color.FromArgb(px.red, px.green, px.blue);
                    bmp.SetPixel(x, y, color);
                }
            }
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "output.bmp");
            bmp.Save(path);
            DisplayImage(segmented, PicBox);



        }
        public static Dictionary<int, RGBPixel>  Coloring(int[,] nodeMap, int width, int height, DisjointSet set)  //avg O(m) , worst O(m^2) 
        {

            Dictionary<int, RGBPixel>  regionColors = new Dictionary<int, RGBPixel>();
            Random rand = new Random(); // O(1)
            for (int y = 0; y < height; y++) //worst O(m^2) average O(m)
            {
                for (int x = 0; x < width; x++) // n*m
                {
                    int root = set.Find(nodeMap[y, x]); //best O(1) Worst O(log(m))
                    if (!regionColors.ContainsKey(root)) //worst O(m) avg O(1)
                    {
                        regionColors[root] = new RGBPixel // Best O(1) worst O(m)
                        {
                            red = (byte)rand.Next(256), // O(1)
                            green = (byte)rand.Next(256), // O(1)
                            blue = (byte)rand.Next(256) // O(1)
                        };
                    }
                }
            }
            return  regionColors; //O(1)
        }
        public static List<KeyValuePair<int,int>> sortComponents(int[,] nodeMap, int width, int height, DisjointSet set)
        {
            var components = new Dictionary<int, int>(); //O(1)
            for (int y = 0; y < height; y++) //avg O(m) , worst O(m^2)
            {
                for (int x = 0; x < width; x++)
                {
                    int root = set.Find(nodeMap[y, x]);// best O(1), worst O(log m)
                    if (!components.ContainsKey(root)) // avg O(1), worst O(m)
                    {
                        components.Add(root, set.GetSize(root)); //best O(1), worst O(log m)
                    }
                }
            }


            var sortedComponents = components.OrderByDescending(x => x.Value).ToList(); //O(mlog(m))
            return sortedComponents; //O(1)

        }


        public static void DisplayMergedComponent(int[,] nodeMap, int width, int height,int component, DisjointSet set,RGBPixel[,] image, PictureBox PicBox) //best O(n^2) worst //O(n^2 * log(m))
        {
            RGBPixel[,] segmented = new RGBPixel[height, width]; //O(n^2)
            Bitmap bmp = new Bitmap(width, height); //O(n^2)
            for (int y = 0; y < height; y++) //best O(n^2) worst //O(n^2 * log(m))
            {
                for (int x = 0; x < width; x++)
                {
                    int root = set.Find(nodeMap[y, x]); // best O(1) , worst O(log m)
                    if (root == component) //O(1)
                    {
                        segmented[y, x] = image[y, x]; //O(1)
                    }
                    else
                    {
                        segmented[y, x].blue = 255; //O(1)
                        segmented[y, x].red = 255; //O(1)
                        segmented[y, x].green = 255; //O(1)
                    }
                    RGBPixel px =segmented[y, x]; //O(1)
                    Color color = Color.FromArgb(px.red, px.green, px.blue); //O(1)
                    bmp.SetPixel(x, y, color); //O(1)

                }
            }
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "output.bmp"); //O(1)
            bmp.Save(path); //O(n^2)

            DisplayImage(segmented, PicBox); //O(n^2)
        }


    }

    public class DisjointSet
    {
        private int[] parent;
        public double [] it ;
        private int[] count;
       
        public DisjointSet(int size)
        {
            parent = new int[size];
            count = new int[size];
            it = new double[size];
        }


        public void make_set(int v)
        {
            it[v]= 0;
            parent[v] = v;
            count[v] = 1;
        }
        public int Find(int x) // Best Case O(1) , Worst Case //O(log m)
        {
            if (parent[x] != x) //O(log m)
            {
                parent[x] = Find(parent[x]); // Path compression
            }
            return parent[x]; //O(1)
        }
       

       
        public void Union(int a, int b)
        {
                int rootA = Find(a);// Best Case O(1) , Worst Case //O(log m)
                int rootB = Find(b);// Best Case O(1) , Worst Case //O(log m)

                if (rootA == rootB) return; //O(1)

                // Union by size: attach smaller to larger
                if (count[rootA] < count[rootB]) //O(1)
                {
                    parent[rootA] = rootB; //O(1)
                    count[rootB] += count[rootA]; //O(1)
                }
                else
                {
                    parent[rootB] = rootA;
                    count[rootA]+= count[rootB];
                }
          
        }
        public int GetSize(int v)
        {
            return count[Find(v)]; // Best Case O(1) , Worst Case //O(log m)
        }
        // Optional: Print sets
       
    }
}

    

