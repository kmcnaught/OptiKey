using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JuliusSweetland.OptiKey.UI.ValueConverters
{
    /// <summary>
    /// Converter that converts string color names to Color objects
    /// </summary>
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorName)
            {
                try
                {
                    // Try to parse as a named color first
                    var color = (Color)ColorConverter.ConvertFromString(colorName);
                    return color;
                }
                catch
                {
                    // Fallback to common colors if parsing fails
                    switch (colorName.ToLower())
                    {
                        case "red": return Colors.Red;
                        case "green": return Colors.Green;
                        case "blue": return Colors.Blue;
                        case "yellow": return Colors.Yellow;
                        case "black": return Colors.Black;
                        case "white": return Colors.White;
                        case "gray": case "grey": return Colors.Gray;
                        case "orange": return Colors.Orange;
                        case "purple": return Colors.Purple;
                        default: return Colors.Gray; // Default fallback
                    }
                }
            }

            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToString();
            }
            return null;
        }
    }
}