using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Static;
using JuliusSweetland.OptiKey.UI.Utilities;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Base;
using JuliusSweetland.OptiKey.UI.Views.Exhibit;
using log4net;
using Microsoft.Win32;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static JuliusSweetland.OptiKey.Native.PInvoke;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    public class Demo
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private OnboardingWindow onboardWindow;
        private Process minecraftProcess;
        private MinecraftWatcher minecraftWatcher;
        private MainViewModel mainViewModel;
        private OnboardingViewModel onboardVM;
        private bool minecraftHasLoaded = false;

        private DispatcherTimer minecraftLoadingTimer;
        private DispatcherTimer focusTimer = new DispatcherTimer();

        private static Process ghostProcess;
        private static ProcessStartInfo ghostStartInfo;
        
        public Demo(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;

            ResetMinecraftWorldFile();

            LaunchOnboarding();

            bool noRepeat = true;
            HotkeyManager.Current.AddOrReplace("Back", Key.Left, ModifierKeys.None, noRepeat, DebouncedAction.CreateDebouncedAction(OnBack, 500));
            HotkeyManager.Current.AddOrReplace("Forward", Key.Right, ModifierKeys.None, noRepeat, DebouncedAction.CreateDebouncedAction(OnForward, 500));
            HotkeyManager.Current.AddOrReplace("Reset", Key.Down, ModifierKeys.None, noRepeat, DebouncedAction.CreateDebouncedAction(OnReset, 500));
            HotkeyManager.Current.AddOrReplace("Info", Key.Up, ModifierKeys.None, noRepeat, DebouncedAction.CreateDebouncedAction(OnInfo, 500));

            // Launch Minecraft
            GetOrLaunchMinecraft();
            UpdateForState();

            // Set up for ghost
            // May be in one of two places
            String ghostFilename = @"C:\Program Files (x86)\Tobii\Tobii EyeX Interaction\GazeNative8.exe";
            if (!File.Exists(ghostFilename))
            {
                ghostFilename = @"C:\Program Files\Tobii\Tobii EyeX\GazeNative8.exe";
            }
            if (!File.Exists(ghostFilename))
            {
                MessageBox.Show("Error finding GazeNative app for overlay");
            }

            ghostStartInfo = new ProcessStartInfo(ghostFilename);
            ghostStartInfo.UseShellExecute = true;
            ghostStartInfo.CreateNoWindow = true;

            // Poll regularly to ensure Minecraft doesn't steal focus at inappropriate time
            // This is important during initial M/C loading where it forces focus
            minecraftLoadingTimer = new DispatcherTimer();
            minecraftLoadingTimer.Tick += (object sender, EventArgs e) =>
            {
                if (!onboardVM.IsContextMenuOpen)
                {
                    UpdateOptiKeyFocusForState();
                }
            };
            minecraftLoadingTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            minecraftLoadingTimer.Start();            
            
            // slight hack: poll to ensure correct focus at all times 
            // (state machine _should_ handle this but there are some corner cases not handled)
            focusTimer.Tick += (object sender, EventArgs e) =>
            {
                if (!onboardVM.IsContextMenuOpen)
                {
                    // We don't take focus away from calibration, otherwise we can't send key presses there
                    if (onboardVM.mainState != OnboardingViewModel.OnboardState.WAIT_CALIB)
                    {   
                        UpdateForState();
                    }
                }
            };
            focusTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            focusTimer.Start();
     
        }

        public static void SetGhostVisible(bool visible)
        {
            if (visible && ghostProcess == null)
            {
                ghostProcess = Process.Start(ghostStartInfo);
                ghostProcess.CloseOnApplicationExit(Log, "Tobii Gaze Overlay");
            }
            else if (!visible && ghostProcess != null)
            {
                ghostProcess.CloseMainWindow();
                ghostProcess = null;

                // TODO: close any other matching processes in case orphaned?
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
                // Close any processes orphaned by previous runs
                CloseProcesses();

                // Launch Minecraft

                String fullCmd = Settings.Default.MinecraftCommand;
                if (!String.IsNullOrEmpty(fullCmd))
                {
                    string javapath1 = @"C:\Program Files (x86)\Minecraft Launcher\runtime\jre-legacy\windows-x64\jre-legacy\bin\javaw.exe";

                    // args need to come from a previous run of Minecraft with valid credentials
                    //FIXME: remove unused dupe
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

                    minecraftWatcher = new MinecraftWatcher(minecraftProcess);
                    minecraftWatcher.MinecraftCrashed += (s, e) =>
                    {
                        onboardVM.SetUnrecoverableError();
                    };
                    minecraftWatcher.MinecraftLoaded += (s, e) =>
                    {
                        if (!minecraftHasLoaded)
                        {
                            // First time we briefly maximise the window to avoid jump glitch later
                            ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
                            ShowWindow(minecraftProcess, PInvoke.SW_SHOWMINNOACTIVE);
                            minecraftHasLoaded = true;
                            minecraftLoadingTimer.Stop();
                        }
                        onboardVM.SetLoadingComplete();
                    };
                }
            }
        }

        public static void CloseProcesses()
        {
            // on startup, close any orphaned minecraft or ghost processes            
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.ToLower().Contains("javaw"))
                {
                    string cmd = p.GetCommandLine();
                    if (cmd.Contains(".minecraft") && cmd.Contains("EyeMineExhibit"))
                    {
                        KillProcess(p, 1000);
                    }
                }
                else if (p.ProcessName.Contains("GazeNative8"))
                {
                    KillProcess(p, 1000);
                }
            }
        }

        private static void KillProcess(Process p, int waitTimeoutMs)
        {
            bool success = p.CloseMainWindow();
            if (success)
            {
                success = p.WaitForExit(waitTimeoutMs);
            }
            if (!success) {
                p.Kill();
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
            Log.Info($"SetAsShellApp? {useAsShell}");

            RegistryKey Key = Registry.CurrentUser;
            Key = Key.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\", true);

            Log.Info($"Registry Key: {Key.ToString()}");

            if (useAsShell)
            {
                Key.SetValue("Shell", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            }
            else
            {
                Key.SetValue("Shell", "");
            }
        }        

        private OnboardingViewModel.OnboardState lastState = OnboardingViewModel.OnboardState.WELCOME;
        private DateTime lastStateChangeTime = DateTime.Now;

        void UpdateForState()
        {                  
            if (onboardVM.mainState != lastState)
            {
                lastStateChangeTime = DateTime.Now;
                lastState = onboardVM.mainState;
            }

            // Check for idle, reset if necessary
            TimeSpan idleTimeSpan = TimeSpan.FromMinutes(1.5);
            TimeSpan idleTimeSpanCalibration = TimeSpan.FromMinutes(3); // TODO: test this, see how long is reasonable
            if (onboardVM.mainState != OnboardingViewModel.OnboardState.IN_MINECRAFT)
            {
                if ((DateTime.Now.Subtract(lastStateChangeTime) > idleTimeSpanCalibration &&
                    onboardVM.mainState == OnboardingViewModel.OnboardState.WAIT_CALIB) ||
                    (DateTime.Now.Subtract(lastStateChangeTime) > idleTimeSpan &&
                    onboardVM.mainState != OnboardingViewModel.OnboardState.WAIT_CALIB))
                {
                    AutoReset();
                    return;
                }
            }

            UpdateKeyboardForState();
            UpdateOptiKeyFocusForState();
            UpdateMinecraftFocusForState();
            UpdateGhostForState();
        }

        void UpdateGhostForState()
        {
            if (onboardVM.tempState == OnboardingViewModel.TempState.NONE &&
                (onboardVM.mainState == OnboardingViewModel.OnboardState.POST_CALIB ||
                 onboardVM.mainState == OnboardingViewModel.OnboardState.IN_MINECRAFT))
            { 
                SetGhostVisible(true);
            }
            else
            {
                SetGhostVisible(false);
            }
        }

        void UpdateMinecraftFocusForState()
        {
            if (minecraftProcess == null) { return; }

            double h = Graphics.PrimaryScreenHeightInPixels;
            double w = Graphics.PrimaryScreenWidthInPixels;

            double dockHeight = mainViewModel.MainWindowManipulationService.GetFullDockThicknessAsPercentageOfScreen();
            double minecraftHeight = 100 - dockHeight;           

            Native.Common.Structs.RECT rect = new Native.Common.Structs.RECT(0, 0, (int)w, (int)(h * minecraftHeight / 100));
            
            if (onboardVM.tempState == OnboardingViewModel.TempState.NONE &&
                onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING &&
                onboardVM.mainState == OnboardingViewModel.OnboardState.IN_MINECRAFT) {            
                ShowWindow(minecraftProcess, PInvoke.SW_SHOWNORMAL, rect);
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
            onboardVM.StateChanged += (s,e) => UpdateForState();
            onboardVM.RequireAutoReset += (s, e) => AutoReset();
        }

        void UpdateOptiKeyFocusForState()
        {
            // Make sure height fits alongside keyboard
            int height = (int)Graphics.PrimaryScreenHeightInPixels;
            if (ShowEyeMineKeyboard())
            {
                double dockHeight = mainViewModel.MainWindowManipulationService.GetFullDockThicknessAsPercentageOfScreen();
                double minecraftHeight = 100 - dockHeight;

                height -= (int)(height * minecraftHeight / 100);
            }
            onboardWindow.Height = height;

            if (onboardVM.tempState != OnboardingViewModel.TempState.NONE ||
                onboardVM.demoState != OnboardingViewModel.DemoState.RUNNING ||
                onboardVM.mainState != OnboardingViewModel.OnboardState.IN_MINECRAFT)
            {
                onboardWindow.Activate();
                onboardWindow.Focus();
                //mainWindow.Focus();
            }
        }

        private bool ShowEyeMineKeyboard()
        {
            return (onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING ||
                 onboardVM.demoState == OnboardingViewModel.DemoState.NO_USER) &&
                onboardVM.mainState == OnboardingViewModel.OnboardState.IN_MINECRAFT &&
                onboardVM.tempState == OnboardingViewModel.TempState.NONE;
        }

        void UpdateKeyboardForState()
        {
            //FIXME: if in minecraft + no user do we want other keyboard? for e.g. temporary lost tracking
            if (ShowEyeMineKeyboard())
            {
                ChangeKeyboardIfRequired(EnabledKeyboard);
            }
            else
            {
                ChangeKeyboardIfRequired(DisabledKeyboard);
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
            if ((onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING ||
                 onboardVM.demoState == OnboardingViewModel.DemoState.TIMED_OUT) &&
                 onboardVM.mainState != OnboardingViewModel.OnboardState.WAIT_CALIB)
            {
                onboardVM.Info();
            }
        }

        private void AutoReset()
        {
            this.PerformResetDemo();            
            onboardVM.ResetViewModel();           
            UpdateForState();
        }

        private void PerformResetDemo()
        {
            // Reset world files (this will silently fail on the 'open' one but we'll swap to the fresh one)
            ResetMinecraftWorldFile();

            // Tell Minecraft to reset 
            ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
            FocusWindow(minecraftProcess);
            Thread.Sleep(100);
            mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.F9));
        }

        private void OnReset(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING ||
                onboardVM.demoState == OnboardingViewModel.DemoState.NO_USER ||
                onboardVM.demoState == OnboardingViewModel.DemoState.TIMED_OUT)
            {
                // update state appropriately
                onboardVM.Reset();
                UpdateForState();
            }
        }

        private void OnForward(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING)
            {
                // Don't go forward while calibrating
                // TODO: for more robustness, keep track of calibration request and
                // identify any other configuring times, e.g. eye tracker reconnecting
                if (IsTobiiCalibrating())
                {
                    // Press Enter to hasten process
                    mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.Return));
                }
                else
                {
                    // In minecraft, turn into keypress to start demo (if it hasn't managed to start automatically)
                    if (onboardVM.mainState == OnboardingViewModel.OnboardState.IN_MINECRAFT)
                    {
                        mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.Return));
                    }

                    // Pass request down
                    onboardVM.Next();
                    UpdateForState();
                }
            }
        }

        private bool IsTobiiCalibrating()
        {
            // FIXME: what happens if Tobii fails here?
            return onboardVM.mainState == OnboardingViewModel.OnboardState.WAIT_CALIB;
            //return Process.GetProcessesByName("Tobii.EyeX.Configuration").Length > 0;
        }

        private void OnBack(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (IsTobiiCalibrating())
            {
                // Press Esc to exit
                // NB: depending on timing this might not catch the calibrating process
                mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.Escape));
                onboardVM.SetState(OnboardingViewModel.OnboardState.EYES);
                return;
            }

            if (onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING ||
                onboardVM.demoState == OnboardingViewModel.DemoState.NO_USER)
            {
                onboardVM.Back();
            }
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

        public static void ShowWindow(Process process, int SHOW_INT, Native.Common.Structs.RECT rect)
        {
            if (process == null) { return; }            
            IDictionary<IntPtr, string> windows = List_Windows_By_PID(process.Id);

            foreach (KeyValuePair<IntPtr, string> pair in windows)
            {
                WINDOWPLACEMENT placement = WINDOWPLACEMENT.Default;
                placement.Length = Marshal.SizeOf(placement);
                GetWindowPlacement(pair.Key, ref placement);

                placement.NormalPosition = rect;
                placement.Length = Marshal.SizeOf(placement);
                
                bool b2 = PInvoke.SetWindowPlacement(pair.Key, ref placement);
                bool b = PInvoke.ShowWindow(pair.Key, SHOW_INT);                                
            }
        }

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

                if (placement.ShowCmd == PInvoke.SW_SHOWMINIMIZED)
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
            // Make two copies we can switch between
            DirectoryCopy(Path.Combine(installedSavesDir, worldName),
                Path.Combine(minecraftSavesDir, worldName),
                true,
                true);
            DirectoryCopy(Path.Combine(installedSavesDir, worldName),
               Path.Combine(minecraftSavesDir, worldName + "2"),
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
