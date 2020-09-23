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
        private double EstimationNoise;
        private double Gain;
        private double SmoothingLevel;
        private KeyValue PreviousKeyValue;
        private Point EstimatedPoint;

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public SmoothingEffect()
        {
            PreviousKeyValue = null;
            EstimatedPoint = new Point();
            SmoothingLevel = Settings.Default.GazeSmoothingLevel;
        }

        public Point Update(Point measuredPoint, KeyValue measuredKeyValue)
        {
            if (Settings.Default.KalmanFilterEnabled)
            {
                Log.InfoFormat("{0} Measurement: {1}", this.GetHashCode(), measuredPoint.X);

                double distanceMoved = (measuredPoint - EstimatedPoint).Length;

                if (measuredKeyValue != null && measuredKeyValue == PreviousKeyValue)
                {
                    Gain = 0.06; //minimal movement if on same key
                }
                else if (measuredKeyValue != null)
                {
                    Gain = 1; //instant movement if on a new key
                }
                else if (distanceMoved < 20 + SmoothingLevel)
                {
                    SmoothingLevel = Settings.Default.GazeSmoothingLevel;
                    measuredPoint = EstimatedPoint;
                }
                else
                {
                    var currentProcessNoise = Math.Exp(distanceMoved / 100);
                    EstimationNoise = EstimationNoise + currentProcessNoise;
                    Gain = (EstimationNoise) / (EstimationNoise + (2000 * SmoothingLevel));
                    EstimationNoise = (1.0 - Gain) * EstimationNoise;
                }

                Point result = EstimatedPoint + (measuredPoint - EstimatedPoint) * Gain;
                PreviousKeyValue = measuredKeyValue;
                EstimatedPoint = result;

                Log.InfoFormat("{0} Prediction: {1}", this.GetHashCode(), result.X);
                Log.InfoFormat("{0} Gain: {1}", this.GetHashCode(), Gain);

                return result;
            }
            else
            {
                return measuredPoint;
            }
        }
    }
}
