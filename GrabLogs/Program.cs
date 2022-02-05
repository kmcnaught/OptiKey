using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabLogs
{
    class Program
    {
        static void Main(string[] args)
        {
            // Output ZIP file to desktop, individual files in Documents
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Create output directory
            string outputName = String.Format("EyeMine Logs {0:yyyy-MM-dd}", DateTime.Now);
            string outputDir = Path.Combine(documents, outputName);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // This is expected to be run on the admin account, so we force the username back to EyeMine
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);            
            string userName = "EyeMine";
            appData = appData.Replace(Environment.UserName, userName);

            string minecraftDir = Path.Combine(appData, @".minecraft");
            string minecraftSubdir = Path.Combine(minecraftDir, @"EyeMineExhibition");

            List<string> directories = new List<string>();
            List<string> files = new List<string>();

            string minecraftCrashReportsDir = Path.Combine(minecraftSubdir, "crash-reports");
            directories.Add(minecraftCrashReportsDir);
            string minecraftLogsDir = Path.Combine(minecraftSubdir, "logs");
            directories.Add(minecraftLogsDir);

            string launcherLogDir = Path.Combine(minecraftDir, "launcher_log.txt");
            files.Add(launcherLogDir);

            // These are from our launcher
            string crashLogsPath = @"C:\CrashDumps\";
            directories.Add(crashLogsPath);

            // These have been backed up since full appdata dir gets reset regularly
            string optikeyLogsDir = Path.Combine(appData, @"SpecialEffectLogs");
            directories.Add(optikeyLogsDir);

            foreach (string dirName in directories)
            {
                try
                {
                    string finalDirName = new FileInfo(dirName).Name;
                    DirectoryCopy(dirName, Path.Combine(outputDir, finalDirName), true, true);
                    Console.WriteLine("Copying directory " + dirName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error copying directory " + dirName);
                }
            }

            foreach(string fileName in files)
            {
                try
                {
                    File.Copy(fileName, Path.Combine(outputDir, fileName), true);
                    Console.WriteLine("Copying file " + fileName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error copying file " + fileName);
                }
            }

            string zipName = Path.Combine(desktop, outputName + ".zip");
            if (File.Exists(zipName)) 
            {
                try
                {
                    File.Delete(zipName);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error deleting zip file " + zipName);
                }
            }
            try
            {
                ZipFile.CreateFromDirectory(outputDir, zipName);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error creating zip file " + zipName);
            }


            Console.WriteLine();
            Console.WriteLine("Finished, press any key to exit");
            Console.Read();
        }

        //FIXME: dupe code
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool allowOverwrite = false)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, allowOverwrite);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs, allowOverwrite);
                }
            }
        }
    }
}
