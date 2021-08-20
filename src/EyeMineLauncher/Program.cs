using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EyeMineLauncher
{
    class Program
    {
        private static int pollTimeSeconds = 1;
        private static Process process = null;

        static void Main(string[] args)
        {
            LaunchEyeMine();

            // Keep polling to ensure EyeMine running
            for (; ; )
            {
                Thread.Sleep(pollTimeSeconds * 1000);

                if (process.HasExited)
                {
                    LaunchEyeMine();
                }
            }
        }

        private static void LaunchEyeMine()
        {
            Console.WriteLine($"{ DateTime.Now }: Launching EyeMine");
#if DEBUG
            string filename = @"..\..\..\JuliusSweetland.OptiKey.EyeMine\bin\Debug\EyeMineExhibition.exe";
#else
            string filename = "EyeMineExhibition.exe";
#endif
            process = Process.Start(filename);
        }

    }
}
