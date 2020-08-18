// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsInput.Native;
using JuliusSweetland.OptiKey.InstallerActions;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.InstallerActionsEyeMine;
using JuliusSweetland.OptiKey.UI.ViewModels.Management;
using Microsoft.Deployment.WindowsInstaller;
using NUnit.Framework;

namespace JuliusSweetland.OptiKey.UnitTests
{
    [TestFixture]
    public class InstallerTests
    {

        [Test]
        public void TestMinecraftSaves()
        {
            CustomActionsEyeMine.GetMinecraftSaves(null);
        }

        [Test]
        public void TestPrettyDates()
        {
            // Test "future" date
            Assert.IsNull(InstallerUtils.GetPrettyDate(DateTime.Now.AddSeconds(90)));

            // Test 90 seconds ago.
            Assert.AreEqual("today", InstallerUtils.GetPrettyDate(DateTime.Now.AddSeconds(-90)));

            // Test 25 minutes ago.
            Assert.AreEqual("today", InstallerUtils.GetPrettyDate(DateTime.Now.AddMinutes(-25)));

            // Test 45 minutes ago.
            Assert.AreEqual("today", InstallerUtils.GetPrettyDate(DateTime.Now.AddMinutes(-45)));

            // Test 4 hours ago.
            Assert.AreEqual("today", InstallerUtils.GetPrettyDate(DateTime.Now.AddHours(-4)));

            // Test 1 days ago.
            Assert.AreEqual("yesterday", InstallerUtils.GetPrettyDate(DateTime.Now.AddHours(-25)));

            // Test 3 days ago.
            Assert.AreEqual("3 days ago", InstallerUtils.GetPrettyDate(DateTime.Now.AddDays(-3)));

            // Test 8 days ago.
            Assert.AreEqual("1 week ago", InstallerUtils.GetPrettyDate(DateTime.Now.AddDays(-8)));

            // Test 15 days ago.
            Assert.AreEqual("2 weeks ago", InstallerUtils.GetPrettyDate(DateTime.Now.AddDays(-15)));

            // Test 40 days ago
            Assert.AreEqual("1 month ago", InstallerUtils.GetPrettyDate(DateTime.Now.AddDays(-40)));

            // Test 70 days ago (2 months)
            Assert.AreEqual("2 months ago", InstallerUtils.GetPrettyDate(DateTime.Now.AddDays(-70)));

            // Test 370 days ago
            Assert.AreEqual("more than a year ago", InstallerUtils.GetPrettyDate(DateTime.Now.AddDays(-370)));
            
            // Test 1000 days ago
            Assert.AreEqual("more than a year ago", InstallerUtils.GetPrettyDate(DateTime.Now.AddDays(-1000)));
        }


        [Test]
        public void TestTobiiSupported()
        {
            bool b = InstallerUtils.IsTobiiSupported();
            Assert.True(b);
        }

        [Test]
        public void TestMinecraftLaunched()
        {
            ActionResult res = CustomActionsEyeMine.CheckForMinecraftHasBeenLaunched(null);
            Assert.AreEqual(res, ActionResult.Success);
        }

        [Test]
        public void TestMinecraftInstalled()
        {
            bool installed = InstallerUtils.IsProgramInstalled("Minecraft Launcher");
            Assert.True(installed);
        }

        [Test]
        public void TestForgeInstalled()
        {
            ActionResult res = CustomActionsEyeMine.CheckForForgeInstallation(null);
            Assert.AreEqual(res, ActionResult.Success);
        }

        [Test]
        public void TestProfilesJson()
        {
            //ActionResult res = CustomActionsEyeMine.UpdateForgeConfig(null);
            //Assert.AreEqual(res, ActionResult.Success);
        }

        [Test]
        public void TestSupportedLanguageOnly()
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("de");
            string match = CustomActions.GetDefaultLanguageCode(culture);
            Assert.AreEqual("de-DE", match);
        }

        [Test]
        public void TestUnsupportedLanguageOnly()
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("vo-001");
            string match = CustomActions.GetDefaultLanguageCode(culture);
            // If not supported at all, default to uk english
            Assert.AreEqual("en-GB", match);
        }

        [Test]
        public void TestSupportedLanguageAndCountry()
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("da-DK");
            string match = CustomActions.GetDefaultLanguageCode(culture);
            Assert.AreEqual("da-DK", match);
        }

        [Test]
        public void TestSupportedLanguageAndUnsupportedCountry()
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("da-GL");
            string match = CustomActions.GetDefaultLanguageCode(culture);

            // Default to matching language, if not country
            Assert.True(match.Contains("da"));
        }

        [Test]
        public void TestSupportedLanguageAndUnsupportedCountryWithDefaults()
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("en-FK");
            string match = CustomActions.GetDefaultLanguageCode(culture);

            // Default to English UK 
            Assert.AreEqual( "en-GB", match);
        }

    }
}
