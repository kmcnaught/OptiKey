using Microsoft.Win32;

// Static utils for installer registry access
namespace JuliusSweetland.OptiKey.InstallerActions
{
    public class RegistryUtils
    {
        private const string KEY_PATH = "SOFTWARE\\SpecialEffect\\EyeMineV2\\";
    
        public static bool GetRegistryBool(string keyName)
        {
            var key = Registry.CurrentUser.CreateSubKey(KEY_PATH);
            int regInt = (int)key.GetValue(keyName, -1);
            return (regInt > 0);
        }

        public static void SetRegistryBool(string keyName)
        {
            var key = Registry.CurrentUser.CreateSubKey(KEY_PATH);
            key.SetValue(keyName, 1);
            key.Close();
        }

        public static string GetRegistryString(string keyName)
        {
            var key = Registry.CurrentUser.CreateSubKey(KEY_PATH);
            string regString = (string)key.GetValue(keyName, "");
            return regString;
        }

        public static void SetRegistryString(string keyName, string value)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(KEY_PATH);
            key.SetValue(keyName, value);
            key.Close();
        }
    }
}
