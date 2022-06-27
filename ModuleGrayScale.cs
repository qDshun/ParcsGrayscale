using Parcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GrayscaleParcs
{
    class ModuleGrayScale : IModule
    {
        private int number;
        private int[][] chunk;

        public void Run(ModuleInfo info, CancellationToken token = default)
        {
            number = info.Parent.ReadInt();
            Console.WriteLine($"Current number {number}");
            chunk = info.Parent.ReadObject<int[][]>();
            int n = chunk[0].Length; //width
            int c = chunk.Length; //height
            //
            const int bytesPerPixel = 4;
            var result = new List<List<int>>();

            for (int i=0; i<n; i++)
                result.Add(new List<int>());

            Console.WriteLine($"Iterating over {c}x ({n / bytesPerPixel} * {bytesPerPixel}) ");
            for (int i=0; i<c; i++)
            {
                for (int j=0; j< chunk[i].Length / 3; j++)
                {
                    int grayScale = (int)(
                        (chunk[i][j * 3 + 0] * 0.30) +
                        (chunk[i][j * 3 + 1] * 0.59) +
                        (chunk[i][j * 3 + 2] * 0.11));
                    result[i].Add(grayScale);
                }
                Console.WriteLine($"Done with line {i}");
            }
            var res = result.Select(l => l.ToArray()).ToArray();
            info.Parent.WriteObject(res);
            Console.WriteLine("Done!");
            //

        }
    }
}
