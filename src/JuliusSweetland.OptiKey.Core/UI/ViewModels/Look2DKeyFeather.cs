// Copyright (c) K McNaught Consulting (UK company number 11297717) - All Rights Reserved

using System;
using System.Collections.Generic;
using System.Windows;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Models.ScalingModels;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Static;
using log4net;
using WindowsInput.Native;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    // This class should eventually be a generic handler for 2D interactions. It currently has some joystick-specific 
    // logic leaking in. 
    public class Look2DKeyFeather: Look2DInteractionHandler // TODO: use base class instead
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IKeyboardOutputService keyboardOutputService;

        

        private enum DirectionKeys
        {
            Up, 
            Down,
            Left,
            Right
        }

        Dictionary<DirectionKeys, VirtualKeyCode> keyMappings;
        Dictionary<DirectionKeys, bool> keyDownStates; // probably superfluous?
        Dictionary<DirectionKeys, DateTime> keyDownUpTimes; // keep track of when last changed (for 'active' keys)

        #endregion

        #region Constructor

        public Look2DKeyFeather(FunctionKeys triggerKey, 
            IKeyStateService keyStateService, 
            MainViewModel mainViewModel)            
            : base(triggerKey, (x, y) => { }, keyStateService, mainViewModel )
        {
            // Replace base update action method with our own
            this.updateAction = this.updateActionFeather;
            
            keyboardOutputService = mainViewModel.KeyboardOutputService;

            // TODO: Will support swapping out for arrows or ijkl etc
            keyMappings = new Dictionary<DirectionKeys, VirtualKeyCode>();
            keyMappings.Add(DirectionKeys.Up, WindowsInput.Native.VirtualKeyCode.VK_W);
            keyMappings.Add(DirectionKeys.Left, WindowsInput.Native.VirtualKeyCode.VK_A);
            keyMappings.Add(DirectionKeys.Down, WindowsInput.Native.VirtualKeyCode.VK_S);
            keyMappings.Add(DirectionKeys.Right, WindowsInput.Native.VirtualKeyCode.VK_D);

            keyDownStates = new Dictionary<DirectionKeys, bool>();
            keyDownStates.Add(DirectionKeys.Up, false);
            keyDownStates.Add(DirectionKeys.Left, false);
            keyDownStates.Add(DirectionKeys.Down, false);
            keyDownStates.Add(DirectionKeys.Right, false);

            keyDownUpTimes = new Dictionary<DirectionKeys, DateTime>();
            keyDownUpTimes.Add(DirectionKeys.Up, DateTime.MaxValue);
            keyDownUpTimes.Add(DirectionKeys.Left, DateTime.MaxValue);
            keyDownUpTimes.Add(DirectionKeys.Down, DateTime.MaxValue);
            keyDownUpTimes.Add(DirectionKeys.Right, DateTime.MaxValue);

        }

        private int pressTimeMs = 100;

        #endregion

        private void PressKey(DirectionKeys key)
        {
            keyboardOutputService.PressKey(keyMappings[key], KeyPressKeyValue.KeyPressType.Press);
            keyDownStates[key] = true;
            keyDownUpTimes[key] = Time.HighResolutionUtcNow;
        }

        private void ReleaseKey(DirectionKeys key, bool stillActive)
        {
            keyboardOutputService.PressKey(keyMappings[key], KeyPressKeyValue.KeyPressType.Release);
            keyDownStates[key] = false;
            if (stillActive)
                keyDownUpTimes[key] = Time.HighResolutionUtcNow;
            else
                keyDownUpTimes[key] = DateTime.MaxValue;
        }

        private void UpdateKey(DirectionKeys key, bool active, float amount)
        {
            pressTimeMs = 10;
            DateTime now = Time.HighResolutionUtcNow;
            int pauseTimeMs = (int)((1.0 - amount) * 100);
            if (active)
            {
                if (keyDownUpTimes[key] < now)
                {
                    // already pressed (or paused), recompute feathering
                    TimeSpan delta = now - keyDownUpTimes[key];
                    if (keyDownStates[key]) // being held down - for pressTimeMs
                    {
                        if (delta.TotalMilliseconds > pressTimeMs)
                        {
                            ReleaseKey(key, true);
                        }
                    }
                    else
                    { 
                        // Pause between presses
                        if (delta.TotalMilliseconds > pauseTimeMs)
                        {
                            PressKey(key);
                        }
                    }
                }
                else
                {
                    PressKey(key);
                }
            }
            else
            {
                if (keyDownUpTimes[key] < now) // was pressed, need to release
                {
                    ReleaseKey(key, false);
                }
            }
        }

        private void updateActionFeather(float x, float y)
        {
            Log.DebugFormat("wasdJoystickAction, ({0}, {1})", x, y);
            float eps = 1e-6f;
            DateTime now = Time.HighResolutionUtcNow;

            bool keyRightActive = x > eps;
            bool keyLeftActive = x < -eps;
            bool keyUpActive = y < -eps;
            bool keyDownActive = y > eps;

            x = Math.Abs(x);
            y = Math.Abs(y);

            UpdateKey(DirectionKeys.Left, keyLeftActive, x);
            UpdateKey(DirectionKeys.Right, keyRightActive, x);
            UpdateKey(DirectionKeys.Up, keyUpActive, y);
            UpdateKey(DirectionKeys.Down, keyDownActive, y);            
        }

    }
}