using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Base;
using JuliusSweetland.OptiKey.UI.Views.Exhibit;
using log4net;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class DemoState
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private OnboardingWindow onboardWindow;
        private Process minecraftProcess;
        private MainViewModel mainViewModel;
        private OnboardingViewModel onboardVM;

        private readonly String enabledKeyboard  = @"C:\Users\Kirsty\AppData\Roaming\SpecialEffect\EyeMineV2\Keyboards\EyeTracker\museum.xml";
        private readonly String disabledKeyboard = @"C:\Users\Kirsty\AppData\Roaming\SpecialEffect\EyeMineV2\Keyboards\EyeTracker\museumDisabled.xml";

        enum Stage
        {
            IDLE,
            ONBOARDING_NO_KEYBOARD,
            ONBOARDING_WITH_KEYBOARD,
            IN_MINECRAFT
        }

        private Stage stage;

        public DemoState(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;

            LaunchOnboarding();

            bool noRepeat = true;
            HotkeyManager.Current.AddOrReplace("Back", Key.Left, ModifierKeys.None, noRepeat, OnBack);
            HotkeyManager.Current.AddOrReplace("Forward", Key.Right, ModifierKeys.None, noRepeat, OnForward);
            HotkeyManager.Current.AddOrReplace("Reset", Key.Down, ModifierKeys.None, noRepeat, OnReset);
            HotkeyManager.Current.AddOrReplace("Info", Key.Up, ModifierKeys.None, noRepeat, OnInfo);

            // Launch Minecraft
            string javapath = @"C:\Program Files (x86)\Minecraft Launcher\runtime\jre-legacy\windows-x64\jre-legacy\bin\javaw.exe";
            
            // args need to come from a previous run of Minecraft with valid credentials
            String cmdTextFile = AppDomain.CurrentDomain.BaseDirectory + @"\Resources\minecraftCommand.txt";
            string minecraftArgs = File.ReadAllText(cmdTextFile);

            minecraftProcess = Process.Start(new ProcessStartInfo(javapath, minecraftArgs));
            minecraftProcess.CloseOnApplicationExit(Log, "Minecraft");

            stage = Stage.IDLE;
            UpdateForState(stage);


            // Poll regularly to ensure Minecraft doesn't steal focus at inappropriate time
            
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += TimerTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();

        }

        private void TimerTick(object sender, EventArgs e)
        {
            UpdateOptiKeyFocusForState(stage);           
        }


        void UpdateForState(Stage stage)
        {
            UpdateMinecraftFocusForState(stage);
            UpdateOptiKeyFocusForState(stage);
            UpdateKeyboardForState(stage);
        }

        void UpdateMinecraftFocusForState(Stage stage)
        {
            if (minecraftProcess == null) { return; }

            if (stage == Stage.IN_MINECRAFT)
            {
                ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
                FocusWindow(minecraftProcess);
            }
            else {
                ShowWindow(minecraftProcess, PInvoke.SW_SHOWMINNOACTIVE);
            }
        }

        void LaunchOnboarding()
        {
            onboardVM = new OnboardingViewModel();
            onboardWindow = new OnboardingWindow();
            onboardWindow.DataContext = onboardVM;
            onboardWindow.Show();            
        }

        void UpdateOptiKeyFocusForState(Stage stage)
        {
            if (stage != Stage.IN_MINECRAFT)
            {
                onboardWindow.Activate();
                //mainWindow.Focus();
            }
        }

        void UpdateKeyboardForState(Stage stage)
        {
            if (stage == Stage.IDLE ||
                stage == Stage.ONBOARDING_NO_KEYBOARD)

            {
                ChangeKeyboardIfRequired(disabledKeyboard);
            }
            else
            {
                ChangeKeyboardIfRequired(enabledKeyboard);
            }
        }

        private void ChangeKeyboardIfRequired(string keyboardFileName)
        {
            IKeyboard currentKeyboard = mainViewModel.Keyboard;
            if (mainViewModel.Keyboard is DynamicKeyboard)
            {
                DynamicKeyboard dynamicKeyboard = (DynamicKeyboard)mainViewModel.Keyboard;
                if (dynamicKeyboard.Link == keyboardFileName)
                {
                    return;
                }
            }
            mainViewModel.ProcessChangeKeyboardKeyValue(new ChangeKeyboardKeyValue(keyboardFileName));
        }

        private void OnInfo(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (onboardVM.CurrentPageViewModel is InfoViewModel)
            {
                onboardVM.Resume();                
            }
            else
            {
                onboardVM.ShowOneOffPage(new InfoViewModel());
            }
        }

        private void OnReset(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (onboardVM.CurrentPageViewModel is ResetViewModel)
            {
                onboardVM.Reset();
                stage = Stage.IDLE;
                UpdateForState(stage);
            }
            else {
                onboardVM.ShowOneOffPage(new ResetViewModel());
            }
        }

        private void OnForward(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (stage != Stage.IN_MINECRAFT)
            {
                bool stillOnboarding = onboardVM.NextPage();
                if (!stillOnboarding)
                {
                    stage = Stage.IN_MINECRAFT;
                    onboardVM.Reset();
                }
            }

            UpdateForState(stage);                    
        }

        private void OnBack(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (stage != Stage.IN_MINECRAFT)
            {
                onboardVM.PrevPage();
            }
            UpdateForState(stage);
        }

        //private void OnboardWindowClosed(object sender, EventArgs e)
        //{
        //    if (minecraftProcess != null)
        //    {
        //        ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
        //        FocusWindow(minecraftProcess);
        //        string enabledKeyboard = @"C:\Users\Kirsty\AppData\Roaming\SpecialEffect\EyeMineV2\Keyboards\EyeTracker\museum.xml";
        //        mainViewModel.ProcessChangeKeyboardKeyValue(new ChangeKeyboardKeyValue(enabledKeyboard));
        //    }
        //    stage = Stage.ONBOARDING_WITH_KEYBOARD;
        //    UpdateForState(stage);
        //}


        public static void ShowWindow(Process process, int SHOW_INT)
        {
            if (process == null) { return; }

            IDictionary<IntPtr, string> windows = List_Windows_By_PID(process.Id);
            foreach (KeyValuePair<IntPtr, string> pair in windows)
            {
                bool b = PInvoke.ShowWindow(pair.Key, SHOW_INT);

            }
        }

        public static void FocusWindow(Process process)
        {
            if (process == null) { return; }

            IDictionary<IntPtr, string> windows = List_Windows_By_PID(process.Id);
            // FIXME: which one? the parent? are there ever more than one?
            foreach (KeyValuePair<IntPtr, string> pair in windows)
            {
                bool b = PInvoke.SetForegroundWindow(pair.Key);
            }
        }

        public static void CloseWindow(Process process)
        {
            if (process == null) { return; }

            IDictionary<IntPtr, string> windows = List_Windows_By_PID(process.Id);
            foreach (KeyValuePair<IntPtr, string> pair in windows)
            {
                var placement = new PInvoke.WINDOWPLACEMENT();
                PInvoke.GetWindowPlacement(pair.Key, ref placement);

                if (placement.showCmd == PInvoke.SW_SHOWMINIMIZED)
                {
                    //if minimized, show maximized
                    PInvoke.ShowWindowAsync(pair.Key, PInvoke.SW_SHOWMAXIMIZED);
                }
                else
                {
                    //default to minimize
                    PInvoke.ShowWindowAsync(pair.Key, PInvoke.SW_SHOWMINIMIZED);
                }
            }
        }

        public static void CloseWindow(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                foreach (var process in processes)
                {
                    CloseWindow(process);
                }
            }
        }

        public static IDictionary<IntPtr, string> List_Windows_By_PID(int processID)
        {
            IntPtr hShellWindow = PInvoke.GetShellWindow();
            Dictionary<IntPtr, string> dictWindows = new Dictionary<IntPtr, string>();

            PInvoke.EnumWindows(delegate (IntPtr hWnd, int lParam)
            {
                //ignore the shell window
                if (hWnd == hShellWindow)
                {
                    return true;
                }

                //ignore non-visible windows
                if (!PInvoke.IsWindowVisible(hWnd))
                {
                    return true;
                }

                //ignore windows with no text
                int length = PInvoke.GetWindowTextLength(hWnd);
                if (length == 0)
                {
                    return true;
                }

                int windowPid;
                PInvoke.GetWindowThreadProcessId(hWnd, out windowPid);

                //ignore windows from a different process
                if (windowPid != processID)
                {
                    return true;
                }

                StringBuilder stringBuilder = new StringBuilder(length);
                PInvoke.GetWindowText(hWnd, stringBuilder, length + 1);
                dictWindows.Add(hWnd, stringBuilder.ToString());

                return true;

            }, 0);

            return dictWindows;
        }

        public void DebugShortcut(int i)
        {
            if (i == 0)
            {
                if (mainViewModel != null)
                {
                    String keyboard = @"C:\Users\Kirsty\AppData\Roaming\SpecialEffect\EyeMineV2\Keyboards\EyeTracker\museumDisabled.xml";
                    mainViewModel.ProcessChangeKeyboardKeyValue(new ChangeKeyboardKeyValue(keyboard));
                }
            }
            else if (i == 1)
            {
                // RESET DEMO

                // Copy world file 
                //fixme: delete first?
                string installedSavesDir = @"C:\Program Files (x86)\SpecialEffect\EyeMineExhibit\ModInstaller\saves";
                string minecraftSavesDir = @"C:\Users\Kirsty\AppData\Roaming\.minecraft\EyeMineExhibition\saves";
                string worldName = "Tutorial";
                DirectoryCopy(Path.Combine(installedSavesDir, worldName),
                    Path.Combine(minecraftSavesDir, worldName),
                    true,
                    true);

                // back keyboard to disabled 
                string enabledKeyboard = @"C:\Users\Kirsty\AppData\Roaming\SpecialEffect\EyeMineV2\Keyboards\EyeTracker\museum.xml";
                mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.BackFromKeyboard));
            }
            else if (i == 2)
            {
                TobiiEyeXPointService.EyeXHost.LaunchGuestCalibration();
            }
            else if (i == 3)
            {
            }
            else if (i == 4)
            {
            }
            else if (i == 5)
            {
                FocusWindow(minecraftProcess);
            }
            else if (i == 6)
            {

            }
            else if (i == 7)
            {
            }
            else if (i == 8)
            {
            }
            else if (i == 9)
            {
            }
        }


        //FIXME: dupe code
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

    }
}
