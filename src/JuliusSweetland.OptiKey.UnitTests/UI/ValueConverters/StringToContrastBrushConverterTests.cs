using System;
using System.Globalization;
using System.Windows.Media;
using JuliusSweetland.OptiKey.UI.ValueConverters;
using NUnit.Framework;

namespace JuliusSweetland.OptiKey.UnitTests.UI.ValueConverters
{
    [TestFixture]
    public class StringToContrastBrushConverterTests
    {
        private StringToContrastBrushConverter converter;

        [SetUp]
        public void Setup()
        {
            converter = new StringToContrastBrushConverter();
        }

        [Test]
        public void Convert_WithNullValue_ReturnsGrayBrush()
        {
            var result = converter.Convert(null, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            Assert.AreEqual(Colors.Gray, brush.Color);
        }

        [Test]
        public void Convert_WithNonStringValue_ReturnsGrayBrush()
        {
            var result = converter.Convert(123, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            Assert.AreEqual(Colors.Gray, brush.Color);
        }

        [Test]
        public void Convert_WithValidColorName_ReturnsBrushWithContrast()
        {
            var result = converter.Convert("Red", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Red should be darkened to ensure 3:1 contrast against white
            Assert.IsTrue(brush.Color.R < Colors.Red.R, "Red component should be darkened");
            Assert.AreEqual(0, brush.Color.G, "Green component should remain 0 for red");
            Assert.AreEqual(0, brush.Color.B, "Blue component should remain 0 for red");
        }

        [Test]
        public void Convert_WithLightRed_PreservesRedColorness()
        {
            // Light red: RGB(255, 128, 128)
            var result = converter.Convert("LightCoral", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Should preserve red dominance while ensuring contrast
            Assert.IsTrue(brush.Color.R >= brush.Color.G, "Red should be dominant component");
            Assert.IsTrue(brush.Color.R >= brush.Color.B, "Red should be dominant component");
            // Should be darkened for contrast
            Assert.IsTrue(brush.Color.R < Colors.LightCoral.R, "Should be darkened from original");
        }

        [Test]
        public void Convert_WithLightGreen_PreservesGreenColorness()
        {
            var result = converter.Convert("LightGreen", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Should preserve green dominance while ensuring contrast
            Assert.IsTrue(brush.Color.G >= brush.Color.R, "Green should be dominant component");
            Assert.IsTrue(brush.Color.G >= brush.Color.B, "Green should be dominant component");
            // Should be darkened for contrast
            Assert.IsTrue(brush.Color.G < Colors.LightGreen.G, "Should be darkened from original");
        }

        [Test]
        public void Convert_WithLightBlue_PreservesBlueColorness()
        {
            var result = converter.Convert("LightBlue", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Should preserve blue dominance while ensuring contrast
            Assert.IsTrue(brush.Color.B >= brush.Color.R, "Blue should be dominant component");
            Assert.IsTrue(brush.Color.B >= brush.Color.G, "Blue should be dominant component");
            // Should be darkened for contrast
            Assert.IsTrue(brush.Color.B < Colors.LightBlue.B, "Should be darkened from original");
        }

        [Test]
        public void Convert_WithDarkColor_PreservesOriginalColor()
        {
            var result = converter.Convert("DarkBlue", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Dark blue already has good contrast, should be unchanged
            Assert.AreEqual(Colors.DarkBlue, brush.Color);
        }

        [Test]
        public void Convert_WithBlackColor_PreservesOriginalColor()
        {
            var result = converter.Convert("Black", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Black already has maximum contrast against white
            Assert.AreEqual(Colors.Black, brush.Color);
        }

        [Test]
        public void Convert_WithWhiteColor_ReturnsDarkenedColor()
        {
            var result = converter.Convert("White", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // White should be significantly darkened to achieve contrast
            Assert.IsTrue(brush.Color.R < 200, "White should be darkened significantly");
            Assert.IsTrue(brush.Color.G < 200, "White should be darkened significantly");
            Assert.IsTrue(brush.Color.B < 200, "White should be darkened significantly");
            // Should maintain equal RGB values (remain grayscale)
            Assert.AreEqual(brush.Color.R, brush.Color.G, "Should remain grayscale");
            Assert.AreEqual(brush.Color.G, brush.Color.B, "Should remain grayscale");
        }

        [Test]
        public void Convert_WithYellowColor_ReturnsDarkenedYellow()
        {
            var result = converter.Convert("Yellow", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Yellow should be darkened while preserving yellow characteristics
            Assert.IsTrue(brush.Color.R < Colors.Yellow.R, "Should be darkened from original");
            Assert.IsTrue(brush.Color.G < Colors.Yellow.G, "Should be darkened from original");
            Assert.AreEqual(0, brush.Color.B, "Blue component should remain 0 for yellow");
            // Red and Green should be approximately equal for yellow
            Assert.IsTrue(Math.Abs(brush.Color.R - brush.Color.G) < 5, "Red and Green should be similar for yellow");
        }

        [Test]
        public void Convert_WithInvalidColorName_ReturnsGrayBrush()
        {
            var result = converter.Convert("InvalidColorName", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            Assert.AreEqual(Colors.Gray, brush.Color);
        }

        [Test]
        public void Convert_WithFallbackColors_WorksCorrectly()
        {
            // Test the fallback color parsing logic
            var testCases = new[] { "red", "green", "blue", "yellow", "black", "white", "gray", "grey", "orange", "purple" };
            
            foreach (var colorName in testCases)
            {
                var result = converter.Convert(colorName, typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
                Assert.IsInstanceOf<SolidColorBrush>(result, $"Failed for color: {colorName}");
                var brush = (SolidColorBrush)result;
                Assert.IsNotNull(brush.Color, $"Color should not be null for: {colorName}");
            }
        }

        [Test]
        public void Convert_EnsuresMinimumContrastRatio()
        {
            // Test with a light color that needs contrast adjustment
            var result = converter.Convert("LightYellow", typeof(SolidColorBrush), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<SolidColorBrush>(result);
            var brush = (SolidColorBrush)result;
            
            // Calculate luminance and verify contrast ratio
            double luminance = CalculateLuminance(brush.Color);
            double contrastRatio = (1.0 + 0.05) / (luminance + 0.05); // Against white background
            
            Assert.IsTrue(contrastRatio >= 3.0, $"Contrast ratio should be at least 3:1, but was {contrastRatio:F2}");
        }

        [Test]
        public void ConvertBack_WithSolidColorBrush_ReturnsColorString()
        {
            var brush = new SolidColorBrush(Colors.Red);
            var result = converter.ConvertBack(brush, typeof(string), null, CultureInfo.InvariantCulture);
            
            Assert.IsInstanceOf<string>(result);
            Assert.AreEqual(Colors.Red.ToString(), result);
        }

        [Test]
        public void ConvertBack_WithNonBrushValue_ReturnsNull()
        {
            var result = converter.ConvertBack("not a brush", typeof(string), null, CultureInfo.InvariantCulture);
            Assert.IsNull(result);
        }

        [Test]
        public void ConvertBack_WithNull_ReturnsNull()
        {
            var result = converter.ConvertBack(null, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.IsNull(result);
        }

        // Helper method to calculate luminance (same formula as in the converter)
        private double CalculateLuminance(Color color)
        {
            double r = GammaCorrect(color.R / 255.0);
            double g = GammaCorrect(color.G / 255.0);
            double b = GammaCorrect(color.B / 255.0);
            
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private double GammaCorrect(double value)
        {
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}