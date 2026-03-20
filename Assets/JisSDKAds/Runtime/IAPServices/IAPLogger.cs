using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace SDK.IAP
{
    public class IAPLogger
    {
        public void LogFetchedProducts(List<Product> products)
        {
            if (products.Count > 0)
            {
                foreach (var product in products)
                {
                    LogConsole($"Fetched {product.definition.id}");
                }
            }
            else
            {
                LogConsole("No Products Fetched.");
            }
        }

        public void LogConfirmedOrder(Product product, IOrderInfo orderInfo)
        {
            LogConsole("===========");
            LogConsole($"Confirmed Product: '{product.definition.id}'");
            LogConsole($"Product transaction id: {orderInfo.TransactionID}.");
            LogConsole($"Product receipt length: {orderInfo.Receipt?.Length}.");
            LogConsole($"Product Type: '{product.definition.type}'");
        }

        public void LogReceiptValidation(IPurchaseReceipt productReceipt)
        {
            LogConsole(
                $"Product ID: '{productReceipt.productID}', Date: '{productReceipt.purchaseDate}', Transaction ID: '{productReceipt.transactionID}'");
            LogGooglePlayReceiptValidationInfo(productReceipt);
            LogAppleReceiptValidationInfo(productReceipt);
        }

        public void LogGooglePlayReceiptValidationInfo(IPurchaseReceipt productReceipt)
        {
            if (productReceipt is GooglePlayReceipt googleReceipt)
            {
                LogConsole(
                    $"GooglePlay - State: '{googleReceipt.purchaseState}', Token: '{googleReceipt.purchaseToken}'");
            }
        }
        

        public void LogAppleReceiptValidationInfo(IPurchaseReceipt productReceipt)
        {
            AppleInAppPurchaseReceipt appleReceipt = productReceipt as AppleInAppPurchaseReceipt;
            if (appleReceipt != null)
            {
                LogConsole(
                    $"Apple - Original Transaction: '{appleReceipt.originalTransactionIdentifier}', Expiration Date : '{appleReceipt.subscriptionExpirationDate}', Cancellation Date : '{appleReceipt.cancellationDate}', Quantity : '{appleReceipt.quantity}'");
            }
        }

        public void LogCompletedPurchase(Product product, IOrderInfo orderInfo)
        {
            LogConsole("===========");
            LogConsole($"Purchased Product: '{product.definition.id}'");
            LogConsole($"Product transaction id: {orderInfo.TransactionID}.");
            LogConsole($"Product receipt length: {orderInfo.Receipt?.Length}.");
            LogConsole($"Product Type: '{product.definition.type}'");
            
            #if UNITY_SOLAR_ENGINE
            double paymentAmount = (double)product.metadata.localizedPrice * 0.65f;
            SolarEngineManager.Instance?.TrackPurchase(product.definition.id, paymentAmount, product.metadata.isoCurrencyCode,"success");
            #endif
        }

        public void LogFailedConfirmation(Product product, PurchaseFailureReason reason)
        {
            LogConsole("===========");
            LogConsole("Purchase Confirmation Failed");
            LogConsole($"Product: '{product.definition.storeSpecificId}'");
            LogConsole($"FailureReason: {reason.ToString()}.");
        }

        public void LogFailedPurchase(Product product, PurchaseFailureReason reason)
        {
            LogConsole("===========");
            LogConsole("PurchaseFailed");
            LogConsole($"Product: '{product.definition.storeSpecificId}'");
            LogConsole($"FailureReason: {reason.ToString()}.");
        }

        public void LogDeferredPurchase(Product product)
        {
            LogConsole("===========");
            LogConsole("PurchaseDeferred");
            LogConsole($"Product: '{product.definition.storeSpecificId}'");
        }

        public void LogConsole(string msg)
        {
            Debug.Log(msg);
        }
    }
}