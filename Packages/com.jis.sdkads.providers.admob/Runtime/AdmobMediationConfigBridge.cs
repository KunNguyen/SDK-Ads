using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Ads.InterstitialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Common;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JisSDKAds.Providers.AdMob
{
    /// <summary>
    /// Applies <see cref="SDKSetup"/> to <see cref="AdmobMediationController"/> (kept in provider assembly).
    /// </summary>
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
                ApplyInterstitialTierConfig(
                    setup.admobAdsSetup.InterstitialTierConfig,
                    setup.admobAdsSetup.InterstitialAdUnitIDList);
            }
            else
            {
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList = new List<string>();
            }

            admobMediationController.m_AdmobAdSetup.RewardedAdUnitIDList =
                setup.rewardedAdsMediationType == adsMediationType
                    ? setup.admobAdsSetup.RewardedAdUnitIDList
                    : new List<string>();

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

        static void ApplyInterstitialTierConfig(InterstitialTierConfig tierConfig, List<string> platformUnitIds)
        {
            if (tierConfig == null) return;
            tierConfig.EnsureDefaultTierSlots();

            if (platformUnitIds != null && platformUnitIds.Count > 0)
            {
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
}
