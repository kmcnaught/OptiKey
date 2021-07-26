using EyeXFramework;
using JuliusSweetland.OptiKey.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        public TobiiWatcher()
        {
            // Register for Tobii events
            TobiiEyeXPointService.EyeXHost.EyeTrackingDeviceStatusChanged += handleTobiiChange;
        }

        private void handleTobiiChange(object sender, EngineStateValue<EyeTrackingDeviceStatus> status)
        {
            if (status.IsValid)
            {
                //can be 0: invalid at first and on shutdown, not sure if this happens at other times
                switch (status.Value)
                {
                    case 0:
                        break;
                    case EyeTrackingDeviceStatus.Initializing:
                        break;
                    case EyeTrackingDeviceStatus.Tracking:
                        EnteredTrackingState(this, null);
                        break;
                    case EyeTrackingDeviceStatus.TrackingPaused:
                        break;
                    case EyeTrackingDeviceStatus.Configuring:
                        break;
                    case EyeTrackingDeviceStatus.NotAvailable:
                    case EyeTrackingDeviceStatus.UnknownError:
                    case EyeTrackingDeviceStatus.ConnectionError:
                    case EyeTrackingDeviceStatus.InvalidConfiguration:
                        EnteredErrorState(this, status.Value);
                        break;
                    case EyeTrackingDeviceStatus.DeviceNotConnected:
                        break;
                }
            }
        }
    }
}
