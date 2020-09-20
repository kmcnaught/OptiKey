// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Properties;
using System.Windows;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class SmoothingEffect
    {
        double ProcessNoise; //Standard deviation - Q
        double MeasurementNoise; // R
        double EstimationConfidence; //P
        double Gain; // K
        private Point lastPoint; // X 
        private KeyValue lastKeyValue;
        private bool newTarget;

        public SmoothingEffect()
        {
            this.ProcessNoise = .01d;
            this.MeasurementNoise = .01d;
            this.EstimationConfidence = 1;
            this.lastPoint = new Point();
            this.lastKeyValue = null;
        }

        public Point Update(Point point, KeyValue keyValue)
        {
            var lockRadius = Settings.Default.PointSelectionTriggerLockOnRadiusInPixels;
            var fixationRadius = Settings.Default.PointSelectionTriggerFixationRadiusInPixels;
            var innerLimit = System.Math.Min(lockRadius, fixationRadius);
            var outerlimit = System.Math.Max(lockRadius, fixationRadius);
            double smoothingLevel = Settings.Default.GazeSmoothingLevel;
            double distanceMoved = (point - lastPoint).Length;
            newTarget = distanceMoved > outerlimit || (distanceMoved > innerLimit / 2 && newTarget);

            if (keyValue != null && keyValue == lastKeyValue) //minimal movement if on same key
                Gain = 0.03;
            else if (keyValue != null) //instant movement if on a new key
                Gain = 1;
            else if (distanceMoved > innerLimit || newTarget)
            {
                MeasurementNoise = smoothingLevel * outerlimit / distanceMoved;
                Gain = newTarget 
                    ? System.Math.Max(Gain, (EstimationConfidence + ProcessNoise) / (EstimationConfidence + ProcessNoise + MeasurementNoise))
                    : (EstimationConfidence + ProcessNoise) / (EstimationConfidence + ProcessNoise + MeasurementNoise);
                EstimationConfidence = MeasurementNoise * (EstimationConfidence + ProcessNoise) / (EstimationConfidence + ProcessNoise + MeasurementNoise);
            }
            else //no movement if within the inner limit
                point = lastPoint;

            Point result = lastPoint + (point - lastPoint) * Gain;
            lastKeyValue = keyValue;
            lastPoint = result;
            return result;
        }
    }
}
