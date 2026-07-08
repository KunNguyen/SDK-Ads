#if UNITY_SOLAR_ENGINE && UNITY_IAP_ACTIVE
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Analytics.SolarEngineIntegration
{
    static class SolarEngineIapIntegration
    {
        /// <summary>
        /// Rough estimate of net revenue after the store's cut (Google Play / App Store
        /// standard commission is 30%, i.e. ~0.70 net; this SDK uses a slightly more
        /// conservative 0.65 to also account for tax/regional variance). This is an
        /// approximation for analytics only — actual store commission varies by program
        /// (e.g. 15% for small business tiers) and region, so treat SolarEngine revenue
        /// figures derived from this as directional, not exact.
        /// </summary>
        const double EstimatedNetRevenueShare = 0.65d;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => IapIntegration.PurchaseCompleted += OnPurchaseCompleted;

        static void OnPurchaseCompleted(IapPurchaseNotification notification)
        {
            if (notification.IsRestore)
                return;

            var paymentAmount = (double)notification.LocalizedPrice * EstimatedNetRevenueShare;
            SolarEngineManager.Instance?.TrackPurchase(
                notification.ProductId,
                paymentAmount,
                notification.CurrencyCode,
                "success");
        }
    }
}
#endif
