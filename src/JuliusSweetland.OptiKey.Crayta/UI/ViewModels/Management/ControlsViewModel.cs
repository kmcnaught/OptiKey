// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using JuliusSweetland.OptiKey.Crayta.Properties;
using log4net;
using Prism.Mvvm;

namespace JuliusSweetland.OptiKey.Crayta.UI.ViewModels.Management
{
    public class ControlsViewModel : BindableBase
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion
        
        #region Ctor

        public ControlsViewModel()
        {
            Load();
        }

        #endregion

        #region Properties

        public List<string> ColourNames
        {
            get
            {
                // Based on: https://stackoverflow.com/a/26287682/9091159
                return typeof(Brushes)
                    .GetProperties()
                    .Where(pi => pi.PropertyType == typeof(SolidColorBrush))
                    .Select(pi => pi.Name)
                    .ToList();
            }
        }

        private bool lookToScrollLockDownBoundsKey;
        public bool LookToScrollLockDownBoundsKey
        {
            get { return lookToScrollLockDownBoundsKey; }
            set { SetProperty(ref lookToScrollLockDownBoundsKey, value); }
        }
        
        private bool lookToScrollDeactivateUponSwitchingKeyboards;
        public bool LookToScrollDeactivateUponSwitchingKeyboards
        {
            get { return lookToScrollDeactivateUponSwitchingKeyboards; }
            set { SetProperty(ref lookToScrollDeactivateUponSwitchingKeyboards, value); }
        }

        private bool lookToScrollShowOverlayWindow;
        public bool LookToScrollShowOverlayWindow
        {
            get { return lookToScrollShowOverlayWindow; }
            set { SetProperty(ref lookToScrollShowOverlayWindow, value); }
        }

        private string lookToScrollOverlayBoundsColour;
        public string LookToScrollOverlayBoundsColour
        {
            get { return lookToScrollOverlayBoundsColour; }
            set { SetProperty(ref lookToScrollOverlayBoundsColour, value); }
        }

        private string lookToScrollOverlayDeadzoneColour;
        public string LookToScrollOverlayDeadzoneColour
        {
            get { return lookToScrollOverlayDeadzoneColour; }
            set { SetProperty(ref lookToScrollOverlayDeadzoneColour, value); }
        }

        private int lookToScrollOverlayBoundsThickness;
        public int LookToScrollOverlayBoundsThickness
        {
            get { return lookToScrollOverlayBoundsThickness; }
            set { SetProperty(ref lookToScrollOverlayBoundsThickness, value); }
        }

        private int lookToScrollOverlayDeadzoneThickness;
        public int LookToScrollOverlayDeadzoneThickness
        {
            get { return lookToScrollOverlayDeadzoneThickness; }
            set { SetProperty(ref lookToScrollOverlayDeadzoneThickness, value); }
        }

        // TODO: single deadzone for both,
        // TODO: as a % of screen?
        private int lookToScrollHorizontalDeadzone;
        public int LookToScrollHorizontalDeadzone
        {
            get { return lookToScrollHorizontalDeadzone; }
            set { SetProperty(ref lookToScrollHorizontalDeadzone, value); }
        }

        private int lookToScrollVerticalDeadzone;
        public int LookToScrollVerticalDeadzone
        {
            get { return lookToScrollVerticalDeadzone; }
            set { SetProperty(ref lookToScrollVerticalDeadzone, value); }
        }

        private double leftStickSensitivityX;
        public double LeftStickSensitivityX
        {
            get { return leftStickSensitivityX; }
            set { SetProperty(ref leftStickSensitivityX, value); }
        }

        private double leftStickSensitivityY;
        public double LeftStickSensitivityY
        {
            get { return leftStickSensitivityY; }
            set { SetProperty(ref leftStickSensitivityY, value); }
        }

        private double rightStickSensitivityX;
        public double RightStickSensitivityX
        {
            get { return rightStickSensitivityX; }
            set { SetProperty(ref rightStickSensitivityX, value); }
        }

