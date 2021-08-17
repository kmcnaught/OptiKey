
using System;
using System.Collections.Generic;
using System.Windows.Threading;
using EyeXFramework;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.UI.Windows;
using Prism.Mvvm;

using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit

{
    public class TobiiViewModel : PageViewModel
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public bool handleAutoRestart = false;

        public TobiiViewModel()
        {
            SetUp();
        }
                
        private static EyePositionDataStream _eyePositionDataStream;

        // Read/write to float variable are atomic so we don't need to worry about mutexes
        public EyeStatus leftEye = new EyeStatus();
        public EyeStatus rightEye = new EyeStatus();

        public override void SetUp()
        {
            SetInitTime();

            CanGoBackward = true;
            CanGoForward = false;

            if (null == _eyePositionDataStream)
            {
                // Set up streams
                _eyePositionDataStream = TobiiEyeXPointService.EyeXHost.CreateEyePositionDataStream();
                _eyePositionDataStream.Next += (s, eyePosition) => {
                    leftEye.Update(eyePosition.LeftEyeNormalized.IsValid,
                                    1.0f - (float)eyePosition.LeftEyeNormalized.X,
                                    (float)eyePosition.LeftEyeNormalized.Y,
                                    (float)eyePosition.LeftEyeNormalized.Z);

                    rightEye.Update(eyePosition.RightEyeNormalized.IsValid,
                                    1.0f - (float)eyePosition.RightEyeNormalized.X,
                                    (float)eyePosition.RightEyeNormalized.Y,
                                    (float)eyePosition.RightEyeNormalized.Z);

                    TimeSpan timeSpan = DateTime.Now - initTime;
                    canGoForward = isGoodEnough = CalculateIsGoodEnough();

                    // TODO: some filtering!
                    //FIXME: this might only gets updated if there's an eye visible?? 
                    RaisePropertyChanged("CanGoForward");
                    RaisePropertyChanged("IsGoodEnough");

                    this.UpdateVisibility();

                    this.UpdateLostTracking();

                    // Update eye sizes, which are relative to UI scale and z distance
                    this.UpdateSize();

                    // Update traffic-light colours of eyes and border
                    this.UpdateColour();

                    RaisePropertyChanged("IsGoodEnough");

                };
            }
        }
        
        private void UpdateLostTracking()
        {
            DateTime now = DateTime.Now;
            int maxSecs = 3;
            TimeSpan maxTimeSpan = new TimeSpan(0, 0, maxSecs);
            lostTracking = (now.Subtract(leftEye.lastSeen) > maxTimeSpan &&
                now.Subtract(rightEye.lastSeen) > maxTimeSpan);            

            RaisePropertyChanged("LostTracking");
        }

        public override void TearDown()
        {
            
        }

        protected bool isGoodEnough = false;
        public bool IsGoodEnough
        {
            get { return isGoodEnough; }
            set { SetProperty(ref isGoodEnough, value); }
        }

        protected bool lostTracking = false;
        public bool LostTracking
        {
            get { UpdateLostTracking();  return lostTracking; }
            set { SetProperty(ref lostTracking, value); }
        }

        private bool CalculateIsGoodEnough()
        {
            // allow user to progress if either:
            // (a) both eyes are visible, and 'okay'
            // (b) one eye is visible, and 'good'

            // ideally we want a timer which reduces in strictness over time
            // and maybe a "are you there? I'm struggling to see you" hint after
            // ages.
            
            if (!leftEyeVisible && !rightEyeVisible)
            {
                return false;
            }

            double absLeftEyeZdiff = Math.Abs(leftEye.zPos - 0.5);
            double absRightEyeZdiff = Math.Abs(rightEye.zPos - 0.5);

            if (leftEyeVisible && rightEyeVisible &&
                (absLeftEyeZdiff < 0.4 || absRightEyeZdiff < 0.4))
            {
                return true;
            }

            if ((leftEyeVisible && absLeftEyeZdiff < 0.2) ||
                (rightEyeVisible && absRightEyeZdiff < 0.2))
            {
                return true;
            }

            return false;

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
        
    }
}
