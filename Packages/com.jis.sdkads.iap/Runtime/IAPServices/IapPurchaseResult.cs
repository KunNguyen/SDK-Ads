#if UNITY_IAP_ACTIVE
using JisSDKAds.Common;

namespace JisSDKAds.IAP
{
    public readonly struct IapPurchaseResult
    {
        public readonly string ProductId;
        public readonly string TransactionId;
        public readonly string Receipt;
        public readonly string PurchaseToken;
        public readonly string CurrencyCode;
        public readonly decimal LocalizedPrice;
        public readonly IapProductKind ProductKind;
        public readonly bool IsRestore;

        public IapPurchaseResult(
            string productId,
            string transactionId,
            string receipt,
            string purchaseToken,
            string currencyCode,
            decimal localizedPrice,
            IapProductKind productKind,
            bool isRestore)
        {
            ProductId = productId;
            TransactionId = transactionId;
            Receipt = receipt;
            PurchaseToken = purchaseToken;
            CurrencyCode = currencyCode;
            LocalizedPrice = localizedPrice;
            ProductKind = productKind;
            IsRestore = isRestore;
        }

        public static IapPurchaseResult FromNotification(IapPurchaseNotification notification) =>
            new IapPurchaseResult(
                notification.ProductId,
                notification.TransactionId,
                notification.Receipt,
                notification.PurchaseToken,
                notification.CurrencyCode,
                notification.LocalizedPrice,
                notification.ProductKind,
                notification.IsRestore);
    }
}
#endif
