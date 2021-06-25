﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using EyeXFramework;
using Microsoft.Win32;
using Tobii.EyeX.Client;

namespace JuliusSweetland.OptiKey.InstallerActionsEyeMine
{
    public class InstallerUtils
    {
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool allowOverwrite = false)
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

        public static string GetPrettyDate(DateTime d)
        {
            DateTime now = DateTime.Now;
            if (d > now)
            { // future date
                return null;
            }

            TimeSpan s = now.Subtract(d);

            int dayDiff = (int)s.TotalDays;
            int secDiff = (int)s.TotalSeconds;
            int weekDiff = (int) dayDiff / 7;
            int monthDiff = (int) dayDiff / 30; // APPROXIMATE ONLY
            int yearDiff = (int) monthDiff / 12;

            if (dayDiff == 0)
            {
                return "today";
            }
            if (dayDiff == 1)
            {
                return "yesterday";
            }
            if (dayDiff < 7)
            {
                return $"{dayDiff} days ago";
            }
            if (monthDiff < 1)
            {
                if (weekDiff == 1)
                    return $"{weekDiff} week ago";
                else
                    return $"{weekDiff} weeks ago";
            }
            if (yearDiff < 1)
            {
                if (monthDiff == 1)
                    return $"{monthDiff} month ago";
                else
                    return $"{monthDiff} months ago";
            }
            
            return "more than a year ago";
        }

        public static string AppendItemToListData(string combodata, string item)
        {
            //# The AI_LISTBOX_DATA must be set like this: CHECKLIST_1_PROP|Value 1|Value 2| Value 3|...
            const string sep1 = "|";
            combodata += sep1 + item;
            return combodata;
        }

        public static string SanitisePropName(string prop_name)
        {
            prop_name = prop_name.Replace(" ", "");
            prop_name = prop_name.Replace(")", "");
            prop_name = prop_name.Replace("(", "");
            return prop_name;
        }

        public static bool IsTobiiSupported()
        {
            switch (EyeXHost.EyeXAvailability)
            {
                case EyeXAvailability.NotAvailable:
                    return false;
                default:
                    return true;
            }
        }

        public static bool HasProperty(dynamic obj, string name)
        {
            if (obj == null) return false;
            if (obj is IDictionary<string, object> dict)
            {
                return dict.ContainsKey(name);
            }
            try
            {
                var dict2 = obj.ToObject < Dictionary<string, dynamic>>();
                return dict2.ContainsKey(name);
            }
            catch (Exception e) { }
            
            return obj.GetType().GetProperty(name) != null;
        }

        public static string GetBackupName(string filename)
        {
            // The following will produce 2011-10-24-13-10
            string datetime = DateTime.Now.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture);

            string path = Path.GetDirectoryName(filename);
            string file = Path.GetFileNameWithoutExtension(filename);
            string extension = Path.GetExtension(filename);

            string newFile = String.Format("{0}_{1}{2}", file, datetime, extension);
            string newDirectory = Path.Combine(path, "backups");
            if (!Directory.Exists(newDirectory))
            {
                Directory.CreateDirectory(newDirectory);
            }
            return Path.Combine(path, newDirectory, newFile);
        }

        public static bool IsProgramInstalled(string programDisplayName)
        {
            List<string> installs = new List<string>();
            List<string> keys = new List<string>()
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" // 32 bit apps on 64 bit OS
            };

            // The RegistryView.Registry64 forces the application to open the registry as x64 even if the application is compiled as x86 
            FindInstalls(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), keys,
                installs);
            FindInstalls(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64), keys,
                installs);
            FindInstalls(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32), keys,
                installs);
            FindInstalls(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32), keys,
                installs);

            installs = installs.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            installs = installs.Where(s => s.ToLowerInvariant().Contains(programDisplayName.ToLowerInvariant())).ToList();
            return (installs.Count > 0);
        }

        private static void FindInstalls(RegistryKey regKey, List<string> keys, List<string> installed)
        {
            foreach (string key in keys)
            {
                using (RegistryKey rk = regKey.OpenSubKey(key))
                {
                    if (rk == null)
                    {
                        continue;
                    }
                    foreach (string skName in rk.GetSubKeyNames())
                    {
                        using (RegistryKey sk = rk.OpenSubKey(skName))
                        {
                            try
                            {
                                installed.Add(Convert.ToString(sk.GetValue("DisplayName") + "\t:\t" + sk.Name));
                            }
                            catch (Exception ex)
                            { }
                        }
                    }
                }
            }
        }

    }
}
