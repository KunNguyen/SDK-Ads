using UnityEngine.Events;

namespace JisSDKAds.Ads
{
    /// <summary>
    /// Public wrappers for legacy format APIs used by <see cref="JisAds"/>.
    /// </summary>
    public partial class AdsManager
    {
        public void ShowAppOpenAd() => AppOpenAdManager?.CallToShowAd();

        public void RequestAppOpenAd() => AppOpenAdManager?.RequestAd();

        public bool IsAppOpenAdLoaded() => AppOpenAdManager != null && AppOpenAdManager.IsLoaded();
    }
}
