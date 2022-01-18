using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            bool success = LaunchEyeMine();

            // Keep polling to ensure EyeMine running
            for (; ; )
            {
                Thread.Sleep(pollTimeSeconds * 1000);

                if (process == null || process.HasExited)
                {
                    Console.WriteLine("App has been closed, will relaunch in 10 seconds");
                    SleepWithFeedback(10);

                    success = LaunchEyeMine();
                    if (!success)
                    {
                        Console.WriteLine("Error launching process, will try again in 30 seconds");
                        SleepWithFeedback(30);                        
                    }
                }
            }
        }

        private static void SleepWithFeedback(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }
            Console.Write("\n");
        }

        private static bool LaunchEyeMine()
        {
            Console.WriteLine($"{ DateTime.Now }: Launching EyeMine");
#if DEBUG
            string exeName = @"..\..\..\JuliusSweetland.OptiKey.EyeMine\bin\Debug\EyeMineExhibition.exe";
#else
            FileInfo fileInfo = new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string exeName = Path.Combine(fileInfo.Directory.ToString(), "EyeMineExhibition.exe");            
#endif
            Console.WriteLine($"Launching {exeName}");
            try
            {
                process = Process.Start(exeName);
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("Error launching application\n");
                Console.WriteLine(e.Message);
                return false;
            }
        }

    }
}
