using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EyeMineLauncher
{
    class Program
    {

        private static string allLogsPath = @"C:\EyeMineLogs\";
        private static string launcherLog;
        private static int numLogsToKeep = 1000;
        
        private static void EnsureExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static void Main(string[] args)
        {
            EnsureExists(allLogsPath);

            // TODO: delete old launcher logs? this is currently done later with KeepMostRecentInSubDirectories
            string launcherLogDir = Path.Combine(allLogsPath, "Launcher");
            EnsureExists(launcherLogDir);
            launcherLog = Path.Combine(launcherLogDir, string.Format("launcher-{0:yyyy-MM-dd_hh-mm-ss}.txt", DateTime.Now));

            // Log tobii binaries info
            Log("Logging Tobii binary information");
            LogExes();
            Console.Out.Flush();

            // Launch AHK to turn Ghost on
            Log("Launching Ghost via AHK");
            LaunchAHK();
            Console.Out.Flush();

            ResetConfigFiles();
            Console.Out.Flush();

            // TODO: make sure keeping some older ones including when it was working?
            // FIXME: this 'keep most recent' is generally also called when moving log files, we might not
            // need to do it here?
            KeepMostRecentInSubDirectories(allLogsPath, numLogsToKeep);            

            while (true)
            {
                // Back logs from last time - avoid local overwriting strategies               
                string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string optikeyDir = Path.Combine(applicationDataPath, @"SpecialEffect");
                string optikeyLogsDir = Path.Combine(optikeyDir, @"EyeMineV2\Logs");
                string minecraftDir = Path.Combine(applicationDataPath, @".minecraft");
                string minecraftLogsDir = Path.Combine(minecraftDir, @"EyeMineExhibition\logs");

                BackupLogs(optikeyLogsDir, allLogsPath, "Optikey");
                BackupLogs(minecraftLogsDir, allLogsPath, "Minecraft");


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
                if (process.ExitCode != 0)
                {
                    Log("App has exited unexpectedly, saving crash log now");
                    SaveCrashLog(outputBuilder);
                }                

                return process.ExitCode;
            }
            catch (Exception e) {
                Log("Exception launching application\n");
                Log(e.Message);
                return -1;
            }            
        }

        private static void SaveCrashLog(StringBuilder stringBuilder)
        {  
            string logsDir = Path.Combine(allLogsPath, "Crashes");

            try
            {
                KeepMostRecent(logsDir, numLogsToKeep); //probably redundant as we also call KeepMostRecentInSubDirectories
            }
            catch
            {
                Log("Unable to delete old crash logs");
            }

            string filename = string.Format("crash-{0:yyyy-MM-dd_hh-mm-ss}.txt", DateTime.Now);
            System.IO.File.WriteAllText(Path.Combine(logsDir, filename), stringBuilder.ToString());

        }
        
        private static void KeepMostRecentInSubDirectories(string directoryName, int numToKeep)
        {
            foreach (string subDirectory in Directory.GetDirectories(directoryName))
            {
                KeepMostRecent(subDirectory, numToKeep);
            }
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

        private static void ReplaceDirectoryFromZip(string zipName, string destDir)
        {
            
            // Generate temporary folder name so we can unzip into TMP and roll back if necessary
            string tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Try unzipping
            // (may throw)
            ZipFile.ExtractToDirectory(zipName, tmpDir);

            // If that was successful, we can continue with deleting original folder             
            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, recursive: true);
            }

            // If that was successful, replace with extracted directory
            Directory.Move(tmpDir, destDir);
        }

        // // Back up the contents a log dir, 
        // into a separate directory where everything gets time-stamped. 
        private static void BackupLogs(string origLogDir, string newLogDir, string name)
        {
            newLogDir = Path.Combine(newLogDir, name);
            EnsureExists(newLogDir);

            if (Directory.Exists(origLogDir))
            {
                try
                {
                    // Move logs out first - renaming by date created             
                    var dir = new DirectoryInfo(origLogDir);
                    foreach (FileInfo file in dir.GetFiles())
                    {
                        string newName = String.Format("{0}-{1:yyyy-MM-dd--HH-mm-ss}.log", name, file.LastWriteTime);
                        File.Copy(file.FullName, Path.Combine(newLogDir, newName), true); //fixme
                    }

                    // Only keep a maximum number of log files
                    KeepMostRecent(newLogDir, 1000);

                    Log($"backed up logs from {origLogDir}");
                    //TODO: consider exponential decay to keep hold of old files
                }
                catch (Exception e)
                {
                    Log($"Error backing up logs from {origLogDir}");
                    Log("");
                    Log(e.ToString());
                }
            }
            else
            {
                Log($"Error backing up logs from {origLogDir}");
                Log("directory doesn't exist");
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
                        ReplaceDirectoryFromZip(configBackup, configDir);
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
                // Optikey config - this can get corrupted. We need a backup so we can recover the minecraft command
                string optikeyDir = Path.Combine(applicationDataPath, @"SpecialEffect");
                string optikeyBackup = Path.Combine(applicationDataPath, @"SpecialEffect.zip");
                if (File.Exists(optikeyBackup))
                {
                    try
                    {
                        ReplaceDirectoryFromZip(optikeyBackup, optikeyDir);
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

        private static void LogExes()
        {
            // Log info for tobii binaries
            var appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var paths = new[] {
                Path.Combine(appDataLocal,"Tobii"),
                Path.Combine(appDataLocal,"TobiiGhost"),
                @"C:\Program Files (x86)\Tobii",
                @"C:\Program Files\Tobii"
            };
            foreach (var path in paths)
            {
                Log(path);
                WalkFileSystem(path, 0);
            }
        }

        private static void WalkFileSystem(string path, int level)
        {
            if (!Directory.Exists(path))
            {
                Log("Directory does not exist: " + path);
                return;
            }

            string indent = new string(' ', level * 2);

            bool hasSomeAncestorExes = Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories).Length > 0;

            if (hasSomeAncestorExes)
            {
                Log($"{indent}{new DirectoryInfo(path).Name}");

                foreach (var directory in Directory.GetDirectories(path))
                {
                    WalkFileSystem(directory, level + 1);
                }

                foreach (var file in Directory.GetFiles(path, "*.exe"))
                {
                    string hash = GetFileHash(file);
                    DateTime lastWriteTime = File.GetLastWriteTime(file);
                    Log($"{indent}  {Path.GetFileName(file)} - {hash} - Last edited: {lastWriteTime}");
                }
            }
        }

        private static string GetFileHash(string filePath)
        {
            using (var hashAlgorithm = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = hashAlgorithm.ComputeHash(stream);
                return BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty);
            }
        }

        private static void LaunchAHK()
        {
            // Ideally we'd have a relative path here. 
            // It works fine when running manually, but in kiosk mode
            // it seems the launcher EXE is run from a different location
            // so i'm just going to hardcode this path here.
            string exeName = @"C:\Program Files (x86)\SpecialEffect\EyeMineExhibit\Ghost_Toggle.ahk";

            Log($"Launching {exeName}");
            Console.Out.Flush();

            try
            {
                // from https://stackoverflow.com/a/9730455
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exeName;

                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                Log("AHK exited with exit code: " + process.ExitCode);
            }
            catch (Exception e)
            {
                Log("Error launching AHK\n");
                Log(e.Message);
            }
        }
    }
}

