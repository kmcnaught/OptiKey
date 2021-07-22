using EyeXFramework;
using JuliusSweetland.OptiKey.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Tobii.EyeX.Framework;

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

        public OnboardState mainState;
        public TempState tempState;        

        private IntroViewModel introViewModel;
        private TobiiViewModel tobiiViewModel;
        private WaitCalibViewModel waitCalibViewModel;
        private PostCalibViewModel postCalibViewModel;
        private InfoViewModel infoViewModel;
        private ResetViewModel resetViewModel;

        public OnboardingViewModel()
        {
            // Create view models
            introViewModel = new IntroViewModel();
            tobiiViewModel = new TobiiViewModel();
            waitCalibViewModel = new WaitCalibViewModel();
            postCalibViewModel = new PostCalibViewModel();
            infoViewModel = new InfoViewModel();
            resetViewModel = new ResetViewModel();

            // Register for Tobii events
            TobiiEyeXPointService.EyeXHost.EyeTrackingDeviceStatusChanged += handleTobiiChange;

            // Initial state
            tempState = TempState.NONE;
            mainState = OnboardState.WELCOME;
        }

        //FIXME: set up tear down?

        public void SetState(OnboardState state) {
            this.mainState = state;
            RaisePropertyChanged("CurrentPageViewModel");
        }

        public void SetTempState(TempState state) {
            this.tempState = state;
            RaisePropertyChanged("CurrentPageViewModel");
        }

        #region Properties         

        public PageViewModel CurrentPageViewModel
        {
            get
            {
                switch (tempState) {
                    case TempState.RESET:
                        return resetViewModel;
                    case TempState.INFO:
                        return infoViewModel;
                    case TempState.NONE:
                        switch (mainState) {
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
            }
            else
            {
                // TODO: think about precedence of reset / info
                SetTempState(TempState.RESET);
            }
        }


        public void Next()
        {   
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
            switch (tempState) {
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
                            SetState(OnboardState.POST_CALIB);
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
