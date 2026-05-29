#if UNITY_IAP_ACTIVE
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace JisSDKAds.IAP
{
    public class IapCallbacks
    {
        readonly InAppPurchaser inAppPurchaser;

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
            inAppPurchaser.BeginEntitlementsReplay();
            inAppPurchaser.FetchExistingPurchases();
        }

        public void OnInitialProductsFetchFailed(ProductFetchFailed failure)
        {
            LogSectionHeader();
            var reason = failure?.FailureReason.ToString() ?? "unknown";
            inAppPurchaser.IAPLogger.LogConsole($"OnInitialProductsFetchFailed: {reason}");
            if (inAppPurchaser.IsAwaitingEntitlementsReplay)
            {
                inAppPurchaser.IAPLogger.LogConsole(
                    "Ignoring product subset fetch failure — entitlements replay already in progress.");
                return;
            }

            inAppPurchaser.CompleteStoreReady(false, $"Product fetch failed: {reason}");
        }

        public void OnExistingPurchasesFetched(Orders existingOrders)
        {
            LogSectionHeader();
            var confirmedCount = existingOrders?.ConfirmedOrders.Count ?? 0;
            var pendingCount = existingOrders?.PendingOrders.Count ?? 0;
            inAppPurchaser.IAPLogger.LogConsole(
                $"OnExistingPurchasesFetched: {confirmedCount} confirmed, {pendingCount} pending.");

            if (existingOrders != null)
            {
                ProcessConfirmedOrders(existingOrders.ConfirmedOrders);
                ProcessPendingOrders(existingOrders.PendingOrders);
            }

            inAppPurchaser.FinishEntitlementsReplay();
        }

        public void OnExistingPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            LogSectionHeader();
            var message = failure?.Message ?? "unknown";
            inAppPurchaser.IAPLogger.LogConsole($"OnExistingPurchasesFetchFailed: {message}");
            inAppPurchaser.FinishEntitlementsReplay(
                $"Existing purchases could not be fetched ({message}). Shop is available; entitlements may need Restore.");
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
                var product = cartItem.Product;
                inAppPurchaser.IAPLogger.LogFailedPurchase(product, reason);
                inAppPurchaser.OnPurchaseFailed(product, reason);
            }
        }

        public void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            foreach (var cartItem in deferredOrder.CartOrdered.Items())
            {
                inAppPurchaser.IAPLogger.LogDeferredPurchase(cartItem.Product);
                inAppPurchaser.NotifyPurchaseDeferred(cartItem.Product.definition.id);
            }
        }

        void OnConfirmationFailed(FailedOrder failedOrder)
        {
            var reason = failedOrder.FailureReason;
            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                inAppPurchaser.IAPLogger.LogFailedConfirmation(cartItem.Product, reason);
                inAppPurchaser.OnPurchaseFailed(cartItem.Product, reason);
            }
        }

        void ProcessConfirmedOrders(IEnumerable<ConfirmedOrder> confirmedOrders)
        {
            foreach (var order in confirmedOrders)
                inAppPurchaser.ProcessRestoredOrder(order);
        }

        void ProcessPendingOrders(IEnumerable<PendingOrder> pendingOrders)
        {
            foreach (var pending in pendingOrders)
                inAppPurchaser.ProcessPendingOrder(pending);
        }

        void LogSectionHeader()
        {
            inAppPurchaser.IAPLogger.LogConsole("===========");
        }
    }
}
#endif
