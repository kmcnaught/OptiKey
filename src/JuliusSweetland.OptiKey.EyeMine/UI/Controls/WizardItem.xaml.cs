// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Properties;

namespace JuliusSweetland.OptiKey.EyeMine.UI.Controls
{
    /// <summary>
    /// Interaction logic for WizardItem.xaml
    /// </summary>
    public partial class WizardItem : UserControl
    {
        public WizardItem()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            "Label", typeof(string), typeof(WizardItem), new PropertyMetadata(""));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
            "State", typeof(PageState), typeof(WizardItem), new PropertyMetadata(default(PageState)));

        public PageState State
        {
            get { return (PageState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public enum PageState
        {
            ComingUp,
            Current,
            Complete
        }
    }
}
