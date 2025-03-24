using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using JuliusSweetland.OptiKey.UI.Windows;
using System.Windows;
using log4net;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class ErrorViewModel : PageViewModel
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        
        public void StartRestartCountdown()
        {
            dispatcherTimer.Tick += Restart;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();
        }

        private void Restart(object sender, EventArgs e)
        {
            dispatcherTimer.Tick -= Restart;
            LogManager.Flush(1000);
            Application.Current.Shutdown(); // service will restart for us
        }
    }
}
