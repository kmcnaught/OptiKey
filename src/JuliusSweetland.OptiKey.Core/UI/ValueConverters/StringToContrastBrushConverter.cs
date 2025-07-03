using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JuliusSweetland.OptiKey.UI.ValueConverters
{
    /// <summary>
    /// Converter that converts string color names to SolidColorBrush with minimum contrast against white background
    /// </summary>
    public class StringToContrastBrushConverter : IValueConverter
    {
        private const double MinContrastRatio = 3.0;
        private const double WhiteLuminance = 1.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorName)
            {
                Color color;
                try
                {
                    // Try to parse as a named color first
                    color = (Color)ColorConverter.ConvertFromString(colorName);
                }
                catch
                {
                    // Fallback to common colors if parsing fails
                    switch (colorName.ToLower())
                    {
                        case "red": color = Colors.Red; break;
                        case "green": color = Colors.Green; break;
                        case "blue": color = Colors.Blue; break;
                        case "yellow": color = Colors.Yellow; break;
                        case "black": color = Colors.Black; break;
                        case "white": color = Colors.White; break;
                        case "gray": case "grey": color = Colors.Gray; break;
                        case "orange": color = Colors.Orange; break;
                        case "purple": color = Colors.Purple; break;
                        default: color = Colors.Gray; break;
                    }
                }

                // Ensure minimum contrast against white background
                color = EnsureContrast(color);
                return new SolidColorBrush(color);
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return null;
        }

        private Color EnsureContrast(Color color)
        {
            double luminance = CalculateLuminance(color);
            double contrastRatio = (WhiteLuminance + 0.05) / (luminance + 0.05);

            if (contrastRatio >= MinContrastRatio)
            {
                return color; // Already has sufficient contrast
            }

            // Calculate target luminance for 3:1 contrast
            double targetLuminance = (WhiteLuminance + 0.05) / MinContrastRatio - 0.05;

            // Darken the color to reach target luminance
            return DarkenToLuminance(color, targetLuminance);
        }

        private double CalculateLuminance(Color color)
        {
            // Convert RGB values to 0-1 range and apply gamma correction
            double r = GammaCorrect(color.R / 255.0);
            double g = GammaCorrect(color.G / 255.0);
            double b = GammaCorrect(color.B / 255.0);

            // Calculate luminance using standard formula
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private double GammaCorrect(double value)
        {
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        private Color DarkenToLuminance(Color color, double targetLuminance)
        {
            // Simple approach: multiply RGB values by a factor until we reach target luminance
            double factor = 1.0;
            Color result = color;
            
            // Binary search for the right darkening factor
            double minFactor = 0.0;
            double maxFactor = 1.0;
            
            for (int i = 0; i < 20; i++) // Limit iterations to prevent infinite loop
            {
                factor = (minFactor + maxFactor) / 2.0;
                
                byte newR = (byte)(color.R * factor);
                byte newG = (byte)(color.G * factor);
                byte newB = (byte)(color.B * factor);
                
                result = Color.FromRgb(newR, newG, newB);
                double currentLuminance = CalculateLuminance(result);
                
                if (Math.Abs(currentLuminance - targetLuminance) < 0.01)
                {
                    break;
                }
                
                if (currentLuminance > targetLuminance)
                {
                    maxFactor = factor; // Too bright, need to darken more
                }
                else
                {
                    minFactor = factor; // Too dark, lighten a bit
                }
            }
            
            return result;
        }
    }
}