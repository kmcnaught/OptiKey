// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using CoreEnums = JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.EyeMine.Properties;
using JuliusSweetland.OptiKey.UI.ViewModels.Management;
using log4net;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using JuliusSweetland.OptiKey.Enums;

namespace JuliusSweetland.OptiKey.EyeMine.UI.ViewModels.Management
{
    public class PointingAndSelectingViewModelEyeMine : PointingAndSelectingViewModel
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Ctor

        public PointingAndSelectingViewModelEyeMine() 
        : base()
        {
            Load();
        }

        #endregion

        #region Properties

        public new static List<KeyValuePair<string, PointsSources>> PointsSources
        {
            get
            {
                return new List<KeyValuePair<string, PointsSources>>
                {
                    new KeyValuePair<string, PointsSources>(PointSourceLongDescription(CoreEnums.PointsSources.GazeTracker), CoreEnums.PointsSources.GazeTracker),
                    new KeyValuePair<string, PointsSources>(PointSourceLongDescription(CoreEnums.PointsSources.MousePosition), CoreEnums.PointsSources.MousePosition),
                    new KeyValuePair<string, PointsSources>(PointSourceLongDescription(CoreEnums.PointsSources.TheEyeTribe), CoreEnums.PointsSources.TheEyeTribe),
                    new KeyValuePair<string, PointsSources>(PointSourceLongDescription(CoreEnums.PointsSources.TobiiEyeX), CoreEnums.PointsSources.TobiiEyeX),
                    new KeyValuePair<string, PointsSources>(PointSourceLongDescription(CoreEnums.PointsSources.TobiiPcEyeGo), CoreEnums.PointsSources.TobiiPcEyeGo),
                };
            }
        }

        private static string PointSourceLongDescription(CoreEnums.PointsSources pointsSource)
        {
            // Optional longer description, for management console
            switch (pointsSource)
            {
                case CoreEnums.PointsSources.TobiiPcEyeGo: return Resources.TOBII_DYNAVOX_LONG;
                case CoreEnums.PointsSources.TobiiEyeX: return Resources.TOBII_GAMING_LONG;
                default: return pointsSource.ToDescription();
            }
        }


        public new bool ChangesRequireRestart
        {
            get
            {
                return base.ChangesRequireRestart ||
                     Settings.Default.PointsSource != PointsSource;
            }
        }

        #endregion

        #region Methods

        private new void Load()
        {
            PointsSource = Settings.Default.PointsSource;
        }

        public new void ApplyChanges()
        {
            base.ApplyChanges();

            Settings.Default.PointsSource = PointsSource;
            
        }

        #endregion
    }
}
