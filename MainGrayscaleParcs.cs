using CommandLine;
using Parcs;
using Parcs.Module.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace GrayscaleParcs
{
    class MainGrayscaleParcs : MainModule
    {
        private static CLIOptions options;
        private static int imgHeight;
        private static int imgWidth;
        class CLIOptions : BaseModuleOptions
        {
            [Option("input", Required = true, HelpText = "File path to the input array.")]
            public string InputFile { get; set; }
            [Option("output", Required = true, HelpText = "File path to the sorted array.")]
            public string OutputFile { get; set; }
            [Option("p", Required = true, HelpText = "Number of points.")]
            public int PointsCount { get; set; }
        }

        private IChannel[] channels;
        private IPoint[] points;
        private int[][] matrix;

        static void Main(string[] args)
        {
            options = new CLIOptions();

            if (args != null)
            {
                if (!Parser.Default.ParseArguments(args, options))
                {
                    throw new ArgumentException($@"Cannot parse the arguments. Possible usages: {options.GetUsage()}");
                }
            }

            (new MainGrayscaleParcs()).RunModule(options);
        }

        public override void Run(ModuleInfo info, CancellationToken token = default)
        {
            int pointsNum = options.PointsCount;
            Stopwatch sw = new Stopwatch();
            //matrix = GetMatrix(options.InputFile);
            matrix = GetMatrixFromImage(options.InputFile);
            Console.WriteLine("Success reading data!");

            if (matrix.Length % pointsNum != 0)
            {
                throw new Exception($"Matrix size (now {matrix.Length}) should be divided by {pointsNum}!");
            }

            channels = new IChannel[pointsNum];
            points = new IPoint[pointsNum];

            for (int i = 0; i < pointsNum; ++i)
            {
                points[i] = info.CreatePoint();
                channels[i] = points[i].CreateChannel();
                points[i].ExecuteClass("GrayscaleParcs.ModuleGrayScale");
            }
            sw.Start();

            DistributeAllData();
            Console.WriteLine("Success distributing data!");

            Console.WriteLine("Success grayscaling data!");

            int[][] result = GatherAllData();

            //SaveMatrix(options.OutputFile, result);
            SaveMatrixToImage(options.OutputFile, result);
            sw.Stop();
            Console.WriteLine("Success saving data!");
            Console.WriteLine("Done");
            Console.WriteLine($"Total time {sw.ElapsedMilliseconds} ms ({sw.ElapsedTicks} ticks)");
            Console.ReadLine();
        }

        static int[][] GetMatrixFromImage(string filename)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            Bitmap img = new Bitmap(path);
            var result = new List<List<int>>();
            imgHeight = img.Height;
            imgWidth = img.Width;
            Console.WriteLine($"Img height x width: {imgHeight} x {imgWidth}");
            for (int i = 0; i < img.Height; i++)
                result.Add(new List<int>());

            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    Color pixel = img.GetPixel(i, j);
                    result[j].Add(pixel.R);
                    result[j].Add(pixel.G);
                    result[j].Add(pixel.B);
                }
            }
            Console.WriteLine($"Now list sizes are: {result.Count} x {result[0].Count}");
            return result.Select(l => l.ToArray()).ToArray();
        }

        static void SaveMatrixToImage(string filename, int[][] m)
        {
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), filename);
            Bitmap bmp = new Bitmap(imgWidth, imgHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Console.WriteLine($"Now Image sizes are: {m.Length} x {m[0].Length}");

            for (int i = 0; i < imgWidth; i++)
            {
                for (int j = 0; j < imgHeight; j++)
                {
                    var c = Color.FromArgb(m[j][i], m[j][i], m[j][i]);
                    bmp.SetPixel(i, j, c);
                }
            }
            bmp.Save(outputPath);

        }

        static int[][] GetMatrix(string filename)
        {
            return File.ReadAllLines(filename)
                   .Select(l => l.Split(' ')
                   .Where(k => k.Length > 0)
                   .Select(i => int.Parse(i.Replace("-1", int.MaxValue.ToString())))
                   .ToArray())
                   .ToArray();
        }

        static void SaveMatrix(string filename, int[][] m)
        {
            using (var file = File.CreateText(filename))
            {
                for (int i = 0; i < m.Length; i++)
                {
                    for (int j = 0; j < m.Length; j++)
                    {
                        file.Write(m[i][j]);
                        if (j != m.Length - 1)
                        {
                            file.Write(" ");
                        }
                    }
                    file.WriteLine();
                }
            }
        }


        private int[][] GatherAllData()
        {
            int chunkSize = matrix.Length / options.PointsCount;
            //int[][] result = new int[matrix.Length][];
            int[][] result = new int[imgHeight][];
            for (int i = 0; i < channels.Length; i++)
            {
                int[][] chunk = channels[i].ReadObject<int[][]>();
                Console.WriteLine($"Read chunk height x width: {chunk.Length}x {chunk[0].Length}");
                for (int j = 0; j < chunkSize; j++)
                {
                    result[i * chunkSize + j] = chunk[j];
                }
            }

            return result;
        }

        private void DistributeAllData()
        {
            for (int i = 0; i < channels.Length; i++)
            {
                Console.WriteLine($"Sent to channel: {i}");
                int height = matrix.Length;
                int width = matrix[0].Length;
                Console.WriteLine($"get length array of {height} x {width}");
                channels[i].WriteData(i);
                int chunkSize = matrix.Length / options.PointsCount;

                int[][] chunk = new int[chunkSize][];

                for (int j = 0; j < chunkSize; j++)
                {
                    chunk[j] = matrix[i * chunkSize + j];
                }
                //Console.WriteLine($"Sending array of {chunk.Length} x {chunk[0].Length}");
                channels[i].WriteObject(chunk);
            }
        }
    }
}
