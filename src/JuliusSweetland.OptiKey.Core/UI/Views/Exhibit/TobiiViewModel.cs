using System;
using System.Collections.Generic;
using EyeXFramework;
using JuliusSweetland.OptiKey.Services;
using Prism.Mvvm;

using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;

namespace Per_FrameAnimation
{
    public class TobiiViewModel : BindableBase
    {
        //private static EngineStateObserver<EyeTrackingDeviceStatus> stateObserver;
        public static EyeXHost EyeXHost { get; private set; }
        private static EyePositionDataStream _eyePositionDataStream;


        // Read/write to float variable are atomic so we don't need to worry about mutexes
        public EyeStatus leftEye = new EyeStatus();
        public EyeStatus rightEye = new EyeStatus();

        public TobiiViewModel()
        {

            TobiiLabel = "wibble";


            // Connect to Tobii
            InitializeHost();

            // Set up streams
            _eyePositionDataStream = EyeXHost.CreateEyePositionDataStream();    
            _eyePositionDataStream.Next +=(s, eyePosition) => {
                leftEye.Update(eyePosition.LeftEyeNormalized.IsValid,
                                1.0f - (float)eyePosition.LeftEyeNormalized.X,
                                (float)eyePosition.LeftEyeNormalized.Y,
                                (float)eyePosition.LeftEyeNormalized.Z);

                rightEye.Update(eyePosition.RightEyeNormalized.IsValid,
                                1.0f - (float)eyePosition.RightEyeNormalized.X,
                                (float)eyePosition.RightEyeNormalized.Y,
                                (float)eyePosition.RightEyeNormalized.Z);
                                
                this.UpdateVisibility();

                // Update eye sizes, which are relative to UI scale and z distance
                this.UpdateSize();

                // Update traffic-light colours of eyes and border
                this.UpdateColour();
            };
        }

        private void UpdateSize()
        {
            // Set size according to z  
            // zDiff = -0.5 = close = small 
            //          0   = midline = average
            //         +0.5 = far = large 

            double leftEyeZdiff = leftEye.zPos - 0.5;
            double rightEyeZdiff = rightEye.zPos - 0.5;

            double sizeLeft = 0.1 - 0.05 * leftEyeZdiff;
            double sizeRight = 0.1 - 0.05 * rightEyeZdiff;

            //FIXME: need to get updates from trackbox height for scaling?
            float scaleY = 200f;
            LeftEyeSize = sizeLeft * scaleY;
            RightEyeSize = sizeRight * scaleY;

        }

        private void UpdateVisibility()
        {
            LeftEyeVisible = leftEye.visible && leftEye.xPos > 0.0f;
            RightEyeVisible = rightEye.visible && rightEye.xPos > 0.0f;
        }

        private void UpdateColour()
        {

        }


        #region Properties

        private double leftEyeSize;
        public double LeftEyeSize
        {
            get { return leftEyeSize; }
            set { SetProperty(ref leftEyeSize, value); }
        }

        private double rightEyeSize;
        public double RightEyeSize
        {
            get { return rightEyeSize; }
            set { SetProperty(ref rightEyeSize, value); }
        }

        private bool leftEyeVisible;
        public bool LeftEyeVisible
        {
            get { return leftEyeVisible; }
            set { SetProperty(ref leftEyeVisible, value); }
        }

        private bool rightEyeVisible;
        public bool RightEyeVisible
        {
            get { return rightEyeVisible; }
            set { SetProperty(ref rightEyeVisible, value); }
        }

        private string label;
        public string TobiiLabel
        {
            get { return label; }
            set { SetProperty(ref label, value); }
        }
        #endregion


        private static List<EyeTrackingDeviceStatus> states;

        #region Tobii handling
        private static void InitializeHost()
        {
            // Everything starts with initializing Host, which manages connection to the 
            // Tobii Engine and provides all the Tobii Core SDK functionality.
            // NOTE: Make sure that Tobii.EyeX.exe is running
            EyeXHost = TobiiEyeXPointService.EyeXHost;
            
            //EyeXHost.Context.ConnectionStateChanged += OnConnectionStateChanged;

            //stateObserver = EyeXHost.States.CreateEyeTrackingDeviceStatusObserver();
        }

        private static void StateObserver_Changed(object sender, EngineStateValue<EyeTrackingDeviceStatus> e)
        {
            EyeTrackingDeviceStatus status = e.Value;
            states.Add(status);
            if (status == EyeTrackingDeviceStatus.Tracking)
            {
                //stateObserver.Changed -= StateObserver_Changed;
            }

            int a = 1;
        }

        private static void DisableConnectionWithTobiiEngine()
        {
            // We should disable connection with TobiiEngine before exit the application.
            if (EyeXHost != null)
            {
                Console.WriteLine("Disposing of the EyeXHost.");
                EyeXHost.Dispose();
                EyeXHost = null;
            }
        }

        public void GuestCalibration()
        {

            //var state2 = EyeXHost.EyeTrackingDeviceStatusChanged;

            //states = new List<EyeTrackingDeviceStatus>();
            //stateObserver.Changed += StateObserver_Changed;
            EyeXHost.LaunchGuestCalibration();            

        }

        //private static void OnConnectionStateChanged(object sender, Tobii.Interaction.Client.ConnectionStateChangedEventArgs e)
        //{
        //    var state = e.State;
        //}

        #endregion
    }
}
