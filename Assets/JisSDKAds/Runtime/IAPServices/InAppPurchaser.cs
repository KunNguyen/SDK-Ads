using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ABIMaxSDKAds.Scripts;
using ABIMaxSDKAds.Scripts.IAPServices;
using ABIMaxSDKAds.Scripts.IAPServices.Setup;
using Sirenix.OdinInspector;
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

          private static InAppPurchaser instance;
          public static InAppPurchaser Instance => instance;

          #endregion

          #region Unity Services

          private StoreController storeController;

          [field: SerializeField] public IAPPackageConfigs IapProductConfigs { get; set; }
          private IStoreService StoreService { get; set; }
          private IProductService ProductService { get; set; }
          private IPurchaseService PurchaseService { get; set; }
          private ICatalogProvider CatalogProvider { get; set; } = new CatalogProvider();
          private IapCallbacks IapCallbacks { get; set; }
          private CrossPlatformValidator CrossPlatformValidator { get; set; }
          public IAPLogger IAPLogger { get; set; } = new IAPLogger();
          
          private UnityAction<string> OnExecutePurchaseCallback { get; set; }
          private UnityAction OnBuySuccessCallback { get; set; }
          private UnityAction OnBuyFailedCallback { get; set; }
          

          #endregion

          #region Initialization

          private void Awake()
          {
               if (instance != null)
               {
                    DestroyImmediate(gameObject);
                    return;
               }

               instance = this;
               DontDestroyOnLoad(gameObject);
               CreateServices();
          }

          private void Start()
          {
               IAPLogger.LogConsole("===========Starting In-App Purchaser===========");
               Initialize();
          }
          
          public void Init(UnityAction<string> onExecutePurchaseCallback)
          {
               OnExecutePurchaseCallback = onExecutePurchaseCallback;
          }

          private void CreateServices()
          {
               IAPLogger.LogConsole("Creating IAP Services...");
               StoreService = UnityIAPServices.DefaultStore();
               ProductService = UnityIAPServices.DefaultProduct();
               PurchaseService = UnityIAPServices.DefaultPurchase();
               ConfigureServiceCallbacks();
          }

          private void ConfigureServiceCallbacks()
          {
               IapCallbacks = new IapCallbacks(this);
               ConfigureProductServiceCallbacks();
               ConfigurePurchasingServiceCallbacks();
          }

          private void ConfigureProductServiceCallbacks()
          {
               ProductService.OnProductsFetched += IapCallbacks.OnInitialProductsFetched;
               ProductService.OnProductsFetchFailed += IapCallbacks.OnInitialProductsFetchFailed;
          }

          private void ConfigurePurchasingServiceCallbacks()
          {
               PurchaseService.OnPurchasesFetched += IapCallbacks.OnExistingPurchasesFetched;
               PurchaseService.OnPurchasesFetchFailed += IapCallbacks.OnExistingPurchasesFetchFailed;
               PurchaseService.OnPurchasePending += IapCallbacks.OnPurchasePending;
               PurchaseService.OnPurchaseConfirmed += IapCallbacks.OnPurchaseConfirmed;
               PurchaseService.OnPurchaseFailed += IapCallbacks.OnPurchaseFailed;
               PurchaseService.OnPurchaseDeferred += IapCallbacks.OnOrderDeferred;
          }

          private void Initialize()
          {
               IAPLogger.LogConsole("Initializing In-App Purchaser...");
               InitCatalog();
               InitializeIapServices();
               CreateCrossPlatformValidator();
               ConnectToStore();
          }

          private void InitCatalog()
          {
               IAPLogger.LogConsole("Initializing Catalog...");
               var initialProductsToFetch = new List<ProductDefinition>();
               var storeSpecificIdsByProductId = new Dictionary<string, StoreSpecificIds>();
               if (IapProductConfigs == null)
               {
                    Debug.LogError("IapProductConfigs is null");
                    return;
               }

               if (IapProductConfigs.Packages == null || IapProductConfigs.Packages.Count == 0)
               {
                    Debug.LogError("IapProductConfigs.Packages is null or empty");
                    return;
               }

               foreach (var productConfig in IapProductConfigs.Packages)
               {
                    var productDefinition = new ProductDefinition(productConfig.ProductID, productConfig.ProductType);
                    initialProductsToFetch.Add(productDefinition);

                    if (string.IsNullOrEmpty(productConfig.AndroidProductID) &&
                        string.IsNullOrEmpty(productConfig.IOSProductID)) continue;
                    var specificID = new StoreSpecificIds()
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

          private void InitializeIapServices()
          {
               IAPLogger.LogConsole("Initializing IAP Services...");
               IAPService.Initialize(OnIAPServiceInitializeSuccess, OnIapServiceInitializeFailed);
          }

          private void CreateCrossPlatformValidator()
          {
#if !UNITY_EDITOR
            try
            {
                if (CanCrossPlatformValidate())
                {
#if !DEBUG_STOREKIT_TEST
                    CrossPlatformValidator =
 new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
#else
                CrossPlatformValidator =
 new CrossPlatformValidator(GooglePlayTangle.Data(), AppleStoreKitTestTangle.Data(), Application.identifier);
#endif
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

          #region Command Methods

          public void BuyIapProduct(string productId, UnityAction buySuccessCallback, UnityAction buyFailCallback)
          {
               OnBuySuccessCallback = buySuccessCallback;
               OnBuyFailedCallback = buyFailCallback;
               var product = FindProduct(productId);

               if (product != null)
               {
                    PurchaseService?.PurchaseProduct(product);
               }
               else
               {
                    IAPLogger.LogConsole($"The product service has no product with the ID {productId}");
               }
          }

          public void ConfirmOrder(PendingOrder pendingOrder)
          {
               PurchaseService.ConfirmPurchase(pendingOrder);
          }

          private async void ConnectToStore()
          {
               try
               {
                    Debug.Log("Connecting to store...");
                    await StoreService.Connect();
                    Debug.Log("Connected to store.");
                    FetchInitialProducts();
               }
               catch (Exception e)
               {
                    Debug.LogError($"ConnectToStore failed: {e}");
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
               PurchaseService.RestoreTransactions(OnTransactionRestored);
          }

          public void ValidatePurchaseIfPossible(IOrderInfo orderInfo)
          {
               if (CanCrossPlatformValidate())
               {
                    ValidatePurchase(orderInfo);
               }
          }

          private void ValidatePurchase(IOrderInfo orderInfo)
          {
               try
               {
                    var result = CrossPlatformValidator.Validate(orderInfo.Receipt);
                    IAPLogger.LogConsole("Validated Receipt. Contents:");
                    foreach (IPurchaseReceipt productReceipt in result)
                    {
                         IAPLogger.LogReceiptValidation(productReceipt);
                         OnPurchaseSuccess(productReceipt.productID, orderInfo, productReceipt);
                    }
               }
               catch (IAPSecurityException ex)
               {
                    IAPLogger.LogConsole("Invalid receipt, not unlocking content. " + ex);
               }
          }

          public void LoadProductDetails(List<Product> products)
          {
               Debug.Log("Start load product detail.");
               foreach (var iPack in IapProductConfigs.Packages)
               {
                    var pack = iPack;
                    var pd = products.Find(x => x.definition.id == pack.ProductID);
                    if (pd == null) continue;
                    var iPrice = pd.metadata.localizedPriceString;
                    if (string.IsNullOrEmpty(iPrice)) continue;
                    iPack.LocalizedPriceString = iPrice;
                    iPack.LocalizedPrice = pd.metadata.localizedPrice;
                    iPack.CurrencyCode = pd.metadata.isoCurrencyCode;
                    iPack.IsConnectedToStore = true;
               }

               Debug.LogWarning($"End load product detail.");
               Debug.LogWarning($"Details {IapProductConfigs.Packages.Count}");
          }

          #endregion

          #region Callbacks

          private void OnIAPServiceInitializeSuccess()
          {
          }

          private void OnIapServiceInitializeFailed(string error)
          {
               DebugAds.LogError($"IAP Service initialization failed: {error}");
          }

          private void OnTransactionRestored(bool success, string error)
          {
               if (success)
               {
                    DebugAds.Log("Purchases restored successfully.");
               }
               else
               {
                    DebugAds.LogError($"Failed to restore purchases: {error}");
               }
          }

          public void OnPurchaseConfirmed(Product product, IOrderInfo orderInfo)
          {
               IAPLogger.LogConfirmedOrder(product, orderInfo);
          }

          public void OnPurchaseSuccess(string productID, IOrderInfo orderInfo, IPurchaseReceipt productReceipt)
          {
               DebugAds.Log("Start OnPurchaseSuccess " + productID);
               string purchaseToken = "";
               if (IsGooglePlay())
               {
                    if (productReceipt is GooglePlayReceipt googlePlayReceipt)
                    {
                         purchaseToken = googlePlayReceipt.purchaseToken;
                    }
               }

               if (IsAppStore())
               {
                    if (productReceipt is AppleInAppPurchaseReceipt appleReceipt)
                    {
                         purchaseToken = appleReceipt.originalTransactionIdentifier;
                    }
               }

               OnExecutePurchaseCallback?.Invoke(productID);
               OnBuySuccessCallback?.Invoke();
               EventManager.TriggerEvent("BuyIAPSuccess");
               DebugAds.Log("End OnPurchaseSuccess " + productID);
          }

          public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
          {
               IAPLogger.LogFailedPurchase(product, reason);
               OnBuyFailedCallback?.Invoke();
               EventManager.TriggerEvent("BuyIAPFail");
               EventManager.TriggerEvent("TurnOffLoading");
          }

          #endregion

          #region Helper Methods

          private bool CanCrossPlatformValidate()
          {
               return IsGooglePlay() ||
                      Application.platform == RuntimePlatform.IPhonePlayer ||
                      Application.platform == RuntimePlatform.OSXPlayer ||
                      Application.platform == RuntimePlatform.tvOS;
          }

          PendingOrder GetPendingOrder(Product product)
          {
               var orders = PurchaseService.GetPurchases();

               foreach (var order in orders)
               {
                    if (order is PendingOrder pendingOrder &&
                        pendingOrder.CartOrdered.Items().First()?.Product.definition.storeSpecificId ==
                        product.definition.storeSpecificId)
                    {
                         return pendingOrder;
                    }
               }

               return null;
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
                      DefaultStoreHelper.GetDefaultStoreName() == UnityEngine.Purchasing.AppleAppStore.Name;
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
          [Button]
          public void CreateIAPPackageConfigs()
          {
               IapProductConfigs = IAPSetup.CreateIAPPackageConfigs();
               UnityEditor.EditorUtility.SetDirty(this);
          }
#endif
          #endregion

          private void OnDestroy()
          {
               // hủy đăng ký an toàn nếu service còn sống
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
          }
     }
}