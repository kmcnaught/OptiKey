// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Properties;

namespace JuliusSweetland.OptiKey.Enums
{
    public enum DataStreamProcessingLevels
    {
        None=0,
        Low=1,
        Medium=2,
        High=3
    }

    public static partial class EnumExtensions
    {
        public static string ToDescription(this DataStreamProcessingLevels pointSource)
        {
            switch (pointSource)
            {
                case DataStreamProcessingLevels.High: return Resources.HIGH;
                case DataStreamProcessingLevels.Medium: return Resources.MEDIUM;
                case DataStreamProcessingLevels.Low: return Resources.LOW;
                case DataStreamProcessingLevels.None: return Resources.NONE;
            }

            return pointSource.ToString();
        }
    }
}
