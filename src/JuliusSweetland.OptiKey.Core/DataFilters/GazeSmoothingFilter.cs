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
            MeasurementNoise = 2000;
        }

        public Point Update(Point measuredPoint)
        {
            // This is a combined "prediction + update" step
            // Ideally we'd have some timings to allow estimate uncertainty to grow when data missing, in which case we'd separate the two

            // Prediction - process model is "we haven't moved" but with some uncertainty
            // The uncertainty increases with distance of new data from current estimate - if within a fixations-distance
            // we have a narrow prior to enforce smoothness. If far away, we want a uniform prior over all positions. 
            // The exponential process noise captures this smoothly


            // ========================================================//
            // SOME VARIABLES TO PLAY WITH PROCESS NOISE MAPPING

            // dictates scale across all deltas
            // lower = more smoothness (mainly noticeable at fixation)
            var processScale = 0.01d;

            // dictates how quickly PN scales up with delta
            // higher = extends smoothness for longer saccades
            var processIncreaseScaleFactor = 10.0d; 

            // extends the "fixation zone" for extra smoothness without affecting later ramp-up (this keeps PN > 0.0)
            var processDeadzoneOffset = 20.0;

            // alternatively extends the "fixation zone" with a subtraction - this supports a region of zero process noise, i.e. a hard deadzone
            var processDeadzone = 0.0;

            // ======================================================== //

            var delta = (measuredPoint - EstimatedPoint).Length;
            //var currentProcessNoise = Math.Exp(delta/processIncreaseScaleFactor);
            var currentProcessNoise = processScale * Math.Exp((delta - processDeadzoneOffset) / processIncreaseScaleFactor) - processDeadzone;

            if (processDeadzone > 0.0)
            {
                currentProcessNoise -= Math.Log(processDeadzone);
                currentProcessNoise = Math.Max(0.0, currentProcessNoise); // force non-negative
            }
            
            // process noise can be very low when fixating since data comes in v fast

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
