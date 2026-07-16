#if UNITY_EDITOR
using System;
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    [CustomEditor(typeof(JisSDKAdsSettings))]
    public class JisSDKAdsSettingsEditor : UnityEditor.Editor
    {
        BuildTargetPlatform _selectedPlatform = BuildTargetPlatform.Android;
        int _selectedFormatTab;
        int _selectedInterstitialProviderTab;
        int _selectedRewardedProviderTab;
        readonly System.Collections.Generic.List<Action> _deferredInspectorChanges = new();
        bool _deferredInspectorChangeScheduled;

        public override void OnInspectorGUI()
        {
            var settings = (JisSDKAdsSettings)target;
            serializedObject.Update();

            DrawHeader(settings);
            EditorGUILayout.Space(8);
            DrawRuntimeSettings();
            EditorGUILayout.Space(8);

            DrawPlatformToolbar();
            EditorGUILayout.Space(4);
            DrawPlatformProfile(settings);

            serializedObject.ApplyModifiedProperties();
            settings.SyncAllProfileMediationToSdkSetups();
            EditorUtility.SetDirty(settings);
        }

        void DrawHeader(JisSDKAdsSettings settings)
        {
            EditorGUILayout.LabelField("JIS SDK Ads — Project Settings", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);
                if (GUILayout.Button("Apply to Scene (active platform)", GUILayout.Height(32)))
                    JisSDKAdsSettingsApplier.Apply(settings, "Inspector Apply");
                GUI.backgroundColor = prev;

                if (GUILayout.Button("Validate", GUILayout.Height(32), GUILayout.Width(90)))
                    DrawValidation(settings);

                EditorGUILayout.HelpBox(
                    "Scripting defines: Android setup → Android platform; iOS setup → iOS platform (Apply syncs both).",
                    MessageType.None);
            }
        }

        void DrawRuntimeSettings()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("adsInitializationMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableAdsDebugLogging"),
                    new GUIContent("Ads debug logging",
                        "Logs init steps, ad load/show with unit IDs, and errors. Applied on Play and Apply to Scene."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("preloadAdsOnGameStart"),
                    new GUIContent("Preload ads on game start",
                        "Load standard formats after init when Remote Config is ready."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("skipStartupAdLoadWhenRemoveAds"),
                    new GUIContent("Skip preload if Remove Ads",
                        "No startup loads for paying remove-ads players (banner, interstitial, rewarded, app open)."));

                var debugOn = serializedObject.FindProperty("enableAdsDebugLogging").boolValue;
                if (debugOn)
                {
                    EditorGUILayout.HelpBox(
                        "Console filter: [JIS Ads] — init [Init], tier loads (interstitial_load_success), legacy AdMob callbacks.",
                        MessageType.Info);
                }
            }
        }

        void DrawFullscreenShowRoutingSettings()
        {
            EditorGUILayout.LabelField("Fullscreen show routing", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                "Used by ShowInterstitialAuto/ShowRewardVideoAuto and legacy fullscreen show calls.",
                EditorStyles.miniLabel);

            var firstProp = serializedObject.FindProperty("autoShowFirstMediation");
            var secondProp = serializedObject.FindProperty("autoShowSecondMediation");

            EditorGUILayout.PropertyField(firstProp,
                new GUIContent("Auto priority 1",
                    "First mediation attempted for auto interstitial/rewarded show."));
            EditorGUILayout.PropertyField(secondProp,
                new GUIContent("Auto priority 2",
                    "Fallback mediation attempted when priority 1 is not loaded."));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("AdMob > MAX"))
                {
                    firstProp.enumValueIndex = (int)AdsMediationType.ADMOB;
                    secondProp.enumValueIndex = (int)AdsMediationType.MAX;
                }

                if (GUILayout.Button("MAX > AdMob"))
                {
                    firstProp.enumValueIndex = (int)AdsMediationType.MAX;
                    secondProp.enumValueIndex = (int)AdsMediationType.ADMOB;
                }

                if (GUILayout.Button("Swap", GUILayout.Width(70)))
                {
                    var first = firstProp.enumValueIndex;
                    firstProp.enumValueIndex = secondProp.enumValueIndex;
                    secondProp.enumValueIndex = first;
                }
            }

            var firstMediation = (AdsMediationType)firstProp.enumValueIndex;
            var secondMediation = (AdsMediationType)secondProp.enumValueIndex;
            if (firstMediation == AdsMediationType.NONE)
            {
                EditorGUILayout.HelpBox("Auto priority 1 is NONE. Auto fullscreen show will fall back to the active platform mediation order.", MessageType.Warning);
            }
            else if (secondMediation == AdsMediationType.NONE)
            {
                EditorGUILayout.HelpBox("Auto priority 2 is NONE. Explicit fallback may still use the remaining mediation when available.", MessageType.Info);
            }
            else if (firstMediation == secondMediation)
            {
                EditorGUILayout.HelpBox("Auto priority 1 and 2 are the same. The fallback mediation will be ignored.", MessageType.Warning);
            }

            EditorGUILayout.HelpBox(
                "Apply to Scene syncs the scripting defines required by these mediation priorities for Android and iOS.",
                MessageType.None);
        }

        void DrawPlatformToolbar()
        {
            EditorGUILayout.LabelField("Platform", EditorStyles.miniBoldLabel);
            _selectedPlatform = (BuildTargetPlatform)GUILayout.Toolbar(
                (int)_selectedPlatform,
                new[] { "Android", "iOS" });
        }

        void DrawPlatformProfile(JisSDKAdsSettings settings)
        {
            var profilePath = _selectedPlatform == BuildTargetPlatform.Android ? "android" : "ios";
            var profileProp = serializedObject.FindProperty(profilePath);
            var profile = settings.GetProfile(_selectedPlatform);
            var setup = profile?.sdkSetup;
            if (setup != null)
                JisSDKAdsInventorySetupUtility.TryInitializeSetupDefaults(setup);

            var primaryMediation = profile?.mediation ?? AdsMediationType.NONE;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{_selectedPlatform} profile", EditorStyles.boldLabel);

                if (setup == null)
                {
                    EditorGUILayout.HelpBox("SDKSetup chưa gán cho platform này.", MessageType.Warning);
                    if (GUILayout.Button("Create & assign SDKSetup"))
                    {
                        JisSDKAdsInventorySetupUtility.EnsureSdkSetup(settings, _selectedPlatform);
                        GUIUtility.ExitGUI();
                    }

                    return;
                }

                if (profileProp != null)
                {
                    EditorGUILayout.PropertyField(profileProp.FindPropertyRelative("mediation"),
                        new GUIContent("Primary mediation"));
                    DrawProviderConfigField(profileProp, profile);
                    primaryMediation = profile.mediation;
                }

                EditorGUILayout.Space(6);
                DrawSingleFormatSection("SDK Keys & Integrations", () =>
                    JisSDKAdsSetupFieldDrawer.DrawSdkKeys(
                        setup,
                        primaryMediation,
                        ShouldShowMaxSdkKey(settings, setup, primaryMediation)));

                EditorGUILayout.Space(6);
                DrawFormatTabs(settings, profile, setup, primaryMediation);
            }
        }

        void DrawFormatTabs(
            JisSDKAdsSettings settings,
            PlatformAdsProfile profile,
            SDKSetup setup,
            AdsMediationType primaryMediation)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Ad Formats", EditorStyles.boldLabel);
                var selectedTab = GUILayout.Toolbar(
                    _selectedFormatTab,
                    new[] { "Interstitial", "Rewarded", "Banner", "App Open" });

                if (selectedTab != _selectedFormatTab)
                {
                    ScheduleInspectorChange(() => _selectedFormatTab = selectedTab);
                }

                EditorGUILayout.Space(6);
                switch (_selectedFormatTab)
                {
                    case 0:
                        DrawInterstitialFormatTab(settings, profile, setup, primaryMediation);
                        break;
                    case 1:
                        DrawRewardedFormatTab(settings, profile, setup, primaryMediation);
                        break;
                    case 2:
                        DrawBannerFormatTab(setup, primaryMediation);
                        break;
                    case 3:
                        DrawAppOpenFormatTab(setup, primaryMediation);
                        break;
                }
            }
        }

        void DrawInterstitialFormatTab(
            JisSDKAdsSettings settings,
            PlatformAdsProfile profile,
            SDKSetup setup,
            AdsMediationType primaryMediation)
        {
            var modeProp = serializedObject.FindProperty("interstitialMediationMode");
            EditorGUILayout.PropertyField(
                modeProp,
                new GUIContent("Mediation mode",
                    "Single uses primary mediation only. Multiple enables interstitial cross-mediation fallback."));

            var usesMultiple = (AdFormatMediationMode)modeProp.enumValueIndex == AdFormatMediationMode.Multiple;
            if (usesMultiple)
            {
                EditorGUILayout.Space(4);
                DrawFullscreenShowRoutingSettings();
                EditorGUILayout.Space(4);
                DrawMultipleFullscreenFormatSection(settings, profile, setup, primaryMediation, "Interstitial", true);
                return;
            }

            DrawFormatInventorySection(
                "Interstitial",
                JisSDKAdsInventorySetupUtility.GetInterstitialMode(profile),
                mode => JisSDKAdsInventorySetupUtility.SetInterstitialMode(settings, _selectedPlatform, mode),
                setup,
                primaryMediation,
                false,
                () => setup.interstitialAdsMediationType,
                v => setup.interstitialAdsMediationType = v,
                () => JisSDKAdsSetupFieldDrawer.DrawInterstitialSingleUnit(
                    setup,
                    primaryMediation,
                    _selectedPlatform,
                    false),
                () => JisSDKAdsSetupFieldDrawer.DrawSequentialTierConfig(
                    JisSDKAdsInventorySetupUtility.GetInterstitialTierConfig(profile),
                    _selectedPlatform,
                    JisSDKAds.Ads.SequentialTier.SequentialTierAdFormat.Interstitial,
                    setup.GetAdsMediationType(AdsType.INTERSTITIAL)));
        }

        void DrawRewardedFormatTab(
            JisSDKAdsSettings settings,
            PlatformAdsProfile profile,
            SDKSetup setup,
            AdsMediationType primaryMediation)
        {
            var modeProp = serializedObject.FindProperty("rewardedMediationMode");
            EditorGUILayout.PropertyField(
                modeProp,
                new GUIContent("Mediation mode",
                    "Single uses primary mediation only. Multiple enables rewarded cross-mediation fallback."));

            var usesMultiple = (AdFormatMediationMode)modeProp.enumValueIndex == AdFormatMediationMode.Multiple;
            if (usesMultiple)
            {
                EditorGUILayout.Space(4);
                DrawFullscreenShowRoutingSettings();
                EditorGUILayout.Space(4);
                DrawMultipleFullscreenFormatSection(settings, profile, setup, primaryMediation, "Rewarded", false);
                return;
            }

            DrawFormatInventorySection(
                "Rewarded",
                JisSDKAdsInventorySetupUtility.GetRewardedMode(profile),
                mode => JisSDKAdsInventorySetupUtility.SetRewardedMode(settings, _selectedPlatform, mode),
                setup,
                primaryMediation,
                false,
                () => setup.rewardedAdsMediationType,
                v => setup.rewardedAdsMediationType = v,
                () => JisSDKAdsSetupFieldDrawer.DrawRewardedSingleUnit(
                    setup,
                    primaryMediation,
                    _selectedPlatform,
                    false),
                () => JisSDKAdsSetupFieldDrawer.DrawSequentialTierConfig(
                    JisSDKAdsInventorySetupUtility.GetRewardedTierConfig(profile),
                    _selectedPlatform,
                    JisSDKAds.Ads.SequentialTier.SequentialTierAdFormat.Rewarded,
                    setup.GetAdsMediationType(AdsType.REWARDED)));
        }

        void DrawBannerFormatTab(SDKSetup setup, AdsMediationType primaryMediation)
        {
            DrawSingleFormatSection("Banner", () =>
            {
                EditorGUILayout.HelpBox("Banner uses Primary mediation only. Multiple mediation is not used for Banner.", MessageType.None);
                JisSDKAdsSetupFieldDrawer.DrawBannerSingle(
                    setup,
                    primaryMediation,
                    _selectedPlatform,
                    false);
            });
        }

        void DrawAppOpenFormatTab(SDKSetup setup, AdsMediationType primaryMediation)
        {
            DrawSingleFormatSection("App Open", () =>
            {
                EditorGUILayout.HelpBox("App Open uses Primary mediation only.", MessageType.None);
                JisSDKAdsSetupFieldDrawer.DrawAppOpenSingle(
                    setup,
                    primaryMediation,
                    _selectedPlatform,
                    false);
            });
        }

        void DrawMultipleFullscreenFormatSection(
            JisSDKAdsSettings settings,
            PlatformAdsProfile profile,
            SDKSetup setup,
            AdsMediationType primaryMediation,
            string title,
            bool isInterstitial)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"Multiple mode: configure AdMob and MAX {title.ToLowerInvariant()} inventory separately.",
                    EditorStyles.miniLabel);

                SyncFullscreenRouteToPrimary(setup, primaryMediation, isInterstitial);

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.EnumPopup("Primary route", primaryMediation);

                if (isInterstitial)
                    JisSDKAdsSetupFieldDrawer.DrawInterstitialFormatOptions(setup);
                else
                    JisSDKAdsSetupFieldDrawer.DrawRewardedFormatOptions(setup);

                EditorGUILayout.Space(6);
                var mediation = DrawProviderMediationTabs(isInterstitial);
                DrawProviderFullscreenInventorySection(
                    settings,
                    profile,
                    setup,
                    title,
                    mediation,
                    isInterstitial);
            }
        }

        AdsMediationType DrawProviderMediationTabs(bool isInterstitial)
        {
            var currentTab = isInterstitial
                ? _selectedInterstitialProviderTab
                : _selectedRewardedProviderTab;

            EditorGUILayout.LabelField("Mediation setup", EditorStyles.miniLabel);
            var selectedTab = GUILayout.Toolbar(
                currentTab,
                new[] { "AdMob", "MAX" });

            if (selectedTab != currentTab)
            {
                ScheduleInspectorChange(() =>
                {
                    if (isInterstitial)
                        _selectedInterstitialProviderTab = selectedTab;
                    else
                        _selectedRewardedProviderTab = selectedTab;
                });
            }

            return currentTab == 0 ? AdsMediationType.ADMOB : AdsMediationType.MAX;
        }

        void DrawProviderFullscreenInventorySection(
            JisSDKAdsSettings settings,
            PlatformAdsProfile profile,
            SDKSetup setup,
            string title,
            AdsMediationType mediation,
            bool isInterstitial)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{GetMediationLabel(mediation)} {title}", EditorStyles.miniBoldLabel);

                var currentMode = isInterstitial
                    ? JisSDKAdsInventorySetupUtility.GetInterstitialMode(profile, mediation)
                    : JisSDKAdsInventorySetupUtility.GetRewardedMode(profile, mediation);

                var selectedMode = DrawInventoryModeToolbar(currentMode);
                if (selectedMode != currentMode)
                {
                    ScheduleInspectorChange(() =>
                    {
                        if (isInterstitial)
                            JisSDKAdsInventorySetupUtility.SetInterstitialMode(settings, _selectedPlatform, mediation, selectedMode);
                        else
                            JisSDKAdsInventorySetupUtility.SetRewardedMode(settings, _selectedPlatform, mediation, selectedMode);
                    });
                }

                EditorGUILayout.Space(4);
                if (currentMode == AdInventorySetupMode.SingleUnit)
                {
                    if (isInterstitial)
                        JisSDKAdsSetupFieldDrawer.DrawInterstitialSingleUnitForMediation(setup, mediation, _selectedPlatform);
                    else
                        JisSDKAdsSetupFieldDrawer.DrawRewardedSingleUnitForMediation(setup, mediation, _selectedPlatform);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Sequential ladder: Premium -> High -> Mid -> Low -> Fill (one load at a time).",
                        MessageType.Info);

                    var tier = isInterstitial
                        ? JisSDKAdsInventorySetupUtility.GetInterstitialTierConfig(profile, mediation)
                        : JisSDKAdsInventorySetupUtility.GetRewardedTierConfig(profile, mediation);
                    var format = isInterstitial
                        ? JisSDKAds.Ads.SequentialTier.SequentialTierAdFormat.Interstitial
                        : JisSDKAds.Ads.SequentialTier.SequentialTierAdFormat.Rewarded;

                    JisSDKAdsSetupFieldDrawer.DrawSequentialTierConfig(
                        tier,
                        _selectedPlatform,
                        format,
                        mediation);
                }
            }
        }

        static void SyncFullscreenRouteToPrimary(
            SDKSetup setup,
            AdsMediationType primaryMediation,
            bool isInterstitial)
        {
            if (setup == null || primaryMediation == AdsMediationType.NONE)
                return;

            if (isInterstitial)
            {
                if (setup.interstitialAdsMediationType == primaryMediation)
                    return;

                setup.interstitialAdsMediationType = primaryMediation;
            }
            else
            {
                if (setup.rewardedAdsMediationType == primaryMediation)
                    return;

                setup.rewardedAdsMediationType = primaryMediation;
            }

            EditorUtility.SetDirty(setup);
        }

        static string GetMediationLabel(AdsMediationType mediation) => mediation switch
        {
            AdsMediationType.ADMOB => "AdMob",
            AdsMediationType.MAX => "MAX",
            _ => mediation.ToString()
        };

        static bool ShouldShowMaxSdkKey(
            JisSDKAdsSettings settings,
            SDKSetup setup,
            AdsMediationType primaryMediation)
        {
            if (primaryMediation == AdsMediationType.MAX)
                return true;

            if (settings != null && settings.GetFullscreenAutoShowPriority().Contains(AdsMediationType.MAX))
                return true;

            return setup != null
                   && (setup.GetAdsMediationType(AdsType.BANNER) == AdsMediationType.MAX
                       || setup.GetAdsMediationType(AdsType.INTERSTITIAL) == AdsMediationType.MAX
                       || setup.GetAdsMediationType(AdsType.REWARDED) == AdsMediationType.MAX
                       || setup.GetAdsMediationType(AdsType.APP_OPEN) == AdsMediationType.MAX);
        }

        static void DrawSingleFormatSection(string title, Action drawContent)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                drawContent();
            }
        }

        void DrawFormatInventorySection(
            string title,
            AdInventorySetupMode currentMode,
            Action<AdInventorySetupMode> setMode,
            SDKSetup setup,
            AdsMediationType primaryMediation,
            bool allowMediationSelection,
            Func<AdsMediationType> getMediation,
            Action<AdsMediationType> setMediation,
            Action drawSingle,
            Action drawTiered)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "Local default (overridden at runtime by Firebase interstitial_inventory_mode / rewarded_inventory_mode).",
                    EditorStyles.miniLabel);

                var selectedMode = DrawInventoryModeToolbar(currentMode);
                if (selectedMode != currentMode)
                {
                    ScheduleInspectorChange(() => setMode(selectedMode));
                }

                EditorGUILayout.Space(4);

                if (currentMode == AdInventorySetupMode.SingleUnit)
                {
                    drawSingle();
                }
                else
                {
                    var currentMediation = getMediation != null ? getMediation() : primaryMediation;
                    var nextMediation = currentMediation;
                    if (allowMediationSelection)
                    {
                        nextMediation = (AdsMediationType)EditorGUILayout.EnumPopup("Mediation", currentMediation);
                    }
                    else
                    {
                        nextMediation = primaryMediation != AdsMediationType.NONE
                            ? primaryMediation
                            : currentMediation;
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.EnumPopup("Mediation", nextMediation);
                    }

                    if (nextMediation != currentMediation)
                    {
                        var mediationToApply = nextMediation;
                        ScheduleInspectorChange(() =>
                        {
                            setMediation?.Invoke(mediationToApply);
                            setMode(AdInventorySetupMode.Tiered);
                            EditorUtility.SetDirty(setup);
                        });
                    }

                    EditorGUILayout.HelpBox(
                        "Sequential ladder: Premium → High → Mid → Low → Fill (one load at a time).",
                        MessageType.Info);
                    drawTiered();
                }
            }
        }

        static AdInventorySetupMode DrawInventoryModeToolbar(AdInventorySetupMode current)
        {
            EditorGUILayout.LabelField("Inventory mode", EditorStyles.miniLabel);
            return (AdInventorySetupMode)GUILayout.Toolbar(
                (int)current,
                new[] { "Single unit", "Tiered" });
        }

        void ScheduleInspectorChange(Action change)
        {
            if (change == null)
                return;

            _deferredInspectorChanges.Add(change);
            if (_deferredInspectorChangeScheduled)
                return;

            _deferredInspectorChangeScheduled = true;
            EditorApplication.delayCall += ApplyDeferredInspectorChanges;
        }

        void ApplyDeferredInspectorChanges()
        {
            _deferredInspectorChangeScheduled = false;
            if (_deferredInspectorChanges.Count == 0)
                return;

            var changes = _deferredInspectorChanges.ToArray();
            _deferredInspectorChanges.Clear();
            foreach (var change in changes)
                change();

            Repaint();
        }

        void DrawProviderConfigField(SerializedProperty profileProp, PlatformAdsProfile profile)
        {
            if (profile == null) return;

            switch (profile.mediation)
            {
                case AdsMediationType.MAX:
                    EditorGUILayout.PropertyField(profileProp.FindPropertyRelative("maxProviderConfig"),
                        new GUIContent("MAX provider config (Core)"));
                    break;
                case AdsMediationType.ADMOB:
                    EditorGUILayout.PropertyField(profileProp.FindPropertyRelative("admobProviderConfig"),
                        new GUIContent("AdMob provider config (Core)"));
                    break;
            }
        }

        void DrawValidation(JisSDKAdsSettings settings)
        {
            var result = JisSDKAdsSettingsApplier.Validate(settings);
            foreach (var err in result.Errors)
                EditorGUILayout.HelpBox(err, MessageType.Error);
            foreach (var warn in result.Warnings)
                EditorGUILayout.HelpBox(warn, MessageType.Warning);

            if (result.IsValid && result.Warnings.Count == 0)
                EditorGUILayout.HelpBox("Validation passed.", MessageType.Info);
        }
    }
}
#endif
