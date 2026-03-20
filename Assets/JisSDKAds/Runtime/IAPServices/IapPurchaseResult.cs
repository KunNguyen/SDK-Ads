namespace ABIMaxSDKAds.Scripts.IAPServices
{
    public readonly struct IapPurchaseResult
    {
        public readonly string ProductId;
        public readonly string TransactionId;
        public readonly string Receipt;
        public readonly string PurchaseToken;

        public readonly string CurrencyCode;
        public readonly decimal LocalizedPrice;

        public IapPurchaseResult(
            string productId,
            string transactionId,
            string receipt,
            string purchaseToken,
            string currencyCode,
            decimal localizedPrice)
        {
            ProductId = productId;
            TransactionId = transactionId;
            Receipt = receipt;
            PurchaseToken = purchaseToken;
            CurrencyCode = currencyCode;
            LocalizedPrice = localizedPrice;
        }
    }
}
