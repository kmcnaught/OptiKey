using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System;

namespace JuliusSweetland.OptiKey.UI.Utilities
{
    class DebouncedAction
    {
        DispatcherTimer debounceTimer = new DispatcherTimer();
        EventHandler<NHotkey.HotkeyEventArgs> eventHandler;

        public DebouncedAction(EventHandler<NHotkey.HotkeyEventArgs> eventHandler, int debounceMs)
        {
            this.eventHandler = eventHandler;
            debounceTimer.Interval = TimeSpan.FromMilliseconds(debounceMs);
            debounceTimer.Tick += (s,e) => { debounceTimer.Stop(); };
        }

        public void PerformAction(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (debounceTimer.IsEnabled) { return; }
            debounceTimer.Start();

            // Only perform action if enough time has passed since last time
            this.eventHandler(sender, e);
        }

        // Factory method allows us to create + use in one fell swoop
        public static EventHandler<NHotkey.HotkeyEventArgs> CreateDebouncedAction(EventHandler<NHotkey.HotkeyEventArgs> eventHandler, int debounceMs)
        {
            DebouncedAction dbAction = new DebouncedAction(eventHandler, debounceMs);
            return dbAction.PerformAction;
        }        
    }
}
