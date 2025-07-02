using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JuliusSweetland.OptiKey.UI.Controls
{
    /// <summary>
    /// Custom button control for Exhibit interface with configurable SVG-based graphics
    /// </summary>
    public class ExhibitButton : ContentControl
    {
        public static readonly DependencyProperty ButtonColorProperty =
            DependencyProperty.Register("ButtonColor", typeof(Color), typeof(ExhibitButton),
                new PropertyMetadata(Colors.Green, OnButtonColorChanged));

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(ExhibitButton),
                new PropertyMetadata(string.Empty));

        public Color ButtonColor
        {
            get { return (Color)GetValue(ButtonColorProperty); }
            set { SetValue(ButtonColorProperty, value); }
        }

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        static ExhibitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExhibitButton), new FrameworkPropertyMetadata(typeof(ExhibitButton)));
        }

        private static void OnButtonColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExhibitButton button)
            {
                button.UpdateButtonVisual();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateButtonVisual();
        }

        private void UpdateButtonVisual()
        {
            // This will be handled by the template
        }

        /// <summary>
        /// Creates the main button shape with outline and glint effect
        /// </summary>
        /// <param name="color">The button color</param>
        /// <param name="size">The button size</param>
        /// <returns>Grid containing the button graphics</returns>
        public static Grid CreateButtonGraphics(Color color, double size)
        {
            var grid = new Grid
            {
                Width = size,
                Height = size
            };

            // Main button circle with gradient
            var mainButton = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)), // Medium dark gray outline
                StrokeThickness = 3,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(color, 0.0),
                        new GradientStop(Color.FromRgb(
                            (byte)(color.R * 0.7), 
                            (byte)(color.G * 0.7), 
                            (byte)(color.B * 0.7)), 1.0)
                    }
                }
            };

            // Glint effect - arc in top-left
            var glintPath = new Path
            {
                Data = Geometry.Parse($"M {size * 0.25} {size * 0.15} A {size * 0.3} {size * 0.3} 30 0 1 {size * 0.4} {size * 0.35}"),
                Stroke = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                StrokeThickness = size * 0.08,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            grid.Children.Add(mainButton);
            grid.Children.Add(glintPath);

            return grid;
        }
    }
}