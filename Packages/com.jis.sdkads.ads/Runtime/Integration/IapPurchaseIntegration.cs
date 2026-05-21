#if UNITY_IAP_ACTIVE
using Firebase.Analytics;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using Parameter = Firebase.Analytics.Parameter;
using UnityEngine;

namespace JisSDKAds.Ads.Integration
{
    static class IapPurchaseIntegration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            IapIntegration.PurchaseCompleted += HandlePurchaseCompleted;
            IapIntegration.StoreReady += HandleStoreReady;
            IapIntegration.ApplyRemoveAdsRequested += ApplyRemoveAds;
        }

        static void HandleStoreReady(bool success)
        {
            if (!success)
                Debug.LogWarning("[JIS SDK] IAP store failed to become ready.");
        }

        static void HandlePurchaseCompleted(IapPurchaseNotification notification)
        {
            if (notification.IsRemoveAds)
                ApplyRemoveAds();

            if (!notification.IsRestore)
                TrackFirebasePurchase(notification);
        }

        static void ApplyRemoveAds()
        {
            if (JisAds.Instance != null)
            {
                JisAds.Instance.SetRemoveAds(true);
                return;
            }

            var legacy = Object.FindFirstObjectByType<AdsManager>();
            if (legacy != null)
                legacy.SetRemoveAds(true);
            else
                Debug.LogWarning("[JIS SDK] RemoveAds IAP purchased but no JisAds/AdsManager in scene.");
        }

        static void TrackFirebasePurchase(IapPurchaseNotification notification)
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsFirebaseReady)
                return;

            var parameters = new[]
            {
                new Parameter("product_id", notification.ProductId),
                new Parameter("product_kind", notification.ProductKind.ToString()),
                new Parameter("value", (double)notification.LocalizedPrice),
                new Parameter("currency", notification.CurrencyCode ?? "USD"),
                new Parameter("is_restore", notification.IsRestore ? 1 : 0)
            };
            FirebaseManager.Instance.LogEvent("iap_purchase", parameters);
        }
    }
}
#endif
