// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class KalmanFilter
    {
        double ProcessNoise; //Standard deviation - Q
        double MeasurementNoise; // R
        double EstimationConfidence; //P
        double? EstimatedValue; // X 
        double Gain; // K

        // We'll discard our model when saccades exceed this: this is the point at which our plant model is very non-gaussian
        private double MaxMicroSaccade; 

        public KalmanFilter(double initialValue = 0f, double confidenceOfInitialValue = 1f, double processNoise = 0.0001f, double measurementNoise = 0.01f)
        {
            // TODO: remove "initial value" settings
            this.ProcessNoise = processNoise;
            this.MeasurementNoise = measurementNoise;
            this.EstimationConfidence = 0.1f;
            this.EstimatedValue = null; 
            this.MaxMicroSaccade = 50; // pixels? default to % of screen?
        }

        public double Update(double measurement)
        {
            // Initialisation, or re-initialisation after a big jump
            if (!EstimatedValue.HasValue || Math.Abs(EstimatedValue.Value - measurement) > MaxMicroSaccade)
            {
                EstimatedValue = measurement;
                EstimationConfidence = MeasurementNoise;
            }

            // This is a combined "prediction + update" step
            // Ideally we'd have some timings to allow estimate uncertainty to grow when data missing, in which case we'd separate the two

            // Prediction - process model is "we haven't moved" but with some uncertainty
            EstimationConfidence = EstimationConfidence + ProcessNoise;

            // Update
            Gain = (EstimationConfidence) / (EstimationConfidence + MeasurementNoise);
            EstimationConfidence = (1.0 - Gain) * EstimationConfidence;
            double result = EstimatedValue.Value + (measurement - EstimatedValue.Value) * Gain;
            EstimatedValue = result;

            return result;
        }
    }
}
