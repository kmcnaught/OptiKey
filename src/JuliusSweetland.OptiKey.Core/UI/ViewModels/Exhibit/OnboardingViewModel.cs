using EyeXFramework;
using JuliusSweetland.OptiKey.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Tobii.EyeX.Framework;
using JuliusSweetland.OptiKey.UI.Windows;
using Prism.Commands;
using System.Windows;
using System.Diagnostics;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Enums;
using System.Threading;
using log4net;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class OnboardingViewModel : PageViewModel
    {

        public enum OnboardState
        {
            WELCOME,
            EYES,
            WAIT_CALIB,            
            CALIB_TIMEOUT,
            CALIB_SUCCESS,
            IN_MINECRAFT
        }

        public enum TempState
        {
            NONE,
            INFO,
            RESET,
            TEMP_ERROR_EYETRACKER
        }

        public enum DemoState
        {
            FIRST_SETUP,
            RESETTING,
            NO_USER,
            ERROR_MC_CRASH,            
            PERM_ERROR_EYETRACKER,
            TIMED_OUT,
            RUNNING
        }
        private readonly ICommand setKioskCommand;
        private readonly ICommand unsetKioskCommand;
        private readonly ICommand captureMinecraftCommand;
        private readonly ICommand restartCommand;
        private readonly ICommand swapLanguageCommand;

        public ICommand SetKioskCommand { get { return setKioskCommand; } }
        public ICommand UnsetKioskCommand { get { return unsetKioskCommand; } }
        public ICommand CaptureMinecraftCommand { get { return captureMinecraftCommand; } }
        public ICommand RestartCommand { get { return restartCommand; } }
        public ICommand SwapLanguageCommand { get { return swapLanguageCommand; } }

        public DemoState demoState = DemoState.FIRST_SETUP;
        public OnboardState mainState;
        public TempState tempState;

        private IntroViewModel introViewModel = new IntroViewModel();
        private TobiiViewModel tobiiViewModel = new TobiiViewModel();
        private WaitCalibViewModel waitCalibViewModel = new WaitCalibViewModel();
        private PostCalibViewModel postCalibViewModel = new PostCalibViewModel();
        private InfoViewModel infoViewModel = new InfoViewModel();
        private ResetViewModel resetViewModel = new ResetViewModel();
        private LoadingViewModel loadingViewModel = new LoadingViewModel();
        private ErrorViewModel errorViewModel = new ErrorViewModel();
        private BlankViewModel blankViewModel = new BlankViewModel();
        private ForcedResetViewModel forcedResetViewModel = new ForcedResetViewModel();
        private ResettingViewModel resettingViewModel = new ResettingViewModel();
        private TempEyeTrackerErrorViewModel tempEyeTrackerErrorViewModel = new TempEyeTrackerErrorViewModel();
        private EyeTrackerErrorViewModel eyeTrackerErrorViewModel = new EyeTrackerErrorViewModel();
        private CalibTimeoutViewModel calibTimeoutViewModel= new CalibTimeoutViewModel();

        // View model requires non-static init
        private EyesLostViewModel eyesLostViewModel;

        private DispatcherTimer tobiiTimer = new DispatcherTimer();
        private DispatcherTimer ingameTimeoutTimer = new DispatcherTimer();
        private DispatcherTimer ingameWarningTimer = new DispatcherTimer();
        private DispatcherTimer forceResetTimer = new DispatcherTimer();
        private DispatcherTimer eyeTrackerErrorTimer = new DispatcherTimer();
        private DispatcherTimer calibrationTimeoutTimer = new DispatcherTimer();

        public event EventHandler RequireAutoReset = delegate { };
        public event EventHandler RequireCloseCalibration = delegate { };
        public event EventHandler TimeoutWarning = delegate { };

        public event EventHandler StateChanged;

        public OnboardingViewModel()
        {
            ExhibitStateLogger.LogSessionStart();
            
            setKioskCommand = new DelegateCommand(() => {
                Demo.SetAsShellApp(true);
                MessageBox.Show("Shell app setup complete. Please restart PC to see changes");
            });
            unsetKioskCommand = new DelegateCommand(() => {
                Demo.SetAsShellApp(false);
                MessageBox.Show("Shell app disabled. Please restart PC to see changes");
            });
            //captureMinecraftCommand = new DelegateCommand(CaptureMinecraft);
            restartCommand = new DelegateCommand(() => {
                MessageBox.Show("Restart all programs?");
                MainWindow.RestartEverything();
            });
            swapLanguageCommand = new DelegateCommand(() => {
                SwapLanguageRequest();
            });
            captureMinecraftCommand = new DelegateCommand(CaptureMinecraft);

            // Create any view models that require non-static initialisation
            eyesLostViewModel = new EyesLostViewModel(tobiiViewModel);
            eyesLostViewModel.RequireAutoReset += (s, e) => { this.RequireAutoReset(eyesLostViewModel, null); };
            
            // Register for Tobii events
            TobiiEyeXPointService.EyeXHost.EyeTrackingDeviceStatusChanged += handleTobiiChange;

            // Timer checks for eye visibility
            tobiiTimer.Tick += TobiiTick;
            tobiiTimer.Interval = new TimeSpan(0, 0, 1);
            tobiiTimer.Start();            

            // overall demo timelimit
            ingameTimeoutTimer.Interval = new TimeSpan(0, Settings.Default.IngameTimeoutMinutes, 0);
            ingameTimeoutTimer.Tick += IngameTimeout_Tick;

            // "1 minute remaining" warning
            ingameWarningTimer.Interval = new TimeSpan(0, Settings.Default.IngameTimeoutMinutes - 1, 0);
            ingameWarningTimer.Tick += IngameTimerWarning_Tick;
                       
            // forcefully reset from end page
            forceResetTimer.Interval = new TimeSpan(0, 0, 35);
            forceResetTimer.Tick += ForceResetTimer_Tick;

            // go to unrecoverable error screen if eye tracker error up for too long
            eyeTrackerErrorTimer.Interval = new TimeSpan(0, 0, 30);
            eyeTrackerErrorTimer.Tick += EyeTrackerErrorTimer_Tick;

            // time out calibration process in case ends in failure
            calibrationTimeoutTimer.Interval = TimeSpan.FromMinutes(1.5); //FIXME: setting?
            calibrationTimeoutTimer.Tick += CalibrationTimeoutTimer_Tick;

            // Initial state
            tempState = TempState.NONE;
            mainState = OnboardState.WELCOME;
#if DEBUG
            demoState = DemoState.RUNNING; // skip wait for minecraft, so can test other things sooner
#endif
        }

        private void CalibrationTimeoutTimer_Tick(object sender, EventArgs e)
        {
            calibrationTimeoutTimer.Stop();
            RequireCloseCalibration(this, null);
            if (mainState == OnboardState.WAIT_CALIB) {
                SetState(OnboardState.CALIB_TIMEOUT);
            }            
        }

        private void EyeTrackerErrorTimer_Tick(object sender, EventArgs e)
        {
            eyeTrackerErrorTimer.Stop();
            SetState(DemoState.PERM_ERROR_EYETRACKER);
            SetTempState(TempState.NONE);
        }

        private void ForceResetTimer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Demo complete/timed out");
            RequireAutoReset(this, null);
            forceResetTimer.Stop();
        }

        private void IngameTimeout_Tick(object sender, EventArgs e)
        {
            this.demoState = DemoState.TIMED_OUT;
            this.ingameTimeoutTimer.Stop();
            this.forceResetTimer.Start();
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        private void IngameTimerWarning_Tick(object sender, EventArgs e)
        {
            this.ingameWarningTimer.Stop();
            TimeoutWarning(this, null);
        }

        private void CaptureMinecraft()
        {
            Process p = Demo.CaptureMinecraftProcess();
            if (p == null)
            {
                MessageBox.Show("Could not find valid Minecraft instance. \n\nPlease run Minecraft Launcher, select the \"EyeMineExhibition\" profile and click PLAY to launch");
            }
            else
            {
                if (MessageBox.Show("Successfully captured Minecraft process.\nPlease close Minecraft.\nEyeMine will now restart and launch it's own copy of Minecraft.",
                         "Capturing Minecraft instance ... ",
                         MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    MainWindow.RestartEverything();
                }
            }
        }

        //FIXME: set up tear down?

        private void TobiiTick(object sender, EventArgs e)
        {
            // handle going in / out of 'no user' state
            //fixme: is it possible to be forced out of this state and leave things running?
            // would that matter?
            if (demoState == DemoState.NO_USER) 
            {
                if (eyesLostViewModel.CanDismiss)
                {
                    demoState = DemoState.RUNNING;
                    eyesLostViewModel.Stop();
                    RaisePropertyChanged("CurrentPageViewModel");
                    StateChanged(this, null);
                }
            }
            else if (demoState == DemoState.RUNNING && 
                tempState == TempState.NONE &&
                mainState == OnboardState.IN_MINECRAFT)
            {
                if (tobiiViewModel.LostTracking)
                {
                    demoState = DemoState.NO_USER;
                    eyesLostViewModel.Start();
                    RaisePropertyChanged("CurrentPageViewModel");
                    StateChanged(this, null);
                }
            }            
        }

        public bool LostTracking()
        {
            return tobiiViewModel.LostTracking;
        }

        public void SetState(DemoState state)
        {
            var oldState = this.demoState;
            this.demoState = state;
            ExhibitStateLogger.LogDemoStateChange(oldState, state);
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        public void SetState(OnboardState state) {
            var oldState = this.mainState;
            this.mainState = state;
            ExhibitStateLogger.LogOnboardStateChange(oldState, state);
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        public void SetTempState(TempState state) {
            var oldState = this.tempState;
            this.tempState = state;
            ExhibitStateLogger.LogStateChange("TempState", oldState.ToString(), state.ToString());
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        public void SetUnrecoverableError()
        {
            this.demoState = DemoState.ERROR_MC_CRASH;
            errorViewModel.StartRestartCountdown();
            LogManager.Flush(1000);
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        public void SetLoadingComplete()
        {
            if (demoState == DemoState.FIRST_SETUP ||
                demoState == DemoState.RESETTING) {

                demoState = DemoState.RUNNING;
                RaisePropertyChanged("CurrentPageViewModel");
                StateChanged(this, null);
            }
        }

        #region Properties         

        public PageViewModel CurrentPageViewModel
        {
            get
            {
                PageViewModel vm = blankViewModel;

                if (tempState == TempState.TEMP_ERROR_EYETRACKER)
                {
                    vm = tempEyeTrackerErrorViewModel;
                }
                else
                {
                    switch (demoState)
                    {
                        case DemoState.ERROR_MC_CRASH:
                            vm = errorViewModel;
                            break;
                        case DemoState.TIMED_OUT:
                            vm = forcedResetViewModel;
                            break;
                        case DemoState.FIRST_SETUP:
                            vm = loadingViewModel;
                            break;
                        case DemoState.RESETTING:
                            vm = resettingViewModel;
                            break;
                        case DemoState.NO_USER:
                            if (tempState == TempState.RESET)
                            {
                                vm = resetViewModel;
                            }
                            else if (tempState == TempState.INFO)
                            {
                                vm = infoViewModel;
                            }
                            else
                            {
                                vm = eyesLostViewModel;
                            }
                            break;
                        case DemoState.PERM_ERROR_EYETRACKER:
                            vm = eyeTrackerErrorViewModel;
                            break;
                        case DemoState.RUNNING:
                            switch (tempState)
                            {
                                case TempState.RESET:
                                    vm = resetViewModel;
                                    break;
                                case TempState.INFO:
                                    vm = infoViewModel;
                                    break;
                                case TempState.NONE:
                                    switch (mainState)
                                    {
                                        case OnboardState.WELCOME:
                                            vm = introViewModel;
                                            break;
                                        case OnboardState.EYES:
                                            vm = tobiiViewModel;
                                            break;
                                        case OnboardState.WAIT_CALIB:
                                            vm = waitCalibViewModel;
                                            break;
                                        case OnboardState.CALIB_SUCCESS:
                                            vm = postCalibViewModel;
                                            break;                                        
                                        case OnboardState.CALIB_TIMEOUT:
                                            vm = calibTimeoutViewModel;
                                            break;
                                        case OnboardState.IN_MINECRAFT:
                                            vm = blankViewModel;
                                            break;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                if (vm != eyesLostViewModel)
                {
                    eyesLostViewModel.Stop();
                }
                return vm;
            }            
        }

        public void TobiiRecovery()
        {
            if (tempState == TempState.TEMP_ERROR_EYETRACKER)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {  
                    // We use dispatcher to run this on UI thread rather than the thread the eye tracker notified us on
                    eyeTrackerErrorTimer.Stop();
                    SetTempState(TempState.NONE);
                });
            } 
        }

        public void TobiiError(Tobii.EyeX.Framework.EyeTrackingDeviceStatus lastTobiiErrorStatus)
        {
            LogManager.Flush(1000);
            if (tempState != TempState.TEMP_ERROR_EYETRACKER)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // We use dispatcher to run this on UI thread rather than the thread the eye tracker notified us on
                    eyeTrackerErrorTimer.Start();
                    eyeTrackerErrorViewModel.ErrorString = "Error code: " + lastTobiiErrorStatus.ToString();
                    SetTempState(TempState.TEMP_ERROR_EYETRACKER);
                });
            }
        }

        #endregion

        #region Methods

        public void Info()
        {
            if (tempState == TempState.INFO)
            {
                SetTempState(TempState.NONE);
            }
            else
            {
                // TODO: think about precedence of reset / info
                SetTempState(TempState.INFO);
            }
        }

        public void StartResetViewModel()
        {
            //DO the reset?!
            calibrationTimeoutTimer.Stop();
            RequireCloseCalibration(this, null);
            SetTempState(TempState.NONE);
            SetState(OnboardState.WELCOME);
            SetState(DemoState.RESETTING);            
            ingameTimeoutTimer.Stop();
            ingameWarningTimer.Stop();
        }

        public void CompleteResetViewModel()
        {        
            SetTempState(TempState.NONE);
            SetState(OnboardState.WELCOME);
            SetState(DemoState.RUNNING);            
        }

        // this is the entry point for the reset BUTTON, not a generic "reset" method
        public void Reset()
        {
            
            RequireCloseCalibration(this, null);
            calibrationTimeoutTimer.Stop();

            if (mainState == OnboardState.WAIT_CALIB)
            {
                // We don't offer confirmation in this context since we can't get focus over calibration
                StartResetViewModel();
                RequireAutoReset(this, null); // pass back up to Demo.cs      
                return;
            }            

            if (tempState == TempState.RESET ||
                demoState == DemoState.TIMED_OUT ||
                demoState == DemoState.NO_USER)
            {
                Console.WriteLine("User-requested reset");
                StartResetViewModel();
                RequireAutoReset(this, null); // pass back up to Demo.cs                
            }
            else
            {
                // TODO: think about precedence of reset / info
                SetTempState(TempState.RESET);
            }
        }


        public void Next()
        {   
            if (demoState != DemoState.RUNNING) 
            {
                return;
            }

            switch (tempState) {
                case TempState.INFO:                    
                    break;
                case TempState.RESET:
                //TODO: should we use reset->reset or reset->next ? prefer different keys to prevent misclicks? or simpler to encourage resets?                    
                //TODO: who is responsible for the actual resetting action?                
                    break;
                case TempState.NONE:

                    switch (mainState)
                    {
                        case OnboardState.WELCOME:
                            SetState(OnboardState.EYES);
                            break;
                        case OnboardState.EYES:
                            if (tobiiViewModel.IsGoodEnough)
                            {
                                TobiiEyeXPointService.EyeXHost.LaunchGuestCalibration();
                                calibrationTimeoutTimer.Start();
                                SetState(OnboardState.WAIT_CALIB);
                            }
                            break;
                        case OnboardState.WAIT_CALIB:                            
                            break;
                        case OnboardState.CALIB_SUCCESS:
                            SetState(OnboardState.IN_MINECRAFT);
                            ingameTimeoutTimer.Start();
                            ingameWarningTimer.Start();
                            Console.WriteLine("Starting minecraft tutorial");
                            break;
                    }
                    break;
            }
        }

        private void handleTobiiChange(object sender, EngineStateValue<EyeTrackingDeviceStatus> status)
        {
            // We use dispatcher to run this on UI thread rather than the thread the eye tracker notified us on
            if (mainState == OnboardState.WAIT_CALIB &&
            status.Value == EyeTrackingDeviceStatus.Tracking)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    calibrationTimeoutTimer.Stop();
                    SetState(OnboardState.CALIB_SUCCESS);
                    Demo.SetGhostVisible(true);
                });
            }
           
        }

        public void Back()
        {
            switch (demoState)
            {
                case DemoState.ERROR_MC_CRASH:                   
                case DemoState.FIRST_SETUP:
                case DemoState.TIMED_OUT:
                case DemoState.RESETTING:
                    break;
                case DemoState.NO_USER:
                    if (!tobiiViewModel.LostTracking) {
                        SetState(DemoState.RUNNING);
                        eyesLostViewModel.Stop();
                    }
                    break;
                case DemoState.RUNNING:
                    switch (tempState)
                    {
                        case TempState.INFO:
                            SetTempState(TempState.NONE);
                            break;
                        case TempState.RESET:
                            SetTempState(TempState.NONE);
                            break;
                        case TempState.NONE:
                            switch (mainState)
                            {
                                case OnboardState.IN_MINECRAFT:
                                    //FIXME: do we want to allow this transition?
                                    //SetState(OnboardState.POST_CALIB);
                                    //ingameTimer.Stop();
                                    break;
                                case OnboardState.WAIT_CALIB:                                
                                case OnboardState.CALIB_TIMEOUT:
                                    calibrationTimeoutTimer.Stop();
                                    RequireCloseCalibration(this, null);
                                    SetState(OnboardState.EYES);
                                    break;                                
                                case OnboardState.CALIB_SUCCESS:
                                    SetState(OnboardState.EYES);
                                    break;
                                case OnboardState.EYES:
                                    SetState(OnboardState.WELCOME);
                                    break;
                            }
                            break;
                    }
                    break;
            }
        }

        bool isContextMenuOpen = false;
        public bool IsContextMenuOpen
        {
            get
            {
                return isContextMenuOpen;
            }
            set
            {
                isContextMenuOpen = value;
            }
        }

        private void SwapLanguageRequest()
        {
            var currentLanguage = Settings.Default.UiLanguage;
            var newLanguageName = currentLanguage == Languages.EnglishUK ? "Japanese" : "English";
            
            var result = MessageBox.Show(
                $"Switch to {newLanguageName} and restart EyeMine?", 
                "Swap language", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                SaveAndRestartWithNewLanguage();
            }
        }

        private void SaveAndRestartWithNewLanguage()
        {
            var currentLanguage = Settings.Default.UiLanguage;
            if (currentLanguage == Languages.EnglishUK)
            {
                Settings.Default.UiLanguage = Languages.JapaneseJapan;
            }
            else
            {
                Settings.Default.UiLanguage = Languages.EnglishUK;
            }
            Settings.Default.Save();
            MainWindow.RestartEverything();
        }

        #endregion
    }
}
