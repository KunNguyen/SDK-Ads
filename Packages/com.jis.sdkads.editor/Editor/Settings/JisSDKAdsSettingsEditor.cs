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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("singleMediationOnly"));
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
                    JisSDKAdsSetupFieldDrawer.DrawSdkKeys(setup, primaryMediation));

                EditorGUILayout.Space(6);
                DrawFormatInventorySection(
                    "Interstitial",
                    JisSDKAdsInventorySetupUtility.GetInterstitialMode(profile),
                    mode => JisSDKAdsInventorySetupUtility.SetInterstitialMode(settings, _selectedPlatform, mode),
                    setup,
                    primaryMediation,
                    () => JisSDKAdsSetupFieldDrawer.DrawInterstitialSingleUnit(setup, primaryMediation, _selectedPlatform),
                    () => JisSDKAdsSetupFieldDrawer.DrawSequentialTierConfig(
                        setup.admobAdsSetup.InterstitialTierConfig,
                        _selectedPlatform,
                        JisSDKAds.Ads.SequentialTier.SequentialTierAdFormat.Interstitial));

                EditorGUILayout.Space(6);
                DrawFormatInventorySection(
                    "Rewarded",
                    JisSDKAdsInventorySetupUtility.GetRewardedMode(profile),
                    mode => JisSDKAdsInventorySetupUtility.SetRewardedMode(settings, _selectedPlatform, mode),
                    setup,
                    primaryMediation,
                    () => JisSDKAdsSetupFieldDrawer.DrawRewardedSingleUnit(setup, primaryMediation, _selectedPlatform),
                    () => JisSDKAdsSetupFieldDrawer.DrawSequentialTierConfig(
                        setup.admobAdsSetup.RewardedTierConfig,
                        _selectedPlatform,
                        JisSDKAds.Ads.SequentialTier.SequentialTierAdFormat.Rewarded));

                EditorGUILayout.Space(6);
                DrawSingleFormatSection("Banner", () =>
                    JisSDKAdsSetupFieldDrawer.DrawBannerSingle(setup, primaryMediation, _selectedPlatform));

                EditorGUILayout.Space(6);
                DrawSingleFormatSection("App Open", () =>
                    JisSDKAdsSetupFieldDrawer.DrawAppOpenSingle(setup, primaryMediation, _selectedPlatform));

            }
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
            Action drawSingle,
            Action drawTiered)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "Local default (overridden at runtime by Firebase interstitial_inventory_mode / rewarded_inventory_mode).",
                    EditorStyles.miniLabel);

                var newMode = DrawInventoryModeToolbar(currentMode);
                if (newMode != currentMode)
                {
                    setMode(newMode);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.Space(4);

                if (newMode == AdInventorySetupMode.SingleUnit)
                {
                    drawSingle();
                }
                else
                {
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
