namespace JisSDKAds.Common
{
    /// <summary>
    /// Purchase payload passed to <see cref="IapIntegration.OnPurchaseCompleted"/> and EventManager.
    /// </summary>
    public struct IapPurchaseNotification
    {
        public string ProductId;
        public IapProductKind ProductKind;
        public string TransactionId;
        public string Receipt;
        public string PurchaseToken;
        public decimal LocalizedPrice;
        public string CurrencyCode;
        public bool IsRestore;

        public bool IsRemoveAds => ProductKind == IapProductKind.RemoveAds;
    }
}
