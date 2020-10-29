
// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Windows.Controls;
using System.Windows;
using JuliusSweetland.OptiKey.EyeMine.UI.ViewModels.Management;

namespace JuliusSweetland.OptiKey.EyeMine.UI.Views.Management
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender,
            System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            AboutViewModel viewModel = this.DataContext as AboutViewModel;
            if (viewModel != null)
            {
                string content = "EyeMineV2 " + viewModel.AppVersion;
                Clipboard.SetText(content);
            }
        }
    }
}
