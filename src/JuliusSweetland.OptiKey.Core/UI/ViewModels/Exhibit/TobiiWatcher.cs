using EyeXFramework;
using JuliusSweetland.OptiKey.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Tobii.EyeX.Framework;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class TobiiWatcher
    {
        public event EventHandler EyesNotVisible = delegate { };

        public event EventHandler<EyeTrackingDeviceStatus> EnteredErrorState = delegate { };
        public event EventHandler EnteredTrackingState = delegate { };
        public event EventHandler RecoveredErrorState = delegate { };

        private EyeTrackingDeviceStatus lastStatus = 0; // i.e. not valid yet
        private bool inErrorState = false; // keep track of this to spot recovery

        public TobiiWatcher()
        {
            // Register for Tobii events
            TobiiEyeXPointService.EyeXHost.EyeTrackingDeviceStatusChanged += handleTobiiChange;
        }

        public EyeTrackingDeviceStatus GetCurrentState()
        {
            return lastStatus;
        }

        private bool IsErrorState(EyeTrackingDeviceStatus state)
        {
            switch (state)
            {
                case EyeTrackingDeviceStatus.DeviceNotConnected:
                case EyeTrackingDeviceStatus.NotAvailable:
                case EyeTrackingDeviceStatus.UnknownError:
                case EyeTrackingDeviceStatus.ConnectionError:
                case EyeTrackingDeviceStatus.InvalidConfiguration:
                    return true;                    
                default:
                    return false;
            }
        }

        private void handleTobiiChange(object sender, EngineStateValue<EyeTrackingDeviceStatus> status)
        {
            if (status.IsValid)
            {
                EyeTrackingDeviceStatus newStatus = status.Value;
                if (newStatus != lastStatus)
                {
                    bool nowInErrorState = IsErrorState(newStatus);

                    if (newStatus == EyeTrackingDeviceStatus.Tracking)
                    {
                        EnteredTrackingState(this, null);
                        if (inErrorState)
                        {
                            inErrorState = false;
                            RecoveredErrorState(this, null);
                            
                        }
                    }
                    else if (IsErrorState(newStatus)) {
                        inErrorState = true;
                        EnteredErrorState(this, status.Value);
                      
                    }

                    lastStatus = newStatus;
                }
            }
        }
    }
}
