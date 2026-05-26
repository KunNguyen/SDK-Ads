namespace JisSDKAds.Common
{
    public static class IapEvents
    {
        public const string StoreReady = "IapStoreReady";
        public const string BuySuccess = "BuyIAPSuccess";
        public const string BuyFail = "BuyIAPFail";
        public const string TurnOffLoading = "TurnOffLoading";
        /// <summary>Deferred purchase (e.g. iOS Ask to Buy). Payload: product id <see cref="string"/>.</summary>
        public const string PurchaseDeferred = "IapPurchaseDeferred";
    }
}
