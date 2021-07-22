using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Exhibit
{
    class MinecraftWatcher
    {
        private Process process;

        private string lockFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\EyeMineExhibition\eyemine.lock");
        private DateTime lockTime;
        private DateTime zeroTime = new DateTime(0);

        private DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public event EventHandler MinecraftCrashed;
        public event EventHandler MinecraftLoaded;

        public MinecraftWatcher(Process p)
        {
            this.process = p;

            dispatcherTimer.Tick += TimerTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (process.HasExited)
            {
                MinecraftCrashed(this, null);
                dispatcherTimer.Tick -= TimerTick;
                dispatcherTimer.Stop();
                return;
            }
            else
            {
                DateTime newLockTime = getLockTimestamp();
                if (newLockTime > zeroTime &&
                    newLockTime != lockTime)
                {
                    MinecraftLoaded(this, null);
                }
                lockTime = newLockTime;
            }
        }

        private DateTime getLockTimestamp()
        {
            if (File.Exists(lockFile))
            {
                DateTime accessTime = new FileInfo(lockFile).LastWriteTime;
                if (accessTime > process.StartTime)
                {
                    return accessTime;
                }
            }

            return zeroTime;
        }
    }
    }
