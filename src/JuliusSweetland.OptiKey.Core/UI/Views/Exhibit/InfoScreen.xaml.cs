using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace JuliusSweetland.OptiKey.UI.Views.Exhibit
{
    /// <summary>
    /// Interaction logic for IntroScreen.xaml
    /// </summary>
    public partial class InfoScreen : UserControl
    {
        public InfoScreen()
        {
            InitializeComponent();

            var fadeInOutAnimation = new DoubleAnimation
            {
                From = 0.25,
                To = 0.5,
                Duration = TimeSpan.FromSeconds(0.25),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            };

            rectangle.BeginAnimation(OpacityProperty, fadeInOutAnimation);
        }        
    }
}
