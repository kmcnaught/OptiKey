// Copyright (c) 2020OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Properties;
using System;
using System.Windows;

namespace JuliusSweetland.OptiKey.DataFilters
{
    public class GazeSmoothingFilter
    {
        private Point EstimatedPoint;   //The point used after applying the filter.
        private double EstimationNoise; //This value fluctuates to balance each new measurment with the previous estimate.
        private Point Measurement1;
        private Point Measurement2;
        private Point Measurement3;
        private Point Measurement4;
        private Point Measurement5;
        private Point Measurement6;
        private Point Measurement7;
        private Point Measurement8;

        public GazeSmoothingFilter()
        {
            EstimatedPoint = new Point(0, 0);
            EstimationNoise = 10000;
        }

        public Point Update(Point measuredPoint)
        {
            //We want process smoothness to dominate within the range of microsaccades, but to converge
            //towards a uniform prior for larger jumps. An exponential mapping lets this happen smoothly.

            //User setting that affects the number of points used to calculate the center.
            //Higher levels use more points, are more forgiving, and are less responsive to input.
            var smoothingLevel = Settings.Default.GazeSmoothingLevel;

            //Use a weighted average of 3 points. Using only a single point would be susceptible to micro jumps
            if (smoothingLevel==Enums.DataStreamProcessingLevels.Low)
            {
                var mX = measuredPoint.X * 0.467 + Measurement1.X * 0.333 + Measurement2.X * 0.2;
                var mY = measuredPoint.Y * 0.467 + Measurement1.Y * 0.333 + Measurement2.Y * 0.2;
                Measurement2 = Measurement1;
                Measurement1 = new Point(mX, mY);
            }
            //Use the weighted average of the last few measurements to steady the cursor
            else if (smoothingLevel == Enums.DataStreamProcessingLevels.Medium)
            {
                var mX = measuredPoint.X * 0.30
                        + Measurement1.X * 0.30
                        + Measurement2.X * 0.175
                        + Measurement3.X * 0.125
                        + Measurement4.X * 0.10;

                var mY = measuredPoint.Y * 0.30
                        + Measurement1.Y * 0.30
                        + Measurement2.Y * 0.175
                        + Measurement3.Y * 0.125
                        + Measurement4.Y * 0.10;

                Measurement4 = Measurement3;
                Measurement3 = Measurement2;
                Measurement2 = Measurement1;
                Measurement1 = new Point(mX, mY);
            }
            //Use the weighted average of the last 9 measurements to steady and slow the cursor
            else if (smoothingLevel == Enums.DataStreamProcessingLevels.High)
            {
                var mX = measuredPoint.X * 0.301
                        + Measurement1.X * 0.176
                        + Measurement2.X * 0.125
                        + Measurement3.X * 0.097
                        + Measurement4.X * 0.079
                        + Measurement5.X * 0.067
                        + Measurement6.X * 0.058
                        + Measurement7.X * 0.051
                        + Measurement8.X * 0.046;

                var mY = measuredPoint.Y * 0.301
                        + Measurement1.Y * 0.176
                        + Measurement2.Y * 0.125
                        + Measurement3.Y * 0.097
                        + Measurement4.Y * 0.079
                        + Measurement5.Y * 0.067
                        + Measurement6.Y * 0.058
                        + Measurement7.Y * 0.051
                        + Measurement8.Y * 0.046;

                Measurement8 = Measurement7;
                Measurement7 = Measurement6;
                Measurement6 = Measurement5;
                Measurement5 = Measurement4;
                Measurement4 = Measurement3;
                Measurement3 = Measurement2;
                Measurement2 = Measurement1;
                Measurement1 = new Point(mX, mY);
            }
            
            var delta = Measurement1 - EstimatedPoint;
            //To avoid a jumpy cursor the lowest level works best with a value of 1
            var noiseCoefficient = smoothingLevel == Enums.DataStreamProcessingLevels.Low ? 1 : 50;
            //This formula is used to create a highly stable cursor
            var processNoise = 1000 / (1 + delta.Length);
            //This formula is used to dampen small movements without affecting large movements
            var currentNoise = noiseCoefficient * Math.Pow(processNoise, 3) / (1 + delta.Length);

            //Scale from 0% to 100% that will be applied to the movement delta when updating the EstimatedPoint
            var gain = EstimationNoise / (EstimationNoise + currentNoise);
            //Update EstimationNoise in preparation for the next iteration
            EstimationNoise = (1.0 - gain) * (EstimationNoise + processNoise);
            EstimatedPoint = EstimatedPoint + delta * gain;
            return EstimatedPoint;
        }
    }
}
