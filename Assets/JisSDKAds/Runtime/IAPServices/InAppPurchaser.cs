using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ABIMaxSDKAds.Scripts;
using ABIMaxSDKAds.Scripts.IAPServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace SDK.IAP
{
    public class InAppPurchaser : MonoBehaviour
    {
        #region Singleton

        public static InAppPurchaser Instance { get; private set; }

        #endregion

        #region Fields

        [SerializeField] private List<IAPPackage> injectedCatalog = new();

        private IStoreService storeService;
        private IProductService productService;
        private IPurchaseService purchaseService;
        private ICatalogProvider catalogProvider = new CatalogProvider();
        private IapCallbacks iapCallbacks;
        private CrossPlatformValidator crossPlatformValidator;

        private readonly Dictionary<string, (UnityAction onSuccess, UnityAction onFail)> purchaseCallbacks = new();
        private readonly HashSet<string> handledTransactionIds = new();
        
        private InitState currentState = InitState.NotInitialized;

        #endregion

        #region Properties

        private IReadOnlyList<IAPPackage> IapProductConfigs => injectedCatalog;
        public bool IsReady => currentState == InitState.Ready;
        public IAPLogger IAPLogger { get; set; } = new IAPLogger();

        #endregion

        #region Events

        public static event Action<IapPurchaseResult> PurchaseSucceeded;
        public static event Action<string, PurchaseFailureReason> PurchaseFailed;
        public static event Action<string, string, double, string> OnTrackingPurchaseEvent;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!InitializeSingleton())
                return;

            CreateServices();
        }

        private void Start()
        {
            IAPLogger.LogConsole("===========Starting In-App Purchaser===========");
            IAPLogger.LogConsole($"App Identifier: {Application.identifier}");
        }

        private void OnDestroy()
        {
            UnsubscribeFromServiceEvents();
        }

        #endregion

        #region Initialization

        private bool InitializeSingleton()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
                return false;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            return true;
        }

        public void BeginInitialize()
        {
            _ = InitializeAsync();
        }

        private void CreateServices()
        {
            IAPLogger.LogConsole("Creating IAP Services...");
            storeService = UnityIAPServices.DefaultStore();
            productService = UnityIAPServices.DefaultProduct();
            purchaseService = UnityIAPServices.DefaultPurchase();
            ConfigureServiceCallbacks();
        }

        private void ConfigureServiceCallbacks()
        {
            iapCallbacks = new IapCallbacks(this);
            SubscribeToProductServiceEvents();
            SubscribeToPurchaseServiceEvents();
        }

        private void SubscribeToProductServiceEvents()
        {
            productService.OnProductsFetched += iapCallbacks.OnInitialProductsFetched;
            productService.OnProductsFetchFailed += iapCallbacks.OnInitialProductsFetchFailed;
        }

        private void SubscribeToPurchaseServiceEvents()
        {
            purchaseService.OnPurchasesFetched += iapCallbacks.OnExistingPurchasesFetched;
            purchaseService.OnPurchasesFetchFailed += iapCallbacks.OnExistingPurchasesFetchFailed;
            purchaseService.OnPurchasePending += iapCallbacks.OnPurchasePending;
            purchaseService.OnPurchaseConfirmed += iapCallbacks.OnPurchaseConfirmed;
            purchaseService.OnPurchaseFailed += iapCallbacks.OnPurchaseFailed;
            purchaseService.OnPurchaseDeferred += iapCallbacks.OnOrderDeferred;
        }

        private void UnsubscribeFromServiceEvents()
        {
            if (productService != null)
            {
                productService.OnProductsFetched -= iapCallbacks.OnInitialProductsFetched;
                productService.OnProductsFetchFailed -= iapCallbacks.OnInitialProductsFetchFailed;
            }

            if (purchaseService != null)
            {
                purchaseService.OnPurchasesFetched -= iapCallbacks.OnExistingPurchasesFetched;
                purchaseService.OnPurchasesFetchFailed -= iapCallbacks.OnExistingPurchasesFetchFailed;
                purchaseService.OnPurchasePending -= iapCallbacks.OnPurchasePending;
                purchaseService.OnPurchaseConfirmed -= iapCallbacks.OnPurchaseConfirmed;
                purchaseService.OnPurchaseFailed -= iapCallbacks.OnPurchaseFailed;
                purchaseService.OnPurchaseDeferred -= iapCallbacks.OnOrderDeferred;
            }
        }

        private async Task InitializeAsync()
        {
            if (currentState == InitState.Initializing || currentState == InitState.Ready) 
                return;

            currentState = InitState.Initializing;

            try
            {
                IAPLogger.LogConsole("Initializing In-App Purchaser...");

                if (!ValidateCatalog())
                {
                    currentState = InitState.Failed;
                    return;
                }

                InitCatalog();

                IAPLogger.LogConsole("Initializing Unity Services...");
                await IAPService.InitializeAsync();

                CreateCrossPlatformValidator();
                await ConnectToStoreAsync();

                currentState = InitState.Ready;
                IAPLogger.LogConsole("IAP Ready.");
            }
            catch (Exception e)
            {
                currentState = InitState.Failed;
                Debug.LogError($"IAP initialize failed: {e}");
            }
        }

        private bool ValidateCatalog()
        {
            if (IapProductConfigs == null || IapProductConfigs.Count == 0)
            {
                Debug.LogError("IAP catalog is empty. You must call InAppPurchaser.SetCatalog(...) before initialization.");
                return false;
            }
            return true;
        }

        private void InitCatalog()
        {
            IAPLogger.LogConsole("Initializing Catalog...");
            var initialProductsToFetch = new List<ProductDefinition>();
            var storeSpecificIdsByProductId = new Dictionary<string, StoreSpecificIds>();

            foreach (var productConfig in IapProductConfigs)
            {
                AddProductToCatalog(productConfig, initialProductsToFetch, storeSpecificIdsByProductId);
            }

            catalogProvider.AddProducts(initialProductsToFetch, storeSpecificIdsByProductId);
            IAPLogger.LogConsole($"Catalog initialized with {initialProductsToFetch.Count} products.");
        }

        private void AddProductToCatalog(
            IAPPackage productConfig, 
            List<ProductDefinition> productDefinitions, 
            Dictionary<string, StoreSpecificIds> storeSpecificIds)
        {
            var productDefinition = new ProductDefinition(productConfig.ProductID, productConfig.ProductType);
            productDefinitions.Add(productDefinition);

            if (HasStoreSpecificIds(productConfig))
            {
                var specificIds = CreateStoreSpecificIds(productConfig);
                storeSpecificIds.Add(productConfig.ProductID, specificIds);
            }
        }

        private bool HasStoreSpecificIds(IAPPackage productConfig)
        {
            return !string.IsNullOrEmpty(productConfig.AndroidProductID) ||
                   !string.IsNullOrEmpty(productConfig.IOSProductID);
        }

        private StoreSpecificIds CreateStoreSpecificIds(IAPPackage productConfig)
        {
            var specificIds = new StoreSpecificIds
            {
                {
                    !string.IsNullOrEmpty(productConfig.AndroidProductID)
                        ? productConfig.AndroidProductID
                        : productConfig.ProductID,
                    GooglePlay.Name
                },
                {
                    !string.IsNullOrEmpty(productConfig.IOSProductID)
                        ? productConfig.IOSProductID
                        : productConfig.ProductID,
                    AppleAppStore.Name
                }
            };
            return specificIds;
        }

        private void CreateCrossPlatformValidator()
        {
#if !UNITY_EDITOR
            try
            {
                if (CanCrossPlatformValidate())
                {
                    crossPlatformValidator = new CrossPlatformValidator(
                        GooglePlayTangle.Data(), 
                        AppleTangle.Data(), 
                        Application.identifier);
                }
            }
            catch (NotImplementedException exception)
            {
                IAPLogger.LogConsole("===========");
                IAPLogger.LogConsole($"Cross Platform Validator Not Implemented: {exception}");
            }
#endif
        }

        #endregion

        #region Catalog Management

        public void SetCatalog(List<IAPPackage> catalog)
        {
            injectedCatalog = catalog ?? new List<IAPPackage>();
        }

        #endregion

        #region Purchase Processing

        public void ProcessPendingOrder(PendingOrder order)
        {
            _ = ProcessPendingOrderAsync(order);
        }

        private async Task ProcessPendingOrderAsync(PendingOrder order)
        {
            try
            {
                if (order == null) 
                    return;

                IAPLogger.LogConsole($"ProcessPendingOrder called for order: {order.Info.Receipt}");
                LogOrderItems(order);

                if (IsDuplicateTransaction(order))
                {
                    ConfirmOrder(order);
                    return;
                }
                
                IAPLogger.LogConsole($"ProcessPendingOrder: {order.Info.TransactionID} {order.Info.Receipt}");

                if (!CanValidateReceipt())
                {
                    ProcessOrderWithoutValidation(order);
                    return;
                }

                await ProcessOrderWithValidation(order);
            }
            catch (Exception e)
            {
                Debug.LogError($"ProcessPendingOrder failed: {e}");
            }
        }

        private void LogOrderItems(PendingOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
                IAPLogger.LogCompletedPurchase(cartItem.Product, order.Info);
        }

        private bool IsDuplicateTransaction(Order order)
        {
            var txId = order.Info.TransactionID ?? string.Empty;
            if (string.IsNullOrEmpty(txId)) 
                return false;

            if (handledTransactionIds.Add(txId))
                return false;

            IAPLogger.LogConsole($"Duplicate pending order ignored (already handled): {txId}");
            return true;
        }

        private bool CanValidateReceipt()
        {
            return CanCrossPlatformValidate() && crossPlatformValidator != null;
        }

        private void ProcessOrderWithoutValidation(PendingOrder order)
        {
            IAPLogger.LogConsole($"ProcessOrderWithoutValidation called for order: {order.Info.Receipt}");
            foreach (var cartItem in order.CartOrdered.Items())
                OnPurchaseSuccess(cartItem.Product.definition.id, order, null, true);

            ConfirmOrder(order);
        }

        private async Task ProcessOrderWithValidation(PendingOrder order)
        {
            IAPLogger.LogConsole($"ProcessOrderWithValidation called.");
            if (!TryValidateReceipt(order.Info.Receipt, out var receipts))
            {
                IAPLogger.LogConsole("Failed to validate receipt");
                foreach (var cartItem in order.CartOrdered.Items())
                {
                    IAPLogger.LogConsole($"Failed to validate receipt for product: {cartItem.Product.definition.id}");
                    OnPurchaseFailed(cartItem.Product, PurchaseFailureReason.SignatureInvalid);
                }
                return;
            }

            IAPLogger.LogConsole($"ProcessOrderWithValidation: Receipt validated receipts: {receipts.Length}");
            foreach (var receipt in receipts)
            {
                IAPLogger.LogReceiptValidation(receipt);
                OnPurchaseSuccess(receipt.productID, order, receipt, true);
            }

            ConfirmOrder(order);
            await Task.CompletedTask;
        }

        public void ProcessRestoredOrder(Order order)
        {
            IAPLogger.LogConsole($"ProcessRestoredOrder called.");
            try
            {
                if (order == null || !CanValidateReceipt()) 
                    return;
                IAPLogger.LogConsole($"ProcessRestoredOrder: {order.Info.TransactionID} {order.Info.Receipt}");
                if (IsDuplicateTransaction(order))
                {
                    IAPLogger.LogConsole($"ProcessRestoredOrder: Duplicate transaction ignored (already handled): {order.Info.TransactionID}");
                    return;
                }

                if (!TryValidateReceipt(order.Info.Receipt, out var receipts))
                {
                    IAPLogger.LogConsole("Failed to validate receipt");
                    ValidateNonconsumableConfirmedOrder(order);
                    return;
                }
                IAPLogger.LogConsole($"ProcessRestoredOrder: Receipt validated");

                foreach (var receipt in receipts)
                {
                    IAPLogger.LogReceiptValidation(receipt);
                    OnPurchaseSuccess(receipt.productID, order, receipt, false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ProcessRestoredOrder failed: {e}");
            }
        }

        private void ValidateNonconsumableConfirmedOrder(Order order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem.Product;
                IAPLogger.LogConsole($"Validating non-consumable confirmed order for product: {product.definition.id}");
                IAPPackage package = GetPackageForProduct(product.definition.id);
                if (package == null)
                {
                    IAPLogger.LogConsole($"Package not found for product: {product.definition.id}");
                    continue;
                }

                if (package.ProductType == ProductType.NonConsumable)
                {
                    OnPurchaseSuccess(product.definition.id, order, null, false);    
                }
            }
        }

        private bool TryValidateReceipt(string receipt, out IPurchaseReceipt[] receipts)
        {
            IAPLogger.LogConsole($"TryValidateReceipt called for receipt: {receipt}");
            receipts = Array.Empty<IPurchaseReceipt>();
            try
            {
                var result = crossPlatformValidator.Validate(receipt);
                receipts = result?.ToArray() ?? Array.Empty<IPurchaseReceipt>();
                return receipts.Length > 0;
            }
            catch (IAPSecurityException ex)
            {
                IAPLogger.LogConsole("Invalid receipt, not unlocking content. " + ex.Message);
                return false;
            }
        }

        #endregion

        #region Public Commands

        public void BuyIapProduct(string productId, UnityAction buySuccessCallback, UnityAction buyFailCallback)
        {
            if (!IsReady)
            {
                IAPLogger.LogConsole($"IAP not ready. Reject purchase: {productId}");
                buyFailCallback?.Invoke();
                return;
            }

            purchaseCallbacks[productId] = (buySuccessCallback, buyFailCallback);

            var product = FindProduct(productId);
            if (product != null)
            {
                purchaseService?.PurchaseProduct(product);
            }
            else
            {
                HandleProductNotFound(productId, buyFailCallback);
            }
        }

        private void HandleProductNotFound(string productId, UnityAction buyFailCallback)
        {
            IAPLogger.LogConsole($"The product service has no product with the ID {productId}");
            purchaseCallbacks.Remove(productId);
            buyFailCallback?.Invoke();
        }

        public void ConfirmOrder(PendingOrder pendingOrder)
        {
            IAPLogger.LogConsole($"ConfirmOrder called for order: {pendingOrder.Info.Receipt}");
            purchaseService?.ConfirmPurchase(pendingOrder);
        }

        public void FetchExistingPurchases()
        {
            purchaseService?.FetchPurchases();
        }

        public void FetchInitialProducts()
        {
            catalogProvider.FetchProducts(
                productService.FetchProductsWithNoRetries,
                DefaultStoreHelper.GetDefaultStoreName());
        }

        public void RestorePurchases()
        {
            purchaseService?.RestoreTransactions(OnTransactionRestored);
        }

        #endregion

        #region Callbacks

        private async Task ConnectToStoreAsync()
        {
            Debug.Log("Connecting to store...");
            await storeService.Connect();
            Debug.Log("Connected to store.");
            FetchInitialProducts();
        }

        private void OnTransactionRestored(bool success, string error)
        {
            if (success)
            {
                IAPLogger.LogConsole("Purchases restored successfully.");
                var purchases = purchaseService.GetPurchases();
                foreach (var purchase in purchases)
                    ProcessRestoredOrder(purchase);
            }
            else
                DebugAds.LogError($"Failed to restore purchases: {error}");
        }

        public void OnPurchaseConfirmed(Product product, IOrderInfo orderInfo)
        {
            IAPLogger.LogConfirmedOrder(product, orderInfo);
        }

        public void OnPurchaseSuccess(string productID, Order order, IPurchaseReceipt productReceipt, bool isTrackingEvent)
        {
            IAPLogger.LogConsole($"Start OnPurchaseSuccess {productID}");

            var purchaseToken = ExtractPurchaseToken(productReceipt);
            var product = FindProduct(productID);
            var result = CreatePurchaseResult(productID, order, purchaseToken, product);

            PurchaseSucceeded?.Invoke(result);
            InvokeSuccessCallback(productID);

            if (order != null && isTrackingEvent)
                TrackPurchase(order);

            IAPLogger.LogConsole($"End OnPurchaseSuccess {productID}");
        }

        private string ExtractPurchaseToken(IPurchaseReceipt productReceipt)
        {
            if (productReceipt == null)
                return string.Empty;

            if (IsGooglePlay() && productReceipt is GooglePlayReceipt googlePlayReceipt)
                return googlePlayReceipt.purchaseToken;

            if (IsAppStore() && productReceipt is AppleInAppPurchaseReceipt appleReceipt)
                return appleReceipt.originalTransactionIdentifier;

            return string.Empty;
        }

        private IapPurchaseResult CreatePurchaseResult(
            string productID, 
            Order order, 
            string purchaseToken, 
            Product product)
        {
            var currency = product?.metadata?.isoCurrencyCode;
            var price = product?.metadata?.localizedPrice ?? 0m;

            return new IapPurchaseResult(
                productID,
                order?.Info?.TransactionID,
                order?.Info?.Receipt,
                purchaseToken,
                currency,
                price
            );
        }

        private void InvokeSuccessCallback(string productID)
        {
            IAPLogger.LogConsole($"InvokeSuccessCallback called for product: {productID}");
            if (purchaseCallbacks.TryGetValue(productID, out var cb))
            {
                cb.onSuccess?.Invoke();
                purchaseCallbacks.Remove(productID);
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            IAPLogger.LogFailedPurchase(product, reason);

            var productId = product?.definition?.id ?? string.Empty;
            InvokeFailCallback(productId);
            PurchaseFailed?.Invoke(productId, reason);
        }

        private void InvokeFailCallback(string productId)
        {
            if (!string.IsNullOrEmpty(productId) && purchaseCallbacks.TryGetValue(productId, out var cb))
            {
                cb.onFail?.Invoke();
                purchaseCallbacks.Remove(productId);
            }
        }

        private void TrackPurchase(Order order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                if (cartItem == null) continue;
                
                var product = cartItem.Product;
                OnTrackingPurchaseEvent?.Invoke(
                    product.definition.id,
                    product.metadata.localizedTitle,
                    (double)product.metadata.localizedPrice,
                    product.metadata.isoCurrencyCode
                );
            }
        }

        public void LoadProductDetails(List<Product> products)
        {
            IAPLogger.LogConsole("Start load product detail.");

            if (products == null || products.Count == 0)
            {
                Debug.LogWarning("LoadProductDetails: products is null/empty.");
                return;
            }

            UpdateCatalogWithProductDetails(products);

            Debug.LogWarning("End load product detail.");
            Debug.LogWarning($"Details {injectedCatalog.Count}");
        }

        private void UpdateCatalogWithProductDetails(List<Product> products)
        {
            foreach (var package in injectedCatalog)
            {
                var product = products.Find(x => x.definition.id == package.ProductID);
                if (product == null) continue;

                var priceString = product.metadata.localizedPriceString;
                if (string.IsNullOrEmpty(priceString)) continue;

                package.LocalizedPriceString = priceString;
                package.LocalizedPrice = product.metadata.localizedPrice;
                package.CurrencyCode = product.metadata.isoCurrencyCode;
                package.IsConnectedToStore = true;
            }
        }

        #endregion

        #region Helper Methods

        private IAPPackage GetPackageForProduct(string productId)
        {
            return injectedCatalog.Find(x => x.ProductID == productId);
        }
        public static bool IsReceiptAvailable(Orders existingOrders)
        {
            return existingOrders != null &&
                   (existingOrders.ConfirmedOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)) ||
                    existingOrders.PendingOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)));
        }

        private bool CanCrossPlatformValidate()
        {
            return IsGooglePlay() ||
                   Application.platform == RuntimePlatform.IPhonePlayer ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.tvOS;
        }

        public Product FindProduct(string productId)
        {
            return GetFetchedProducts()?.FirstOrDefault(product => product.definition.id == productId);
        }

        private bool IsGooglePlay()
        {
            return Application.platform == RuntimePlatform.Android &&
                   DefaultStoreHelper.GetDefaultStoreName() == GooglePlay.Name;
        }

        private bool IsAppStore()
        {
            return Application.platform == RuntimePlatform.IPhonePlayer &&
                   DefaultStoreHelper.GetDefaultStoreName() == AppleAppStore.Name;
        }

        public ReadOnlyObservableCollection<Product> GetFetchedProducts()
        {
            return productService?.GetProducts();
        }

        #endregion

        #region Nested Types

        private enum InitState
        {
            NotInitialized,
            Initializing,
            Ready,
            Failed
        }

        #endregion
    }
}