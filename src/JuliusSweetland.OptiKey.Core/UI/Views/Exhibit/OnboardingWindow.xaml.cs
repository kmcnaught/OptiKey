using JuliusSweetland.OptiKey.UI.ViewModels.Exhibit;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using JuliusSweetland.OptiKey.UI.Windows;


namespace JuliusSweetland.OptiKey.UI.Views.Exhibit
{
    /// <summary>
    /// Interaction logic for OnboardingWindow.xaml
    /// </summary>
    public partial class OnboardingWindow : Window
    {

        private readonly ICommand setKioskCommand;
        private readonly ICommand unsetKioskCommand;
        private readonly ICommand captureMinecraftCommand;
        private readonly ICommand restartCommand;

        public ICommand SetKioskCommand { get { return setKioskCommand; } }
        public ICommand UnsetKioskCommand { get { return unsetKioskCommand; } }
        public ICommand CaptureMinecraftCommand { get { return captureMinecraftCommand; } }
        public ICommand RestartCommand { get { return restartCommand; } }

        public OnboardingWindow()
        {
            InitializeComponent();

            setKioskCommand = new DelegateCommand(() => { Demo.SetAsShellApp(true); });
            unsetKioskCommand = new DelegateCommand(() => { Demo.SetAsShellApp(false); });
            captureMinecraftCommand = new DelegateCommand(CaptureMinecraft);
            restartCommand = new DelegateCommand(() => { MainWindow.RestartEverything(); });

        }

        private void CaptureMinecraft()
        {
            Process p = Demo.CaptureMinecraftProcess();
            if (p == null)
            {
                MessageBox.Show("Could not find valid Minecraft instance. \n\nPlease run Minecraft Launcher, select the \"EyeMineExhibition\" profile and click PLAY to launch");
            }
            else
            {
                if (MessageBox.Show("Successfully captured Minecraft process.\nPlease close Minecraft.\nEyeMine will now restart and launch it's own copy of Minecraft.",
                         "Capturing Minecraft instance ... ",
                         MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    MainWindow.RestartEverything();
                }
            }
        }
    }
}
