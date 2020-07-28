using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using EyeXFramework;
using Microsoft.Win32;
using Tobii.EyeX.Client;
using Environment = System.Environment;


namespace JuliusSweetland.OptiKey.InstallerActionsEyeMine
{
    public class CustomActionsEyeMine
    {

        private static string forgeVersion = "1.14.4-forge-28.2.0";
        private static string forgeVersionOld = "1.11.2-forge1.11.2-13.20.0.2228";
        private static string launcherProfiles = "launcher_profiles.json";

        private static string appDataPath, minecraftPath, versionsPath, forgePath;

        static CustomActionsEyeMine()
        {
            // Set up directory names

            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            minecraftPath = Path.Combine(appDataPath, ".minecraft");
            versionsPath = Path.Combine(minecraftPath, "versions");
            forgePath = Path.Combine(versionsPath, forgeVersion);
            launcherProfiles = Path.Combine(minecraftPath, launcherProfiles);

        }

        private static bool HasProperty(dynamic obj, string name)
        {
            if (obj == null) return false;
            if (obj is IDictionary<string, object> dict)
            {
                return dict.ContainsKey(name);
            }
            return obj.GetType().GetProperty(name) != null;
        }

        private static string GetBackupName(string filename)
        {
            // The following will produce 2011-10-24-13-10
            string datetime = DateTime.Now.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture);

            string path = Path.GetDirectoryName(filename);
            string file = Path.GetFileNameWithoutExtension(filename);
            string extension = Path.GetExtension(filename);

            string newFile = String.Format("{0}_{1}{2}", file, datetime, extension);
            return Path.Combine(path, newFile);
        }