        private double rightStickSensitivityY;
        public double RightStickSensitivityY
        {
            get { return rightStickSensitivityY; }
            set { SetProperty(ref rightStickSensitivityY, value); }
        }

        private double legacyStickSensitivityX;
        public double LegacyStickSensitivityX
        {
            get { return legacyStickSensitivityX; }
            set { SetProperty(ref legacyStickSensitivityX, value); }
        }

        private double legacyStickSensitivityY;
        public double LegacyStickSensitivityY
        {
            get { return legacyStickSensitivityY; }
            set { SetProperty(ref legacyStickSensitivityY, value); }
        }

        //TODO: Add non-linear mapping to joystick control?

        public bool ChangesRequireRestart
        {
            get
            {
                return false;
            }
        }
        
        #endregion
        
        #region Methods

        private void Load()
        {
            LookToScrollLockDownBoundsKey = Settings.Default.LookToScrollLockDownBoundsKey;
            LookToScrollDeactivateUponSwitchingKeyboards = Settings.Default.LookToScrollDeactivateUponSwitchingKeyboards;
            LookToScrollShowOverlayWindow = Settings.Default.LookToScrollShowOverlayWindow;
            LookToScrollOverlayBoundsColour = Settings.Default.LookToScrollOverlayBoundsColour;
            LookToScrollOverlayDeadzoneColour = Settings.Default.LookToScrollOverlayDeadzoneColour;
            LookToScrollOverlayBoundsThickness = Settings.Default.LookToScrollOverlayBoundsThickness;
            LookToScrollOverlayDeadzoneThickness = Settings.Default.LookToScrollOverlayDeadzoneThickness;
            LookToScrollHorizontalDeadzone = Settings.Default.LookToScrollHorizontalDeadzone;
            LookToScrollVerticalDeadzone = Settings.Default.LookToScrollVerticalDeadzone;

            LeftStickSensitivityX = Settings.Default.LeftStickSensitivityX;
            LeftStickSensitivityY = Settings.Default.LeftStickSensitivityY;
            RightStickSensitivityX = Settings.Default.RightStickSensitivityX;
            RightStickSensitivityY = Settings.Default.RightStickSensitivityY;
            LegacyStickSensitivityX = Settings.Default.LegacyStickSensitivityX;
            LegacyStickSensitivityY = Settings.Default.LegacyStickSensitivityY;
        }

        public void ApplyChanges()
        {
            Settings.Default.LookToScrollLockDownBoundsKey = LookToScrollLockDownBoundsKey;
            Settings.Default.LookToScrollDeactivateUponSwitchingKeyboards = LookToScrollDeactivateUponSwitchingKeyboards;
            Settings.Default.LookToScrollShowOverlayWindow = LookToScrollShowOverlayWindow;
            Settings.Default.LookToScrollOverlayBoundsColour = LookToScrollOverlayBoundsColour;
            Settings.Default.LookToScrollOverlayDeadzoneColour = LookToScrollOverlayDeadzoneColour;
            Settings.Default.LookToScrollOverlayBoundsThickness = LookToScrollOverlayBoundsThickness;
            Settings.Default.LookToScrollOverlayDeadzoneThickness = LookToScrollOverlayDeadzoneThickness;
            Settings.Default.LookToScrollHorizontalDeadzone = LookToScrollHorizontalDeadzone;
            Settings.Default.LookToScrollVerticalDeadzone = LookToScrollVerticalDeadzone;

            Settings.Default.LeftStickSensitivityX = LeftStickSensitivityX;
            Settings.Default.LeftStickSensitivityY = LeftStickSensitivityY;
            Settings.Default.RightStickSensitivityX = RightStickSensitivityX;
            Settings.Default.RightStickSensitivityY = RightStickSensitivityY;
            Settings.Default.LegacyStickSensitivityX = LegacyStickSensitivityX;
            Settings.Default.LegacyStickSensitivityY = LegacyStickSensitivityY;
        }

        #endregion
    }
}