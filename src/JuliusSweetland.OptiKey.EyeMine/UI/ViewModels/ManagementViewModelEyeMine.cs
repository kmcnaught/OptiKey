// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System;
using System.Diagnostics;
using System.Linq;
using JuliusSweetland.OptiKey.Properties;
using EyeMineResources = JuliusSweetland.OptiKey.EyeMine.Properties.Resources;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.UI.ViewModels.Management;
using JuliusSweetland.OptiKey.InstallerActionsEyeMine;
using log4net;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System.Windows;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.EyeMine.UI.ViewModels.Management;
using EyeXFramework;
using Tobii.EyeX.Client;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    public class ManagementViewModelEyeMine 
    {

        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Ctor

        public ManagementViewModelEyeMine(
            IAudioService audioService,
            IDictionaryService dictionaryService,
            IWindowManipulationService windowManipulationService)
        {
            //Instantiate child VMs
            DictionaryViewModel = new DictionaryViewModel(dictionaryService);
            PointingAndSelectingViewModel = new PointingAndSelectingViewModelEyeMine();
            SoundsViewModel = new SoundsViewModel(audioService);
            VisualsViewModel = new VisualsViewModelEyeMine(windowManipulationService);
            FeaturesViewModel = new FeaturesViewModel();
            WordsViewModel = new WordsViewModel(dictionaryService);
            AboutViewModel = new AboutViewModel();
            OtherViewModel = new OtherViewModel();

            //Instantiate interaction requests and commands
            ConfirmationRequest = new InteractionRequest<Confirmation>();
            OkCommand = new DelegateCommand<Window>(Ok); //Can always click Ok
            CancelCommand = new DelegateCommand<Window>(Cancel); //Can always click Cancel
        }

        private bool IsMinecraftRunning()
        {
            foreach (Process process in Process.GetProcesses())
            {
                // We don't have a "minecraft" process, we have a "java" process which we can interrogate for minecraft keywords
                if (process.ProcessName.Contains("java"))
                {
                    try
                    {
                        string commandLine = process.GetCommandLine();
                        if (commandLine.ToLower().Contains("minecraft"))
                            return true;
                    }
                    // Catch and ignore exceptions, e.g. "Access denied" or "Cannot process request"
                    catch (Exception ex)
                    {
                        Log.Error("Exception querying java process", ex);
                    }
                }
            }

            return false;
        }

        #endregion

        #region Properties

        public bool ChangesRequireRestart
        {
            get
            {
                return DictionaryViewModel.ChangesRequireRestart
                    || PointingAndSelectingViewModel.ChangesRequireRestart
                    || SoundsViewModel.ChangesRequireRestart
                    || VisualsViewModel.ChangesRequireRestart
                    || FeaturesViewModel.ChangesRequireRestart
                    || AboutViewModel.ChangesRequireRestart
                    || WordsViewModel.ChangesRequireRestart;
            }
        }

        public DictionaryViewModel DictionaryViewModel { get; private set; }
        public PointingAndSelectingViewModelEyeMine PointingAndSelectingViewModel { get; private set; }
        public SoundsViewModel SoundsViewModel { get; private set; }
        public VisualsViewModelEyeMine VisualsViewModel { get; protected set; }
        public FeaturesViewModel FeaturesViewModel { get; private set; }
        public WordsViewModel WordsViewModel { get; private set; }
        public AboutViewModel AboutViewModel { get; private set; }
        public OtherViewModel OtherViewModel { get; private set; }

        public InteractionRequest<Confirmation> ConfirmationRequest { get; private set; }
        public DelegateCommand<Window> OkCommand { get; private set; }
        public DelegateCommand<Window> CancelCommand { get; private set; }

        #endregion

        #region Methods
        
        private void CoerceValues()
        {
            CoercePersianSettings();
            CoerceUrduSettings();
        }

        private void CoercePersianSettings()
        {
            if (WordsViewModel.KeyboardAndDictionaryLanguage == Languages.PersianIran
                && WordsViewModel.UiLanguage != Languages.PersianIran)
            {
                ConfirmationRequest.Raise(
                    new Confirmation
                    {
                        Title = Resources.UILANGUAGE_AND_KEYBOARDANDDICTIONARYLANGUAGE_DIFFER_TITLE,
                        Content = Resources.DEFAULT_UILANGUAGE_TO_PERSIAN
                    }, confirmation =>
                    {
                        if (confirmation.Confirmed)
                        {
                            Log.Info("Prompting user to change the UiLanguage to Persian as the KeyboardAndDictionaryLanguage is Persian. The UiLanguage controls whether the scratchpad has text flow RightToLeft, which Persian requires.");
                            WordsViewModel.UiLanguage = Languages.PersianIran;
                        }
                    });
            }

            if ((WordsViewModel.KeyboardAndDictionaryLanguage == Languages.PersianIran
                 || WordsViewModel.UiLanguage == Languages.PersianIran)
                && !new[]
                {
                    VisualsViewModelEyeMine.ElhamUrl,
                    VisualsViewModelEyeMine.HomaUrl,
                    VisualsViewModelEyeMine.KoodakUrl,
                    VisualsViewModelEyeMine.NazliUrl,
                    VisualsViewModelEyeMine.RoyaUrl,
                    VisualsViewModelEyeMine.TerafikUrl,
                    VisualsViewModelEyeMine.TitrUrl
                }.Contains(VisualsViewModel.FontFamily))
            {
                ConfirmationRequest.Raise(
                    new Confirmation
                    {
                        Title = Resources.LANGUAGE_SPECIFIC_FONT_RECOMMENDED,
                        Content = Resources.FONTFAMILY_IS_NOT_COMPATIBLE_WITH_PERSIAN_LANGUAGE
                    }, confirmation =>
                    {
                        if (confirmation.Confirmed)
                        {
                            Log.Info("Prompting user to change the font to an Persian compatible font. If another font is used then text may be displayed incorrectly.");
                            VisualsViewModel.FontFamily = VisualsViewModelEyeMine.NazliUrl;
                            VisualsViewModel.FontStretch = Enums.FontStretches.Normal;
                            VisualsViewModel.FontWeight = Enums.FontWeights.Regular;
                        }
                    });
            }
        }

        private void CoerceUrduSettings()
        {
            if (WordsViewModel.KeyboardAndDictionaryLanguage == Languages.UrduPakistan
                && WordsViewModel.UiLanguage != Languages.UrduPakistan)
            {
                ConfirmationRequest.Raise(
                    new Confirmation
                    {
                        Title = Resources.UILANGUAGE_AND_KEYBOARDANDDICTIONARYLANGUAGE_DIFFER_TITLE,
                        Content = Resources.DEFAULT_UILANGUAGE_TO_URDU
                    }, confirmation =>
                    {
                        if (confirmation.Confirmed)
                        {
                            Log.Info("Prompting user to change the UiLanguage to Urdu as the KeyboardAndDictionaryLanguage is Urdu. The UiLanguage controls whether the scratchpad has text flow RightToLeft, which Urdu requires.");
                            WordsViewModel.UiLanguage = Languages.UrduPakistan;
                        }
                    });
            }

            if ((WordsViewModel.KeyboardAndDictionaryLanguage == Languages.UrduPakistan
                 || WordsViewModel.UiLanguage == Languages.UrduPakistan)
                && !new[]
                {
                    VisualsViewModelEyeMine.FajerNooriNastaliqueUrl,
                    VisualsViewModelEyeMine.NafeesWebNaskhUrl,
                    VisualsViewModelEyeMine.PakNastaleeqUrl
                }.Contains(VisualsViewModel.FontFamily))
            {
                ConfirmationRequest.Raise(
                    new Confirmation
                    {
                        Title = Resources.LANGUAGE_SPECIFIC_FONT_RECOMMENDED,
                        Content = Resources.FONTFAMILY_IS_NOT_COMPATIBLE_WITH_URDU_LANGUAGE
                    }, confirmation =>
                    {
                        if (confirmation.Confirmed)
                        {
                            Log.Info("Prompting user to change the font to an Urdu compatible font. If another font is used then text (especially numbers which are only displayed correctly in Urdu if an Urdu font is used) may be displayed incorrectly.");
                            VisualsViewModel.FontFamily = VisualsViewModelEyeMine.NafeesWebNaskhUrl;
                            VisualsViewModel.FontStretch = Enums.FontStretches.Normal;
                            VisualsViewModel.FontWeight = Enums.FontWeights.Regular;
                        }
                    });
            }
        }

        protected void ApplyChanges()
        {
            DictionaryViewModel.ApplyChanges();
            PointingAndSelectingViewModel.ApplyChanges();
            SoundsViewModel.ApplyChanges();
            VisualsViewModel.ApplyChanges();
            FeaturesViewModel.ApplyChanges();
            WordsViewModel.ApplyChanges();
            OtherViewModel.ApplyChanges();
        }

        private void RestartRequest()
        {
            //Warn if restart required and prompt for Confirmation before restarting
            ConfirmationRequest.Raise(
                new Confirmation
                {
                    Title = EyeMineResources.VERIFY_RESTART,
                    Content = EyeMineResources.RESTART_MESSAGE
                }, confirmation =>
                {
                    if (confirmation.Confirmed)
                    {
                        SaveAndRestart();
                    }
                });
        }

        private void UpdateMinecraftConfig()
        {
            Log.Info("UpdateMinecraftConfig");
            bool useMouseEmulation = Settings.Default.PointsSource == PointsSources.MousePosition;
            bool success = CustomActionsEyeMine.UpdateModConfig(useMouseEmulation);
            Log.Info($"Updated minecraft config: useMouseEmulation={ useMouseEmulation } success={ success }");
        }

        private void SaveAndRestart()
        {
            // Cache RequireMinecraftUpdate before saving settings
            bool requireMinecraftUpdate = PointingAndSelectingViewModel.RequireMinecraftUpdate;

            Log.Info("Applying management changes and attempting to restart EyeMine");
            ApplyChanges();
            Settings.Default.Save();

            // Update Minecraft config if required
            if (requireMinecraftUpdate)
            {
                UpdateMinecraftConfig();
            }

            try
            {
                OptiKeyApp.RestartApp();
            }
            catch { } //Swallow any exceptions (e.g. DispatcherExceptions) - we're shutting down so it doesn't matter.
            Application.Current.Shutdown();
        }

        private void RestartMinecraftRequest()
        {
            //Warn if restart required and prompt for Confirmation before restarting
            ConfirmationRequest.Raise(
                new Confirmation
                {
                    Title = EyeMineResources.VERIFY_RESTART,
                    Content = EyeMineResources.RESTART_MESSAGE + "\n\nWARNING! Please close Minecraft and restart to load the new EyeMine mod settings."
                }, confirmation =>
                {
                    if (confirmation.Confirmed)
                    {
                        SaveAndRestart();
                    }
                });
        }

        private void Ok(Window window)
        {
            Action OkImpl = () =>
            {
                if (PointingAndSelectingViewModel.RequireMinecraftUpdate &&
                IsMinecraftRunning())
                {
                    // Warnings from any changes/selections
                    RestartMinecraftRequest();
                }
                else if (ChangesRequireRestart)
                {
                    RestartRequest();
                }
                else
                {
                    Log.Info("Applying management changes");
                    ApplyChanges();
                    window.Close();
                }
            };

            // Special warning (chance to back out) 
            // if Tobii has been selected but isn't supported           
            if (PointingAndSelectingViewModel.HasChangedToTobii)
            {
                // This tells us if the Tobii eye tracking engine is installed, regardless of whether
                // it is turned on or an eye tracker is connected                
                bool tobiiSupported = (EyeXHost.EyeXAvailability != EyeXAvailability.NotAvailable);
                if (tobiiSupported)
                { 
                    OkImpl();
                }
                else 
                {
                    ConfirmationRequest.Raise(
                        new Confirmation
                        {
                            Title = "Tobii support not found",
                            Content = "EyeMine cannot detect the Tobii engine.\n\n" +
                                      "If you are using an older Tobii Dynavox setup with Windows Control V1 installed you may not be able to use Tobii directly and would be advised to use mouse control instead.\n\n" +
                                      "Are you sure you want to continue with Tobii selected?"
                        }, confirmation =>
                        {
                            if (confirmation.Confirmed)
                                OkImpl();
                        });
                }                    
            }    
            else {
                OkImpl();
            }
        }

        private static void Cancel(Window window)
        {
            window.Close();
        }

        #endregion
    }
}
