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
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private TobiiWatcher tobiiWatcher;
        private MainViewModel mainViewModel;
        private OnboardingViewModel onboardVM;
        private bool minecraftHasLoaded = false;

        private DispatcherTimer minecraftLoadingTimer;
        private DispatcherTimer focusTimer = new DispatcherTimer();
        private DispatcherTimer idleTimer = new DispatcherTimer();
        private DispatcherTimer resetPageTimer = new DispatcherTimer();

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
            String ghostFilename = FindGhostFilename();
            if (String.IsNullOrEmpty(ghostFilename))
            {                
                MessageBox.Show("Error finding overlay app, please install Tobii Ghost");
            }

            ghostStartInfo = new ProcessStartInfo(ghostFilename);
            ghostStartInfo.UseShellExecute = true;
            ghostStartInfo.CreateNoWindow = true;

            // Poll regularly to ensure Minecraft doesn't steal focus at inappropriate time
            // This is important during initial M/C loading where it forces focus
            minecraftLoadingTimer = new DispatcherTimer();
            minecraftLoadingTimer.Tick += (object sender, EventArgs e) =>
            {
                if (!onboardVM.IsContextMenuOpen && !TaskManagerProcessIsRunning())
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
                if (!onboardVM.IsContextMenuOpen && !mainViewModel.IsToastOpen && !TaskManagerProcessIsRunning())
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

            idleTimer.Tick += (object sender, EventArgs e) => { CheckIdle(); };
            idleTimer.Interval = new TimeSpan(0, 0, 5);
            idleTimer.Start();
             
            resetPageTimer.Interval = new TimeSpan(0, 0, 3);
            resetPageTimer.Tick += (s, e) => { CompleteAutoReset(); };

        }

        private string FindGhostFilename()
        {
            string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            List<string> locations = new List<string>() {
                @"C:\Program Files (x86)\Tobii\",
                @"C:\Program Files\Tobii\",
                Path.Combine(appDataLocal, "TobiiGhost"),
                Path.Combine(appDataLocal, "Tobii"),
                Path.Combine(appDataLocal, "TobiiAB")
            };
            List<string> filenames = new List<string>() {
                "GazeNative8.exe",    // old version in Program Files
                "PreviewOverlay.exe", // new version in Local AppData
                //"SSOverlay.exe"
            };

            foreach (var location in locations)
            {
                foreach (var filename in filenames)
                {
                    try
                    {
                        var files = Directory.GetFiles(location, filename, SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            Log.Info($"Found Ghost EXE: {file}");
                            return file;
                        }
                    }
                    catch (DirectoryNotFoundException e)
                    { 
                        // noop - we don't expect all dirs to exist
                    }
                    catch (Exception e)
                    {
                        Log.Info($"{e.GetType()} : {e.Message}");
                    }
                }
            }

            return null;
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
                ghostProcess = null;
                ghostProcess = null;

                // for some reason, the ghostProcess reports as
                // exited, but another orphaned ghost process does
                // persist, so we kill any we find
                CloseGhostProcesses();                
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

                        if (MessageBox.Show("Saving Minecraft instance...\n\n EyeMine can now launch Minecraft itself. Please restart the PC.",
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
                Console.WriteLine("Launching Minecraft...");

                // Force refresh of minecraft config dir (fml.toml is getting corrupted for some reason)
                // 01/02/22: We've replaced this with resetting to known good state via the launcher console app
                //DeleteMinecraftConfig();

                // Always ensure shell app (might have been undone for installing new version)
                SetAsShellApp(true);

                // Close any processes orphaned by previous runs
                CloseProcesses();

                // Launch Minecraft

                String fullCmd = Settings.Default.MinecraftCommand;
                if (!String.IsNullOrEmpty(fullCmd))
                {
                    Log.Info($"Full command: {fullCmd}");

                    // java path may be quoted, in which case we take the quotes. 
                    string javaPath = "";
                    string minecraftArgs = "";
                    string[] parts;
                    if (fullCmd.Contains("javaw.exe\""))
                    {
                        parts = fullCmd.SplitAtEndOfSubstring("javaw.exe\"");
                        javaPath = parts[0];
                        minecraftArgs = parts[1].TrimStart();
                    }
                    else if (fullCmd.Contains("javaw.exe")) { 
                        parts = fullCmd.SplitAtEndOfSubstring("javaw.exe");
                        javaPath = parts[0];
                        minecraftArgs = parts[1].TrimStart();
                    }
                    else
                    {
                        Log.Error("Cannot parse minecraft command"); //TODO: what do we do then?
                        onboardVM.SetUnrecoverableError();
                        return;
                    }

                    Log.Info($"Java path: {javaPath}");
                    Log.Info($"Args: {minecraftArgs}");

                    // Fix mid-argument whitespace
                    //FIXME: make this generic? it's when there's explicit double quotes, so e.g.
                    // \".* .*\" or something
                    string find = "Windows 10";
                    string replace = "Windows\\ 10";
                    minecraftArgs = minecraftArgs.Replace(find, replace);
                    find = "Windows 11"; // just in case this changes.. ! currently is "Windows 10" even on Windows 11
                    replace = "Windows\\ 11";
                    minecraftArgs = minecraftArgs.Replace(find, replace);

                    try
                    {
                        string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string minecraftDir = Path.Combine(applicationDataPath, @".minecraft\EyeMineExhibition\");

                        ProcessStartInfo startInfo = new ProcessStartInfo(javaPath, minecraftArgs)
                        {
                            WorkingDirectory = minecraftDir,
                        };
                        minecraftProcess = Process.Start(startInfo);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to start Minecraft process: {ex.Message}");
                        onboardVM.SetUnrecoverableError();
                        return;
                    }

                    if (minecraftProcess == null)
                    {
                        Log.Error("Minecraft process is null");
                        onboardVM.SetUnrecoverableError();
                        return;
                    }
                    
                    minecraftProcess.CloseOnApplicationExit(Log, "Minecraft");

                    minecraftWatcher = new MinecraftWatcher(minecraftProcess);
                    minecraftWatcher.MinecraftCrashed += (s, e) =>
                    {
                        Console.WriteLine("Minecraft has crashed");
                        onboardVM.SetUnrecoverableError();
                        return;
                    };                    
                    
                    minecraftWatcher.MinecraftLoaded += (s, e) =>
                    {
                        Console.WriteLine("Minecraft has finished loading");
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

                    tobiiWatcher = new TobiiWatcher();
                    tobiiWatcher.EnteredErrorState += TobiiWatcher_EnteredErrorState;
                    tobiiWatcher.EnteredTrackingState += TobiiWatcher_EnteredTrackingState;
                    tobiiWatcher.RecoveredErrorState += TobiiWatcher_RecoveredErrorState;
                }
            }
        }

        private void DeleteMinecraftConfig()
        {
            var  appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);            
            var minecraftPath = Path.Combine(appDataPath, ".minecraft");
            var configPath = Path.Combine(minecraftPath, "EyeMineExhibition", "config");
            if (Directory.Exists(configPath))
            {
                try
                {
                    Directory.Delete(configPath);
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Error deleting config folder: {0}", configPath);
                    Log.Error(e.ToString());
                }
            }
        }

        private void TobiiWatcher_RecoveredErrorState(object sender, EventArgs e)
        {            
            Console.WriteLine("Tobii: recovered");
            onboardVM.TobiiRecovery();
        }

        private void TobiiWatcher_EnteredTrackingState(object sender, EventArgs e)
        {
            Console.WriteLine("Tobii: tracking");
            Settings.Default.TobiiErrorCount = 0;
        }

        private void TobiiWatcher_EnteredErrorState(object sender, Tobii.EyeX.Framework.EyeTrackingDeviceStatus e)
        {
            Console.WriteLine($"Tobii: error status {e}");            
            Settings.Default.TobiiErrorCount++;
            onboardVM.TobiiError(e);
        }

        private static void CloseMinecraftProcess()
        {
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.ToLower().Contains("javaw"))
                {
                    string cmd = p.GetCommandLine();
                    if (cmd.Contains(".minecraft") && cmd.Contains("EyeMineExhibit"))
                    {
                        KillProcess(p, 1000, true, "minecraft");
                    }
                }
            }
        }

        private static void CloseGhostProcesses()
        {
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains("GazeNative8") ||
                         p.ProcessName.Contains("SSOverlay") ||
                         p.ProcessName.Contains("PreviewOverlay")
                    )
                {
                    KillProcess(p, 1000, false, "ghost");
                }
            }
        }

        public static void CloseProcesses()
        {
            Console.WriteLine("Closing processes...");
            // on startup, close any orphaned minecraft or ghost processes            
            CloseMinecraftProcess();
            CloseGhostProcesses();
        }

        private static bool KillProcess(Process p, int waitTimeoutMs=1000, bool tryMainWindow=true, string name="")
        {
            bool success = false;
            
            if (tryMainWindow)
                success = p.CloseMainWindow();

            if (success)
            {
                success = p.WaitForExit(waitTimeoutMs);
            }
            else {
                try
                {
                    p.Kill();
                    success = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception killing process ({name})");
                    Log.Error($"Exception killing process ({name})");
                    Log.Error(e.ToString());
                }
            }
            return success;
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
                            Console.WriteLine($"Error: Couldn't recognise bin directory in minecraft command: {cmd}");
                            Log.ErrorFormat("Couldn't recognise bin directory in minecraft command: {0}", cmd);
                        }
                    }
                }
            }

            return capturedProcess;
        }

        public static void QuitLauncher()
        {
            Process p = Process.GetProcessesByName("EyeMineLauncher").FirstOrDefault();
            if (p != null)
            {
                KillProcess(p, 1000, true, "launcher");
            }
        }

        public static void SetAsShellApp(bool useAsShell)
        {
            Log.Info($"SetAsShellApp? {useAsShell}");

            RegistryKey Key = Registry.CurrentUser;
            Key = Key.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\", true);

            Log.Info($"Registry Key: {Key.ToString()}");

            if (useAsShell)
            {   
                // We use the adjacent service rather than launching this app directly
                FileInfo fileInfo = new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);                
                Key.SetValue("Shell", Path.Combine(fileInfo.Directory.ToString(), "EyeMineLauncher.exe"));
            }
            else
            {
                Key.SetValue("Shell", "");
            }
        }        

        private OnboardingViewModel.OnboardState lastState = OnboardingViewModel.OnboardState.WELCOME;
        private DateTime lastStateChangeTime = DateTime.Now;
        
        void CheckIdle()
        {
            if (onboardVM.mainState != lastState)
            {
                lastStateChangeTime = DateTime.Now;
                lastState = onboardVM.mainState;
            }

            if (onboardVM.mainState == OnboardingViewModel.OnboardState.WELCOME)
            {
                return;
            }

            if (!onboardVM.LostTracking())
            {
                return;
            }

            if (onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING)
            {
                // Check for idle, reset if necessary
                TimeSpan idleTimeSpan = TimeSpan.FromMinutes(1.5);
                //TODO: longer on eye gauge page?
                TimeSpan idleTimeSpanCalibTimeout = TimeSpan.FromSeconds(30);
                if (onboardVM.mainState != OnboardingViewModel.OnboardState.IN_MINECRAFT &&
                    onboardVM.mainState != OnboardingViewModel.OnboardState.WAIT_CALIB //(we have separate timeout for this)                    
                    )
                {
                    if (onboardVM.mainState == OnboardingViewModel.OnboardState.CALIB_TIMEOUT &&
                            DateTime.Now.Subtract(lastStateChangeTime) > idleTimeSpanCalibTimeout)
                    {
                        StartAutoReset();
                        return;
                    }
                    else if (DateTime.Now.Subtract(lastStateChangeTime) > idleTimeSpan)
                    {
                        StartAutoReset();
                        return;
                    }
                }
            }
        }

        void UpdateForState()
        {               
            UpdateKeyboardForState();
            UpdateOptiKeyFocusForState();
            UpdateMinecraftFocusForState();
            UpdateGhostForState();
        }

        void UpdateGhostForState()
        {
            if (onboardVM.tempState == OnboardingViewModel.TempState.NONE &&
                (onboardVM.mainState == OnboardingViewModel.OnboardState.CALIB_SUCCESS ||
                 onboardVM.mainState == OnboardingViewModel.OnboardState.IN_MINECRAFT))
            { 
                SetGhostVisible(true);
            }
            else
            {
                SetGhostVisible(false);
            }
        }

        void RaiseToast(string title, string content)
        {
            mainViewModel.RaiseToastNotification(
                   title,
                   content,
                   NotificationTypes.Normal,
                   () => { });
        }

        void UpdateMinecraftFocusForState()
        {
            if (minecraftProcess == null) { return; }
#if DEBUG
            return;
#endif

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
            
            onboardWindow.Height = (int)Graphics.PrimaryScreenHeightInPixels; 
            onboardWindow.Width = (int)Graphics.PrimaryScreenWidthInPixels; 
            onboardWindow.Left = 0;
            onboardWindow.Top = 0;
            onboardWindow.Show();

            onboardVM.StateChanged += (s,e) => UpdateForState();
            onboardVM.RequireAutoReset += (s, e) => StartAutoReset();
            onboardVM.RequireCloseCalibration += (s, e) => TryCloseTobiiCalibration();
            onboardVM.TimeoutWarning += (s, e) => RaiseToast("One minute remaining", "You're doing great!");

        }

        void UpdateOptiKeyFocusForState()
        {
            // Make sure height fits alongside keyboard
            int height = (int)Graphics.PrimaryScreenHeightInPixels;
            int width = (int)Graphics.PrimaryScreenWidthInPixels;
            if (ShowEyeMineKeyboard())
            {
                double dockHeight = mainViewModel.MainWindowManipulationService.GetFullDockThicknessAsPercentageOfScreen();
                double minecraftHeight = 100 - dockHeight;

                height = (int)(height * minecraftHeight / 100);
            }
            onboardWindow.Height = height;

#if DEBUG
            return;
#endif

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
                 onboardVM.demoState != OnboardingViewModel.DemoState.PERM_ERROR_EYETRACKER &&
                 onboardVM.tempState != OnboardingViewModel.TempState.TEMP_ERROR_EYETRACKER &&
                 onboardVM.mainState != OnboardingViewModel.OnboardState.WAIT_CALIB)
            {
                onboardVM.Info();
            }
        }

        private void StartAutoReset()
        {
            Log.Info("Auto Reset");            
            this.PerformResetDemo();             
            onboardVM.StartResetViewModel();           
            UpdateForState();
            resetPageTimer.Start();
        }

        private void CompleteAutoReset()
        {
            resetPageTimer.Stop();
            this.PerformResetDemo();
            onboardVM.CompleteResetViewModel();
            UpdateForState();
        }

        private void PerformResetDemo()
        {
            TryCloseTobiiCalibration();

            // Reset world files (this will silently fail on the 'open' one but we'll swap to the fresh one)
            ResetMinecraftWorldFile();

            // Tell Minecraft to reset 
            ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
            FocusWindow(minecraftProcess);
            Thread.Sleep(100);
            mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.F9));
            Thread.Sleep(100);

        }

        private void OnReset(object sender, NHotkey.HotkeyEventArgs e)
        {
            if ((onboardVM.demoState == OnboardingViewModel.DemoState.RUNNING ||
                onboardVM.demoState == OnboardingViewModel.DemoState.NO_USER ||
                onboardVM.demoState == OnboardingViewModel.DemoState.TIMED_OUT) &&
                 onboardVM.demoState != OnboardingViewModel.DemoState.PERM_ERROR_EYETRACKER &&
                 onboardVM.tempState != OnboardingViewModel.TempState.TEMP_ERROR_EYETRACKER)
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
            return GetTobiiCalibProcess() != null || onboardVM.mainState == OnboardingViewModel.OnboardState.WAIT_CALIB;
        }

        private Process GetTobiiCalibProcess()
        {
            foreach (Process p in System.Diagnostics.Process.GetProcesses())
            {
                // depending on exact tobii install the process name could be subtly different
                // e.g. Tobii.EyeX.Configuration vs Tobii.Core.Config vs Tobii.Configuration
                string lowerName = p.ProcessName.ToLower();
                if (lowerName.Contains("tobii") && lowerName.Contains("config"))
                {
                    return p;
                }
            }

            return null;
        }

        public void TryCloseTobiiCalibration()
        {
            Process tobiiProcess = GetTobiiCalibProcess();
            if (tobiiProcess != null) { 
                int timeoutSeconds = 10;
                bool success = KillProcess(tobiiProcess, timeoutSeconds*1000, true, "tobii calibration");
                if (!success)
                {
                    FocusWindow(tobiiProcess);
                    mainViewModel.HandleFunctionKeySelectionResult(new KeyValue(FunctionKeys.Escape));
                }
            }
        }

        private static bool TaskManagerProcessIsRunning()
        {
            return Process.GetProcesses().Where(p => p.ProcessName.ToLower().Equals("taskmgr")).Count() > 0;
        }

        private void OnBack(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (IsTobiiCalibrating())
            {
                TryCloseTobiiCalibration();

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
            Console.WriteLine("Resetting Minecraft data");

            //fixme: delete first?
            string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string installedPath = AppContext.BaseDirectory;

            string installedSavesDir = Path.Combine(installedPath, @"ModInstaller\saves");
            string minecraftSavesDir = Path.Combine(applicationDataPath, @".minecraft\EyeMineExhibition\saves");


            string origFolderName = "Tutorial";
            
            // Allow translated worlds e.g. "Tutorial-en", "Tutorial-ja"
            var twoLetterLanguage = Settings.Default.UiLanguage.ToCultureInfo().TwoLetterISOLanguageName;
            string langFolderName = "Tutorial-" + twoLetterLanguage;

            // Use translated world if available, or fall back
            string origFullPath = Path.Combine(installedSavesDir, origFolderName);
            string langFullPath = Path.Combine(installedSavesDir, langFolderName);
            string worldPathSrc = File.Exists(langFullPath) ? langFullPath : origFullPath;

            Log.Info($"Looking for world: {langFullPath}");
            Log.Info($"Using world: {worldPathSrc}");

            // Make two copies we can switch between
            DirectoryCopy(worldPathSrc, 
                Path.Combine(minecraftSavesDir, origFolderName),
                true,
                true);
            DirectoryCopy(worldPathSrc,
               Path.Combine(minecraftSavesDir, origFolderName + "2"),
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
