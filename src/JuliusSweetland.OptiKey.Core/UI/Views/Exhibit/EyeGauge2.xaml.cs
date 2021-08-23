using JuliusSweetland.OptiKey.UI.Utilities;
using JuliusSweetland.OptiKey.UI.ViewModels.Exhibit;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JuliusSweetland.OptiKey.Extensions;
using System.Windows.Threading;

namespace JuliusSweetland.OptiKey.UI.Views.Exhibit
{
    /// <summary>
    /// Interaction logic for EyeGauge.xaml
    /// </summary>
    public partial class EyeGauge2 : UserControl
    {
        public EyeGauge2()
        {
            InitializeComponent();
            
            CompositionTarget.Rendering += UpdateRectangle;

            pressNextTimer.Interval = new TimeSpan(0, 0, 8);
            pressNextTimer.Tick += PressNextTimer_Tick;
        }

        private void PressNextTimer_Tick(object sender, EventArgs e)
        {
            pressNextTimer.Stop();
        }

        DispatcherTimer pressNextTimer = new DispatcherTimer();

        // Keep track of continous measure of 'goodness'
        private double filteredGoodnessLeft = 0;
        private double filteredGoodnessRight = 0;

        // Keep track of binary states, smooshed to [0, 1] allow non-immediate feedback
        private double filteredTooClose = 0;
        private double filteredTooFar = 0;
        private double filteredNotVisible = 0;
        private double filteredAllowNextHint = 0;

