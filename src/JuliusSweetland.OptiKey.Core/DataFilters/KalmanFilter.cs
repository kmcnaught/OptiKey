// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using log4net;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class KalmanFilter
    {
        double ProcessNoise; //Standard deviation - Q
        double MeasurementNoise; // R
        double EstimationConfidence; //P
        double EstimatedValue; // X 
        double Gain; // K

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public KalmanFilter(double initialValue = 0f, double confidenceOfInitialValue = 1f, double processNoise = 0.0001f, double measurementNoise = 0.01f)
        {
            this.ProcessNoise = processNoise;
            this.MeasurementNoise = measurementNoise;
            this.EstimationConfidence = confidenceOfInitialValue;
            this.EstimatedValue = initialValue;
        }

        public double Update(double measurement)
        {
            Gain = (EstimationConfidence + ProcessNoise) / (EstimationConfidence + ProcessNoise + MeasurementNoise);
            EstimationConfidence = MeasurementNoise * (EstimationConfidence + ProcessNoise) / (MeasurementNoise + EstimationConfidence + ProcessNoise);
            double result = EstimatedValue + (measurement - EstimatedValue) * Gain;
            EstimatedValue = result;

            Log.InfoFormat("{0} Measurement: {1}", this.GetHashCode(), measurement);
            Log.InfoFormat("{0} Prediction: {1}", this.GetHashCode(), EstimatedValue);
            Log.InfoFormat("{0} Uncertainty: {1}", this.GetHashCode(), EstimationConfidence);
            Log.InfoFormat("{0} Gain: {1}", this.GetHashCode(), Gain);

            return result;
        }
    }
}
