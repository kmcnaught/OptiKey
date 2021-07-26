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

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class OnboardingViewModel : BindableBase
    {

        public enum OnboardState
        {
            WELCOME,
            EYES,
            WAIT_CALIB,
            POST_CALIB,
            IN_MINECRAFT
        }

        public enum TempState
        {
            NONE,
            INFO,
            RESET
        }

        public enum DemoState
        {
            FIRST_SETUP,
            RESETTING,
            NO_USER,
            ERROR,
            RUNNING
        }
        private readonly ICommand setKioskCommand;
        private readonly ICommand unsetKioskCommand;
        private readonly ICommand captureMinecraftCommand;
        private readonly ICommand restartCommand;

        public ICommand SetKioskCommand { get { return setKioskCommand; } }
        public ICommand UnsetKioskCommand { get { return unsetKioskCommand; } }
        public ICommand CaptureMinecraftCommand { get { return captureMinecraftCommand; } }
        public ICommand RestartCommand { get { return restartCommand; } }

        public DemoState demoState = DemoState.FIRST_SETUP;
        public OnboardState mainState;
        public TempState tempState;        

        private IntroViewModel introViewModel;
        private TobiiViewModel tobiiViewModel;
        private WaitCalibViewModel waitCalibViewModel;
        private PostCalibViewModel postCalibViewModel;
        private InfoViewModel infoViewModel;
        private ResetViewModel resetViewModel;
        private LoadingViewModel loadingViewModel;
        private ErrorViewModel errorViewModel;
        private EyesLostViewModel eyesLostViewModel;

        private DispatcherTimer tobiiTimer = new DispatcherTimer();

        public event EventHandler RequireAutoReset = delegate { };


        public event EventHandler StateChanged;

        public OnboardingViewModel()
        {
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
            captureMinecraftCommand = new DelegateCommand(CaptureMinecraft);

            // Create view models
            introViewModel = new IntroViewModel();
            tobiiViewModel = new TobiiViewModel();
            waitCalibViewModel = new WaitCalibViewModel();
            postCalibViewModel = new PostCalibViewModel();
            infoViewModel = new InfoViewModel();
            resetViewModel = new ResetViewModel();
            loadingViewModel = new LoadingViewModel();
            errorViewModel = new ErrorViewModel();
            eyesLostViewModel = new EyesLostViewModel(tobiiViewModel);
            eyesLostViewModel.RequireAutoReset += (s, e) => { this.RequireAutoReset(eyesLostViewModel, null); };

            // Register for Tobii events
            TobiiEyeXPointService.EyeXHost.EyeTrackingDeviceStatusChanged += handleTobiiChange;
            tobiiTimer.Tick += TobiiTick;
            tobiiTimer.Interval = new TimeSpan(0, 0, 1);
            tobiiTimer.Start();

            // Initial state
            tempState = TempState.NONE;
            mainState = OnboardState.WELCOME;
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

        public void SetState(DemoState state)
        {
            this.demoState = state;
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        public void SetState(OnboardState state) {
            this.mainState = state;
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        public void SetTempState(TempState state) {
            this.tempState = state;
            RaisePropertyChanged("CurrentPageViewModel");
            StateChanged(this, null);
        }

        public void SetUnrecoverableError()
        {
            this.demoState = DemoState.ERROR;
            errorViewModel.StartRestartCountdown();
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
                switch (demoState)
                {
                    case DemoState.ERROR:
                        return errorViewModel;
                    case DemoState.FIRST_SETUP:
                    case DemoState.RESETTING:
                        return loadingViewModel;
                    case DemoState.NO_USER:
                        if (tempState == TempState.RESET)
                        {
                            return resetViewModel;
                        }
                        else if (tempState == TempState.INFO)
                        {
                            return infoViewModel;
                        }
                        else
                        {
                            return eyesLostViewModel;
                        }
                    case DemoState.RUNNING:
                        switch (tempState)
                        {
                            case TempState.RESET:
                                return resetViewModel;
                            case TempState.INFO:
                                return infoViewModel;
                            case TempState.NONE:
                                switch (mainState)
                                {
                                    case OnboardState.WELCOME:
                                        return introViewModel;
                                    case OnboardState.EYES:
                                        return tobiiViewModel;
                                    case OnboardState.WAIT_CALIB:
                                        return waitCalibViewModel;
                                    case OnboardState.POST_CALIB:
                                        return postCalibViewModel;
                                }
                                break;
                        }
                        break;
                }
                //TODO: blank page?
                return introViewModel;
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

        public void Reset()
        {
            if (tempState == TempState.RESET)
            {
                //DO the reset?!
                SetTempState(TempState.NONE);
                SetState(OnboardState.WELCOME);
                SetState(DemoState.RUNNING);
                tobiiViewModel.SetAutoRestart(false);
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
                    SetTempState(TempState.NONE);
                    break;
                case TempState.RESET:
                //TODO: should we use reset->reset or reset->next ? prefer different keys to prevent misclicks? or simpler to encourage resets?                    
                //TODO: who is responsible for the actual resetting action?
                    SetTempState(TempState.NONE);
                    break;
                case TempState.NONE:

                    switch (mainState)
                    {
                        case OnboardState.WELCOME:
                            SetState(OnboardState.EYES);
                            break;
                        case OnboardState.EYES:
                            if (tobiiViewModel.CanGoForward)
                            {
                                TobiiEyeXPointService.EyeXHost.LaunchGuestCalibration();
                                SetState(OnboardState.WAIT_CALIB);
                            }
                            break;
                        case OnboardState.WAIT_CALIB:
                            SetState(OnboardState.POST_CALIB);
                            break;
                        case OnboardState.POST_CALIB:
                            SetState(OnboardState.IN_MINECRAFT);
                            tobiiViewModel.SetAutoRestart(true);
                            break;
                    }
                    break;
            }
        }

        private void handleTobiiChange(object sender, EngineStateValue<EyeTrackingDeviceStatus> status)
        {
            if (mainState == OnboardState.WAIT_CALIB &&
                status.Value == EyeTrackingDeviceStatus.Tracking)
            {
                SetState(OnboardState.POST_CALIB);
                Demo.SetGhostVisible(true);
            }            
        }

        public void Back()
        {
            switch (demoState)
            {
                case DemoState.ERROR:                   
                case DemoState.FIRST_SETUP:                    
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
                                    break;
                                case OnboardState.WAIT_CALIB:
                                case OnboardState.POST_CALIB:
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

        // FIXME: do we need the setup/teardown stuff?
        //private void SetPageViewModel(PageViewModel pageVM, bool teardownCurrent)
        //{            
        //    if (teardownCurrent) //TOO: distinguish between "stuff to do on finishing page" and "stuff to do whenever closed" e.g. prev vs next
        //    {
        //        // cleanly leave current page
        //        CurrentPageViewModel.TearDown();
        //    }

        //    // set up new page      
        //    pageVM.SetUp();      
        //    CurrentPageViewModel = pageVM;

        //    RaisePropertyChanged("CurrentPageViewModel");                        
        //}

        #endregion
    }
}