        private void UpdateRectangle(object sender, EventArgs e)
        {

            // TODO: Avoid reaching into VM!!
            TobiiViewModel viewModel = (TobiiViewModel)this.DataContext;
            if (viewModel == null) { return;  }                       

            float scaleX = (float)trackBox.Width;
            float scaleY = (float)trackBox.Height;

            var locationL = new Point(scaleX * viewModel.leftEye.xPos, scaleY * viewModel.leftEye.yPos);
            var locationR = new Point(scaleX * viewModel.rightEye.xPos, scaleY * viewModel.rightEye.yPos);

            // set new position of eye circles
            Canvas.SetLeft(followRectangle1, locationL.X);
            Canvas.SetTop(followRectangle1, locationL.Y);
            Canvas.SetLeft(followRectangle2, locationR.X);
            Canvas.SetTop(followRectangle2, locationR.Y);

            // assess 'goodness' based on Z distances
            double leftEyeZdiff = viewModel.leftEye.zPos - 0.5;
            double rightEyeZdiff = viewModel.rightEye.zPos - 0.5;
            double currentGoodnessLeft = 1.0 - 2*Math.Abs(leftEyeZdiff); // [0, 1]
            double currentGoodnessRight = 1.0 - 2*Math.Abs(rightEyeZdiff); // [0, 1]

            if (!viewModel.leftEye.visible) { currentGoodnessLeft = 0.0; }
            if (!viewModel.rightEye.visible) { currentGoodnessRight = 0.0; }

            // Update 'goodness' metric, filtered for smooth changes
            double alpha = 0.02;
            filteredGoodnessLeft = filteredGoodnessLeft.UpdateIIR(currentGoodnessLeft, alpha);
            filteredGoodnessRight= filteredGoodnessRight.UpdateIIR(currentGoodnessRight, alpha);
            double filteredGoodnessBest = Math.Max(filteredGoodnessLeft, filteredGoodnessRight);

            // Update some binary states, also filtered for smooth changes            
            // NOT VISIBLE
            alpha = (viewModel.leftEye.visible || viewModel.leftEye.visible) ? 0.1 : 0.01; // go into 'not visible' state slowly, recover quickly
            double currentNotVisible = !viewModel.leftEye.visible && !viewModel.leftEye.visible ? 1.0 : 0.0;
            filteredNotVisible = filteredNotVisible.UpdateIIR(currentNotVisible, alpha); 

            // TOO CLOSE / FAR            
            alpha = 0.05;
            double tooClose = (viewModel.leftEye.visible && viewModel.leftEye.zPos < 0.15 ||
                               viewModel.rightEye.visible && viewModel.rightEye.zPos < 0.15) ?
                            1.0 : 0.0;
            double tooFar = (viewModel.leftEye.visible && viewModel.leftEye.zPos > 0.85) ||
                            (viewModel.rightEye.visible && viewModel.rightEye.zPos > 0.85) ?
                            1.0 : 0.0;

            filteredTooClose = filteredTooClose.UpdateIIR(tooClose, alpha);
            filteredTooFar = filteredTooFar.UpdateIIR(tooFar, alpha);

            bool goodCurrently = (currentNotVisible < 1.0 && tooClose < 1.0 && tooFar < 1.0);            
            // fades in more slowly than out
            filteredAllowNextHint = filteredAllowNextHint.UpdateIIR(goodCurrently ? 1.0 : 0.0, goodCurrently ? 0.0025 : 0.05);

            // Traffic light colours for eyes and border                
            // green when goodness ~= 1, to red when goodness -> 0
            // hue: 0=red, yellow, green=120, 180=teal, blue, purple, pink, red=360
            double hueLeft = 120.0f * filteredGoodnessLeft;
            double hueRight = 120.0f * filteredGoodnessRight;
            double hueBest = 120.0f * filteredGoodnessBest;

            double lightness = 0.5;
            double saturation = 0.75;
            animatedBrush1.Color = ColorConversions.HlsToRgb(hueLeft, lightness, saturation);
            animatedBrush2.Color = ColorConversions.HlsToRgb(hueRight, lightness, saturation);
            animatedBrushBorder.Color = ColorConversions.HlsToRgb(hueBest, lightness, saturation);

            // Show/hide labels for discrete states
            arrowCloser.Visibility = Visibility.Hidden;
            labelCloser.Visibility = Visibility.Hidden;
            arrowFurther.Visibility = Visibility.Hidden;
            labelFurther.Visibility = Visibility.Hidden;
            labelNotVisible.Visibility = Visibility.Hidden;
            labelNotVisibleInstructions.Visibility = Visibility.Hidden;
            labelVisibleInstructions.Visibility = Visibility.Hidden;
            labelPressNext.Visibility = Visibility.Hidden;

            if (filteredNotVisible > 0.7)
            {
                labelNotVisible.Visibility = Visibility.Visible;
                labelNotVisible.Opacity = filteredNotVisible * filteredNotVisible;
                labelNotVisibleInstructions.Visibility = Visibility.Visible;
                labelNotVisibleInstructions.Opacity = filteredNotVisible * filteredNotVisible;
                labelPressNext.Visibility = Visibility.Hidden;
            }
            else
            {
                labelVisibleInstructions.Visibility = Visibility.Visible;
                labelVisibleInstructions.Opacity = 1.0 - filteredNotVisible * filteredNotVisible;
                if (filteredTooFar > 0.5)
                {
                    arrowCloser.Visibility = Visibility.Visible;
                    labelCloser.Visibility = Visibility.Visible;
                    arrowCloser.Opacity = filteredTooFar;
                    labelCloser.Opacity = filteredTooFar;
                    pressNextTimer.Stop();
                }
                else if (filteredTooClose > 0.5)
                {
                    arrowFurther.Visibility = Visibility.Visible;
                    labelFurther.Visibility = Visibility.Visible;
                    arrowFurther.Opacity = filteredTooClose;
                    labelFurther.Opacity = filteredTooClose;
                    pressNextTimer.Stop();
                }
                else
                {
                    if (filteredAllowNextHint > 0.5)
                    {
                        labelPressNext.Visibility = Visibility.Visible;
                        labelPressNext.Opacity = filteredAllowNextHint;
                    }
                }
            }
        }        
    }
}
