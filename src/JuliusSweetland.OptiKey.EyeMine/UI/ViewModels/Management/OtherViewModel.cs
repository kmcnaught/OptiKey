using log4net;
using System;
using System.Collections.Generic;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services.PluginEngine;
using log4net;
using Prism.Mvvm;
using JuliusSweetland.OptiKey.Services.Translation.Languages;
using JuliusSweetland.OptiKey.Services.Translation;
using System.Windows.Media;
using System.Linq;

namespace JuliusSweetland.OptiKey.EyeMine.UI.ViewModels.Management
{
    public class OtherViewModel : BindableBase
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Ctor

        public OtherViewModel()
        {
            Load();
        }

        #endregion

        #region Properties

        private bool showSplashScreen;
        public bool ShowSplashScreen
        {
            get { return showSplashScreen; }
            set { SetProperty(ref showSplashScreen, value); }
        }

        private bool checkForUpdates;
        public bool CheckForUpdates
        {
            get { return checkForUpdates; }
            set { SetProperty(ref checkForUpdates, value); }
        }

        private bool suppressTriggerWarning;
        public bool SuppressTriggerWarning
        {
            get { return suppressTriggerWarning; }
            set { SetProperty(ref suppressTriggerWarning, value); }
        }

        private bool magnifySuppressedForScrollingActions;
        public bool MagnifySuppressedForScrollingActions
        {
            get { return magnifySuppressedForScrollingActions; }
            set { SetProperty(ref magnifySuppressedForScrollingActions, value); }
        }

        private bool debug;
        public bool Debug
        {
            get { return debug; }
            set { SetProperty(ref debug, value); }
        }

        private bool allowRepeatKeyActionsAwayFromKey;
        public bool AllowRepeatKeyActionsAwayFromKey
        {
            get { return allowRepeatKeyActionsAwayFromKey; }
            set { SetProperty(ref allowRepeatKeyActionsAwayFromKey, value); }
        }

        public bool ChangesRequireRestart
        {
            get
            {

                return (Settings.Default.Debug != Debug);
            }
        }

        #endregion

        #region Methods

        private void Load()
        {
            ShowSplashScreen = Settings.Default.ShowSplashScreen;
            CheckForUpdates = Settings.Default.CheckForUpdates;
            SuppressTriggerWarning = Settings.Default.SuppressTriggerWithoutPositionError;
            MagnifySuppressedForScrollingActions = Settings.Default.MagnifySuppressedForScrollingActions;
            Debug = Settings.Default.Debug;
            AllowRepeatKeyActionsAwayFromKey = Settings.Default.AllowRepeatKeyActionsAwayFromKey;
            
        }

        public void ApplyChanges()
        {
            Settings.Default.ShowSplashScreen = ShowSplashScreen;
            Settings.Default.CheckForUpdates = CheckForUpdates;
            Settings.Default.SuppressTriggerWithoutPositionError = SuppressTriggerWarning;
            Settings.Default.MagnifySuppressedForScrollingActions = MagnifySuppressedForScrollingActions;
            Settings.Default.Debug = Debug;
            Settings.Default.AllowRepeatKeyActionsAwayFromKey = AllowRepeatKeyActionsAwayFromKey;

        }

        #endregion
    }
}
