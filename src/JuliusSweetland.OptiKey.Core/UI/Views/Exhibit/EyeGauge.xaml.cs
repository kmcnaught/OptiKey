using JuliusSweetland.OptiKey.UI.Utilities;
using JuliusSweetland.OptiKey.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JuliusSweetland.OptiKey.UI.Views
{
    /// <summary>
    /// Interaction logic for EyeGauge.xaml
    /// </summary>
    public partial class EyeGauge : Window
    {
        public EyeGauge()
        {
            InitializeComponent();

            // Set up ViewModel
            TobiiViewModel viewModel = new TobiiViewModel();
            this.DataContext = viewModel;

            CompositionTarget.Rendering += UpdateRectangle;

            //FIXME: Disconnect from tobii on exit!!

        }

        private void UpdateRectangle(object sender, EventArgs e)
        {

            // TODO: Avoid reaching into VM!!
            TobiiViewModel viewModel = (TobiiViewModel)this.DataContext;

            float scaleX = (float)trackBox.Width;
            float scaleY = (float)trackBox.Height;

            var locationL = new Point(scaleX * viewModel.leftEye.xPos, scaleY * viewModel.leftEye.yPos);
            var locationR = new Point(scaleX * viewModel.rightEye.xPos, scaleY * viewModel.rightEye.yPos);

            // set new position
            Canvas.SetLeft(followRectangle1, locationL.X);
            Canvas.SetTop(followRectangle1, locationL.Y);
            Canvas.SetLeft(followRectangle2, locationR.X);
            Canvas.SetTop(followRectangle2, locationR.Y);

            // set colour based on Z            
            double lightness = 0.5;
            double saturation = 0.75;

            // hue: 0=red, yellow, green=120, 180=teal, blue, purple, pink, red=360
            double leftEyeZdiff = viewModel.leftEye.zPos - 0.5;
            double rightEyeZdiff = viewModel.rightEye.zPos - 0.5;

            // green when Zdiff ~= 0, to red when Zdiff ~0.5;
            double hueLeft = 120.0f * (1.0 - Math.Abs(leftEyeZdiff));
            double hueRight = 120.0f * (1.0 - Math.Abs(rightEyeZdiff));

            // FIXME: poss non-linear scaling

            animatedBrush1.Color = ColorConversions.HlsToRgb(hueLeft, lightness, saturation);
            animatedBrush2.Color = ColorConversions.HlsToRgb(hueRight, lightness, saturation);

            // Show labels if outside ideal area
            // TODO: animate show/hide
            if ((viewModel.leftEye.visible && viewModel.leftEye.zPos > 0.85) ||
                (viewModel.rightEye.visible && viewModel.rightEye.zPos > 0.85))
                arrowCloser.Visibility = Visibility.Visible;
            else
                arrowCloser.Visibility = Visibility.Hidden;

            if (viewModel.leftEye.zPos < 0.15 || viewModel.rightEye.zPos < 0.15)
                arrowFurther.Visibility = Visibility.Visible;
            else
                arrowFurther.Visibility = Visibility.Hidden;

            // Set up traffic-light border:
            // RED: cannot see eyes
            // GREEN: in good spot
            // ORANGE: can improve
            if (!viewModel.leftEye.visible && !viewModel.rightEye.visible)
            {
                trackBorder.BorderBrush = Brushes.Red;
            }
            else if (viewModel.leftEye.zPos < 0.85 &&
                viewModel.rightEye.zPos < 0.85 &&
                viewModel.leftEye.zPos > 0.15 &&
                viewModel.rightEye.zPos > 0.15)
            {
                trackBorder.BorderBrush = Brushes.Green;
            }
            else
            {
                trackBorder.BorderBrush = Brushes.Orange;
            }

            // if both eyes gone, update label
            if (!viewModel.leftEye.visible && !viewModel.rightEye.visible)
            {
                Random rnd = new Random();
                int rndInt = rnd.Next(1, 5);

            }

            // TODO: add "are you there? cannot see you"..?
        }
    }
}
