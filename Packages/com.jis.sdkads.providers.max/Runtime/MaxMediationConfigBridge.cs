using JisSDKAds.Ads;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JisSDKAds.Providers.Max
{
    public static class MaxMediationConfigBridge
    {
        public static void ApplyFromSdkSetup(AdsManager manager, SDKSetup setup)
        {
#if UNITY_AD_MAX
            if (manager == null || setup == null) return;
            setup.EnsureMediationSetups();

            const AdsMediationType adsMediationType = AdsMediationType.MAX;
            var maxMediationController =
                manager.GetAdsMediationController(adsMediationType) as MaxMediationController
                ?? manager.GetComponentInChildren<MaxMediationController>(true);
            if (maxMediationController == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[JIS SDK] MaxMediationController not found under AdsManager.");
#endif
                return;
            }

            maxMediationController.AdsMediationType = adsMediationType;
            maxMediationController.m_MaxAdConfig ??= new MaxAdSetup();

            maxMediationController.m_MaxAdConfig.SDKKey = setup.maxAdsSetup.SDKKey;
            maxMediationController.m_MaxAdConfig.InterstitialTierConfig = setup.maxAdsSetup.InterstitialTierConfig;
            maxMediationController.m_MaxAdConfig.RewardedTierConfig = setup.maxAdsSetup.RewardedTierConfig;

            maxMediationController.m_MaxAdConfig.InterstitialAdUnitID =
                setup.interstitialAdsMediationType == adsMediationType
                    ? setup.maxAdsSetup.InterstitialAdUnitID
                    : "";

            maxMediationController.m_MaxAdConfig.RewardedAdUnitID =
                setup.rewardedAdsMediationType == adsMediationType ? setup.maxAdsSetup.RewardedAdUnitID : "";

            maxMediationController.m_MaxAdConfig.BannerAdUnitID = setup.bannerAdsMediationType == adsMediationType
                ? setup.maxAdsSetup.BannerAdUnitID
                : "";

            maxMediationController.m_BannerPosition =
                (MaxSdkBase.BannerPosition)(int)setup.maxBannerAdsPosition;

            maxMediationController.m_MaxAdConfig.AppOpenAdUnitID =
                setup.appOpenAdsMediationType == adsMediationType ? setup.maxAdsSetup.AppOpenAdUnitID : "";

#if UNITY_EDITOR
            EditorUtility.SetDirty(maxMediationController);
            DebugAds.Log("Update Max Mediation Done");
#endif
#endif
        }

        public static void ApplyInventoryModesFromRemoteConfig(
            AdsManager manager,
            SDKSetup setup,
            AdInventorySetupMode interstitialMode,
            AdInventorySetupMode rewardedMode)
        {
#if UNITY_AD_MAX
            if (manager == null || setup?.maxAdsSetup == null) return;

            var controller = manager.GetAdsMediationController(AdsMediationType.MAX) as MaxMediationController;
            if (controller?.m_MaxAdConfig == null) return;

            setup.maxAdsSetup.InterstitialTierConfig.enableSequentialLadder =
                interstitialMode == AdInventorySetupMode.Tiered;
            setup.maxAdsSetup.RewardedTierConfig.enableSequentialLadder =
                rewardedMode == AdInventorySetupMode.Tiered;

            controller.m_MaxAdConfig.InterstitialTierConfig = setup.maxAdsSetup.InterstitialTierConfig;
            controller.m_MaxAdConfig.RewardedTierConfig = setup.maxAdsSetup.RewardedTierConfig;
            controller.ResetSequentialTierLoadersAfterRemoteConfig();
            DebugAds.Log(
                $"[MAX] Remote inventory mode - interstitial: {interstitialMode}, rewarded: {rewardedMode}");
#endif
        }

        public static void ApplyTierRemoteUnitIds(
            AdsManager manager,
            IReadOnlyDictionary<AdTier, string> interstitialIds,
            IReadOnlyDictionary<AdTier, string> rewardedIds)
        {
#if UNITY_AD_MAX
            if (manager == null) return;

            var controller = manager.GetAdsMediationController(AdsMediationType.MAX) as MaxMediationController;
            if (controller?.m_MaxAdConfig == null) return;

            if (interstitialIds != null && interstitialIds.Count > 0)
            {
                SequentialTierRemoteConfigResolver.ApplyToConfig(
                    controller.m_MaxAdConfig.InterstitialTierConfig, interstitialIds);
                SequentialTierRemoteConfigResolver.LogAppliedIds(
                    SequentialTierAdFormat.Interstitial,
                    controller.m_MaxAdConfig.InterstitialTierConfig);
            }

            if (rewardedIds != null && rewardedIds.Count > 0)
            {
                SequentialTierRemoteConfigResolver.ApplyToConfig(
                    controller.m_MaxAdConfig.RewardedTierConfig, rewardedIds);
                SequentialTierRemoteConfigResolver.LogAppliedIds(
                    SequentialTierAdFormat.Rewarded,
                    controller.m_MaxAdConfig.RewardedTierConfig);
            }

            controller.ResetSequentialTierLoadersAfterRemoteConfig();
            DebugAds.Log("[MAX] Applied sequential tier unit IDs from Remote Config.");
#endif
        }
    }
}
