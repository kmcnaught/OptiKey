// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class KalmanFilter
    {
        private readonly double ProcessNoise; // Q (1/variance)
        private readonly double MeasurementNoise; // R (1/variance)
        private double EstimationNoise; //P (1/variance)
        private double? EstimatedValue; // X 
        private double Gain; // K

        // We'll discard our model when saccades exceed this: this is the point at which our plant model is very non-gaussian
        private double MaxMicroSaccade; 

        public KalmanFilter(double initialValue = 0f, double confidenceOfInitialValue = 1f, double processNoise = 0.0001f, double measurementNoise = 0.01f)
        {
            // TODO: remove "initial value" settings
            this.ProcessNoise = processNoise;
            this.MeasurementNoise = measurementNoise;
            this.EstimationNoise = 0f;
            this.EstimatedValue = null; 
            this.MaxMicroSaccade = 50; // pixels? default to % of screen?
        }

        public double Update(double measurement)
        {
            // Initialisation, or re-initialisation after a big jump
            if (!EstimatedValue.HasValue || Math.Abs(EstimatedValue.Value - measurement) > MaxMicroSaccade)
            {
                EstimatedValue = measurement;
                EstimationNoise = MeasurementNoise;
            }

            // This is a combined "prediction + update" step
            // Ideally we'd have some timings to allow estimate uncertainty to grow when data missing, in which case we'd separate the two

            // Prediction - process model is "we haven't moved" but with some uncertainty
            EstimationNoise = EstimationNoise + ProcessNoise;

            // Update
            Gain = (EstimationNoise) / (EstimationNoise + MeasurementNoise);
            EstimationNoise = (1.0 - Gain) * EstimationNoise;
            double result = EstimatedValue.Value + (measurement - EstimatedValue.Value) * Gain;
            EstimatedValue = result;

            return result;
        }
    }
}
