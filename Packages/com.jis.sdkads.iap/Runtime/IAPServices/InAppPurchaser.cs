#if UNITY_IAP_ACTIVE
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using JisSDKAds.Common;
using JisSDKAds.IAP.Setup;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace JisSDKAds.IAP
{
    public class InAppPurchaser : MonoBehaviour
    {
        public enum IapInitializationMode
        {
            AutoOnStart = 0,
            Manual = 1
        }

        #region Singleton

        static InAppPurchaser instance;
        public static InAppPurchaser Instance => instance;

        #endregion

        #region State

        [field: SerializeField] public IAPPackageConfigs IapProductConfigs { get; set; }
        [field: SerializeField] public IapInitializationMode InitializationMode { get; set; } = IapInitializationMode.AutoOnStart;

        [SerializeField] bool areServicesCreated;
        [SerializeField] bool isStoreReady;

        /// <summary>Unity IAP services created (Awake).</summary>
        public bool AreServicesCreated => areServicesCreated;

        /// <summary>Store connected and catalog products fetched.</summary>
        public bool IsStoreReady => isStoreReady;

        /// <summary>Alias for <see cref="IsStoreReady"/>.</summary>
        public bool IsInitialized => IsStoreReady;

        public event Action<bool> OnStoreReady;

        IStoreService StoreService { get; set; }
        IProductService ProductService { get; set; }
        IPurchaseService PurchaseService { get; set; }
        ICatalogProvider CatalogProvider { get; set; } = new CatalogProvider();
        IapCallbacks IapCallbacks { get; set; }
        CrossPlatformValidator CrossPlatformValidator { get; set; }
        public IAPLogger IAPLogger { get; set; } = new IAPLogger();

        UnityAction<string> OnExecutePurchaseCallback { get; set; }
        UnityAction OnBuySuccessCallback { get; set; }
        UnityAction OnBuyFailedCallback { get; set; }
        bool isInitializing;
        bool _awaitingEntitlementsReplay;
        TaskCompletionSource<bool> _readyTcs;
        string _lastInitError;

        #endregion

        #region Initialization

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateServices();
            areServicesCreated = true;
        }

        void Start()
        {
            if (InitializationMode == IapInitializationMode.AutoOnStart)
            {
                IAPLogger.LogConsole("===========Starting In-App Purchaser===========");
                _ = InitializeAsync();
            }
        }

        public void Init(UnityAction<string> onExecutePurchaseCallback)
        {
            OnExecutePurchaseCallback = onExecutePurchaseCallback;
        }

        void CreateServices()
        {
            IAPLogger.LogConsole("Creating IAP Services...");
            StoreService = UnityIAPServices.DefaultStore();
            ProductService = UnityIAPServices.DefaultProduct();
            PurchaseService = UnityIAPServices.DefaultPurchase();
            ConfigureServiceCallbacks();
        }

        void ConfigureServiceCallbacks()
        {
            IapCallbacks = new IapCallbacks(this);
            ProductService.OnProductsFetched += IapCallbacks.OnInitialProductsFetched;
            ProductService.OnProductsFetchFailed += IapCallbacks.OnInitialProductsFetchFailed;
            PurchaseService.OnPurchasesFetched += IapCallbacks.OnExistingPurchasesFetched;
            PurchaseService.OnPurchasesFetchFailed += IapCallbacks.OnExistingPurchasesFetchFailed;
            PurchaseService.OnPurchasePending += IapCallbacks.OnPurchasePending;
            PurchaseService.OnPurchaseConfirmed += IapCallbacks.OnPurchaseConfirmed;
            PurchaseService.OnPurchaseFailed += IapCallbacks.OnPurchaseFailed;
            PurchaseService.OnPurchaseDeferred += IapCallbacks.OnOrderDeferred;
        }

        public async Task InitializeAsync()
        {
            if (IsStoreReady)
                return;

            if (isInitializing && _readyTcs != null)
            {
                await _readyTcs.Task;
                return;
            }

            isInitializing = true;
            _lastInitError = null;
            _readyTcs = new TaskCompletionSource<bool>();

            if (IapProductConfigs == null)
            {
                FailReady("IapProductConfigs is not assigned.");
                await _readyTcs.Task;
                return;
            }

            if (!IapProductConfigs.Validate(out var validationErrors))
            {
                foreach (var err in validationErrors)
                    Debug.LogError($"[IAP] Config: {err}");
                FailReady("IAPPackageConfigs validation failed.");
                await _readyTcs.Task;
                return;
            }

            IAPLogger.LogConsole("Initializing In-App Purchaser...");
            InitCatalog();

            if (!await IAPService.TryInitializeAsync())
            {
                FailReady("Unity Gaming Services initialization failed.");
                await _readyTcs.Task;
                return;
            }

            CreateCrossPlatformValidator();
            await ConnectToStoreAsync();
            await _readyTcs.Task;
        }

        async Task ConnectToStoreAsync()
        {
            try
            {
                IAPLogger.LogConsole("Connecting to store...");
                await StoreService.Connect();
                IAPLogger.LogConsole("Connected to store.");
                FetchInitialProducts();
            }
            catch (Exception e)
            {
                FailReady($"ConnectToStore failed: {e.Message}");
            }
        }

        void InitCatalog()
        {
            IAPLogger.LogConsole("Initializing Catalog...");
            var initialProductsToFetch = new List<ProductDefinition>();
            var storeSpecificIdsByProductId = new Dictionary<string, StoreSpecificIds>();

            foreach (var productConfig in IapProductConfigs.Packages)
            {
                productConfig.SyncProductKindFromUnityType();
                var productDefinition = new ProductDefinition(productConfig.ProductID, productConfig.ProductType);
                initialProductsToFetch.Add(productDefinition);

                if (string.IsNullOrEmpty(productConfig.AndroidProductID) &&
                    string.IsNullOrEmpty(productConfig.IOSProductID))
                    continue;

                var specificID = new StoreSpecificIds
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
                storeSpecificIdsByProductId.Add(productConfig.ProductID, specificID);
            }

            CatalogProvider.AddProducts(initialProductsToFetch, storeSpecificIdsByProductId);
            IAPLogger.LogConsole($"Catalog initialized with {initialProductsToFetch.Count} products.");
        }

        void CreateCrossPlatformValidator()
        {
#if !UNITY_EDITOR
            try
            {
                if (!CanCrossPlatformValidate())
                    return;

                if (!IapTangleLoader.TryGetTangleData(out var googlePlay, out var apple))
                {
                    IAPLogger.LogConsole(
                        "Receipt validation disabled: missing GooglePlayTangle / AppleTangle in the project. " +
                        "Generate them via Window > Unity IAP > IAP Receipt Validation Obfuscator.");
                    return;
                }

                CrossPlatformValidator = new CrossPlatformValidator(googlePlay, apple, Application.identifier);
            }
            catch (NotImplementedException exception)
            {
                IAPLogger.LogConsole($"Cross Platform Validator Not Implemented: {exception}");
            }
#endif
        }

        internal void CompleteStoreReady(bool success, string errorMessage = null)
        {
            isStoreReady = success;
            isInitializing = false;
            _lastInitError = errorMessage;
            _readyTcs?.TrySetResult(success);
            OnStoreReady?.Invoke(success);
            IapIntegration.NotifyStoreReady(success);
            EventManager.Trigger(IapEvents.StoreReady, success);
            if (success)
            {
                IAPLogger.LogConsole("IAP store ready.");
                ApplyLocalPersistedEntitlements();
            }
            else
                Debug.LogError($"[IAP] Store not ready: {errorMessage ?? "unknown"}");
        }

        void ApplyLocalPersistedEntitlements()
        {
            if (IapProductConfigs?.Packages == null)
                return;

            foreach (var pack in IapProductConfigs.Packages)
            {
                if (pack == null || !pack.IsRemoveAds)
                    continue;
                if (!HasPurchased(pack.ProductID))
                    continue;
                IapIntegration.RequestApplyRemoveAds();
                return;
            }
        }

        void FailReady(string message)
        {
            CompleteStoreReady(false, message);
        }

        /// <summary>Called after existing purchases fetch completes (success or failure).</summary>
        internal void FinishEntitlementsReplay(string warningMessage = null)
        {
            if (!_awaitingEntitlementsReplay)
                return;

            _awaitingEntitlementsReplay = false;
            if (!string.IsNullOrEmpty(warningMessage))
                Debug.LogWarning($"[IAP] {warningMessage}");
            CompleteStoreReady(true);
        }

        internal void BeginEntitlementsReplay()
        {
            _awaitingEntitlementsReplay = true;
        }

        #endregion

        #region Command Methods

        public void BuyIapProduct(string productId, UnityAction buySuccessCallback, UnityAction buyFailCallback)
        {
            OnBuySuccessCallback = buySuccessCallback;
            OnBuyFailedCallback = buyFailCallback;

            if (!IsStoreReady)
            {
                var msg = string.IsNullOrEmpty(_lastInitError)
                    ? "IAP store is not ready. Call InitializeAsync() and wait for success."
                    : _lastInitError;
                IAPLogger.LogConsole(msg);
                NotifyPurchaseFailed(productId, msg);
                return;
            }

            var product = FindProduct(productId);
            if (product != null)
            {
                PurchaseService?.PurchaseProduct(product);
                return;
            }

            IAPLogger.LogConsole($"No product with ID '{productId}'. Check IAPPackageConfigs and store connection.");
            NotifyPurchaseFailed(productId, "Product not found in store catalog.");
        }

        public void ConfirmOrder(PendingOrder pendingOrder)
        {
            PurchaseService.ConfirmPurchase(pendingOrder);
        }

        public void ProcessRestoredOrder(ConfirmedOrder confirmedOrder)
        {
            foreach (var cartItem in confirmedOrder.CartOrdered.Items())
            {
                var product = cartItem.Product;
                IAPLogger.LogCompletedPurchase(product, confirmedOrder.Info);
                TryFulfillOrder(product.definition.id, confirmedOrder.Info, product, isRestore: true);
            }
        }

        public void ProcessPendingOrder(PendingOrder pendingOrder)
        {
            var allSucceeded = true;
            foreach (var cartItem in pendingOrder.CartOrdered.Items())
            {
                var product = cartItem.Product;
                IAPLogger.LogCompletedPurchase(product, pendingOrder.Info);
                if (!TryFulfillOrder(product.definition.id, pendingOrder.Info, product, isRestore: false))
                    allSucceeded = false;
            }

            if (allSucceeded)
                ConfirmOrder(pendingOrder);
            else
                IAPLogger.LogConsole("Pending order not confirmed — fulfillment or receipt validation failed.");
        }

        public void FetchExistingPurchases()
        {
            PurchaseService.FetchPurchases();
        }

        public void FetchInitialProducts()
        {
            CatalogProvider.FetchProducts(ProductService.FetchProductsWithNoRetries,
                DefaultStoreHelper.GetDefaultStoreName());
        }

        public void RestorePurchases()
        {
            if (!IsStoreReady)
            {
                Debug.LogWarning("[IAP] RestorePurchases called before store is ready.");
                return;
            }

            PurchaseService.RestoreTransactions(OnTransactionRestored);
        }

        public bool ValidatePurchaseIfPossible(IOrderInfo orderInfo, bool isRestore = false)
        {
            if (!CanCrossPlatformValidate() || orderInfo == null)
                return false;
            return TryValidateAndFulfill(orderInfo, null, isRestore);
        }

        bool TryValidateAndFulfill(IOrderInfo orderInfo, Product product, bool isRestore)
        {
            if (CrossPlatformValidator == null)
            {
                if (product != null)
                    return TryFulfillOrder(product.definition.id, orderInfo, product, isRestore);
                IAPLogger.LogConsole("Cannot validate purchase: CrossPlatformValidator not configured.");
                return false;
            }

            try
            {
                var result = CrossPlatformValidator.Validate(orderInfo.Receipt);
                IAPLogger.LogConsole("Validated Receipt. Contents:");
                var allSucceeded = true;
                foreach (IPurchaseReceipt productReceipt in result)
                {
                    IAPLogger.LogReceiptValidation(productReceipt);
                    if (!TryFulfillFromReceipt(productReceipt, orderInfo, isRestore))
                        allSucceeded = false;
                }

                return allSucceeded;
            }
            catch (IAPSecurityException ex)
            {
                IAPLogger.LogConsole("Invalid receipt, not unlocking content. " + ex);
                NotifyPurchaseFailed(product?.definition?.id, "Receipt validation failed.");
                return false;
            }
        }

        bool TryFulfillFromReceipt(IPurchaseReceipt productReceipt, IOrderInfo orderInfo, bool isRestore)
        {
            var productId = productReceipt?.productID;
            if (string.IsNullOrEmpty(productId))
                return false;
            return TryFulfillOrder(productId, orderInfo, null, isRestore, productReceipt);
        }

        /// <returns>True when the transaction is safe to confirm (fulfilled or already processed).</returns>
        bool TryFulfillOrder(string productId, IOrderInfo orderInfo, Product product, bool isRestore,
            IPurchaseReceipt productReceipt = null)
        {
            var transactionId = ResolveTransactionId(orderInfo, productReceipt);
            if (IapPurchasePersistence.WasTransactionProcessed(transactionId))
            {
                IAPLogger.LogConsole($"Skipping already processed transaction: {transactionId}");
                return true;
            }

            if (CanCrossPlatformValidate() && productReceipt == null && orderInfo != null)
                return TryValidateAndFulfill(orderInfo, product, isRestore);

            FulfillPurchase(productId, orderInfo, product, productReceipt, isRestore, transactionId);
            return true;
        }

        void FulfillPurchase(string productId, IOrderInfo orderInfo, Product product, IPurchaseReceipt productReceipt,
            bool isRestore, string transactionId)
        {
            IapPurchasePersistence.MarkTransactionProcessed(transactionId);

            var pack = IapProductConfigs?.FindPackage(productId);
            var purchaseToken = ExtractPurchaseToken(productReceipt);
            var receipt = orderInfo?.Receipt ?? string.Empty;

            if (ShouldPersistEntitlement(pack))
            {
                IapPurchasePersistence.RecordPurchase(new PurchasedData(
                    productId,
                    transactionId,
                    purchaseToken,
                    receipt,
                    DateTime.UtcNow.ToString("o")));
            }

            var notification = BuildNotification(productId, pack, orderInfo, productReceipt, purchaseToken, receipt,
                isRestore);
            DispatchPurchaseSuccess(notification);
        }

        IapPurchaseNotification BuildNotification(string productId, IAPPackage pack, IOrderInfo orderInfo,
            IPurchaseReceipt productReceipt, string purchaseToken, string receipt, bool isRestore)
        {
            var kind = pack?.ProductKind ?? IapProductKind.Consumable;
            decimal price = pack?.LocalizedPrice ?? 0m;
            var currency = pack?.CurrencyCode ?? "USD";
            if (productReceipt != null && pack != null)
            {
                // keep pack metadata
            }

            return new IapPurchaseNotification
            {
                ProductId = productId,
                ProductKind = kind,
                TransactionId = ResolveTransactionId(orderInfo, productReceipt),
                Receipt = receipt,
                PurchaseToken = purchaseToken,
                LocalizedPrice = price,
                CurrencyCode = currency,
                IsRestore = isRestore
            };
        }

        static string ResolveTransactionId(IOrderInfo orderInfo, IPurchaseReceipt productReceipt)
        {
            if (productReceipt != null)
            {
                if (productReceipt is GooglePlayReceipt gp && !string.IsNullOrEmpty(gp.orderID))
                    return gp.orderID;
                if (productReceipt is AppleInAppPurchaseReceipt apple &&
                    !string.IsNullOrEmpty(apple.originalTransactionIdentifier))
                    return apple.originalTransactionIdentifier;
                if (!string.IsNullOrEmpty(productReceipt.transactionID))
                    return productReceipt.transactionID;
            }

            // TransactionID availability varies by Unity Purchasing version.
            var orderType = orderInfo?.GetType();
            var txProp = orderType?.GetProperty("TransactionID") ?? orderType?.GetProperty("TransactionId");
            if (txProp != null)
            {
                var tx = txProp.GetValue(orderInfo) as string;
                if (!string.IsNullOrEmpty(tx))
                    return tx;
            }
            if (orderInfo != null && !string.IsNullOrEmpty(orderInfo.Receipt))
                return orderInfo.Receipt.GetHashCode().ToString();
            return Guid.NewGuid().ToString();
        }

        static string ExtractPurchaseToken(IPurchaseReceipt productReceipt)
        {
            if (productReceipt is GooglePlayReceipt googlePlayReceipt)
                return googlePlayReceipt.purchaseToken ?? string.Empty;
            if (productReceipt is AppleInAppPurchaseReceipt appleReceipt)
                return appleReceipt.originalTransactionIdentifier ?? string.Empty;
            return string.Empty;
        }

        void DispatchPurchaseSuccess(IapPurchaseNotification notification)
        {
            DebugAds.Log($"IAP purchase success: {notification.ProductId} (restore={notification.IsRestore})");
            OnExecutePurchaseCallback?.Invoke(notification.ProductId);
            OnBuySuccessCallback?.Invoke();
            IapIntegration.NotifyPurchaseCompleted(notification);
            EventManager.Trigger(IapEvents.BuySuccess, notification);
            EventManager.Trigger(IapEvents.BuySuccess);
        }

        static bool ShouldPersistEntitlement(IAPPackage pack)
        {
            if (pack == null)
                return true;
            return pack.ProductKind != IapProductKind.Consumable;
        }

        void NotifyPurchaseFailed(string productId, string reason)
        {
            OnBuyFailedCallback?.Invoke();
            var failure = new IapPurchaseFailure { ProductId = productId, Reason = reason };
            IapIntegration.NotifyPurchaseFailed(productId, reason);
            EventManager.Trigger(IapEvents.BuyFail, failure);
            EventManager.Trigger(IapEvents.BuyFail);
            EventManager.Trigger(IapEvents.TurnOffLoading);
            if (!string.IsNullOrEmpty(productId))
                Debug.LogWarning($"[IAP] Purchase failed ({productId}): {reason}");
            else
                Debug.LogWarning($"[IAP] Purchase failed: {reason}");
        }

        public void LoadProductDetails(List<Product> products)
        {
            if (IapProductConfigs?.Packages == null)
                return;

            foreach (var pack in IapProductConfigs.Packages)
            {
                var pd = products.Find(x => x.definition.id == pack.ProductID);
                if (pd == null)
                    continue;
                var iPrice = pd.metadata.localizedPriceString;
                if (string.IsNullOrEmpty(iPrice))
                    continue;
                pack.LocalizedPriceString = iPrice;
                pack.LocalizedPrice = pd.metadata.localizedPrice;
                pack.CurrencyCode = pd.metadata.isoCurrencyCode;
                pack.IsConnectedToStore = true;
            }

            IAPLogger.LogConsole($"Loaded store prices for {products.Count} product(s).");
        }

        /// <summary>
        /// Local entitlement check for non-consumables, subscriptions, and remove-ads.
        /// Consumables always return false — use game state for coin packs, etc.
        /// </summary>
        public bool HasPurchased(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return false;

            var pack = IapProductConfigs?.FindPackage(productId);
            if (pack != null && pack.ProductKind == IapProductKind.Consumable)
                return false;

            return IapPurchasePersistence.IsPurchased(productId);
        }

        public void NotifyPurchaseDeferred(string productId)
        {
            IAPLogger.LogConsole($"Purchase deferred: {productId}");
            IapIntegration.NotifyPurchaseDeferred(productId);
            EventManager.Trigger(IapEvents.PurchaseDeferred, productId);
        }

        #endregion

        #region Callbacks

        void OnTransactionRestored(bool success, string error)
        {
            if (success)
                DebugAds.Log("Purchases restored successfully.");
            else
                DebugAds.LogError($"Failed to restore purchases: {error}");
        }

        public void OnPurchaseConfirmed(Product product, IOrderInfo orderInfo)
        {
            IAPLogger.LogConfirmedOrder(product, orderInfo);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            IAPLogger.LogFailedPurchase(product, reason);
            NotifyPurchaseFailed(product?.definition?.id, reason.ToString());
        }

        #endregion

        #region Helper Methods

        bool CanCrossPlatformValidate()
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

        bool IsGooglePlay()
        {
            return Application.platform == RuntimePlatform.Android &&
                   DefaultStoreHelper.GetDefaultStoreName() == GooglePlay.Name;
        }

        public ReadOnlyObservableCollection<Product> GetFetchedProducts()
        {
            return ProductService?.GetProducts();
        }

        public static bool IsReceiptAvailable(Orders existingOrders)
        {
            return existingOrders != null &&
                   (existingOrders.ConfirmedOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)) ||
                    existingOrders.PendingOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)));
        }

#if UNITY_EDITOR
        public void CreateIAPPackageConfigs()
        {
            IapProductConfigs = IAPSetup.CreateIAPPackageConfigs();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        #endregion

        void OnDestroy()
        {
            if (ProductService != null)
            {
                ProductService.OnProductsFetched -= IapCallbacks.OnInitialProductsFetched;
                ProductService.OnProductsFetchFailed -= IapCallbacks.OnInitialProductsFetchFailed;
            }

            if (PurchaseService != null)
            {
                PurchaseService.OnPurchasesFetched -= IapCallbacks.OnExistingPurchasesFetched;
                PurchaseService.OnPurchasesFetchFailed -= IapCallbacks.OnExistingPurchasesFetchFailed;
                PurchaseService.OnPurchasePending -= IapCallbacks.OnPurchasePending;
                PurchaseService.OnPurchaseConfirmed -= IapCallbacks.OnPurchaseConfirmed;
                PurchaseService.OnPurchaseFailed -= IapCallbacks.OnPurchaseFailed;
                PurchaseService.OnPurchaseDeferred -= IapCallbacks.OnOrderDeferred;
            }

            if (instance == this)
                instance = null;
        }
    }
}
#endif
