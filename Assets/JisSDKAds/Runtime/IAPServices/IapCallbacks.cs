using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace SDK.IAP
{
    public class IapCallbacks
    {
        private InAppPurchaser InAppPurchaser { get; set; }
        
        public IapCallbacks (InAppPurchaser inAppPurchaser)
        {
            this.InAppPurchaser = inAppPurchaser;
        }
        
        public void OnInitialProductsFetched(List<Product> products)
        {
            InAppPurchaser.IAPLogger.LogConsole("===========");
            InAppPurchaser.IAPLogger.LogConsole("OnInitialProductsFetched:");
            InAppPurchaser.IAPLogger.LogFetchedProducts(products);
            InAppPurchaser.LoadProductDetails(products);
            InAppPurchaser.FetchExistingPurchases();
        }
        public void OnInitialProductsFetchFailed(ProductFetchFailed failure)
        {
            InAppPurchaser.IAPLogger.LogConsole("===========");
            InAppPurchaser.IAPLogger.LogConsole("OnInitialProductsFetchFailed: " + failure.FailureReason);
        }
        public void OnExistingPurchasesFetched(Orders existingOrders)
        {
            InAppPurchaser.IAPLogger.LogConsole("===========");
            InAppPurchaser.IAPLogger.LogConsole("OnExistingPurchasesFetched: " + existingOrders?.ConfirmedOrders.Count + " confirmed orders found.");
            if (existingOrders == null) return;
            foreach (var order in existingOrders.ConfirmedOrders)
            {
                InAppPurchaser.Instance.ValidatePurchaseIfPossible(order.Info);
            }
        }
        public void OnExistingPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            InAppPurchaser.IAPLogger.LogConsole("===========");
            InAppPurchaser.IAPLogger.LogConsole("OnExistingPurchasesFetchFailed: " + failure.Message);
        }
        public void OnPurchasePending(PendingOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem.Product;
                InAppPurchaser.IAPLogger.LogCompletedPurchase(product, order.Info);
                InAppPurchaser.ValidatePurchaseIfPossible(order.Info);
            }

            InAppPurchaser.ConfirmOrder(order);
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

        void OnConfirmationFailed(FailedOrder failedOrder)
        {
            var reason = failedOrder.FailureReason;

            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                InAppPurchaser.IAPLogger.LogFailedConfirmation(cartItem.Product, reason);
                InAppPurchaser.OnPurchaseFailed(cartItem.Product, reason);
            }
        }

        public void OnPurchaseConfirmed(ConfirmedOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem.Product;

                InAppPurchaser.IAPLogger.LogConfirmedOrder(product, order.Info);
                InAppPurchaser.OnPurchaseConfirmed(product, order.Info);
            }
        }
        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            var reason = failedOrder.FailureReason;

            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                InAppPurchaser.IAPLogger.LogFailedPurchase(cartItem.Product, reason);
                InAppPurchaser.OnPurchaseFailed(cartItem.Product, reason);
            }
        }
        public void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            foreach (var cartItem in deferredOrder.CartOrdered.Items())
            {
                InAppPurchaser.IAPLogger.LogDeferredPurchase(cartItem.Product);
            }
        }
    }
}