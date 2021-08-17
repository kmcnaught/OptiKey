using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

namespace EyeMineService
{
    // Service launches EyeMine and auto-re-launches if it has quit or crashed or anything
    public partial class Service1 : ServiceBase
    {
        private Timer timer;
        private int pollTimeSeconds = 10;
        Process process = null;

        public Service1()
        {
            InitializeComponent();
        }

        public void onDebug()
        {
            OnStart(null);
        }

        private void LaunchEyeMine()
        {
#if DEBUG
            string filename = @"..\..\..\JuliusSweetland.OptiKey.EyeMine\bin\Debug\EyeMineExhibition.exe";
#else
            string filename = "EyeMineExhibition.exe";
#endif
            process = Process.Start(filename);
        }

        protected override void OnStart(string[] args)
        {
            LaunchEyeMine();

            timer = new Timer(pollTimeSeconds * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (process.HasExited)
            {
                LaunchEyeMine();
            }
        }

        protected override void OnStop()
        {
            timer.Stop();
        }
    }
}
