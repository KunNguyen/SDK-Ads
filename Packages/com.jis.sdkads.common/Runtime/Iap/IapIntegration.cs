using System;

namespace JisSDKAds.Common
{
    /// <summary>
    /// Hooks from IAP into Ads / Analytics without tight assembly coupling.
    /// </summary>
    public static class IapIntegration
    {
        static Action<IapPurchaseNotification> _purchaseCompleted;
        static Action<string> _purchaseFailed;
        static Action<bool> _storeReady;
        static Action _applyRemoveAdsRequested;

        public static event Action<IapPurchaseNotification> PurchaseCompleted
        {
            add => _purchaseCompleted += value;
            remove => _purchaseCompleted -= value;
        }

        public static event Action<string> PurchaseFailed
        {
            add => _purchaseFailed += value;
            remove => _purchaseFailed -= value;
        }

        public static event Action<bool> StoreReady
        {
            add => _storeReady += value;
            remove => _storeReady -= value;
        }

        public static void NotifyPurchaseCompleted(IapPurchaseNotification notification) =>
            _purchaseCompleted?.Invoke(notification);

        public static void NotifyPurchaseFailed(string reason) =>
            _purchaseFailed?.Invoke(reason);

        public static void NotifyStoreReady(bool success) =>
            _storeReady?.Invoke(success);

        /// <summary>Raised when local persistence shows a RemoveAds product without re-tracking analytics.</summary>
        public static event Action ApplyRemoveAdsRequested
        {
            add => _applyRemoveAdsRequested += value;
            remove => _applyRemoveAdsRequested -= value;
        }

        public static void RequestApplyRemoveAds() =>
            _applyRemoveAdsRequested?.Invoke();
    }
}
