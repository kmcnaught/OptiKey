using System;
using System.Windows;
using System.Windows.Media.Animation;


namespace JuliusSweetland.OptiKey.UI.Views
{
    /// <summary>
    /// Interaction logic for PostCalib.xaml
    /// </summary>
    public partial class PostCalib : Window
    {
        public PostCalib()
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
            rectangle2.BeginAnimation(OpacityProperty, fadeInOutAnimation);
        }        
    }
}
