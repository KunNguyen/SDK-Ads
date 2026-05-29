#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using JisSDKAds.Ads;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using SequentialAdTier = JisSDKAds.Ads.SequentialTier.AdTier;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    static class JisSDKAdsSetupFieldDrawer
    {
        public static void DrawInterstitialSingleUnit(
            SDKSetup setup,
            AdsMediationType primaryMediation,
            BuildTargetPlatform platform)
        {
            DrawSetupFields(setup, () =>
            {
                setup.interstitialAdsMediationType = DrawMediationField(
                    "Mediation",
                    setup.interstitialAdsMediationType,
                    primaryMediation);

                setup.IsActiveCooldownInterstitialFromStart = EditorGUILayout.Toggle(
                    new GUIContent("Cooldown from start", "Apply interstitial cooldown when the app starts."),
                    setup.IsActiveCooldownInterstitialFromStart);

                DrawUnitIds(
                    setup.interstitialAdsMediationType,
                    platform,
                    "Default ad unit ID (single)",
                    () => setup.interstitialAdUnitID_MAX,
                    v => setup.interstitialAdUnitID_MAX = v,
                    setup.admobAdsSetup.InterstitialAdUnitID);
            });
        }

        public static void DrawRewardedSingleUnit(
            SDKSetup setup,
            AdsMediationType primaryMediation,
            BuildTargetPlatform platform)
        {
            DrawSetupFields(setup, () =>
            {
                setup.rewardedAdsMediationType = DrawMediationField(
                    "Mediation",
                    setup.rewardedAdsMediationType,
                    primaryMediation);

                if (setup.rewardedAdsMediationType != AdsMediationType.NONE)
                {
                    setup.IsLinkToRemoveAds = EditorGUILayout.Toggle(
                        new GUIContent("Link to remove ads", "Rewarded completion can grant remove-ads state."),
                        setup.IsLinkToRemoveAds);
                }

                DrawUnitIds(
                    setup.rewardedAdsMediationType,
                    platform,
                    "Default ad unit ID (single)",
                    () => setup.rewardedAdUnitID_MAX,
                    v => setup.rewardedAdUnitID_MAX = v,
                    setup.admobAdsSetup.RewardedAdUnitID);
            });
        }

        public static void DrawSequentialTierConfig(
            SequentialTierConfig tier,
            BuildTargetPlatform platform,
            SequentialTierAdFormat format)
        {
            if (tier == null) return;
            tier.EnsureDefaultTierSlots();

            var platformLabel = platform == BuildTargetPlatform.iOS ? "iOS" : "Android";
            var modeKey = format == SequentialTierAdFormat.Interstitial
                ? Keys.key_remote_interstitial_inventory_mode
                : Keys.key_remote_rewarded_inventory_mode;
            var tierKeys = format == SequentialTierAdFormat.Interstitial
                ? "inter_premium_id … inter_fill_id"
                : "reward_premium_id … reward_fill_id";
            EditorGUILayout.HelpBox(
                $"Remote Config overrides Single/Tiered at runtime (default: single).\n" +
                $"Mode key: {modeKey} — values: single | tiered\n" +
                $"Tier IDs (tiered only): {tierKeys}\n" +
                $"Platform: {platformLabel}, AdMob. Editor toolbar = local default before RC fetch.",
                MessageType.Info);

            if (platform == BuildTargetPlatform.iOS)
                tier.defaultIosAdUnitId = EditorGUILayout.TextField("Fallback unit ID (optional)", tier.defaultIosAdUnitId);
            else
                tier.defaultAndroidAdUnitId = EditorGUILayout.TextField("Fallback unit ID (optional)", tier.defaultAndroidAdUnitId);

            tier.enableTierMemoryCooldown = EditorGUILayout.Toggle("Tier memory + cooldown", tier.enableTierMemoryCooldown);
            tier.premiumRetryCooldownMinutes = EditorGUILayout.FloatField(
                "Premium retry cooldown (min)", tier.premiumRetryCooldownMinutes);
            tier.consecutiveFailuresBeforeDowngrade = EditorGUILayout.IntField(
                "Failures before downgrade", tier.consecutiveFailuresBeforeDowngrade);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Per-tier load timeout (Premium → Fill)", EditorStyles.miniBoldLabel);
            foreach (SequentialAdTier t in Enum.GetValues(typeof(SequentialAdTier)))
            {
                var entry = tier.GetEntry(t);
                if (entry == null) continue;
                entry.timeoutSeconds = EditorGUILayout.FloatField($"  {entry.tier} timeout (s, 0=none)", entry.timeoutSeconds);
            }
        }

        public static void DrawBannerSingle(SDKSetup setup, AdsMediationType primaryMediation, BuildTargetPlatform platform)
        {
            DrawSetupFields(setup, () =>
            {
                setup.bannerAdsMediationType = DrawMediationField(
                    "Mediation",
                    setup.bannerAdsMediationType,
                    primaryMediation);

                if (setup.bannerAdsMediationType == AdsMediationType.NONE)
                    return;

                if (setup.bannerAdsMediationType == AdsMediationType.MAX)
                    DrawEnumMember(setup, "maxBannerAdsPosition", "Banner position");
                else if (setup.bannerAdsMediationType == AdsMediationType.ADMOB)
                    DrawEnumMember(setup, "admobBannerAdsPosition", "Banner position");

                setup.isBannerShowingOnStart = EditorGUILayout.Toggle(
                    "Show on start",
                    setup.isBannerShowingOnStart);
                setup.isAutoRefreshBannerByCode = EditorGUILayout.Toggle(
                    "Auto refresh (local default)",
                    setup.isAutoRefreshBannerByCode);
                if (setup.isAutoRefreshBannerByCode)
                {
                    setup.bannerAutoRefreshIntervalSeconds = EditorGUILayout.FloatField(
                        "Refresh interval (seconds)",
                        setup.bannerAutoRefreshIntervalSeconds);
                }

                EditorGUILayout.HelpBox(
                    "Banner uses a single ad unit (no tiered inventory). " +
                    "When Firebase RC is active, banner_auto_refresh and banner_auto_refresh_time override local values.",
                    MessageType.None);

                DrawUnitIds(
                    setup.bannerAdsMediationType,
                    platform,
                    "Banner ad unit ID",
                    () => setup.bannerAdUnitID_MAX,
                    v => setup.bannerAdUnitID_MAX = v,
                    setup.admobAdsSetup.BannerAdUnitID,
                    singleIdOnly: true);
            });
        }

        public static void DrawAppOpenSingle(SDKSetup setup, AdsMediationType primaryMediation, BuildTargetPlatform platform)
        {
            DrawSetupFields(setup, () =>
            {
                setup.appOpenAdsMediationType = DrawMediationField(
                    "Mediation",
                    setup.appOpenAdsMediationType,
                    primaryMediation);

                DrawUnitIds(
                    setup.appOpenAdsMediationType,
                    platform,
                    "App Open ad unit ID",
                    () => setup.appOpenAdUnitID_MAX,
                    v => setup.appOpenAdUnitID_MAX = v,
                    setup.admobAdsSetup.AppOpenAdUnitID);
            });
        }

        public static void DrawSdkKeys(SDKSetup setup, AdsMediationType primaryMediation)
        {
            DrawSetupFields(setup, () =>
            {
                setup.IsActiveAppsflyer = EditorGUILayout.Toggle("AppsFlyer", setup.IsActiveAppsflyer);
                setup.IsActiveFirebaseAuth = EditorGUILayout.Toggle("Firebase Auth", setup.IsActiveFirebaseAuth);
                setup.IsActiveIAP = EditorGUILayout.Toggle("IAP integration", setup.IsActiveIAP);
                setup.IsActiveAdImpressionTracking = EditorGUILayout.Toggle(
                    "Ad impression tracking",
                    setup.IsActiveAdImpressionTracking);
                setup.IsActiveCustomAdImpressionTracking = EditorGUILayout.Toggle(
                    "Custom impression event",
                    setup.IsActiveCustomAdImpressionTracking);

                if (setup.IsActiveCustomAdImpressionTracking)
                {
                    setup.CustomAdImpressionEventName = EditorGUILayout.TextField(
                        "Impression event name",
                        setup.CustomAdImpressionEventName ?? string.Empty);
                }

                if (primaryMediation == AdsMediationType.MAX)
                {
                    setup.sdkKey_MAX = EditorGUILayout.TextField(
                        "AppLovin SDK Key",
                        setup.sdkKey_MAX ?? string.Empty);
                }
            });
        }

        static void DrawSetupFields(SDKSetup setup, Action draw)
        {
            if (setup == null) return;

            JisSDKAdsInventorySetupUtility.TryInitializeSetupDefaults(setup);
            EditorGUI.BeginChangeCheck();
            draw();
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(setup);
        }

        static AdsMediationType DrawMediationField(
            string label,
            AdsMediationType current,
            AdsMediationType primaryMediation)
        {
            var mediation = (AdsMediationType)EditorGUILayout.EnumPopup(label, current);

            if (mediation == AdsMediationType.NONE && primaryMediation != AdsMediationType.NONE)
            {
                EditorGUILayout.HelpBox(
                    $"Mediation is NONE — unit IDs are hidden. Use primary mediation ({primaryMediation}) or pick MAX/AdMob.",
                    MessageType.Info);

                if (GUILayout.Button($"Use primary mediation ({primaryMediation})"))
                    mediation = primaryMediation;
            }

            return mediation;
        }

        static void DrawUnitIds(
            AdsMediationType mediation,
            BuildTargetPlatform platform,
            string maxLabel,
            Func<string> getMaxId,
            Action<string> setMaxId,
            AdScheduleUnitID adMobSchedule,
            bool singleIdOnly = false)
        {
            switch (mediation)
            {
                case AdsMediationType.MAX:
                    setMaxId(EditorGUILayout.TextField(maxLabel, getMaxId() ?? string.Empty));
                    break;

                case AdsMediationType.ADMOB:
                    if (singleIdOnly)
                        DrawAdMobSingleUnitId(platform, adMobSchedule, maxLabel);
                    else
                        DrawAdMobUnitIdList(platform, adMobSchedule);
                    break;

                case AdsMediationType.NONE:
                    break;
            }
        }

        static void DrawAdMobSingleUnitId(BuildTargetPlatform platform, AdScheduleUnitID schedule, string label)
        {
            if (schedule == null) return;

            var ids = schedule.GetPlatformList(platform) ?? new List<string>();
            var current = ids.Count > 0 ? ids[0] : string.Empty;
            var next = EditorGUILayout.TextField(label, current ?? string.Empty);
            schedule.SetPlatformList(platform, new List<string> { next ?? string.Empty });
        }

        static void DrawAdMobUnitIdList(BuildTargetPlatform platform, AdScheduleUnitID schedule)
        {
            if (schedule == null) return;

            var ids = schedule.GetPlatformList(platform) ?? new List<string>();
            var platformLabel = platform == BuildTargetPlatform.iOS ? "iOS" : "Android";

            EditorGUILayout.LabelField($"AdMob unit IDs ({platformLabel})", EditorStyles.boldLabel);
            var count = Mathf.Max(0, EditorGUILayout.IntField("Count", ids.Count));

            if (count != ids.Count)
            {
                while (ids.Count < count)
                    ids.Add(string.Empty);
                while (ids.Count > count)
                    ids.RemoveAt(ids.Count - 1);
            }

            for (var i = 0; i < ids.Count; i++)
                ids[i] = EditorGUILayout.TextField($"  Unit [{i}]", ids[i] ?? string.Empty);

            schedule.SetPlatformList(platform, ids);
        }

        static void DrawProperty(SerializedObject so, string propertyName)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
        }

        static bool DrawEnumMember(SDKSetup setup, string memberName, string label)
        {
            var field = typeof(SDKSetup).GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null || !field.FieldType.IsEnum)
                return false;

            var current = (Enum)field.GetValue(setup);
            var next = EditorGUILayout.EnumPopup(label, current);
            field.SetValue(setup, next);
            return true;
        }

        static void DrawMember(SDKSetup setup, string memberName, string label)
        {
            var field = typeof(SDKSetup).GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
                return;

            if (field.FieldType == typeof(int))
            {
                field.SetValue(setup, EditorGUILayout.IntField(label, (int)field.GetValue(setup)));
                return;
            }

            if (field.FieldType == typeof(float))
            {
                field.SetValue(setup, EditorGUILayout.FloatField(label, (float)field.GetValue(setup)));
            }
        }
    }
}
#endif
