using System.IO;

namespace JuliusSweetland.OptiKey.InstallerActionsEyeMine
{
    // Encapsulate all the logic / file tree around an appdata dir
    internal struct InstallationPaths
    {
        internal readonly static string forgeVersionRequired = "1.14.4-forge"; // suffix can vary
        internal readonly static string forgeVersionRecommended = "1.14.4-forge-28.2.26";
        internal readonly static string forgeVersionOld = "1.11.2-forge1.11.2-13.20.0.2228"; // from EyeMine Classic install

        public InstallationPaths(string usingAppDataPath)
        {
            appDataPath = usingAppDataPath;
            minecraftPath = Path.Combine(appDataPath, ".minecraft");
            versionsPath = Path.Combine(minecraftPath, "versions");
            eyemineGameDir = Path.Combine(minecraftPath, "EyeMineV2");

            // Look for an appropriate forge install
            // We will accept any directory matching the Minecraft version, not necessarily the exact forge version  
            forgePath = null;
            if (Directory.Exists(versionsPath))
            {
                foreach (var tentativeVersionDir in Directory.GetDirectories(versionsPath))
                {
                    if (new FileInfo(tentativeVersionDir).Name.Contains(forgeVersionRequired))
                    {
                        forgePath = tentativeVersionDir;
                    }
                }
            }
        }

        public string appDataPath;
        public string minecraftPath;
        public string forgePath;
        public string versionsPath;
        public string eyemineGameDir;

        public string forgeVersion
        {
            get
            {
                if (forgePath == null)
                    return null;
                else
                    return new FileInfo(forgePath).Name;
            }
        }
        public string oldForgePath { get { return Path.Combine(versionsPath, forgeVersionOld); } }
        public string launcherProfilesClassic { get { return Path.Combine(minecraftPath, "launcher_profiles.json"); } }
        public string launcherProfilesWin10 { get { return Path.Combine(minecraftPath, "launcher_profiles_microsoft_store.json"); } }
    }
}
