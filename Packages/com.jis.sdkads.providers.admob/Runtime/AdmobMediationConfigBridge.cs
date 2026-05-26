using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Common;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JisSDKAds.Providers.AdMob
{
    public static class AdmobMediationConfigBridge
    {
        public static void ApplyFromSdkSetup(AdsManager manager, SDKSetup setup)
        {
#if UNITY_AD_ADMOB
            if (manager == null || setup == null) return;

            const AdsMediationType adsMediationType = AdsMediationType.ADMOB;
            var admobMediationController =
                manager.GetAdsMediationController(adsMediationType) as AdmobMediationController;
            if (admobMediationController == null) return;

            if (setup.interstitialAdsMediationType == adsMediationType)
            {
                manager.MainAdsMediationType = adsMediationType;
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList =
                    setup.admobAdsSetup.InterstitialAdUnitIDList;
                ApplySequentialTierConfig(
                    admobMediationController.m_AdmobAdSetup.InterstitialTierConfig,
                    ResolvePlatformUnitIds(manager, setup, setup.admobAdsSetup.InterstitialAdUnitID));
            }
            else
            {
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList = new List<string>();
            }

            admobMediationController.m_AdmobAdSetup.RewardedAdUnitIDList =
                setup.rewardedAdsMediationType == adsMediationType
                    ? setup.admobAdsSetup.RewardedAdUnitIDList
                    : new List<string>();

            if (setup.rewardedAdsMediationType == adsMediationType)
            {
                ApplySequentialTierConfig(
                    admobMediationController.m_AdmobAdSetup.RewardedTierConfig,
                    ResolvePlatformUnitIds(manager, setup, setup.admobAdsSetup.RewardedAdUnitID));
            }

            admobMediationController.m_AdmobAdSetup.BannerAdUnitIDList =
                setup.bannerAdsMediationType == adsMediationType
                    ? setup.admobAdsSetup.BannerAdUnitIDList
                    : new List<string>();
            admobMediationController.IsBannerShowingOnStart = setup.isBannerShowingOnStart;
            admobMediationController.m_BannerPosition = setup.admobBannerAdsPosition;

            admobMediationController.m_AdmobAdSetup.CollapsibleBannerAdUnitIDList =
                setup.collapsibleBannerAdsMediationType == adsMediationType
                    ? setup.admobAdsSetup.CollapsibleBannerAdUnitIDList
                    : new List<string>();
            admobMediationController.IsCollapsibleBannerShowingOnStart = setup.isShowingOnStartCollapsibleBanner;
            var collapsible = manager.CollapsibleBannerAdManager;
            if (collapsible != null)
            {
                collapsible.IsAutoRefresh = setup.isAutoRefreshCollapsibleBanner;
                collapsible.AutoRefreshTime = setup.autoRefreshTime;
            }
            admobMediationController.m_CollapsibleBannerPosition = setup.adsPositionCollapsibleBanner;

            admobMediationController.m_AdmobAdSetup.MrecAdUnitIDList =
                setup.mrecAdsMediationType == adsMediationType
                    ? setup.admobAdsSetup.MrecAdUnitIDList
                    : new List<string>();
            admobMediationController.m_MRecPosition = setup.mrecAdsPosition;

            admobMediationController.m_AdmobAdSetup.AppOpenAdUnitIDList =
                setup.appOpenAdsMediationType == adsMediationType
                    ? setup.admobAdsSetup.AppOpenAdUnitIDList
                    : new List<string>();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(admobMediationController);
            DebugAds.Log("Update Admob Mediation Done");
#endif
#endif
        }

        static List<string> ResolvePlatformUnitIds(AdsManager manager, SDKSetup setup, AdScheduleUnitID schedule)
        {
            if (schedule == null) return new List<string>();
            var platform = manager != null && setup == manager.IOSSdkSetup
                ? BuildTargetPlatform.iOS
                : BuildTargetPlatform.Android;
            return schedule.GetPlatformList(platform) ?? new List<string>();
        }

        public static void ApplyInventoryModesFromRemoteConfig(
            AdsManager manager,
            SDKSetup setup,
            AdInventorySetupMode interstitialMode,
            AdInventorySetupMode rewardedMode)
        {
#if UNITY_AD_ADMOB
            if (manager == null || setup?.admobAdsSetup == null) return;

            var controller = manager.GetAdsMediationController(AdsMediationType.ADMOB) as AdmobMediationController;
            if (controller?.m_AdmobAdSetup == null) return;

            ApplyInventoryModeToRuntime(
                setup,
                controller.m_AdmobAdSetup,
                isInterstitial: true,
                interstitialMode);
            ApplyInventoryModeToRuntime(
                setup,
                controller.m_AdmobAdSetup,
                isInterstitial: false,
                rewardedMode);

            controller.ResetSequentialTierLoadersAfterRemoteConfig();
            DebugAds.Log(
                $"[AdMob] Remote inventory mode — interstitial: {interstitialMode}, rewarded: {rewardedMode}");
#endif
        }

        static void ApplyInventoryModeToRuntime(
            SDKSetup setup,
            AdmobAdSetup admob,
            bool isInterstitial,
            AdInventorySetupMode mode)
        {
            var tierConfig = isInterstitial
                ? admob.InterstitialTierConfig
                : admob.RewardedTierConfig;
            if (tierConfig == null) return;

            var tiered = mode == AdInventorySetupMode.Tiered;
            tierConfig.enableSequentialLadder = tiered;

            if (!tiered) return;

            if (isInterstitial)
                setup.interstitialAdsMediationType = AdsMediationType.ADMOB;
            else
                setup.rewardedAdsMediationType = AdsMediationType.ADMOB;
        }

        public static void ApplyTierRemoteUnitIds(
            AdsManager manager,
            IReadOnlyDictionary<AdTier, string> interstitialIds,
            IReadOnlyDictionary<AdTier, string> rewardedIds)
        {
#if UNITY_AD_ADMOB
            if (manager == null) return;

            var controller = manager.GetAdsMediationController(AdsMediationType.ADMOB) as AdmobMediationController;
            if (controller?.m_AdmobAdSetup == null) return;

            if (interstitialIds != null && interstitialIds.Count > 0)
            {
                SequentialTierRemoteConfigResolver.ApplyToConfig(
                    controller.m_AdmobAdSetup.InterstitialTierConfig, interstitialIds);
                SequentialTierRemoteConfigResolver.LogAppliedIds(
                    SequentialTierAdFormat.Interstitial,
                    controller.m_AdmobAdSetup.InterstitialTierConfig);
            }

            if (rewardedIds != null && rewardedIds.Count > 0)
            {
                SequentialTierRemoteConfigResolver.ApplyToConfig(
                    controller.m_AdmobAdSetup.RewardedTierConfig, rewardedIds);
                SequentialTierRemoteConfigResolver.LogAppliedIds(
                    SequentialTierAdFormat.Rewarded,
                    controller.m_AdmobAdSetup.RewardedTierConfig);
            }

            controller.ResetSequentialTierLoadersAfterRemoteConfig();
            DebugAds.Log("[AdMob] Applied sequential tier unit IDs from Remote Config.");
#endif
        }

        static void ApplySequentialTierConfig(SequentialTierConfig tierConfig, List<string> platformUnitIds)
        {
            if (tierConfig == null) return;
            tierConfig.EnsureDefaultTierSlots();

            if (platformUnitIds == null || platformUnitIds.Count == 0) return;

            if (string.IsNullOrWhiteSpace(tierConfig.defaultAndroidAdUnitId))
                tierConfig.defaultAndroidAdUnitId = platformUnitIds[0];
            if (string.IsNullOrWhiteSpace(tierConfig.defaultIosAdUnitId))
                tierConfig.defaultIosAdUnitId = platformUnitIds[0];

            var order = new[]
            {
                AdTier.Premium, AdTier.High, AdTier.Mid, AdTier.Low, AdTier.Fill
            };
            for (var i = 0; i < order.Length && i < platformUnitIds.Count; i++)
            {
                var entry = tierConfig.GetEntry(order[i]);
                if (entry == null || string.IsNullOrWhiteSpace(platformUnitIds[i])) continue;
                if (string.IsNullOrWhiteSpace(entry.androidAdUnitId))
                    entry.androidAdUnitId = platformUnitIds[i];
                if (string.IsNullOrWhiteSpace(entry.iosAdUnitId))
                    entry.iosAdUnitId = platformUnitIds[i];
            }
        }
    }
}
