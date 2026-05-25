#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Core.Tiered.Config;
using SequentialAdTier = JisSDKAds.Ads.SequentialTier.AdTier;
using JisSDKAds.Core.Tiered.Models;
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
            JisSDKAds.Ads.SequentialTier.SequentialTierConfig tier,
            BuildTargetPlatform platform)
        {
            if (tier == null) return;
            tier.EnsureDefaultTierSlots();

            var platformLabel = platform == BuildTargetPlatform.iOS ? "iOS" : "Android";
            EditorGUILayout.HelpBox(
                $"Tiered mode: ad unit IDs are read from Firebase Remote Config at runtime " +
                $"(inter_* / reward_* keys — see docs/TIERED_INTERSTITIAL.md). " +
                $"Fields below are fallback only. Platform: {platformLabel}. Mediation: AdMob.",
                MessageType.Info);

            if (platform == BuildTargetPlatform.iOS)
                tier.defaultIosAdUnitId = EditorGUILayout.TextField("Fallback unit ID", tier.defaultIosAdUnitId);
            else
                tier.defaultAndroidAdUnitId = EditorGUILayout.TextField("Fallback unit ID", tier.defaultAndroidAdUnitId);

            tier.enableTierMemoryCooldown = EditorGUILayout.Toggle("Tier memory + cooldown", tier.enableTierMemoryCooldown);
            tier.premiumRetryCooldownMinutes = EditorGUILayout.FloatField(
                "Premium retry cooldown (min)", tier.premiumRetryCooldownMinutes);
            tier.consecutiveFailuresBeforeDowngrade = EditorGUILayout.IntField(
                "Failures before downgrade", tier.consecutiveFailuresBeforeDowngrade);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Tier units (Premium → Fill)", EditorStyles.miniBoldLabel);
            foreach (SequentialAdTier t in Enum.GetValues(typeof(SequentialAdTier)))
            {
                var entry = tier.GetEntry(t);
                if (entry == null) continue;
                EditorGUILayout.LabelField(entry.tier.ToString(), EditorStyles.miniBoldLabel);
                if (platform == BuildTargetPlatform.iOS)
                    entry.iosAdUnitId = EditorGUILayout.TextField("  Ad unit ID", entry.iosAdUnitId);
                else
                    entry.androidAdUnitId = EditorGUILayout.TextField("  Ad unit ID", entry.androidAdUnitId);
                entry.timeoutSeconds = EditorGUILayout.FloatField("  Timeout (s, 0=none)", entry.timeoutSeconds);
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
                    "Auto refresh by code",
                    setup.isAutoRefreshBannerByCode);

                DrawUnitIds(
                    setup.bannerAdsMediationType,
                    platform,
                    "Banner ad unit ID",
                    () => setup.bannerAdUnitID_MAX,
                    v => setup.bannerAdUnitID_MAX = v,
                    setup.admobAdsSetup.BannerAdUnitID);
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

        public static void DrawMrecSingle(SDKSetup setup, AdsMediationType primaryMediation, BuildTargetPlatform platform)
        {
            DrawSetupFields(setup, () =>
            {
                setup.mrecAdsMediationType = DrawMediationField(
                    "Mediation",
                    setup.mrecAdsMediationType,
                    primaryMediation);

                if (setup.mrecAdsMediationType == AdsMediationType.NONE)
                    return;

                if (setup.mrecAdsMediationType == AdsMediationType.ADMOB)
                {
                    if (!DrawEnumMember(setup, "mrecAdsPosition", "MREC position"))
                        DrawMember(setup, "mrecAdsPositionFallback", "MREC position (enum index)");
                }

                DrawUnitIds(
                    setup.mrecAdsMediationType,
                    platform,
                    "MREC ad unit ID",
                    () => setup.mrecAdUnitID_MAX,
                    v => setup.mrecAdUnitID_MAX = v,
                    setup.admobAdsSetup.MrecAdUnitID);
            });
        }

        public static void DrawCollapsibleBannerSingle(SDKSetup setup, AdsMediationType primaryMediation, BuildTargetPlatform platform)
        {
            DrawSetupFields(setup, () =>
            {
                setup.collapsibleBannerAdsMediationType = DrawMediationField(
                    "Mediation",
                    setup.collapsibleBannerAdsMediationType,
                    primaryMediation);

                if (setup.collapsibleBannerAdsMediationType == AdsMediationType.NONE)
                    return;

                if (setup.collapsibleBannerAdsMediationType == AdsMediationType.ADMOB)
                {
                    if (!DrawEnumMember(setup, "adsPositionCollapsibleBanner", "Banner position"))
                        DrawMember(setup, "adsPositionCollapsibleBannerFallback", "Banner position (enum index)");
                }

                setup.isShowingOnStartCollapsibleBanner = EditorGUILayout.Toggle(
                    "Show on start",
                    setup.isShowingOnStartCollapsibleBanner);
                setup.isAutoRefreshCollapsibleBanner = EditorGUILayout.Toggle(
                    "Auto refresh",
                    setup.isAutoRefreshCollapsibleBanner);

                if (setup.isAutoRefreshCollapsibleBanner)
                {
                    setup.isAutoRefreshExtendCollapsibleBanner = EditorGUILayout.Toggle(
                        "Extend on refresh",
                        setup.isAutoRefreshExtendCollapsibleBanner);
                    setup.autoRefreshTime = EditorGUILayout.Slider(
                        "Refresh interval (s)",
                        setup.autoRefreshTime,
                        20f,
                        60f);
                }

                setup.isAutoCloseCollapsibleBanner = EditorGUILayout.Toggle(
                    "Auto close",
                    setup.isAutoCloseCollapsibleBanner);

                if (setup.isAutoCloseCollapsibleBanner)
                {
                    setup.autoCloseTime = EditorGUILayout.Slider(
                        "Auto close delay (s)",
                        setup.autoCloseTime,
                        20f,
                        60f);
                }

                DrawUnitIds(
                    setup.collapsibleBannerAdsMediationType,
                    platform,
                    "Collapsible banner ad unit ID",
                    () => setup.collapsibleBannerAdUnitID_MAX,
                    v => setup.collapsibleBannerAdUnitID_MAX = v,
                    setup.admobAdsSetup.CollapsibleBannerAdUnitID);
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

        public static void DrawTierUnit(TierUnit unit, TieredAdsConfig owner, string label)
        {
            if (unit == null || owner == null) return;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            unit.High = EditorGUILayout.TextField("High tier", unit.High ?? string.Empty);
            unit.Mid = EditorGUILayout.TextField("Mid tier", unit.Mid ?? string.Empty);
            unit.Low = EditorGUILayout.TextField("Low tier", unit.Low ?? string.Empty);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(owner);
        }

        public static void DrawTierScheduler(TieredAdsConfig tiered)
        {
            if (tiered == null) return;

            var so = new SerializedObject(tiered);
            so.Update();

            DrawProperty(so, "EnableDynamicPromotion");
            DrawProperty(so, "PreferLastSuccessfulTier");
            DrawProperty(so, "DelayBetweenLoads");
            DrawProperty(so, "MaxParallelLoads");
            DrawProperty(so, "TierDisableDuration");
            DrawProperty(so, "PromotionLockDuration");
            DrawProperty(so, "RollingWindowSize");

            so.ApplyModifiedProperties();
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
            AdScheduleUnitID adMobSchedule)
        {
            switch (mediation)
            {
                case AdsMediationType.MAX:
                    setMaxId(EditorGUILayout.TextField(maxLabel, getMaxId() ?? string.Empty));
                    break;

                case AdsMediationType.ADMOB:
                    DrawAdMobUnitIdList(platform, adMobSchedule);
                    break;

                case AdsMediationType.NONE:
                    break;
            }
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
