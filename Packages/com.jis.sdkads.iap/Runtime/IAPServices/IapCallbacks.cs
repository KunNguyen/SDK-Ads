#if UNITY_IAP_ACTIVE
using System.Collections.Generic;
using JisSDKAds.IAP;
using UnityEngine.Purchasing;

namespace JisSDKAds.IAP
{
    public class IapCallbacks
    {
        private readonly InAppPurchaser inAppPurchaser;
        
        public IapCallbacks(InAppPurchaser inAppPurchaser)
        {
            this.inAppPurchaser = inAppPurchaser;
        }
        
        public void OnInitialProductsFetched(List<Product> products)
        {
            LogSectionHeader();
            inAppPurchaser.IAPLogger.LogConsole("OnInitialProductsFetched:");
            inAppPurchaser.IAPLogger.LogFetchedProducts(products);
            inAppPurchaser.LoadProductDetails(products);
            inAppPurchaser.FetchExistingPurchases();
        }
        
        public void OnInitialProductsFetchFailed(ProductFetchFailed failure)
        {
            LogSectionHeader();
            inAppPurchaser.IAPLogger.LogConsole($"OnInitialProductsFetchFailed: {failure.FailureReason}");
        }
        
        public void OnExistingPurchasesFetched(Orders existingOrders)
        {
            LogSectionHeader();
            var confirmedCount = existingOrders?.ConfirmedOrders.Count ?? 0;
            var pendingCount = existingOrders?.PendingOrders.Count ?? 0;
            inAppPurchaser.IAPLogger.LogConsole(
                $"OnExistingPurchasesFetched: {confirmedCount} confirmed, {pendingCount} pending.");

            if (existingOrders == null) return;

            ProcessConfirmedOrders(existingOrders.ConfirmedOrders);
            ProcessPendingOrders(existingOrders.PendingOrders);
        }

        public void OnExistingPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            LogSectionHeader();
            inAppPurchaser.IAPLogger.LogConsole($"OnExistingPurchasesFetchFailed: {failure.Message}");
        }

        public void OnPurchasePending(PendingOrder order)
        {
            inAppPurchaser.ProcessPendingOrder(order);
        }

        public void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case FailedOrder failedOrder:
                    OnConfirmationFailed(failedOrder);
                    break;
                case ConfirmedOrder confirmedOrder:
                    OnPurchaseConfirmed(confirmedOrder);
                    break;
            }
        }

        public void OnPurchaseConfirmed(ConfirmedOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem.Product;
                inAppPurchaser.IAPLogger.LogConfirmedOrder(product, order.Info);
                inAppPurchaser.OnPurchaseConfirmed(product, order.Info);
            }
        }

        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            var reason = failedOrder.FailureReason;
            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                inAppPurchaser.IAPLogger.LogFailedPurchase(cartItem.Product, reason);
            }
        }

        public void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            foreach (var cartItem in deferredOrder.CartOrdered.Items())
            {
                inAppPurchaser.IAPLogger.LogDeferredPurchase(cartItem.Product);
            }
        }

        private void OnConfirmationFailed(FailedOrder failedOrder)
        {
            var reason = failedOrder.FailureReason;
            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                inAppPurchaser.IAPLogger.LogFailedConfirmation(cartItem.Product, reason);
                inAppPurchaser.OnPurchaseFailed(cartItem.Product, reason);
            }
        }

        private void ProcessConfirmedOrders(IEnumerable<ConfirmedOrder> confirmedOrders)
        {
            foreach (var order in confirmedOrders)
            {
                inAppPurchaser.ProcessRestoredOrder(order);
            }
        }

        private void ProcessPendingOrders(IEnumerable<PendingOrder> pendingOrders)
        {
            foreach (var pending in pendingOrders)
            {
                inAppPurchaser.ProcessPendingOrder(pending);
            }
        }

        private void LogSectionHeader()
        {
            inAppPurchaser.IAPLogger.LogConsole("===========");
        }
    }
}
#endif
