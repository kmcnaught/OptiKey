using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnsetKiosk
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {

                DirectoryEntry AD = new DirectoryEntry("WinNT://" +
                                        Environment.MachineName + ",computer");

                foreach (DirectoryEntry child in AD.Children)
                {
                    if (child.SchemaClassName == "User")
                    {
                        if (child.Name == "EyeMine")
                        {
                            Console.WriteLine("");
                            Console.WriteLine("EyeMine user has the following SID");
                            NTAccount f = new NTAccount(child.Name);
                            SecurityIdentifier s = (SecurityIdentifier)f.Translate(typeof(SecurityIdentifier));
                            String sidString = s.ToString();
                            Console.WriteLine(sidString);

                            string path = $"Computer\\HKEY_USERS\\{sidString}\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";

                            var names = Registry.Users.GetSubKeyNames();
                            foreach (var name in names)
                            {
                                Console.WriteLine(name);
                            }

                            //OurKey = OurKey.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\", true);

                            //SetValueWithLogging(OurKey, "Shell", "", RegistryValueKind.String);

                            Console.WriteLine("\nFinished, press Enter to continue....");
                            Console.ReadLine();                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleColor origColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR\n");
                Console.WriteLine(ex.Message);

                Console.ForegroundColor = ConsoleColor.DarkYellow;

                Console.WriteLine("\nNote that this executable must be run as administrator\n");

                Console.ForegroundColor = origColor;
                Console.WriteLine("Press Enter to quit....");
                Console.ReadLine();

            }            
        }


        static void SetValueWithLogging(RegistryKey key, string name, object value, RegistryValueKind kind)
        {
            Console.WriteLine($"\nSetting registry value \"{name}\": {value} ({kind.ToString()}) in \n {key.ToString()}");
            key.SetValue(name, value, kind);
        }
    }

}
