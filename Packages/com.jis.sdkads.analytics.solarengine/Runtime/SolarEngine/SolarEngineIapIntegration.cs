#if UNITY_SOLAR_ENGINE && UNITY_IAP_ACTIVE
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Analytics.SolarEngine
{
    static class SolarEngineIapIntegration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => IapIntegration.PurchaseCompleted += OnPurchaseCompleted;

        static void OnPurchaseCompleted(IapPurchaseNotification notification)
        {
            var paymentAmount = (double)notification.LocalizedPrice * 0.65d;
            SolarEngineManager.Instance?.TrackPurchase(
                notification.ProductId,
                paymentAmount,
                notification.CurrencyCode,
                "success");
        }
    }
}
#endif
