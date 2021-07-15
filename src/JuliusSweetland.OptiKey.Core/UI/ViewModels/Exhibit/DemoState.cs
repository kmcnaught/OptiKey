using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Static;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Base;
using JuliusSweetland.OptiKey.UI.Views.Exhibit;
using log4net;
using Microsoft.Win32;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    public class DemoState
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private OnboardingWindow onboardWindow;
        private Process minecraftProcess;
        private MainViewModel mainViewModel;
        private OnboardingViewModel onboardVM;
        private DispatcherTimer keyDebounceTimer;

        private static Process ghostProcess;
        private static ProcessStartInfo ghostStartInfo;

        enum Stage
        {
            IDLE,
            ONBOARDING_NO_KEYBOARD,
            ONBOARDING_WITH_KEYBOARD,
            TEMP_SCREEN,
            IN_MINECRAFT
        }

        private Stage stage;
        private Stage prevStage;

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
            GetOrLaunchMinecraft();
            stage = Stage.IDLE;
            UpdateForState(stage);

            // Set up for ghost
            ghostStartInfo = new ProcessStartInfo(@"C:\Program Files (x86)\Tobii\Tobii EyeX Interaction\GazeNative8.exe");
            ghostStartInfo.UseShellExecute = true;
            ghostStartInfo.CreateNoWindow = true;
            ghostStartInfo.CloseOnApplicationExit(Log, "Tobii Gaze Overlay");

            // Poll regularly to ensure Minecraft doesn't steal focus at inappropriate time

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += TimerTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();

            int debounceMs = 500;
            keyDebounceTimer = new DispatcherTimer();
            keyDebounceTimer.Tick += AllowKeyPress;
            keyDebounceTimer.Interval = new TimeSpan(0, 0, 0, 0, debounceMs);           
            
        }

        public static void SetGhostVisible(bool visible)
        {
            if (visible && ghostProcess == null)
            {
                ghostProcess = Process.Start(ghostStartInfo);
            }
            else if (!visible && ghostProcess != null)
            {
                ghostProcess.CloseMainWindow();
                ghostProcess = null;
            }
        }

        public static string EnabledKeyboard
        {
            get
            {
                var applicationDataPath = DiagnosticInfo.GetAppDataPath(@"Keyboards");
                return Path.Combine(applicationDataPath, @"EyeTracker\museum.xml");
            }
        }

        public static string DisabledKeyboard
        {
            get
            {
                var applicationDataPath = DiagnosticInfo.GetAppDataPath(@"Keyboards");
                return Path.Combine(applicationDataPath, @"EyeTracker\museumDisabled.xml");
            }
        }

        private void GetOrLaunchMinecraft()
        {
            //Settings.Default.MinecraftCommand = "";
            //Settings.Default.Save();

            if (String.IsNullOrEmpty(Settings.Default.MinecraftCommand))
            {
                // Grab Minecraft command (first time)
                minecraftProcess = CaptureMinecraftProcess();

                if (minecraftProcess == null)
                {
                    if (MessageBox.Show("No minecraft instance saved.\n\nEyeMine will quit now. \n\nPlease launch the correct Minecraft instance before restarting so EyeMine can learn to launch it automatically.",
                         "No Minecraft instance found. ",
                         MessageBoxButton.OK) == MessageBoxResult.OK)
                    {
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    // Set up shell launcher
                    if (Environment.UserName.Contains("EyeMine"))
                    {
                        SetAsShellApp(true);

                        if (MessageBox.Show("Saving Minecraft instance...\n\n EyeMine can now launch Minecraft itself. Please log out then back in again",
                         "Capturing Minecraft instance ... ",
                         MessageBoxButton.OK) == MessageBoxResult.OK)
                        {
                            Application.Current.Shutdown();
                        }
                    }
                    else
                    {
                        if (MessageBox.Show("Saving Minecraft instance...\n\n Please close Minecraft and then click OK for EyeMine to quit. Next time EyeMine will launch Minecraft itself",
                         "Capturing Minecraft instance ... ",
                         MessageBoxButton.OK) == MessageBoxResult.OK)
                        {
                            Application.Current.Shutdown();
                        }
                    }
                }
            }
            else
            {

                // Launch Minecraft

                String fullCmd = Settings.Default.MinecraftCommand;
                if (!String.IsNullOrEmpty(fullCmd))
                {
                    string javapath1 = @"C:\Program Files (x86)\Minecraft Launcher\runtime\jre-legacy\windows-x64\jre-legacy\bin\javaw.exe";

                    // args need to come from a previous run of Minecraft with valid credentials
                    String cmdTextFile = AppDomain.CurrentDomain.BaseDirectory + @"\Resources\minecraftCommand.txt";
                    string minecraftArgs1 = File.ReadAllText(cmdTextFile);


                    string[] parts = fullCmd.SplitAtEndOfSubstring("javaw.exe\"");
                    string javaPath = parts[0];
                    string minecraftArgs = parts[1].TrimStart();

                    // Fix mid-argument whitespace
                    string find = "Windows 10";
                    string replace = "Windows\\ 10";
                    minecraftArgs = minecraftArgs.Replace(find, replace);

                    minecraftProcess = Process.Start(new ProcessStartInfo(javaPath, minecraftArgs));
                    minecraftProcess.CloseOnApplicationExit(Log, "Minecraft");
                }
            }
        }

        public static Process CaptureMinecraftProcess()
        {
            Process capturedProcess = null;
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.ToLower().Contains("javaw"))
                {
                    string cmd = p.GetCommandLine();
                    if (cmd.Contains(".minecraft") && cmd.Contains("EyeMineExhibit"))
                    {
                        // Launcher creates temporary bin dir, we want to make a copy for reuse
                        Regex regex = new Regex(@"-Djava.library.path=(.*.minecraft\\bin\\)([0-9a-f\-]*)");
                        Match match = regex.Match(cmd);
                        if (match.Groups.Count >= 3)
                        {
                            String tempDir = match.Groups[1].Value + match.Groups[2].Value;
                            String newDir = match.Groups[1].Value + "EyeMineExhibit";
                            DirectoryCopy(tempDir, newDir, true, true);

                            cmd = cmd.Replace(tempDir, newDir);

                            // Save command for next time
                            capturedProcess = p;
                            Settings.Default.MinecraftCommand = cmd;
                            Settings.Default.Save();
                        }
                        else
                        {
                            Log.ErrorFormat("Couldn't recognise bin directory in minecraft command: {0}", cmd);
                        }
                    }
                }
            }

            return capturedProcess;
        }

        public static void SetAsShellApp(bool useAsShell)
        {
            RegistryKey Key = Registry.CurrentUser;
            Key = Key.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\", true);

            if (useAsShell)
            {
                Key.SetValue("Shell", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            }
            else
            {
                Key.SetValue("Shell", "");
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (stage != Stage.TEMP_SCREEN)
            {
                UpdateOptiKeyFocusForState(stage);
            }
        }

        private void AllowKeyPress(object sender, EventArgs e)
        {
            keyDebounceTimer.Stop();
        }

        void UpdateForState(Stage stage)
        {
            prevStage = this.stage;
            this.stage = stage;
            UpdateMinecraftFocusForState(stage);
            UpdateOptiKeyFocusForState(stage);
            UpdateKeyboardForState(stage);
            UpdateGhostForState(stage);
        }

        void UpdateGhostForState(Stage stage)
        {
            if (stage == Stage.IN_MINECRAFT || stage == Stage.ONBOARDING_WITH_KEYBOARD)
            {
                SetGhostVisible(true);
            }
            else
            {
                SetGhostVisible(false);
            }
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
            onboardWindow.Closed += (s, e) => { Application.Current.Shutdown(); };
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
                ChangeKeyboardIfRequired(DisabledKeyboard);
            }
            else
            {
                ChangeKeyboardIfRequired(EnabledKeyboard);
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
            if (keyDebounceTimer.IsEnabled) { return; }
            keyDebounceTimer.Start();

            if (onboardVM.CurrentPageViewModel is InfoViewModel)
            {
                onboardVM.Resume();
                UpdateForState(prevStage);
            }
            else
            {
                onboardVM.ShowOneOffPage(new InfoViewModel());
                UpdateForState(Stage.TEMP_SCREEN);
            }
        }

        private void OnReset(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (keyDebounceTimer.IsEnabled) { return;  }
            keyDebounceTimer.Start();
            if (onboardVM.CurrentPageViewModel is ResetViewModel)
            {
                //bool needResetMinecraft = stage == Stage.IN_MINECRAFT;
                if (stage == Stage.IN_MINECRAFT || 
                    (stage == Stage.TEMP_SCREEN && prevStage == Stage.IN_MINECRAFT) )
                {
                    ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
                    FocusWindow(minecraftProcess);
                    Thread.Sleep(50);
                    // Send shortcut to M/C to unload world
                    mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.F8));
                    Thread.Sleep(50);
                    // Reset world files
                    ResetMinecraftWorldFile();
                    Thread.Sleep(50);
                    // Send shortcut to M/C to reload
                    mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.F9));
                    Thread.Sleep(50);
                }

                onboardVM.Reset();
                stage = Stage.IDLE;
                UpdateForState(stage);                
            }
            else {
                onboardVM.ShowOneOffPage(new ResetViewModel());
                UpdateForState(Stage.TEMP_SCREEN);
            }
        }

        private void OnForward(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (keyDebounceTimer.IsEnabled) { return; }
            keyDebounceTimer.Start();

            // Don't go forward while calibrating
            // TODO: for more robustness, keep track of calibration request and
            // identify any other configuring times, e.g. eye tracker reconnecting
            if (IsTobiiCalibrating())
            {
                // Press Enter to hasten process
                mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.Return));
            }

            if (stage != Stage.IN_MINECRAFT)
            {
                bool stillOnboarding = onboardVM.NextPage();
                if (!stillOnboarding)
                {
                    stage = Stage.IN_MINECRAFT;
                    onboardVM.Reset();
                    ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
                    FocusWindow(minecraftProcess);
                    Thread.Sleep(50);
                    mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.Return));
                }
            }

            UpdateForState(stage);                    
        }

        private bool IsTobiiCalibrating()
        {
            // FIXME: what happens if Tobii fails here?
            return onboardVM.WaitingForCalibration;
            //return Process.GetProcessesByName("Tobii.EyeX.Configuration").Length > 0;
        }

        private void OnBack(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (keyDebounceTimer.IsEnabled) { return; }
            keyDebounceTimer.Start();

            if (IsTobiiCalibrating())
            {
                // Press Esc to exit
                mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.Escape));
            }

            if (stage == Stage.TEMP_SCREEN)
            {
                onboardVM.Resume();
                UpdateForState(prevStage);
            }
            else if (stage != Stage.IN_MINECRAFT)
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

        private void ResetMinecraftWorldFile()
        {
            //fixme: delete first?
            string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string installedPath = AppContext.BaseDirectory;

            string installedSavesDir = Path.Combine(installedPath, @"ModInstaller\saves");
            string minecraftSavesDir = Path.Combine(applicationDataPath, @".minecraft\EyeMineExhibition\saves");
            string worldName = "Tutorial";
            DirectoryCopy(Path.Combine(installedSavesDir, worldName),
                Path.Combine(minecraftSavesDir, worldName),
                true,
                true);

        }

        public void DebugShortcut(int i)
        {
            if (i == 0)
            {
                if (mainViewModel != null)
                {
                    String keyboard = DisabledKeyboard;
                    mainViewModel.ProcessChangeKeyboardKeyValue(new ChangeKeyboardKeyValue(keyboard));
                }
            }
            else if (i == 1)
            {
                // RESET DEMO

                ResetMinecraftWorldFile();

                // back keyboard to disabled 
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
