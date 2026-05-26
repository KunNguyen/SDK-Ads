#if UNITY_APPSFLYER && UNITY_IAP_ACTIVE
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Analytics.AppsFlyer
{
    static class IapAppsflyerIntegration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            IapIntegration.PurchaseCompleted += OnPurchaseCompleted;
        }

        static void OnPurchaseCompleted(IapPurchaseNotification notification)
        {
            if (notification.IsRestore || notification.LocalizedPrice <= 0)
                return;

            AppsflyerTracking.TrackAppflyerPurchase(
                notification.ProductId,
                notification.LocalizedPrice,
                notification.CurrencyCode);
        }
    }
}
#endif
