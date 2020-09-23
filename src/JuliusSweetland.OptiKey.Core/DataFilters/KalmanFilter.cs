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
            Log.InfoFormat("{0} Measurement: {1}", this.GetHashCode(), measurement);

            return measurement;
        }
    }
}
