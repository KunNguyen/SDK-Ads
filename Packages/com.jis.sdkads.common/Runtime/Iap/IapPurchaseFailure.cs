namespace JisSDKAds.Common
{
    /// <summary>
    /// Purchase failure payload for <see cref="IapIntegration.PurchaseFailed"/> and EventManager.
    /// </summary>
    public struct IapPurchaseFailure
    {
        public string ProductId;
        public string Reason;

        public bool HasProductId => !string.IsNullOrEmpty(ProductId);
    }
}
