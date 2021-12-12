using System;
using System.ComponentModel;
using System.Threading;
using SharpDX.XInput;
using System.Linq;
using log4net;

namespace JuliusSweetland.OptiKey.Models
{
    class XInputListener
    {

        private Controller controller;
        private BackgroundWorker pollWorker;
        private GamepadButtonFlags buttons;
        private UserIndex userIndex;
        private int pollDelayMs;
        private int reconnectDelayMs = 1000;

        public EventHandler ButtonDown = delegate { };
        public EventHandler ButtonUp = delegate { };

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public class GamepadButtonDownEventArgs : EventArgs
        {
            public GamepadButtonDownEventArgs(GamepadButtonFlags button)
            {
                this.button = button;
            }
            public GamepadButtonFlags button;
        }

        public class GamepadButtonUpEventArgs : EventArgs
        {
            public GamepadButtonUpEventArgs(GamepadButtonFlags button)
            {
                this.button = button;
            }
            public GamepadButtonFlags button;
        }

        public delegate void GamepadButtonDownEventHandler(object sender, GamepadButtonDownEventArgs e);
        public delegate void GamepadButtonUpEventHandler(object sender, GamepadButtonUpEventArgs e);

        public XInputListener(UserIndex userIndex, int pollDelayMs = 20)
        {
            this.userIndex = userIndex;
            this.pollDelayMs = pollDelayMs;

            // TODO: support multiple controllers, optionally poll all of them?
            TryConnect();

            pollWorker = new BackgroundWorker();
            pollWorker.DoWork += pollGamepadButtons;
            pollWorker.RunWorkerAsync();
        }

        private void TryConnect()
        {
            controller = new Controller(userIndex);
            IsConnected = controller.IsConnected;
            Log.InfoFormat("Controller connected? {0}", IsConnected);
        }

        private void pollGamepadButtons(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                IsConnected = controller.IsConnected;
                if (IsConnected)
                {
                    var state = controller.GetState();

                    GamepadButtonFlags currentButtons = state.Gamepad.Buttons;
                    GamepadButtonFlags changedButtons = currentButtons ^ buttons;

                    if (changedButtons > 0)
                    {
                        var splitButtonsChanged = Enum.GetValues(typeof(GamepadButtonFlags))
                                                 .Cast<GamepadButtonFlags>()
                                                 .Where(b => b != GamepadButtonFlags.None && changedButtons.HasFlag(b));
                        foreach (GamepadButtonFlags b in splitButtonsChanged)
                        {
                            if ((currentButtons & b) > 0)
                                this.ButtonDown(this, new GamepadButtonDownEventArgs(b));
                            else
                                this.ButtonUp(this, new GamepadButtonUpEventArgs(b));
                        }
                    }
                    buttons = currentButtons;

                }
                else
                {
                    // TODO: warning toast after certain number of reconnects?
                    Log.Warn("Gamepad not connected, will try to reconnect");                    
                    Thread.Sleep(reconnectDelayMs);
                    TryConnect();                    
                }
                Thread.Sleep(pollDelayMs);
            }
        }

        #region Properties

        public bool IsConnected
        {
            get; set;
        }

        #endregion

    }
}
