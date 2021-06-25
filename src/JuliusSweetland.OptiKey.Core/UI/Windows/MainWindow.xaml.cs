// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Observables.PointSources;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Static;
using JuliusSweetland.OptiKey.UI.ViewModels;
using JuliusSweetland.OptiKey.UI.ViewModels.Exhibit;
using JuliusSweetland.OptiKey.UI.Views;
using JuliusSweetland.OptiKey.UI.Views.Exhibit;
using log4net;
using NHotkey.Wpf;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;

namespace JuliusSweetland.OptiKey.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IAudioService audioService;
        private readonly IDictionaryService dictionaryService;
        private readonly IInputService inputService;
        private readonly IKeyStateService keyStateService;
        private readonly IPointSource defaultPointSource;
        private readonly IPointSource manualModePointSource;
        private readonly InteractionRequest<NotificationWithServicesAndState> managementWindowRequest;
        private readonly ICommand managementWindowRequestCommand;
        private readonly ICommand toggleManualModeCommand;
        private readonly ICommand backCommand;
        private readonly ICommand quitCommand;
        private readonly ICommand restartCommand;
        private readonly List<ICommand> demoCommands;

        public MainWindow(
            IAudioService audioService,
            IDictionaryService dictionaryService,
            IInputService inputService,
            IKeyStateService keyStateService)
        {
            InitializeComponent();

            if (Settings.Default.EnableResizeWithMouse
                && (Settings.Default.MainWindowState == WindowStates.Floating
                    || Settings.Default.MainWindowState == WindowStates.Docked))
            {
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            this.audioService = audioService;
            this.dictionaryService = dictionaryService;
            this.inputService = inputService;
            this.keyStateService = keyStateService;

            demoCommands = new List<ICommand>();
            List<Key> numKeys = new List<Key> () { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9 };            

            for (int i = 0; i < 10; i++) {
                int iCaptured = i;
                demoCommands.Add(new DelegateCommand(() => { DemoShortcut(iCaptured); }));

                InputBindings.Add(new KeyBinding
                {
                    Command = demoCommands[iCaptured],
                    Modifiers = ModifierKeys.Control,
                    Key = numKeys[iCaptured]
                });
            }

            defaultPointSource = inputService.PointSource;
            manualModePointSource = new MousePositionSource(Settings.Default.PointTtl) { State = RunningStates.Paused };

            managementWindowRequest = new InteractionRequest<NotificationWithServicesAndState>();
            managementWindowRequestCommand = new DelegateCommand(RequestManagementWindow);
            toggleManualModeCommand = new DelegateCommand(ToggleManualMode, () => !(defaultPointSource is MousePositionSource));
            quitCommand = new DelegateCommand(Quit);
            backCommand = new DelegateCommand(Back);
            restartCommand = new DelegateCommand(Restart);

            //Setup key binding (Alt+M and Shift+Alt+M) to open settings
            InputBindings.Add(new KeyBinding
            {
                Command = managementWindowRequestCommand,
                Modifiers = ModifierKeys.Alt,
                Key = Key.M
            });
            InputBindings.Add(new KeyBinding
            {
                Command = managementWindowRequestCommand,
                Modifiers = ModifierKeys.Shift | ModifierKeys.Alt,
                Key = Key.M
            });

            //Setup key binding (Alt+Enter and Shift+Alt+Enter) to open settings
            InputBindings.Add(new KeyBinding
            {
                Command = toggleManualModeCommand,
                Modifiers = ModifierKeys.Alt,
                Key = Key.Enter
            });
            InputBindings.Add(new KeyBinding
            {
                Command = toggleManualModeCommand,
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Key = Key.D1
            });

            // Enable mouse drag on keyboard
            MainView.KeyboardHost.MouseDown += OnMouseDown;

            Title = string.Format(Properties.Resources.WINDOW_TITLE, DiagnosticInfo.AssemblyVersion);

            //Set the window size to 0x0 as this prevents a flicker where OptiKey would be displayed in the default position and then repositioned
            Width = 0;
            Height = 0;

            this.Closing += (sender, args) =>
            {
                //https://stackoverflow.com/questions/26863458/handle-the-close-event-via-task-bar
                Log.Info("Main window closing event detected. In some circumstances, such as closing OptiKey from the taskbar when a background thread is running, OptiKey will not close and instead become a background process. Forcing a full shutdown.");
                Application.Current.Shutdown();
            };

            HotkeyManager.Current.AddOrReplace("Back", Key.Left, ModifierKeys.Control, OnBack);
            HotkeyManager.Current.AddOrReplace("Forward", Key.Right, ModifierKeys.Control, OnForward);

            // Launch Minecraft
            string javapath = @"C:\Program Files (x86)\Minecraft Launcher\runtime\jre-legacy\windows-x64\jre-legacy\bin\javaw.exe";
            string minecraftArgs = @"-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump ""-Dos.name\ =\ Windows\ 10"" -Dos.version=10.0 -Xss1M -Djava.library.path=C:\Users\Kirsty\AppData\Roaming\.minecraft\bin\EyeMineExhibit -Dminecraft.launcher.brand=minecraft-launcher -Dminecraft.launcher.version=2.2.3125 -cp C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\minecraftforge\forge\1.14.4-28.2.0\forge-1.14.4-28.2.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\ow2\asm\asm\6.2\asm-6.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\ow2\asm\asm-commons\6.2\asm-commons-6.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\ow2\asm\asm-tree\6.2\asm-tree-6.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\cpw\mods\modlauncher\4.1.0\modlauncher-4.1.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\cpw\mods\grossjava9hacks\1.1.0\grossjava9hacks-1.1.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\minecraftforge\accesstransformers\1.0.1-milestone.0.1+94458e7-shadowed\accesstransformers-1.0.1-milestone.0.1+94458e7-shadowed.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\minecraftforge\eventbus\1.0.0-service\eventbus-1.0.0-service.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\minecraftforge\forgespi\1.5.0\forgespi-1.5.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\minecraftforge\coremods\1.0.0\coremods-1.0.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\minecraftforge\unsafe\0.2.0\unsafe-0.2.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\electronwill\night-config\core\3.6.0\core-3.6.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\electronwill\night-config\toml\3.6.0\toml-3.6.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\jline\jline\3.12.1\jline-3.12.1.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\apache\maven\maven-artifact\3.6.0\maven-artifact-3.6.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\jodah\typetools\0.6.0\typetools-0.6.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\java3d\vecmath\1.5.2\vecmath-1.5.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\apache\logging\log4j\log4j-api\2.11.2\log4j-api-2.11.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\apache\logging\log4j\log4j-core\2.11.2\log4j-core-2.11.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\minecrell\terminalconsoleappender\1.2.0\terminalconsoleappender-1.2.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\sf\jopt-simple\jopt-simple\5.0.4\jopt-simple-5.0.4.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\mojang\patchy\1.1\patchy-1.1.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\oshi-project\oshi-core\1.1\oshi-core-1.1.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\java\dev\jna\jna\4.4.0\jna-4.4.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\java\dev\jna\platform\3.4.0\platform-3.4.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\ibm\icu\icu4j-core-mojang\51.2\icu4j-core-mojang-51.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\mojang\javabridge\1.0.22\javabridge-1.0.22.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\io\netty\netty-all\4.1.25.Final\netty-all-4.1.25.Final.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\google\guava\guava\21.0\guava-21.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\apache\commons\commons-lang3\3.5\commons-lang3-3.5.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\commons-io\commons-io\2.5\commons-io-2.5.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\commons-codec\commons-codec\1.10\commons-codec-1.10.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\java\jinput\jinput\2.0.5\jinput-2.0.5.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\net\java\jutils\jutils\1.0.0\jutils-1.0.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\mojang\brigadier\1.0.17\brigadier-1.0.17.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\mojang\datafixerupper\2.0.24\datafixerupper-2.0.24.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\google\code\gson\gson\2.8.0\gson-2.8.0.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\mojang\authlib\1.5.25\authlib-1.5.25.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\apache\commons\commons-compress\1.8.1\commons-compress-1.8.1.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\apache\httpcomponents\httpclient\4.3.3\httpclient-4.3.3.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\commons-logging\commons-logging\1.1.3\commons-logging-1.1.3.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\apache\httpcomponents\httpcore\4.3.2\httpcore-4.3.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\it\unimi\dsi\fastutil\8.2.1\fastutil-8.2.1.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl\3.2.2\lwjgl-3.2.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl-jemalloc\3.2.2\lwjgl-jemalloc-3.2.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl-openal\3.2.2\lwjgl-openal-3.2.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl-opengl\3.2.2\lwjgl-opengl-3.2.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl-glfw\3.2.2\lwjgl-glfw-3.2.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl-stb\3.2.2\lwjgl-stb-3.2.2.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\libraries\com\mojang\text2speech\1.11.3\text2speech-1.11.3.jar;C:\Users\Kirsty\AppData\Roaming\.minecraft\versions\1.14.4-forge-28.2.0\1.14.4-forge-28.2.0.jar -Xmx2G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M cpw.mods.modlauncher.Launcher --username kirstym --version 1.14.4-forge-28.2.0 --gameDir C:\Users\Kirsty\AppData\Roaming\.minecraft\EyeMineExhibition --assetsDir C:\Users\Kirsty\AppData\Roaming\.minecraft\assets --assetIndex 1.14 --uuid edbd23475bd5466f8a3e5fceb85cd13b --accessToken ACCESS_TOKEN_HERE --userType mojang --versionType release --launchTarget fmlclient --fml.forgeVersion 28.2.0 --fml.mcVersion 1.14.4 --fml.forgeGroup net.minecraftforge --fml.mcpVersion 20190829.143755";

            minecraftProcess = Process.Start(new ProcessStartInfo(javapath, minecraftArgs));
            minecraftProcess.CloseOnApplicationExit(Log, "Minecraft");
        }

        private void OnForward(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (onboardWindow == null || onboardWindow.IsClosed)
            {
                if (minecraftProcess != null)
                {
                    ShowWindow(minecraftProcess, PInvoke.SW_SHOWMINNOACTIVE);
                }
                onboardWindow = new OnboardingWindow();
                onboardWindow.Show();                
                onboardWindow.Focus();                
                onboardWindow.Closed += OnboardWindowClosed;
            }
            else
            {
                onboardWindow.Focus();
                onboardWindow.Next();                
            }
        }

        private void OnBack(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (onboardWindow != null)
            {
                onboardWindow.Focus();
                onboardWindow.Previous();
            }
        }

        public IList<Tuple<KeyValue, KeyValue>> KeyFamily { get { return keyStateService.KeyFamily; } }
        public IDictionary<string, List<KeyValue>> KeyValueByGroup { get { return keyStateService.KeyValueByGroup; } }
        public IDictionary<KeyValue, TimeSpanOverrides> OverrideTimesByKey  { get { return inputService.OverrideTimesByKey; } }

        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Don't take focus away from any existing toast notifications
            if (MainView.ToastNotificationPopup.IsOpen)
                return;

            if (e.LeftButton == MouseButtonState.Pressed && Settings.Default.EnableResizeWithMouse)
            {
                // This prevents win7 aerosnap, which otherwise might snap to edges and expand unexpectedly
                ResizeMode origResizeMode = this.ResizeMode;
                this.ResizeMode = ResizeMode.NoResize;
                this.UpdateLayout();
                
                DragMove();
                
                // Restore original resize mode 
                this.ResizeMode = origResizeMode;
                this.UpdateLayout();
            }
        }

        public IWindowManipulationService WindowManipulationService { get; set; }

        public InteractionRequest<NotificationWithServicesAndState> ManagementWindowRequest { get { return managementWindowRequest; } }
        public ICommand ManagementWindowRequestCommand { get { return managementWindowRequestCommand; } }
        public ICommand ToggleManualModeCommand { get { return toggleManualModeCommand; } }
        public ICommand QuitCommand { get { return quitCommand; } }
        public ICommand BackCommand { get { return backCommand; } }
        public ICommand RestartCommand { get { return restartCommand; } }

        public static readonly DependencyProperty BackgroundColourOverrideProperty =
            DependencyProperty.Register("BackgroundColourOverride", typeof(Brush), typeof(MainWindow), new PropertyMetadata(default(Brush)));

        public Brush BackgroundColourOverride
        {
            get { return (Brush)GetValue(BackgroundColourOverrideProperty); }
            set { SetValue(BackgroundColourOverrideProperty, value); }
        }

        public static readonly DependencyProperty BorderBrushOverrideProperty =
            DependencyProperty.Register("BorderBrushOverride", typeof(Brush), typeof(MainWindow), new PropertyMetadata(default(Brush)));

        public Brush BorderBrushOverride
        {
            get { return (Brush)GetValue(BorderBrushOverrideProperty); }
            set { SetValue(BorderBrushOverrideProperty, value); }
        }

        private void RequestManagementWindow()
        {
            Log.Info("RequestManagementWindow called.");

            var modalManagementWindow = WindowManipulationService != null &&
                                        WindowManipulationService.WindowState == WindowStates.Maximised;

            if (modalManagementWindow)
            {
                inputService.RequestSuspend();
            }
            var restoreModifierStates = keyStateService.ReleaseModifiers(Log);
            ManagementWindowRequest.Raise(
                new NotificationWithServicesAndState
                {
                    ModalWindow = modalManagementWindow,
                    AudioService = audioService,
                    DictionaryService = dictionaryService,
                    WindowManipulationService = WindowManipulationService
                },
                _ =>
                {
                    if (modalManagementWindow)
                    {
                        inputService.RequestResume();
                    }
                    restoreModifierStates();
                });

            Log.Info("RequestManagementWindow complete.");
        }

        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            MainView.DataContext = mainViewModel;
        }

        public void AddOnMainViewLoadedAction(Action postMainViewLoaded)
        {
            if (MainView.IsLoaded)
            {
                postMainViewLoaded();
            }
            else
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (s, a) =>
                {
                    postMainViewLoaded();
                    MainView.Loaded -= loadedHandler; //Ensure this handler only triggers once
                };
                MainView.Loaded += loadedHandler;
            }
        }

        private OnboardingWindow onboardWindow;

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

        private void DemoShortcut(int i)
        {
            if (i == 0)
            {
                var mainViewModel = MainView.DataContext as MainViewModel;
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
                var mainViewModel = MainView.DataContext as MainViewModel;
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

            //ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
            //ShowWindow(minecraftProcess, PInvoke.SW_MINIMIZE);
            //ShowWindow(minecraftProcess, PInvoke.SW_SHOWMINNOACTIVE);

        }

        private void OnboardWindowClosed(object sender, EventArgs e)
        {
            if (minecraftProcess != null)
            {
                ShowWindow(minecraftProcess, PInvoke.SW_SHOWMAXIMIZED);
                FocusWindow(minecraftProcess);
                var mainViewModel = MainView.DataContext as MainViewModel;
                string enabledKeyboard = @"C:\Users\Kirsty\AppData\Roaming\SpecialEffect\EyeMineV2\Keyboards\EyeTracker\museum.xml";
                mainViewModel.ProcessChangeKeyboardKeyValue(new ChangeKeyboardKeyValue(enabledKeyboard));
            }
        }

        private Process minecraftProcess;

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
            if (process == null) { return;  }

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

        private void ToggleManualMode()
        {
            Log.Info("ToggleManualMode called.");

            if (MessageBox.Show(Properties.Resources.MANUAL_MODE_MESSAGE, Properties.Resources.MANUAL_MODE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var mainViewModel = MainView.DataContext as MainViewModel;
                if (mainViewModel != null)
                {
                    inputService.RequestSuspend();
                    mainViewModel.DetachInputServiceEventHandlers();
                    var changingToManualMode = inputService.PointSource == defaultPointSource;
                    inputService.PointSource = changingToManualMode ? manualModePointSource : defaultPointSource;
                    mainViewModel.AttachInputServiceEventHandlers();
                    mainViewModel.RaiseToastNotification(Properties.Resources.MANUAL_MODE_CHANGED,
                        changingToManualMode ? Properties.Resources.MANUAL_MODE_ENABLED : Properties.Resources.MANUAL_MODE_DISABLED,
                        NotificationTypes.Normal, () => inputService.RequestResume());
                    mainViewModel.ManualModeEnabled = changingToManualMode;
                    keyStateService.ClearKeyHighlightStates(); //Clear any in-progress multi-key selection highlighting
                }
            }

            Log.Info("ToggleManualMode complete.");
        }

        private void Quit()
        {
            if (MessageBox.Show(Properties.Resources.QUIT_MESSAGE, Properties.Resources.QUIT, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Settings.Default.CleanShutdown = true;
                Application.Current.Shutdown();
            }
        }

        private void Back()
        {
            var mainViewModel = MainView.DataContext as MainViewModel;
            if (null != mainViewModel)
            {
                mainViewModel.BackFromKeyboard();   
            }            
        }

        private void Restart()
        {
            if (MessageBox.Show(Properties.Resources.REFRESH_MESSAGE, Properties.Resources.RESTART, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                OptiKeyApp.RestartApp();
                Application.Current.Shutdown();
            }
        }
    }
}
