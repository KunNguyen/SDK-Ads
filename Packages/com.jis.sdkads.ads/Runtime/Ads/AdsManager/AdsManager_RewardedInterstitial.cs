#if UNITY_AD_ADMOB
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads
{
    public partial class AdsManager
    {
        [Header("AdMob Rewarded Interstitial")]
        [SerializeField] private string admobRewardedInterstitialUnitIdAndroid;
        [SerializeField] private string admobRewardedInterstitialUnitIdIOS;

        private IAdMobRewardedInterstitialHost AdMobRewardedInterstitialHost =>
            GetAdsMediationController(AdsMediationType.ADMOB) as IAdMobRewardedInterstitialHost;

        private void SyncAdmobRewardedInterstitialConfig()
        {
            AdMobRewardedInterstitialHost?.ConfigureRewardedInterstitial(
                CurrentSDKSetup?.admobAdsSetup,
                admobRewardedInterstitialUnitIdAndroid,
                admobRewardedInterstitialUnitIdIOS);
        }

        public void LoadAdmobRewardedInterstitial()
        {
            SyncAdmobRewardedInterstitialConfig();
            AdMobRewardedInterstitialHost?.LoadRewardedInterstitial();
        }

        public bool IsAdmobRewardedInterstitialLoaded() =>
            AdMobRewardedInterstitialHost != null && AdMobRewardedInterstitialHost.IsRewardedInterstitialLoaded();

        public void ShowAdmobRewardedInterstitial(
            UnityAction rewardCallback,
            UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null)
        {
            if (IsRemoveAds)
            {
                failedCallback?.Invoke();
                closedCallback?.Invoke(false);
                return;
            }

            SyncAdmobRewardedInterstitialConfig();
            AdMobRewardedInterstitialHost?.ShowRewardedInterstitial(rewardCallback, closedCallback, failedCallback);
        }
    }
}
#endif