        public static bool IsProgramInstalled(string programDisplayName)
        {
            bool b = false;
            string regKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";

            foreach (var item in Registry.LocalMachine.OpenSubKey(regKey).GetSubKeyNames())
            {
                object programName = Registry.LocalMachine.OpenSubKey(regKey + "\\" + item).GetValue("DisplayName");

                if (string.Equals(programName, programDisplayName))
                {
                    return true;
                }
            }
            return false;
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


        [CustomAction]
        public static ActionResult EyeMineProperties(Session session)
        {
            // Run all the other actions and set installer properties
            ActionResult success = ActionResult.Success;
            
            if (CheckForMinecraftInstallation(session) != success)
            {
                session.Log("Error checking for Minecraft install");
                return ActionResult.Failure;
            }
            if (CheckForForgeInstallation(session) != success)
            {
                session.Log("Error checking for Forge install");
                return ActionResult.Failure;
            }
            if (QueryTobiiSupported(session) != success)
            {
                session.Log("Error checking for Tobii support");
                return ActionResult.Failure;
            }

            return ActionResult.Success;

        }

        [CustomAction]
        public static ActionResult QueryTobiiSupported(Session session)
        {
            bool supported = false;
            session["TOBII_SUPPORTED"] = "unknown";
            try
            {
                supported = IsTobiiSupported();
            }
            catch
            {
                return ActionResult.Failure;
            }

            session["TOBII_SUPPORTED"] = supported.ToString().ToLower();

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CopySaves(Session session)
        {
            session.Log("Start CopySaves action");

            // This CustomAction happens after main install has happened. At this point you cannot
            // access installer properties, so any data must be passed via CustomActionData.
            var actionData = session["CustomActionData"];
            session.Log("actionData: "+actionData);
            if (String.IsNullOrEmpty(actionData))
            {
                session.Log("No world files to be copied");
                return ActionResult.Success;
            }

            string eyemineGameDir = Path.Combine(minecraftPath, "EyeMineV2");
            string newSavesDir = Path.Combine(eyemineGameDir, "saves");
            string oldSavesDir = Path.Combine(minecraftPath, "saves");

            var worlds = actionData.Split(',');
            foreach (var world in worlds)
            {
                // world string is currently, e.g.:
                // My World Name (last played 3 days ago)
                
                // Split folder name from recency text
                var i = world.LastIndexOf(" (");
                var worldName = world.Substring(0, i);
                session.Log($"Copying everything from {Path.Combine(oldSavesDir, worldName)} to {Path.Combine(newSavesDir, worldName)}");
                // Copy folder to new location
                DirectoryCopy(Path.Combine(oldSavesDir, worldName), 
                                Path.Combine(newSavesDir, worldName), 
                                true);

                // TODO: split path and creation date, copy it.
                session.Log(worldName);
            }
            session.Log("End CopySaves action");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult InstallMod(Session session)
        {
            // This CustomAction happens after main install has happened. At this point you cannot
            // access installer properties, so any data must be passed via CustomActionData.
            session.Log("Installing mod");

            var actionData = session["CustomActionData"];
            bool tobiiSupport = false;
            Boolean.TryParse(actionData, out tobiiSupport);
            session.Log(String.Format("actionData: {0} {1}", actionData, tobiiSupport));
            
            // First time we'll do some forge config-poking
            // This will also ensure all paths have been created
            bool alreadyInstalled = false;
            try
            {
                alreadyInstalled = UpdateForgeConfig();
            }
            catch (Exception e)
            {
                session.Log(e.ToString());
                return ActionResult.Failure;
            }
            session.Log("alreadyInstalled? "+ alreadyInstalled);
            
            // Extra copying of files first time
            // TODO: On upgrades we'll want mod *replacing* but no config copying
            if (!alreadyInstalled)
            {

                // Note that when AI runs this DLL from the installed (Program Files) directory, it 
                // actually runs it in a temporary subdir, so all local paths need one level of redirection up
                string rootDir = "..";
                string modFile = "eyemine2.0.12.jar";
                string configFileSrc = "eyemine-client-mouse.toml";
                if (tobiiSupport)
                {
                    configFileSrc = "eyemine-client-tobii.toml";
                }

                string configFileDst = "eyemine-client.toml";

                if (!File.Exists(Path.Combine(rootDir, modFile)))
                {
                    session.Log("Cannot find mod file to install");
                    return ActionResult.Failure;
                }

                if (!File.Exists(Path.Combine(rootDir, configFileSrc)))
                {
                    session.Log("Cannot find config file to install");
                    return ActionResult.Failure;
                }

                string eyemineGameDir = Path.Combine(minecraftPath, "EyeMineV2");
                string modDir = Path.Combine(eyemineGameDir, "mods");
                string configDir = Path.Combine(eyemineGameDir, "config");

                try
                {
                    session.Log("copying from:" + modFile);
                    session.Log("to:" + Path.Combine(modDir, modFile));
                    File.Copy(Path.Combine(rootDir, modFile), Path.Combine(modDir, modFile));
                    File.Copy(Path.Combine(rootDir, configFileSrc), Path.Combine(configDir, configFileDst));
                }
                catch (Exception e)
                {
                    session.Log("Error copying files");
                    session.Log(e.ToString());
                    return ActionResult.Failure;
                }
            }

            return ActionResult.Success;

        }

        public static bool UpdateForgeConfig()
        {

            // Check launcher_profiles file exists
            if (!File.Exists(launcherProfiles))
            {
                throw new FileNotFoundException("Could not find launcher profiles: " + launcherProfiles);
            }

            // Backup current file
            string launcherProfilesBackup = GetBackupName(launcherProfiles);
            File.Copy(launcherProfiles, launcherProfilesBackup);

            // Read JSON
            string json = File.ReadAllText(launcherProfiles);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            // We'll take existing list of profiles and create new list, so we control the order
            Dictionary<string, dynamic> origProfiles = jsonObj["profiles"].ToObject<Dictionary<string, dynamic>>();
            var newProfiles = new JObject(); 

            // Create separate directory for EyeMineV2 files (if not already)
            string eyemineGameDir = Path.Combine(minecraftPath, "EyeMineV2");
            bool alreadyInstalled = Directory.Exists(eyemineGameDir);
            if (!alreadyInstalled)
            {
                // Create directories (our installer needs it to exist so it can copy mod over)
                Directory.CreateDirectory(eyemineGameDir);
                Directory.CreateDirectory(Path.Combine(eyemineGameDir, "mods"));
                Directory.CreateDirectory(Path.Combine(eyemineGameDir, "saves"));
                Directory.CreateDirectory(Path.Combine(eyemineGameDir, "config"));

                // Add new profile for EyeMineV2
                string eyeMineUuid = System.Guid.NewGuid().ToString("N");

                newProfiles[eyeMineUuid] = new JObject(
                    new JProperty("gameDir", eyemineGameDir),
                    new JProperty("icon",
                        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAG0wAABtMBs4encQAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAABZpSURBVHic7Z1/VFVV2se/53C5BJdfCqKWgVcUmykxBKVykiQsNBzLyclmosnB6cesGWxcU9RoL83Uel+rWY0zvf7BPzVZivm+Obn0LdQRyN+AaGGJIBcQcvidA8gVvRee948DJnDhPPvcc+8Fup+1niXruO+zfzz7nLPP3s9+toTxhwRgBoAf9P07HUAkgEkAwvrEr08C+n5jBXC1T9oAtAJoAVAHoBZADYCzff+SW2rhJiRPF0AHggAkAVgM4G4Ad/RdcwWdAL4CcAxAAYBDfdfGLGOxA/gBWATF4IsBJAAweKgsdgAlUDpDPoAjUJ4iXlxAPIC/AmiG8hgejXIJwFYAKRibN9eowwzgPwCch+eNKyqVAF7pq4MXQe4EsBNALzxvSGelF8AeAIm6ttA45UdQGsvTRnOVHAGwXLfWGkcsgdI4njaQu+QwlHHC956boQyaPG0QT8keAFFOt6ITeGqk6gtgPZRBkskdGZpMJoSHhyMsLAwTJ04EAAQHBwMAOjo6AADffvst2tra0Nraiq6uLncUCwAuA/gTgM0AbO7KtB9PdID7AGwB8ENXKI+IiMBdd92F2NhYxMbGIjo6GlFRUQgLCxPS09bWhtraWlgsFpSVlaGsrAxFRUVobm52RbEB4GsAv4YyuTQuMQB4FUAPdHyMBgYGUkpKCm3atIlOnjxJvb295EosFgvl5OTQqlWraOLEiXq/EnqhzHX4usYEniMSyvSpLg0VEhJCa9eupUOHDpHdbnepwUfCbrfT559/ThkZGRQSEqJnRzgC4FYX2sOtLIeywOJUoxgMBnrooYdox44ddOXKFY8ZfTisVivl5ubSsmXLyGAw6NEJWgE85GLbuBQZwFtwcjLHz8+Pnn32Waqurva0jdlYLBZ65plnyM/PT49Xwqa+thxTGAHsgBOVN5lMlJmZSfX19Z62p2YaGxspKyuLTCaTsx1hF4CbXG41nQgCcBAaKyvLMj333HPU0tLiafvpRnNzMz399NMky7IzneAAXLfMrRuTAZRCYyXj4uLo+PHjnraXyygtLaUFCxY40wlOAohwgx01MR0aV+1CQ0Npy5YtbhvRX758mZqamshisZDFYqGmpibq6upyS952u53eeecdZ74aKqHj7KFeE0GToMxvzxb94fz58/HRRx/BbNZ31bSyshKnT59GRUUFzp07h8rKSly4cAFtbW0gIoe/kSQJYWFhiIqKwuzZs3HbbbchJiYGcXFxiImJ0bV89fX1ePzxx3H06FEtP7cAWAigSddCaSQIGh77kiRRZmYmXb16VZc7q66ujt59911KT0+nW265xdlB1xCZNm0apaen03vvvafbwNRms1F2drbWsUEJRsGYwAhgHwQLHxYWRnv37nW6Aa1WK+3cuZNSUlJIkiTdjT6cyLJMCxcupJycHGpvb3e6Hnv27KGwsDAtZcmH4iLnEWQoThtChY6JiaGamhqnGuzLL7+kJ554gvz9/d1m9OEkICCA0tPTqayszKk6WSwWmjlzppYy5MJD8wRviRY2ISGBmpubNTfSqVOnaNWqVW6927kiSRKlpaU59RXT1NRE8fHxWvL/L10sKsBDEJzhS05O1vy4LC8vpwceeMDjRubK0qVLqaKiQlNdOzs76cEHHxTNsxfAj520KZtICM7tP/roo5oGe1arlbKzs/WYUnW7+Pr6UmZmJnV2dgrXu7u7m1auXCmaZwuAaU5ZloEBgq5bycnJ1N3dLdwIeXl5FBUV5XFDOitms5n2798vXP+rV69qeRIch4uXkoXe+wkJCdTR0SFUcSc/jUal9H/yXrt2TagtOjo6tIwJ/lOzdVW4DwLv/ZiYGOEBX319Pd17770eN5irZNGiRfTNN98ItUljY6Po10EPgHu1GHgkjFBclliFCA8PF/7UKy4upoiICI8bydUyefJkOnnypFDbWCwWUe+jMui8Xe4lbuaSJNHu3buFKnjw4EEKDg72uHHcJYGBgZSXlyfURnv37hX9/F2vwc4OuRXKDlhWxi+++KJQxXbt2kU33XSTx43ibjEajZSbmyvUVr/73e9E8ugAcIugrR2yi5tpYmKi0Odebm7uuBrsiYosy7Rjxw52e127do3uuecekTx2qBlXbTXwAShz/aqEhoairKwMt97K82U8cOAA0tLScO3aNVZ6LSxcuBD+/v4u03/x4kWUl5c7pcNoNOLTTz/F/fffz0pfW1uLuXPnXt/LwCAZyvZ1TRwGs7dt2bKF3ZNLSkooMDDQ5XdYVVUVu0xayMnJ0aWcQUFBQgPDzZs3i+jXbPz7uJnEx8eznTnq6+vdNtofKx0AUL4OLl68yMrXbrdTXFyciP4faekABzjKZVlmL4DYbDa3fuePpQ4AKPMENpuNlXdxcbHI+OlTUeMv4Bb6ueeeYzfYSy+95Dbjj8UOAIA2btzIzj8jI0NE93xHhh5uELgbjNUlk8mE2tpahIeHqyXFvn37sGzZMvT29qqm1cLy5csxderUAddef/11TJo0ySX5AcDhw4fx4YcfqqY7dOgQzp07x9IpyzL27duHlBT13eNNTU0wm824cuUKR/U/AKzkJDSDOeW7fv16Vk+1Wq1kNptdercXFhZqvZFdztq1a4XqEhkZSZcvX2bpzszM5OrtgeK4OwBHniRPguEs6ufnh/XreZNNr732GmpqalhpvQB1dXV44403WGlfeOEFGI1GTlIZwM85CSvA6FHPPvssq4dWVFS4ZT1/PD0BAGWmsLy8nKX/V7/6FVdvJVRu7ns4igwGA3uvnrs8ecZbBwAUzyIOVVVV5OPjw9U7IFjV4BWj9JF6Rz+pqaksP/4TJ05g//79HJVOk5ubixMnTgy49swzzyA0NNQt+buCzz77DCUlJZg/3+EA/jrR0dFYsmQJ8vLyOGrTARQ5+g8/AN+C0Yu489dpaWluufuHE1d/BnLR+gQAQA8//DArj+3bt3N1tkJZ3h/CEo6C4OBgslqtqgX64osvPO69Ox46gCRJLJdzq9Uqst1scb/Rb/wKWAwGjz32GGuB5c9//vOwW7C88CEivPXWW6rp/P39sXIl6zMfcKYDpKerDxM6Ojqwa9cubmG8qPDxxx+js1M9KPmTTz7JVZnc/0f/J0EQFFfvET1KQ0ND0draCh8fnxG1v/vuu8jIyOAWxmWsXr36eig4EaZNm4ZXXnlFt3I4mgn829/+hq+//pqt4+9//zt+8YtfjJimp6cH4eHh+Pe//62mzg5gIm4IdZ8GHQckSUlJHn33Oytz58517qXPIDU1VahM999/P0vv8uXLuTpTge9eAazH/+LF6snq6+tx6NC4DXXnMQoKCvCvf/1LNR3HRv1Jge86wN2cXyQnJ6umOXDggHfw5wJ6e3tx8OBB1XRczyIo8QUg98kctdQRERG4/fbbVbUWFBRwC+BFkPz8fNU0c+bM4a6AzgEgGaCs/gWqpb7rrrsgSeoBRcZaBwgODsbHH3884FpgoGpzeAROB5AkCYmJidi7d69a0mAAkQYwY/bGxsaqpqmsrMTFixc56kYNvr6+rLX30UBdXR2qqqowc+bMEdPNmTOH0wEA4IcyHKwRD6dUjVOnTnFUeXGC06dPq6bh2KoPM7sDqPU6AKioqOBm7EUjnDaOjo7mqjPLUPb7qzJ9+nTVNN4O4Ho4bSwQcS3KACXE24iYTKbrhyyMRGVlJTdjj3Hs2LEBHjQGA28P5dGjR7Fu3boB1x555BFs2LBB1/KpwekAkyZNgr+/P8dXMJzVAbiOldXV1ax0niQuLg433SQedre9vR2lpaUDrsXHx+tVLDbcNg4PD0d9fb1qMhnABLVUnNM2iEhku5IXjbS3t7PScTy1AUyU8d0BysMSEhKiqunKlSvo6enhZOrFCex2O7q7u1XTcWwGIEDGMN4hN+Lnpx6LkLNc6UUfOG3NsRkAPwMYHYDjdjwaO0BpaemQETGnYb766issWrRowDWbze0Heg1LR0eH6rhMpAOMW0JCQjBhguoQZwg9PT24dOmSC0o0+pABqG7Q5+zhDwryeNzi7w0cJxfOOAHAVVYH4CgbrQso4xHOzcYMvHFVBmBVS8X59AgICFB1FfPiPAaDgTWPwXALAwCrDGUvwIi0tbWpapIkifvp4RIKCwtBRAOEMyfe1tYGSZIGyJ133umGEmuDu9GltbWVk6xNhrJRQA9lIosQXjTCWZQDeDctuB2gq6uLpVDvY1W8DIXTxs3NzdyYAa0ygAuclLW1tappZs2axVHlxQk4bSywFb/WAKCWk7Kqqkp18eO2227jZuwUERERQ746uOHgampqBjitMgdLDuno6BiyODNhwoQhcw8tLS1DJsqsVtWxt0Nmz1Y/l8tisXDV1QLMPQEbNmxQ9Umvrq52i9/+tm3bNPvjuzoq6auvvjokT2f2Bg6W2tpa1ToKxGJaKgNgRTosKytTTWM2mxEVFcVR50UDM2bMYLXvmTNnuCrPygCqocSVHZETJ06w/P3vu+8+buZeBOFs+iAiFBU53P4/mHYAdTKUR4HqJrWWlhZWz0pKSuJk7kUDnLYtKyvjfrZ/BYD6F4OOg7E7qKCgQNU9PDU1FT4+Prr5BsyaNWvIY2/KlCms3x49enTI55CrwtT1U11djX/+858DrunhKm8wGPDggw+qpuPsHupjwJGlrIHgihUrWAMtPeMCvfHGG4LDvO+Ijo52y6DUHcKNFyQQlWXA5tDPAagueBcWFrLu7J//nBWNzIsAnDa12+3cjbl29D0B+jtAJ5Tzf0ekvb0dR44cUdX+k5/8BCaTiVMQLwxMJhNWrFihmu7w4cNcv8wi9MUGuDFCSAHnl5zQqCaTCT/96U856rwwWL16NWu5fevWrVyV1wcKN+72XAJANaZbcHAwGhoaEBAwsi9peXk57rjjDqFB16xZs4asxD3xxBP48Y/VD8UsLCxES0vLgGu//e1v0dQ0Kk5Y14wsyzh79qzqDOCVK1cwdepUrtfwYgCFgy8aoSwMqQ4guGHiHnnkEaGBzrp16zQP+MZ6VJLhZNWqVaz6b9u2jatzQJi4G18B1wD8z+Be4YgPPviAkwwvv/wyK52X4cnKymKl49oEymnjw7oL3Q1GLzIYDGSxWFg9UyRYpPcJMFC4MZnOnz8vEip2wY0GHxwt/DiUYNEjYrfb8eabb6olAwBs3rxZ01as7zv+/v54++23WWk3bdrEnXg7D6D4xguO3MK3A/ijmqb33nsPf/jDHxAZOfLm4ujoaLzwwgt47bXXOAVksWfPHpw9e3bAtbq6Ot30jwaysrJYu3zr6+tFHv/vcxKxD4x4/vnnWY8o7oER3FfAz372M48/nl0pkZGR1NXVxWqL3/zmN1y9PQCiOB0AAD7hKA0ICGAfEH3w4EHVQ468HQDk4+NDBQUFrHZoaGggf39/rm6HoVsdnRgCAH/q+9GIWK1WbNy4US0ZACXEnLv30o9FNm7cyF5Sf/nll7m+f4CGY+X3g9GzZFmmY8eOsXpsT08PJScne58Aw0hSUhL7/MUjR46IRGP/v+GMPFLctyQ4mC1yxLx581BcXMzaGHLx4kXEx8c7nKFLSEhgBTp0NAgc60yZMgWlpaW4+eabVdPa7XbMnz8fX3zxBVf9vQDUF3EccAjM3vvOO++wei4RUVlZGYWGhnr8jhstEhQURKWlpez2e/vtt0X0sx0EHJHCzSgkJITlsNhPfn6+Ww6TGu1iNBpp37597Harrq6m4OBgkTyShCzugP/lZrZgwQKh4+N37NjhPT5e8Pj4u+++WySP7YK2dsg0KGvHrEx///vfsytERLRr1y6Xu2qPRjEajZSbmyvUVuvWrRPJowPALYK2HpYXuRlLkkSffPKJUMXy8/NFH2tjWgIDAykvL0+ojfbs2SN6BtPzGuw8LL7o8yLlSFhYGHuxqJ/S0lKaPHmyx43japkyZQqdOnVKqG0qKytpwoQJIvl8CcfT/E6xCMwpYgA0c+ZMampqEqpoY2MjLVmyxONGcpUkJSXRN998I9QmDQ0NNGPGDJF8etB3FoAr2CRS4djYWLp06ZJQhe12O2VnZ4ssb456kSSJsrKy2JM8/bS3t1NcXJxofq9rti4DA4DDIgVavHgxdXd3C1WcSFk7mD59useN56yYzWbKz88Xrv/Vq1e1PA0/hwse/YO5FUzXsX5ZuXKlpk5gtVopOzt7TH4l+Pv7U1ZWFvsY+Bvp7u6mhx9+WDTPZug46ldjKQTGA4DyJGhvbxduDCLlcGSB07A8LitWrGAfrj2Yzs5OLRtregEsd9KmwgiNBwDQvHnzqLGxUVPDEBGdPn2a0tPTR+X4QJZlSktLo6KiIs31a2ho0PLOJ2hY6dMDGcAO0cJGR0c7faZveXk5/fKXv6SAgACPG95kMlFGRgadO3fOqTqdP39edLTfL9sw/LK+y/EFkKdSwCESEhJCO3fudKrBiJQxws6dOyktLY18fX3dZnQfHx9auHAh5eTkUEdHh9P12L17N02cOFFLWQ5COfXdowQAOAbBwkuSRJmZmUJrByPR2NhI77//Pj311FMs9zNRMZvNtGbNGtq6davw/MZw2Gw2ysrK0nrKegkYp72poX4OHI9wKOvN6gFsBpGQkICPPvoIM2bM0KkoChcuXEBRUREqKyuvS01NDVpbW4fdrSTLMsLDw2E2mxETE4PZs2dj1qxZSExM1D3ySV1dHVavXo3jx49r+XkVgB8BGFXbnqIAVELD3RUSEkKbN28mm82my52lRldXFzU3N5PFYiGLxULNzc1sJ0xnsdls9Je//MWZtY8KMM958gQToewt0FS5uXPn0tGjR91iCE9QUlJCCQkJzryKSgBEuMGOThEE4AA0VlKSJMrIyHDqc3G00dDQQGvWrNH6ru+XPOjwzncXRih70DRX2M/Pj55++mmqq6vztP00c+HCBcrMzBRx3R5OtkH54hpTyFAmi4RmDAeL0WiktWvXOj134E6qqqooIyODjEajs4bvgbK4o9dg3SMsA9AC5xqCfHx8aOnSpbR9+3ayWq2etvEQurq6aNu2bZSamqrXTGUz+uL4jAemQXAVcSQJDg6mNWvWUEFBgfASq57YbDbKz8+np556ioKCgnSpW598Djcu7LgLA4BXoTzWdGssk8lEKSkptGnTJjp58iT19PS41OgWi4VycnJo1apVol46HOkF8Fe48X3viXfLvQC2AGAfcS1CeHg4EhMTMWfOHMTGxiI6OhrTp09HRITY11NzczNqa2tRVVWFM2fO4MyZMyguLh4ShkZHygD8GoPi97kaTw0uDAAyoTwR3HLaVEBAAMLCwq6LJEkIDAyEJEno7OwEEaGtre26aI3mrYEOANkA/htK+LbvFVMBbIW+j9GxJHugONh87+mPWuVpg7hL8qHDjp3xyEIod4VTcwejWI4ASNattcYxCQD+gfHREXqgBGeYp2sLfU+IArARygqYpw0pKucAbIBAWBYvI3M7lKnlJnjeuMPJtwByoKzVj4np2zFRyEEYoYwVkqEMHhPhBl/4YbBBCbuW3yfHwDiKdzQxFjvAYAKhTC4lQwl0OQeA+unK2mgHcAaKz0M+lOntLhfl5RbGQwdwxHQAPwAwo+/vSCiOFGF94g9lurV/fb0TyiSMFcpjvA3Kq6YeQA2Uc5XKwTxjcSzx/xHr8kL+SuqKAAAAAElFTkSuQmCC"),
                    new JProperty("lastVersionId", forgeVersion),
                    new JProperty("name", "EyeMine v2"),
                    new JProperty("type", "custom"));
            }

            // Add old entry for classic if forge-1.11.2 exists. 
            // (on the first install it often gets overwritten by forge install)
            if (!alreadyInstalled)
            {
                string oldForgeDir = Path.Combine(versionsPath, forgeVersionOld);
                if (Directory.Exists(oldForgeDir))
                {
                    string forgeUuid = System.Guid.NewGuid().ToString("N");

                    newProfiles[forgeUuid] = new JObject(
                        new JProperty("name", "forge-1.11.2"),
                        new JProperty("lastVersionId", forgeVersionOld),
                        new JProperty("type", ""));
                }
            }

            // Add back all original profiles to new dictionary
            foreach (KeyValuePair<string, dynamic> profile in origProfiles)
            {
                newProfiles[profile.Key] = profile.Value;
            }

            // Replace profiles entry in JSON (we made our own copy to get some control over the order)
            jsonObj["profiles"] = newProfiles;

            // Save new file
            string output =
                Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(launcherProfiles, output);
            
            return alreadyInstalled;
        }

        [CustomAction]
        public static ActionResult CheckForForgeInstallation(Session session)
        {
            //session.Log("Begin CheckForForgeInstallation");
            session["FORGE_VERSION_REQUIRED"] = forgeVersion;
            session["FORGE_INSTALLED"] = "unknown";

            if (Directory.Exists(forgePath))
            {
                session["FORGE_INSTALLED"] = true.ToString().ToLower();
                return ActionResult.Success;
            }
            else
            {
                session["FORGE_INSTALLED"] = false.ToString().ToLower();
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult CheckForMinecraftInstallation(Session session)
        {
            //session.Log("Begin CheckForMinecraftInstallation");
            session["MINECRAFT_INSTALLED"] = "unknown";
            if (IsProgramInstalled("Minecraft Launcher"))
            {
                session["MINECRAFT_INSTALLED"] = true.ToString().ToLower();
                return ActionResult.Success;
            }
            else
            {
                session["MINECRAFT_INSTALLED"] = false.ToString().ToLower();
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult CheckForMinecraftHasBeenLaunched(Session session)
        {
            //session.Log("Begin CheckForMinecraftInstallation");
            if (Directory.Exists(minecraftPath))
                return ActionResult.Success;
            else
                return ActionResult.Failure;
        }

        private static string AppendItemToListData(string combodata, string item)
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

        private static string ForceAscii(string inputString)
        {
            return Encoding.ASCII.GetString(
                Encoding.Convert(
                    Encoding.UTF8,
                    Encoding.GetEncoding(
                        Encoding.ASCII.EncodingName,
                        new EncoderReplacementFallback(string.Empty),
                        new DecoderExceptionFallback()
                    ),
                    Encoding.UTF8.GetBytes(inputString)
                )
            );
        }

        [CustomAction]
        public static ActionResult GetMinecraftSaves(Session session)
        {
            //session.Log("Begin CheckForMinecraftInstallation");

            //string savesPath = Path.Combine(minecraftPath, "saves")
            string savesRoot = Path.Combine(appDataPath, ".minecraft250620", "saves");

            string checkListData = ""; // we'll append to this as we go

            List<KeyValuePair<string, DateTime>> saves = new List<KeyValuePair<string, DateTime>>();

            if (Directory.Exists(savesRoot))
            {
                string[] saveDirs = Directory.GetDirectories(savesRoot);
                Console.WriteLine("Directories:");
                foreach (var saveDir in saveDirs)
                {
                    Console.WriteLine(saveDir);
                    string file = Path.Combine(saveDir, "level.dat");
                    if (File.Exists(file))
                    {
                        DateTime latestTime = File.GetLastWriteTime(file);
                        Console.WriteLine(latestTime);

                        string dirName = new DirectoryInfo(saveDir).Name;
                        if (latestTime > DateTime.MinValue)
                        {
                            saves.Add(new KeyValuePair<string, DateTime>(dirName, latestTime));
                        }
                    }
                }
            }

            // Set combobox data
            saves.Sort((x, y) => y.Value.CompareTo(x.Value)); // order by date
            foreach (var save in saves)
            {
                string filenameAndDate = String.Format("{0} (last played {1})", save.Key, GetPrettyDate(save.Value));
                checkListData = AppendItemToListData(checkListData, ForceAscii(filenameAndDate));
            }
            session["SAVES_CHECKLIST_DATA"] = checkListData;

            // Set defaults: any from the last month, or most recent one. 
            DateTime now = DateTime.Now;
            String defaultData = "";
            foreach (var save in saves)
            {
                TimeSpan age = now.Subtract(save.Value);

                if (age < TimeSpan.FromDays(31))
                {
                    if (!String.IsNullOrEmpty(defaultData))
                        defaultData += ",";
                    string filenameAndDate = String.Format("{0} (last played {1})", save.Key, GetPrettyDate(save.Value));
                    defaultData += filenameAndDate;
                }
            }
            // default first in list
            if (String.IsNullOrEmpty(defaultData))
            {
                var firstElement = saves.ElementAt(0);
                string filenameAndDate = String.Format("{0} (last played {1})", firstElement.Key, GetPrettyDate(firstElement.Value));
                defaultData = filenameAndDate;
            }
            session["SAVES_CHECKLIST_DEFAULT"] = defaultData;

            return ActionResult.Success;
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

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
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
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
