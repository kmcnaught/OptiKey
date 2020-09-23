// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Properties;
using System;
using System.Windows;
using log4net;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class SmoothingEffect
    {
        private double innerLimit;
        private double outerLimit;
        private double smoothingLevel;
        private DateTime anchorTime;
        private KeyValue keyValue;
        private Point smoothedPoint;
        private bool unAnchored;

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public SmoothingEffect()
        {
            LoadSettings();
            keyValue = null;
            smoothedPoint = new Point();
        }

        private void LoadSettings()
        {
            var lockRadius = Settings.Default.PointSelectionTriggerLockOnRadiusInPixels;
            var fixationRadius = Settings.Default.PointSelectionTriggerFixationRadiusInPixels;
            innerLimit = Math.Min(lockRadius, fixationRadius);
            outerLimit = Math.Max(lockRadius, fixationRadius);
            smoothingLevel = Settings.Default.GazeSmoothingLevel;
        }

        public Point Update(Point nextPoint, KeyValue nextKeyValue)
        {
            Log.InfoFormat("{0} Measurement: {1}", this.GetHashCode(), nextPoint.X);

            double distanceMoved = (nextPoint - smoothedPoint).Length;
            double gain = (distanceMoved / ((innerLimit + outerLimit) / 2)) 
                        / (distanceMoved / ((innerLimit + outerLimit) / 2) + smoothingLevel);

            if (nextKeyValue != null && nextKeyValue == keyValue) //minimal movement if on same key
            {
                gain = 0.06;
            }
            else if (nextKeyValue != null) //instant movement if on a new key
            {
                gain = 1;
            }
            else if (unAnchored) //faster movement if we have gone outside the outer limit
            {
                anchorTime = DateTime.Now;
                if (distanceMoved < innerLimit)
                    unAnchored = false;
            }
            else if (distanceMoved > innerLimit) //slower movement if we have not gone outside of the outer limit
            {
                gain = (distanceMoved / outerLimit) / (distanceMoved / outerLimit + smoothingLevel);
                anchorTime = DateTime.Now;
                if (distanceMoved > outerLimit)
                    unAnchored = true;
            }
            else if (anchorTime < DateTime.Now - TimeSpan.FromMilliseconds(200)) //no movement after 200ms within the inner limit
            {
                LoadSettings();
                nextPoint = smoothedPoint;
            }

            Point result = smoothedPoint + (nextPoint - smoothedPoint) * gain;
            keyValue = nextKeyValue;
            smoothedPoint = result;

            Log.InfoFormat("{0} Prediction: {1}", this.GetHashCode(), result.X);
            Log.InfoFormat("{0} Gain: {1}", this.GetHashCode(), gain);

            return result;
        }
    }
}
