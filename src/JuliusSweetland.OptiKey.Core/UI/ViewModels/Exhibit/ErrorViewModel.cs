using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using JuliusSweetland.OptiKey.UI.Windows;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class ErrorViewModel : PageViewModel
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public ErrorViewModel()
        {
            CanGoBackward = false;
            CanGoForward = true;
        }

        public void StartRestartCountdown()
        {
            dispatcherTimer.Tick += Restart;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();
        }

        private void Restart(object sender, EventArgs e)
        {
            dispatcherTimer.Tick -= Restart;
            MainWindow.RestartEverything();
        }

        public override void SetUp()
        {
            SetInitTime();
        }

        public override void TearDown()
        {
        }
    }
}
