using UnityEngine.Events;

namespace JisSDKAds.Ads
{
    /// <summary>
    /// Optional AdMob rewarded-interstitial API (implemented by <see cref="AdmobMediationController"/> when AdMob package is enabled).
    /// </summary>
    public interface IAdMobRewardedInterstitialHost
    {
        void ConfigureRewardedInterstitial(AdmobAdSetup setup, string androidUnitId, string iosUnitId);
        void LoadRewardedInterstitial();
        bool IsRewardedInterstitialLoaded();
        void ShowRewardedInterstitial(UnityAction rewardCallback, UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null);
    }
}
