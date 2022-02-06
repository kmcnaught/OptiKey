using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EyeMineLauncher
{
    class Program
    {

        private static string crashLogsPath = @"C:\CrashDumps\";
        private static string launcherLog;
        private static int crashLogsCount = 3;
        
        static void Main(string[] args)
        {
            //TODO: delete old launcher logs
            string filename = string.Format("launcher-{0:yyyy-MM-dd_hh-mm-ss}.txt", DateTime.Now);
            launcherLog = Path.Combine(crashLogsPath, filename);

            ResetConfigFiles();
            Console.Out.Flush();                 

            KeepMostRecent(crashLogsPath, crashLogsCount);            

            while (true)
            {
                // Synchronous - won't return until app closed
                ResetConfigFiles();           
                long exitCode = LaunchEyeMine();

                if (exitCode == 0)
                {
                    Log("App has been closed, will relaunch in 10 seconds");
                }
                Console.Write("About to launch");
                Console.Out.Flush();
                SleepWithFeedback(10);
            }
        }

        private static void Log(string msg)
        {
            Console.WriteLine(msg);
            Console.Out.Flush();
            
            using (StreamWriter sw = File.AppendText(launcherLog))
            {
                try
                {
                    sw.WriteLine(msg);
                }
                catch (Exception e)
                {
                    Log("Error logging to file\n");
                    Log(e.Message);                    
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

        private static long LaunchEyeMine()
        {
            Log($"{ DateTime.Now }: Launching EyeMine");
#if DEBUG
            string exeName = @"..\..\..\JuliusSweetland.OptiKey.EyeMine\bin\Debug\EyeMineExhibition.exe";            
#else
            FileInfo fileInfo = new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string exeName = Path.Combine(fileInfo.Directory.ToString(), "EyeMineExhibition.exe");            
#endif
            Log($"Launching {exeName}");
            Console.Out.Flush();

            try
            {
                // from https://stackoverflow.com/a/9730455
                StringBuilder outputBuilder;
                ProcessStartInfo processStartInfo;
                Process process;

                outputBuilder = new StringBuilder();

                processStartInfo = new ProcessStartInfo();
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardInput = true;
                processStartInfo.UseShellExecute = false;
                processStartInfo.FileName = exeName;

                process = new Process();
                process.StartInfo = processStartInfo;

                // enable raising events because Process does not raise events by default
                process.EnableRaisingEvents = true;
                // attach the event handler for OutputDataReceived before starting the process
                process.OutputDataReceived += new DataReceivedEventHandler
                (
                    delegate (object sender, DataReceivedEventArgs e)
                    {
                        Log(e.Data);
                        // append the new data to the data already read-in
                        outputBuilder.Append(e.Data);
                        outputBuilder.Append("\n");
                    }
                );

                // start the process
                // then begin asynchronously reading the output
                // then wait for the process to exit
                // then cancel asynchronously reading the output
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                process.CancelOutputRead();

                Log($"Exit code: {process.ExitCode}");
                if (process.ExitCode < 0)
                {
                    Log("App has exited unexpectedly, saving crash log now");
                    SaveCrashLog(outputBuilder);
                }                

                return process.ExitCode;
            }
            catch (Exception e) {
                Log("Error launching application\n");
                Log(e.Message);
                return -1;
            }            
        }

        private static void SaveCrashLog(StringBuilder stringBuilder)
        {
            try
            {
                KeepMostRecent(crashLogsPath, crashLogsCount);                
            }
            catch
            {
                Log("Unable to delete old crash logs");
            }
            string filename = string.Format("crash-{0:yyyy-MM-dd_hh-mm-ss}.txt", DateTime.Now);
            System.IO.File.WriteAllText(Path.Combine(crashLogsPath, filename), stringBuilder.ToString());

        }
        
        private static void KeepMostRecent(string directoryName, int numToKeep)
        {
            DirectoryInfo info = new DirectoryInfo(directoryName);
            var files = info.GetFiles().OrderBy(p => p.CreationTime);
            int numToDelete = files.Count() - numToKeep;

            var filenamesToDelete = files.Take(numToDelete).Select(f => f.FullName);
            foreach (var filename in filenamesToDelete)
            {
                File.Delete(filename);
            }
        }


        private static void ResetConfigFiles()
        {
            // If there are backup config files available, restore those - this helps us to 
            // recover from situations in which config files were corrupted for any reason
            string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Minecraft config - including fml.toml which is sometimes empty
            {
                string minecraftDir = Path.Combine(applicationDataPath, @".minecraft\EyeMineExhibition\");
                string configDir = Path.Combine(minecraftDir, "config");
                string configBackup = Path.Combine(minecraftDir, "config.zip");
                if (File.Exists(configBackup))
                {
                    try
                    {
                        bool recursive = true;
                        if (Directory.Exists(configDir))
                        {
                            Directory.Delete(configDir, recursive);
                        }
                        ZipFile.ExtractToDirectory(configBackup, configDir);
                        Log($"Resetting config folder: {configDir}");
                    }
                    catch (Exception e)
                    {
                        Log($"Exception resetting config folder: {configDir}");
                        Log("");
                        Log(e.ToString());
                    }
                }
            }

            {

                string optikeyDir = Path.Combine(applicationDataPath, @"SpecialEffect");
                string logsDir = Path.Combine(optikeyDir, @"EyeMineV2\Logs");
                string savedLogsDir = Path.Combine(applicationDataPath, @"SpecialEffectLogs");

                try
                {
                    if (!Directory.Exists(savedLogsDir))
                    {
                        Directory.CreateDirectory(savedLogsDir);
                    }

                    // Move logs out first - renaming by date created             
                    var dir = new DirectoryInfo(logsDir);                    
                    foreach (FileInfo file in dir.GetFiles())
                    {
                        string newName = String.Format("EyeMine-{0:yyyy-MM-dd--HH-mm-ss}.log", file.LastWriteTime);
                        File.Copy(file.FullName, Path.Combine(savedLogsDir, newName), true);
                    }

                    // Only keep a maximum number of log files
                    KeepMostRecent(savedLogsDir, 100);
                }
                catch (Exception e)
                {
                    Log("Error backing up EyeMine logs");
                    Log("");
                    Log(e.ToString());
                }

                // Optikey config - this can get corrupted. We need a backup so we can recover the minecraft command
                string optikeyBackup = Path.Combine(applicationDataPath, @"SpecialEffect.zip");
                if (File.Exists(optikeyBackup))
                {
                    try
                    {
                        bool recursive = true;
                        if (Directory.Exists(optikeyDir))
                        {
                            Directory.Delete(optikeyDir, recursive);
                        }
                        ZipFile.ExtractToDirectory(optikeyBackup, optikeyDir);
                        Log($"Resetting config folder: {optikeyDir}");                   
                    }
                    catch (Exception e)
                    {
                        Log($"Exception resetting config folder: {optikeyDir}");                        
                        Log("");
                        Log(e.ToString());
                    }
                }
            }
        }
    }
}
