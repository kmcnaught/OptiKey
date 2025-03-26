// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Reactive;
using System.Windows;
using EyeXFramework;
using JuliusSweetland.OptiKey.Enums;
using log4net;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using JuliusSweetland.OptiKey.Properties;

namespace JuliusSweetland.OptiKey.Services
{
    public class TobiiEyeXPointService : IPointService
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private EyePositionDataStream eyePositionDataStream;
        private GazePointDataStream gazeDataStream;
        private FixationDataStream fixationDataStream;

        private event EventHandler<Timestamped<Point>> pointEvent;

        public event EventHandler<EyeTrackingDeviceStatus> tobiiError;


        #endregion

        #region Ctor

        public TobiiEyeXPointService()
        {
            KalmanFilterSupported = true;
            EyeXHost = new EyeXHost();
            Log.Info("Starting the EyeX Host");
            bool success = StartHost();
            Log.Info(success);

            //Disconnect (deactivate) from the EyeXHost on shutdown - otherwise the process can hang
            Application.Current.Exit += (sender, args) =>
            {
                if (EyeXHost != null)
                {
                    Log.Info("Disposing of the EyeXHost.");
                    EyeXHost.Dispose();
                    EyeXHost = null;
                }
            };
        }

        #endregion

        #region Properties

        public bool KalmanFilterSupported {get; private set; }
        public static EyeXHost EyeXHost { get; private set; }

        #endregion

        #region Events

        public event EventHandler<Exception> Error;

        private DateTime firstConnectAttempt = new DateTime(0);
        private DateTime zeroTime = new DateTime(0);
        private int timeoutSeconds = 3*60; // for first connection
        private bool haveConnectedSuccessfully = false;

        bool streamActive = false;

        public bool StartHost()
        {
            //if (EyeXHost.EyeXAvailability == EyeXAvailability.Running &&
              //  !EyeXHost.IsStarted)
            if (!EyeXHost.IsStarted)
            {
                EyeXHost.EyeTrackingDeviceStatusChanged += EyeXHost_EyeTrackingDeviceStatusChanged;

                EyeXHost.Start(); // Start the EyeX host
                return true;
            }
            else return EyeXHost.IsStarted;
        }

        private bool SetupEyeXStreams()
        {
            DisposeEyeXStreams(); // no-op if not setup

            Log.Info("Checking the state of the Tobii service...");

            if (firstConnectAttempt == zeroTime)
            {
                firstConnectAttempt = DateTime.Now;
            }

            TimeSpan timeSinceFirstAttempt = DateTime.Now.Subtract(firstConnectAttempt);

            switch (EyeXHost.EyeXAvailability)
            {
                case EyeXAvailability.NotAvailable:
                    PublishError(this, new ApplicationException(Resources.TOBII_EYEX_ENGINE_NOT_FOUND));
                    return false;

                case EyeXAvailability.NotRunning:
                    if (!haveConnectedSuccessfully && timeSinceFirstAttempt > new TimeSpan(0, 0, timeoutSeconds))
                    {
                        PublishError(this, new ApplicationException(Resources.TOBII_EYEX_ENGINE_NOT_RUNNING));
                    }
                    return false;

                case EyeXAvailability.Running:
                    haveConnectedSuccessfully = true;
                    break;
            }

            Log.Info("Attaching eye tracking device status changed listener to the Tobii service.");

            EyeXHost.EyeTrackingDeviceStatusChanged += EyeXHost_EyeTrackingDeviceStatusChanged;

            if (Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.None ||
               Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.Low)
            {
                gazeDataStream = EyeXHost.CreateGazePointDataStream(
                    Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.None
                        ? GazePointDataMode.Unfiltered //None
                        : GazePointDataMode.LightlyFiltered); //Low

                if (!EyeXHost.IsStarted)
                {
                    EyeXHost.Start(); // Start the EyeX host
                }

                gazeDataStream.Next += (s, data) =>
                {
                    if (pointEvent != null
                        && !double.IsNaN(data.X)
                        && !double.IsNaN(data.Y))
                    {
                        pointEvent(this, new Timestamped<Point>(new Point(data.X, data.Y),
                            new DateTimeOffset(DateTime.UtcNow).ToUniversalTime())); //EyeX does not publish a useable timestamp
                    }
                };
            }
            else
            {
                fixationDataStream = EyeXHost.CreateFixationDataStream(
                    Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.Medium
                        ? FixationDataMode.Sensitive //Medium
                        : FixationDataMode.Slow); //Hight

                if (!EyeXHost.IsStarted)
                {
                    EyeXHost.Start(); // Start the EyeX host
                }

                fixationDataStream.Next += (s, data) =>
                {
                    if (pointEvent != null
                        && !double.IsNaN(data.X)
                        && !double.IsNaN(data.Y))
                    {
                        pointEvent(this, new Timestamped<Point>(new Point(data.X, data.Y),
                            new DateTimeOffset(DateTime.UtcNow).ToUniversalTime())); //EyeX does not publish a useable timestamp
                    }
                };
            }            
            return true;
        }

        private void DisposeEyeXStreams()
        {
            if (gazeDataStream != null)
            {
                gazeDataStream.Dispose();
                gazeDataStream = null;
            }

            if (fixationDataStream != null)
            {
                fixationDataStream.Dispose();
                fixationDataStream = null;
            }
        }

        public event EventHandler<Timestamped<Point>> Point
        {
            add
            {
                if (pointEvent == null) // i.e. no one has subscribed yet?
                {
                    // "NotAvailable" means the library isn't even installed
                    if (EyeXHost.EyeXAvailability == EyeXAvailability.NotAvailable)
                    {
                        PublishError(this, new ApplicationException(Resources.TOBII_EYEX_ENGINE_NOT_FOUND));
                        return;
                    }

                    //streamActive = SetupEyeXStreams();
                }

                pointEvent += value;
            }
            remove
            {
                pointEvent -= value;

                if (pointEvent == null)
                {
                    Log.Info("Last listener of Point event has unsubscribed. Disposing gaze data & fixation data streams.");

                    DisposeEyeXStreams();
                }
            }
        }

        private void EyeXHost_EyeTrackingDeviceStatusChanged(object sender, EngineStateValue<EyeTrackingDeviceStatus> e)
        {
            Log.InfoFormat("Tobii EyeX tracking device status changed to {0} (IsValid={1})", e, e.IsValid);
            
            //can be 0: invalid at first and on shutdown, not sure if this happens at other times
            switch(e.Value)
            {
                case 0:
                    break;
                case EyeTrackingDeviceStatus.Initializing:
                    break;                
                case EyeTrackingDeviceStatus.InvalidConfiguration:
                    break;
                case EyeTrackingDeviceStatus.DeviceNotConnected:
                    break;
                case EyeTrackingDeviceStatus.Tracking:
                    SetupEyeXStreams();
                    break;
                case EyeTrackingDeviceStatus.TrackingPaused:
                    break;
                case EyeTrackingDeviceStatus.Configuring:
                    break;
                case EyeTrackingDeviceStatus.NotAvailable:
                    DisposeEyeXStreams();
                    break;
                case EyeTrackingDeviceStatus.UnknownError:                    
                case EyeTrackingDeviceStatus.ConnectionError:
                    tobiiError(this, e.Value);
                    break;
            }
        }

        #endregion

        #region Publish Error

        private void PublishError(object sender, Exception ex)
        {
            Log.Error("Publishing Error event (if there are any listeners)", ex);
            if (Error != null)
            {
                Error(sender, ex);
            }
        }

        #endregion
    }
}