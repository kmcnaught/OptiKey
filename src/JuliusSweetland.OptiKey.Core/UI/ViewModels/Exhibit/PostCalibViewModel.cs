using EyeXFramework;
using JuliusSweetland.OptiKey.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.EyeX.Framework;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class PostCalibViewModel : PageViewModel
    {
        public PostCalibViewModel()
        {
            CanGoBackward = true;
            CanGoForward = true;
        }

        public override void SetUp()
        {
            SetInitTime();

            // Sign up for "tracking status changed" event - this is triggered at end of calibration 
            // (regardless of success or failure..)            
            TobiiEyeXPointService.EyeXHost.EyeTrackingDeviceStatusChanged += handleTobiiChange;

            TobiiEyeXPointService.EyeXHost.LaunchGuestCalibration();
        }

        private void handleTobiiChange(object sender, EngineStateValue<EyeTrackingDeviceStatus> status)
        {
            if (status.Value == EyeTrackingDeviceStatus.Tracking)
            {
                waitingForCalibration = false;
            }
            else
            {
                waitingForCalibration = true;
            }
            RaisePropertyChanged("WaitingForCalibration");
        }

        public override void TearDown()
        {            
            waitingForCalibration = false;
            RaisePropertyChanged("WaitingForCalibration");

            TobiiEyeXPointService.EyeXHost.EyeTrackingDeviceStatusChanged -= handleTobiiChange;
        }


        private bool waitingForCalibration = true;
        public bool WaitingForCalibration
        {
            get { return waitingForCalibration; }
            set { SetProperty(ref waitingForCalibration, value); }
        }
    }
}
