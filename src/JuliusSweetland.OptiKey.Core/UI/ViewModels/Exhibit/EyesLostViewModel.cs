using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class EyesLostViewModel : PageViewModel
    {
        TobiiViewModel vm;

        DateTime timeWindowAppeared;
        DateTime timeEyesAppeared = new DateTime(0);
        readonly DateTime timeZero = new DateTime(0);

        TimeSpan timeBeforeResetOffered = new TimeSpan(0, 0, 5);
        TimeSpan timeBeforeResetAuto = new TimeSpan(0, 0, 35);
        TimeSpan timeBeforeDismissEarly = new TimeSpan(0, 0, 2);
        TimeSpan timeBeforeDismissLate = new TimeSpan(0, 0, 7);

        DispatcherTimer resetTimer = new DispatcherTimer();
        DispatcherTimer dismissTimer = new DispatcherTimer();

        bool waitingForAutoReset = false;

        public event EventHandler RequireAutoReset = delegate {};
        public event EventHandler RequireDismiss = delegate { };

        public EyesLostViewModel(TobiiViewModel vm) 
        {
            this.vm = vm;
            CanGoBackward = false;
            CanGoForward = true;           
        }

        public TobiiViewModel GetTobiiVM()
        {
            return vm;
        }        

        public void Start()
        {
            timeWindowAppeared = DateTime.Now;
            canDismiss = false;
            canReset = false;
            shouldAutoReset = false;
            timeEyesAppeared = timeZero;

            RaisePropertyChanged("CanReset");
            RaisePropertyChanged("ShouldAutoReset");
            RaisePropertyChanged("CanReset");

            resetTimer.Tick += TimerTick;
            resetTimer.Interval = new TimeSpan(0, 0, 1);
            resetTimer.Start();
        }        

        private void TimerTick(object sender, EventArgs e)
        {
            timeBeforeDismissEarly = new TimeSpan(0, 0, 5);
            timeBeforeDismissLate = new TimeSpan(0, 0, 10);
            if (!vm.LostTracking && timeEyesAppeared == timeZero)
            {
                timeEyesAppeared = DateTime.Now;
            }
            canReset = DateTime.Now.Subtract(timeWindowAppeared) > timeBeforeResetOffered;
            shouldAutoReset = vm.LostTracking && DateTime.Now.Subtract(timeWindowAppeared) > timeBeforeResetAuto;

            if (!vm.LostTracking && timeEyesAppeared != timeZero)
            {                
                canDismiss = !canReset && DateTime.Now.Subtract(timeEyesAppeared) < timeBeforeDismissEarly;
            }

            canGoBack = canReset && !vm.LostTracking;

            RaisePropertyChanged("CanReset");
            RaisePropertyChanged("ShouldAutoReset");
            RaisePropertyChanged("CanGoBack");

            if (shouldAutoReset && !waitingForAutoReset)
            {
                RequireAutoReset(this, null);
                waitingForAutoReset = true;
            }
        }

        public void Stop()
        {            
            resetTimer.Tick -= TimerTick;
            resetTimer.Stop();

            canGoBack = false;
            shouldAutoReset = false;
            canReset = false;
            waitingForAutoReset = false;

            RaisePropertyChanged("CanReset");
            RaisePropertyChanged("ShouldAutoReset");
            RaisePropertyChanged("CanGoBack");

        }

        public override void SetUp()
        {
            SetInitTime();
        }

        public override void TearDown()
        {
        }
        
        protected bool canDismiss = false;
        public bool CanDismiss
        {
            get { return canDismiss; }
        }


        protected bool canReset = false;
        public bool CanReset
        {
            get { return canReset; }
        }

        protected bool canGoBack = false;
        public bool CanGoBack
        {
            get { return canGoBack; }
        }

        protected bool shouldAutoReset = false;
        public bool ShouldAutoReset
        {
            get { return shouldAutoReset; }            
        }
    }
}
