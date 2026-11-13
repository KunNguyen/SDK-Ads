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

        /// <summary>
        /// Per-product success/fail callbacks for in-flight <see cref="BuyIapProduct"/> calls.
        /// Keyed by productId instead of a single global pair so two concurrent purchase
        /// requests (e.g. two buy buttons tapped in quick succession) can't overwrite each
        /// other's callback and notify the wrong caller.
        /// </summary>
        readonly Dictionary<string, (UnityAction onSuccess, UnityAction onFail)> _pendingPurchaseCallbacks =
            new Dictionary<string, (UnityAction onSuccess, UnityAction onFail)>();

        /// <summary>How long a buy tap waits for a not-ready / disconnected store to recover before failing.</summary>
        const float k_BuyWaitForStoreSeconds = 10f;
        /// <summary>Minimum gap between store recovery attempts triggered by the app regaining focus.</summary>
        const float k_FocusRecoveryCooldownSeconds = 30f;

        bool isInitializing;
        bool _initRequested;
        bool _catalogInitialized;
        bool _awaitingEntitlementsReplay;
        bool _storeDisconnected;
        int _disconnectCount;
        bool _hasDeferredOrders;
        // Granted but not confirmed/consumed (confirm failed or grant threw): Google keeps the SKU owned until we
        // re-fetch and confirm, so the next purchase of that pack would fail with ITEM_ALREADY_OWNED.
        bool _hasUnconfirmedOrders;
        // Set by RestorePurchases until the purchases fetch it triggers completes: already-processed
        // entitlements found by that fetch are reported to the game again (see NotifyOwnedEntitlementIfRequested).
        bool _restoreInProgress;
        readonly HashSet<string> _awaitingStore = new HashSet<string>();
        float _lastFocusRecoveryTime = float.NegativeInfinity;
        TaskCompletionSource<bool> _readyTcs;
        Task _reconnectTask;
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
            // Pending orders found by FetchPurchases are processed once, by IapCallbacks.OnExistingPurchasesFetched.
            // Letting Unity IAP process them too confirms each order twice; the second confirm comes back as a
            // FailedOrder and used to be reported to the game as a failed purchase.
            PurchaseService.ProcessPendingOrdersOnPurchasesFetched(false);
            // Keep Unity IAP's bounded default reconnection policy: the same policy drives the initial Connect(),
            // so an unbounded one would hang init forever on devices without working Play Billing.
            // Recovery after it gives up is ours (buy tap / app focus), see TryRecoverStoreAsync.
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
            StoreService.OnStoreDisconnected += OnStoreDisconnected;
        }

        /// <summary>
        /// Safe to call again after a failure: a failed init leaves <see cref="IsStoreReady"/> false and is retried
        /// on the next buy tap and when the app regains focus.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (IsStoreReady)
                return;

            _initRequested = true;

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
            if (!_catalogInitialized)
            {
                InitCatalog();
                _catalogInitialized = true;
            }

            // Unity IAP 5 talks to the store without UGS; a UGS hiccup (offline boot) must not disable the shop.
            if (!await IAPService.TryInitializeAsync())
                Debug.LogWarning("[IAP] Unity Gaming Services init failed; continuing with the store connection.");

            if (CrossPlatformValidator == null)
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
                // ProductKind is already synced by IapProductConfigs.Validate(), which
                // InitializeAsync always calls right before InitCatalog().
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
            // A late failure (e.g. a product subset fetch) must not flip a working store back to not-ready.
            if (!success && isStoreReady)
            {
                Debug.LogWarning($"[IAP] Ignoring store failure while already ready: {errorMessage ?? "unknown"}");
                return;
            }

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

        internal bool IsAwaitingEntitlementsReplay => _awaitingEntitlementsReplay;

        #endregion

        #region Command Methods

        public void BuyIapProduct(string productId, UnityAction buySuccessCallback, UnityAction buyFailCallback)
        {
            if (!string.IsNullOrEmpty(productId))
                _pendingPurchaseCallbacks[productId] = (buySuccessCallback, buyFailCallback);

            if (!IsStoreReady || _storeDisconnected || IsReconnecting)
            {
                // A second tap while this product already waits for the store only refreshes its callbacks
                // (done above); starting another wait would purchase twice once the store is back.
                if (string.IsNullOrEmpty(productId) || _awaitingStore.Add(productId))
                    _ = BuyWhenStoreAvailableAsync(productId);
                return;
            }

            PurchaseNow(productId);
        }

        bool IsReconnecting => _reconnectTask != null && !_reconnectTask.IsCompleted;

        async Task BuyWhenStoreAvailableAsync(string productId)
        {
            try
            {
                await BuyWhenStoreAvailableCoreAsync(productId);
            }
            finally
            {
                if (!string.IsNullOrEmpty(productId))
                    _awaitingStore.Remove(productId);
            }
        }

        async Task BuyWhenStoreAvailableCoreAsync(string productId)
        {
            try
            {
                if (await TryRecoverStoreAsync())
                {
                    PurchaseNow(productId);
                    return;
                }

                var msg = !IsStoreReady
                    ? (string.IsNullOrEmpty(_lastInitError) ? "IAP store is not ready (init timed out)." : _lastInitError)
                    : "Store is not connected (reconnect timed out).";
                IAPLogger.LogConsole(msg);
                NotifyPurchaseFailed(productId, msg);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                NotifyPurchaseFailed(productId, $"Store recovery failed: {e.Message}");
            }
        }

        /// <summary>
        /// Re-runs init when the store never became ready, or reconnects when it dropped after init.
        /// Waits at most <see cref="k_BuyWaitForStoreSeconds"/>; recovery keeps running in the background after that.
        /// </summary>
        async Task<bool> TryRecoverStoreAsync()
        {
            var recovery = IsStoreReady ? ReconnectAsync() : InitializeAsync();
            var finished = await Task.WhenAny(recovery, Task.Delay(TimeSpan.FromSeconds(k_BuyWaitForStoreSeconds)));
            if (finished != recovery)
                return false;
            await recovery;
            return IsStoreReady && !_storeDisconnected;
        }

        /// <summary>Single shared reconnect. <c>Connect()</c> does not throw when it gives up (it raises
        /// OnStoreDisconnected instead), and the next <c>StoreNotConnected</c> purchase failure also re-marks the store.</summary>
        Task ReconnectAsync()
        {
            if (IsReconnecting)
                return _reconnectTask;

            IAPLogger.LogConsole("Reconnecting to store...");
            _reconnectTask = ReconnectCoreAsync();
            return _reconnectTask;
        }

        async Task ReconnectCoreAsync()
        {
            var disconnectsBefore = _disconnectCount;
            await StoreService.Connect();
            // Connect() also completes when it gives up, right after raising OnStoreDisconnected.
            if (_disconnectCount != disconnectsBefore)
                return;
            // Cleared only once Connect() succeeded: a buy tap in between still waits instead of hitting
            // PurchaseService while the store is Connecting (instant StoreNotConnected).
            _storeDisconnected = false;
            if (_hasUnconfirmedOrders)
                FetchUnconfirmedOrders();
        }

        void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            _storeDisconnected = true;
            _disconnectCount++;
            Debug.LogWarning($"[IAP] Store disconnected: {failure?.message ?? "unknown"}");
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !areServicesCreated || !_initRequested)
                return;
            if (Time.realtimeSinceStartup - _lastFocusRecoveryTime < k_FocusRecoveryCooldownSeconds)
                return;

            if (!IsStoreReady)
            {
                if (isInitializing)
                    return;
                _lastFocusRecoveryTime = Time.realtimeSinceStartup;
                IAPLogger.LogConsole("App focused with store not ready; retrying init.");
                _ = InitializeAsync();
            }
            else if (_storeDisconnected)
            {
                _lastFocusRecoveryTime = Time.realtimeSinceStartup;
                _ = ReconnectAsync();
            }
            else if (_hasUnconfirmedOrders)
            {
                _lastFocusRecoveryTime = Time.realtimeSinceStartup;
                FetchUnconfirmedOrders();
            }
            else if (_hasDeferredOrders)
            {
                // Cash / parental-approval payments complete outside the app; pick them up without a restart.
                _lastFocusRecoveryTime = Time.realtimeSinceStartup;
                IAPLogger.LogConsole("App focused with deferred orders; fetching purchases.");
                FetchExistingPurchases();
            }
        }

        /// <summary>Re-fetch so unconfirmed orders come back as pending and get confirmed (already processed → no re-grant).
        /// The flag is raised again if confirmation fails once more.</summary>
        void FetchUnconfirmedOrders()
        {
            _hasUnconfirmedOrders = false;
            IAPLogger.LogConsole("Fetching purchases to confirm unconfirmed orders.");
            FetchExistingPurchases();
        }

        void PurchaseNow(string productId)
        {
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
                try
                {
                    if (!TryFulfillOrder(product.definition.id, pendingOrder.Info, product, isRestore: false))
                        allSucceeded = false;
                }
                catch (Exception e)
                {
                    // Unconfirmed + not marked processed: Google re-delivers the order on the next fetch.
                    Debug.LogException(e);
                    allSucceeded = false;
                }
            }

            if (allSucceeded)
                ConfirmOrder(pendingOrder);
            else
            {
                _hasUnconfirmedOrders = true;
                IAPLogger.LogConsole("Pending order not confirmed — fulfillment or receipt validation failed.");
            }
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

            _restoreInProgress = true;
            PurchaseService.RestoreTransactions(OnTransactionRestored);
        }

        /// <summary>Called when a purchases fetch completes (success or failure).</summary>
        internal void EndRestore()
        {
            _restoreInProgress = false;
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
                NotifyOwnedEntitlementIfRequested(productId, orderInfo, productReceipt);
                return true;
            }

            // Only enter receipt validation when a validator exists; otherwise TryValidateAndFulfill
            // would recurse back into TryFulfillOrder and freeze the main thread.
            if (CrossPlatformValidator != null && CanCrossPlatformValidate() && productReceipt == null &&
                orderInfo != null)
                return TryValidateAndFulfill(orderInfo, product, isRestore);

            return FulfillPurchase(productId, orderInfo, product, productReceipt, isRestore, transactionId);
        }

        /// <returns>False when the game's grant callback threw: the transaction is left unprocessed and unconfirmed
        /// so the store re-delivers it, instead of being confirmed with nothing granted.</returns>
        bool FulfillPurchase(string productId, IOrderInfo orderInfo, Product product, IPurchaseReceipt productReceipt,
            bool isRestore, string transactionId)
        {
            try
            {
                OnExecutePurchaseCallback?.Invoke(productId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAP] Grant callback threw for '{productId}' ({transactionId}); order left unconfirmed: {e}");
                // Only a purchase the player is waiting on is reported as failed; a replay at boot / on focus fetch
                // has nobody waiting and must not pop a failure in the UI.
                if (!isRestore && !string.IsNullOrEmpty(productId) && _pendingPurchaseCallbacks.ContainsKey(productId))
                    NotifyPurchaseFailed(productId, "Grant failed.");
                return false;
            }

            // Marked only after the grant ran, so a crash before this point re-grants on the next launch
            // rather than confirming a purchase the player never received.
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
            return true;
        }

        /// <summary>
        /// The store re-delivers the same transaction for an owned non-consumable / subscription when the player
        /// buys it again or taps Restore. It was granted once already, but the game may have lost it (save reset,
        /// cloud overwrite) and a waiting buy UI needs an answer, so report it again as a restore
        /// (<c>IsRestore = true</c>: no analytics). Boot / focus replays stay silent.
        /// </summary>
        void NotifyOwnedEntitlementIfRequested(string productId, IOrderInfo orderInfo, IPurchaseReceipt productReceipt)
        {
            if (string.IsNullOrEmpty(productId))
                return;
            var pack = IapProductConfigs?.FindPackage(productId);
            if (pack == null || pack.ProductKind == IapProductKind.Consumable)
                return;
            if (!_restoreInProgress && !_pendingPurchaseCallbacks.ContainsKey(productId))
                return;

            var purchaseToken = ExtractPurchaseToken(productReceipt);
            var receipt = orderInfo?.Receipt ?? string.Empty;
            DispatchPurchaseSuccess(BuildNotification(productId, pack, orderInfo, productReceipt, purchaseToken,
                receipt, isRestore: true));
        }

        IapPurchaseNotification BuildNotification(string productId, IAPPackage pack, IOrderInfo orderInfo,
            IPurchaseReceipt productReceipt, string purchaseToken, string receipt, bool isRestore)
        {
            var kind = pack?.ProductKind ?? IapProductKind.Consumable;
            decimal price = pack?.LocalizedPrice ?? 0m;
            var currency = pack?.CurrencyCode ?? "USD";

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
            // The grant (OnExecutePurchaseCallback) already ran in FulfillPurchase. These are UI / analytics
            // listeners: one throwing must not stop the others or the order confirmation.
            DebugAds.Log($"IAP purchase success: {notification.ProductId} (restore={notification.IsRestore})");
            var onSuccess = TakePendingCallback(notification.ProductId).onSuccess;
            SafeInvoke("buy onSuccess", () => onSuccess?.Invoke());
            SafeInvoke("PurchaseCompleted", () => IapIntegration.NotifyPurchaseCompleted(notification));
            SafeInvoke("BuySuccess", () => EventManager.Trigger(IapEvents.BuySuccess, notification));
            SafeInvoke("BuySuccess", () => EventManager.Trigger(IapEvents.BuySuccess));
            SafeInvoke("TurnOffLoading", () => EventManager.Trigger(IapEvents.TurnOffLoading));
        }

        static void SafeInvoke(string label, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAP] '{label}' listener threw: {e}");
            }
        }

        static bool ShouldPersistEntitlement(IAPPackage pack)
        {
            if (pack == null)
                return true;
            return pack.ProductKind != IapProductKind.Consumable;
        }

        void NotifyPurchaseFailed(string productId, string reason)
        {
            var onFail = TakePendingCallback(productId).onFail;
            var failure = new IapPurchaseFailure { ProductId = productId, Reason = reason };
            SafeInvoke("buy onFail", () => onFail?.Invoke());
            SafeInvoke("PurchaseFailed", () => IapIntegration.NotifyPurchaseFailed(productId, reason));
            SafeInvoke("BuyFail", () => EventManager.Trigger(IapEvents.BuyFail, failure));
            SafeInvoke("BuyFail", () => EventManager.Trigger(IapEvents.BuyFail));
            SafeInvoke("TurnOffLoading", () => EventManager.Trigger(IapEvents.TurnOffLoading));
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
            _hasDeferredOrders = true;
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
            {
                // No purchases fetch follows a failed restore.
                _restoreInProgress = false;
                DebugAds.LogError($"Failed to restore purchases: {error}");
            }
        }

        public void OnPurchaseConfirmed(Product product, IOrderInfo orderInfo)
        {
            IAPLogger.LogConfirmedOrder(product, orderInfo);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            IAPLogger.LogFailedPurchase(product, reason);
            if (reason == PurchaseFailureReason.StoreNotConnected)
                _storeDisconnected = true;
            NotifyPurchaseFailed(product?.definition?.id, reason.ToString());
        }

        /// <summary>
        /// Confirm/consume failed for an order that was already granted. Not a purchase failure for the player:
        /// the order is re-fetched as pending on the next fetch and confirmed again (already processed → no re-grant).
        /// </summary>
        public void OnConfirmationFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogWarning($"[IAP] Confirm failed for '{product?.definition?.id}' ({reason}); will retry on next fetch.");
            _hasUnconfirmedOrders = true;
            if (reason == PurchaseFailureReason.StoreNotConnected)
                _storeDisconnected = true;
        }

        internal void SetHasDeferredOrders(bool hasDeferredOrders)
        {
            _hasDeferredOrders = hasDeferredOrders;
        }

        #endregion

        #region Helper Methods

        /// <summary>Removes and returns the pending buy callback for a product, if any.</summary>
        (UnityAction onSuccess, UnityAction onFail) TakePendingCallback(string productId)
        {
            if (string.IsNullOrEmpty(productId) || !_pendingPurchaseCallbacks.TryGetValue(productId, out var callbacks))
                return (null, null);
            _pendingPurchaseCallbacks.Remove(productId);
            return callbacks;
        }

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

            if (StoreService != null)
                StoreService.OnStoreDisconnected -= OnStoreDisconnected;

            if (instance == this)
                instance = null;
        }
    }
}
#endif
