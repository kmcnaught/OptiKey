// Copyright (c) 2020OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Properties;
using System;
using System.Windows;
using log4net;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class GazeSmoothingFilter
    {
        // FIXME: only required for development
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Current estimate
        private Point EstimatedPoint;   //The point used after applying the filter.
        private double EstimationNoise; //This value fluctuates to balance each new measurment with the previous estimate.

        // Model parameters
        //FIXME: remove? private readonly double ProcessNoise; // Q (variance)
        private readonly double MeasurementNoise; // R (variance)
        private double Gain; // K

        public GazeSmoothingFilter()
        {
            EstimatedPoint = new Point(0, 0);
            EstimationNoise = 10000;

            // FIXME: these will be set according to tuning and smoothing level
            //ProcessNoise = 5;
            MeasurementNoise = 1000;
        }

        public Point Update(Point measuredPoint)
        {
            // This is a combined "prediction + update" step
            // Ideally we'd have some timings to allow estimate uncertainty to grow when data missing, in which case we'd separate the two

            // Prediction - process model is "we haven't moved" but with some uncertainty
            // The uncertainty increases with distance of new data from current estimate - if within a fixations-distance
            // we have a narrow prior to enforce smoothness. If far away, we want a uniform prior over all positions. 
            // The exponential process noise captures this smoothly
            var delta = (measuredPoint - EstimatedPoint).Length;
            var currentProcessNoise = Math.Exp(delta / 10);
            currentProcessNoise = 5;

            EstimationNoise = EstimationNoise + currentProcessNoise;

            // Update
            Gain = (EstimationNoise) / (EstimationNoise + MeasurementNoise);
            EstimationNoise = (1.0 - Gain) * EstimationNoise;

            Log.InfoFormat("{0} Measurement: {1}", this.GetHashCode(), measuredPoint);
            Log.InfoFormat("{0} Prediction: {1}", this.GetHashCode(), EstimatedPoint);
            Log.InfoFormat("{0} Uncertainty: {1}", this.GetHashCode(), EstimationNoise);
            Log.InfoFormat("{0} Process Noise: {1}", this.GetHashCode(), currentProcessNoise);
            Log.InfoFormat("{0} Gain: {1}", this.GetHashCode(), Gain);

            EstimatedPoint = EstimatedPoint + (measuredPoint - EstimatedPoint) * Gain;

            return EstimatedPoint;

        }
    }
}
