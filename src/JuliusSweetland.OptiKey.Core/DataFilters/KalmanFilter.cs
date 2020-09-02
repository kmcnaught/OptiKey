// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System;
using System.Diagnostics;
using log4net;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class KalmanFilter
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly double ProcessNoise; // Q (variance)
        private readonly double MeasurementNoise; // R (variance)
        private double EstimationNoise; // P (variance)
        private double? EstimatedValue; // X 
        private double Gain; // K

        // We'll discard our model when saccades exceed this many sigmas from the estimate: this is the point at which our process model
        // stops being relevant
        private double MaxMicroSaccadeSigma; 

        public KalmanFilter(double initialValue = 0f, double confidenceOfInitialValue = 1f, double processNoise = 0.0001f, double measurementNoise = 0.01f)
        {
            // TODO: remove "initial value" settings which are now unused
            // TODO: consider processNoise and measurementNoise as % of screen? 
            this.ProcessNoise = processNoise;
            this.MeasurementNoise = measurementNoise;
            this.EstimationNoise = 0f;
            this.EstimatedValue = null;
            this.MaxMicroSaccadeSigma = 6; 

            // HACK FOR TESTING
            this.ProcessNoise = 5;
            this.MeasurementNoise = 2000;
        }

        private void Init(double measurement)
        {
            EstimatedValue = measurement;
            EstimationNoise = ProcessNoise + MeasurementNoise;
        }

        public double Update(double measurement)
        {
            // This is a combined "prediction + update" step
            // Ideally we'd have some timings to allow estimate uncertainty to grow when data missing, in which case we'd separate the two

            if (!EstimatedValue.HasValue)
                Init(measurement);

            // Prediction - process model is "we haven't moved" but with some uncertainty
            // The uncertainty increases with distance of new data from current estimate - if within a fixations-distance
            // we have a narrow prior to enforce smoothness. If far away, we want a uniform prior over all positions. 
            // The exponential process noise captures this smoothly
            var delta = Math.Abs(measurement - EstimatedValue.Value);
            var currentProcessNoise = Math.Exp(delta/10);

            EstimationNoise = EstimationNoise + currentProcessNoise;
            
            // Update
            Gain = (EstimationNoise) / (EstimationNoise + MeasurementNoise);
            EstimationNoise = (1.0 - Gain) * EstimationNoise;

            Log.InfoFormat("{0} Measurement: {1}", this.GetHashCode(), measurement);
            Log.InfoFormat("{0} Prediction: {1}", this.GetHashCode(), EstimatedValue.Value);
            Log.InfoFormat("{0} Uncertainty: {1}", this.GetHashCode(), EstimationNoise);
            Log.InfoFormat("{0} Gain: {1}", this.GetHashCode(), Gain);

            EstimatedValue = EstimatedValue.Value + (measurement - EstimatedValue.Value) * Gain;

            return EstimatedValue.Value;
        }
    }
}
