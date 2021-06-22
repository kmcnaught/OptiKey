using JuliusSweetland.OptiKey.UI.ViewModels.Exhibit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JuliusSweetland.OptiKey.UI.Views.Exhibit
{
    /// <summary>
    /// Interaction logic for OnboardingWindow.xaml
    /// </summary>
    public partial class OnboardingWindow : Window
    {
        public OnboardingWindow()
        {
            InitializeComponent();

            //Instantiate ViewModel and set as DataContext 
            //var onboardingVM = new OnboardingViewModel();
            //this.DataContext = onboardingVM;
        }

        public void Prev_Clicked(object sender, RoutedEventArgs e)
        {
            OnboardingViewModel vm = (OnboardingViewModel)
                (this.DataContext);

            if (vm != null) { vm.PrevPage(); }
        }

        public void Next_Clicked(object sender, RoutedEventArgs e)
        {
            OnboardingViewModel vm = (OnboardingViewModel)
                (this.DataContext);

            if (vm != null) { vm.NextPage(); }
        }
    }
}
