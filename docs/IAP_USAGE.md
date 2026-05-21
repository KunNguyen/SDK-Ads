# Hướng dẫn IAP — JIS SDK v4

Tài liệu runtime cho **Unity Purchasing 5.x** qua package `com.jis.sdkads.iap`.

**Editor:** [IAP_EDITOR_SETUP.md](IAP_EDITOR_SETUP.md)  
**Cài package:** [UPM_INSTALL.md](UPM_INSTALL.md) → Hub → module **IAP**

---

## 1. Yêu cầu

| Thành phần | Ghi chú |
|------------|---------|
| `com.jis.sdkads.iap` | Hub import IAP |
| `com.unity.purchasing` ≥ 5.0.4 | Hub thêm vào manifest |
| `com.unity.services.core` | Unity Gaming Services init |
| Scripting define | `UNITY_IAP_ACTIVE` (Hub / **Enable IAP** menu / `SDKSetup.IsActiveIAP`) |
| Store config | Google Play + App Store product IDs |

---

## 2. Setup nhanh

1. **JIS SDK → Hub** → import **IAP**
2. **JIS SDK → IAP → Enable IAP** (nếu menu IAP đầy đủ chưa hiện)
3. **JIS SDK → IAP → Create/Open Packages Config** → `IAPPackageConfigs.asset`
4. Thêm sản phẩm trong Inspector (Product ID, loại, Android/iOS ID nếu khác)
5. **JIS SDK → IAP → Validate Packages Config**
6. **JIS SDK → IAP → Scene → Add InApp Purchaser Prefab**
7. Gán `IAPPackageConfigs` vào `InAppPurchaser`

---

## 3. Cấu hình sản phẩm (`IAPPackage`)

| Field | Ý nghĩa |
|-------|---------|
| `ProductID` | ID logic trong game (dùng khi mua) |
| `ProductType` | Unity: Consumable / NonConsumable / Subscription |
| `ProductKind` | **RemoveAds**, Consumable, NonConsumable, Subscription |
| `AndroidProductID` / `IOSProductID` | ID store (để trống → dùng `ProductID`) |
| `Price` | Giá hiển thị Editor / fallback |

**Remove Ads:** đặt `ProductKind = RemoveAds` và `ProductType = NonConsumable`.  
SDK tự gọi `JisAds.SetRemoveAds(true)` sau mua thành công (không cần code thủ công).

---

## 4. Khởi tạo

### Tự động (mặc định)

`InAppPurchaser.InitializationMode = AutoOnStart` → `Start()` gọi `InitializeAsync()`.

### Thủ công (khuyến nghị với loading screen)

```csharp
using JisSDKAds.IAP;
using JisSDKAds.Common;

public class ShopBootstrap : MonoBehaviour
{
    async void Start()
    {
        await InAppPurchaser.Instance.InitializeAsync();

        if (!InAppPurchaser.Instance.IsStoreReady)
        {
            Debug.LogWarning("IAP not ready");
            return;
        }

        EventManager.StartListening<bool>(IapEvents.StoreReady, OnStoreReady);
    }

    void OnStoreReady(bool ok) { /* enable shop UI */ }
}
```

| Property / event | Ý nghĩa |
|------------------|---------|
| `AreServicesCreated` | Service IAP tạo xong (Awake) |
| `IsStoreReady` / `IsInitialized` | Store connect + catalog fetch OK |
| `OnStoreReady` | Callback bool trên component |
| `IapEvents.StoreReady` | EventManager với tham số `bool` |

---

## 5. Mua hàng

```csharp
using JisSDKAds.IAP;

public void BuyRemoveAds()
{
    InAppPurchaser.Instance.BuyIapProduct(
        "remove_ads",
        buySuccessCallback: () => Debug.Log("UI: success"),
        buyFailCallback: () => Debug.Log("UI: failed"));
}
```

**Luồng nội bộ:**

1. Kiểm tra `IsStoreReady`
2. Purchase → validate receipt (Android/iOS)
3. Idempotent theo `transactionId` (không unlock trùng)
4. Lưu `PurchasedData` vào PlayerPrefs
5. `IapIntegration.NotifyPurchaseCompleted` → Ads remove ads + Firebase event `iap_purchase`
6. `EventManager.Trigger(IapEvents.BuySuccess, notification)` (và event không tham số legacy)

### Lắng nghe sự kiện

```csharp
using JisSDKAds.Common;
using UnityEngine.Events;

// Có tham số (khuyến nghị)
EventManager.StartListening<IapPurchaseNotification>(IapEvents.BuySuccess, OnBuySuccess);

void OnBuySuccess(IapPurchaseNotification n)
{
    Debug.Log($"{n.ProductId} {n.LocalizedPrice} {n.CurrencyCode} restore={n.IsRestore}");
}

// Legacy không tham số
EventManager.StartListening(IapEvents.BuySuccess, () => { });
EventManager.StartListening(IapEvents.BuyFail, () => { });
```

### Grant thưởng tùy game (`Init` callback)

```csharp
InAppPurchaser.Instance.Init(productId =>
{
    switch (productId)
    {
        case "coin_pack_1": AddCoins(1000); break;
        case "remove_ads": break; // SDK đã xử lý ads
    }
});
```

---

## 6. Restore & persistence

```csharp
// Restore giao dịch (iOS thường cần nút Restore)
InAppPurchaser.Instance.RestorePurchases();

// Đã mua (local persistence SDK)
bool owned = InAppPurchaser.Instance.HasPurchased("remove_ads");
```

- **Transaction đã xử lý** lưu riêng → tránh cộng coin / analytics trùng khi fetch purchase cũ.
- **Restore** vẫn apply Remove Ads nhưng **không** gửi lại Firebase `iap_purchase` (`IsRestore = true`).
- Khi store ready, SDK tự apply Remove Ads nếu đã có trong persistence local.

---

## 7. Tích hợp Ads / Analytics (tự động)

| Tích hợp | Điều kiện | Hành vi |
|----------|-----------|---------|
| **Remove ads** | `ProductKind == RemoveAds` | `JisAds.SetRemoveAds(true)` hoặc `AdsManager` |
| **Firebase** | `FirebaseManager` ready | Event `iap_purchase` |
| **AppsFlyer** | `UNITY_APPSFLYER` + package analytics | `TrackAppflyerPurchase` |

Game **không bắt buộc** gọi `SetRemoveAds` nếu dùng product kind RemoveAds.

---

## 8. Hiển thị giá

```csharp
var pack = configs.Packages.Find(p => p.ProductID == "coin_pack_1");
string label = pack?.GetPrice(); // localized khi đã fetch store
```

Giá store cập nhật sau `IsStoreReady` (callback `OnInitialProductsFetched`).

---

## 9. Checklist lỗi thường gặp

| Triệu chứng | Xử lý |
|-------------|--------|
| Menu IAP thiếu mục | **Enable IAP**, chờ recompile, kiểm tra `UNITY_IAP_ACTIVE` |
| `IAP store not ready` | Gọi `await InitializeAsync()`, đợi `IsStoreReady` |
| Product not found | Validate config, đúng Product ID store, đã fetch catalog |
| Mua không remove ads | `ProductKind = RemoveAds`, scene có `JisAds`/`AdsManager` |
| AppsFlyer không track | Define `UNITY_APPSFLYER` trên **cả iOS và Android** |
| Receipt invalid | Kiểm tra Tangle / bundle id / sandbox account |

---

## 10. API tóm tắt

| API | Mô tả |
|-----|--------|
| `InAppPurchaser.Instance.InitializeAsync()` | Init + chờ store ready |
| `InAppPurchaser.Instance.BuyIapProduct(...)` | Mua |
| `InAppPurchaser.Instance.RestorePurchases()` | Restore |
| `InAppPurchaser.Instance.HasPurchased(id)` | Local owned |
| `InAppPurchaser.Instance.FindProduct(id)` | `Product` Unity IAP |
| `IapEvents.*` | Event bus |
| `IapIntegration.PurchaseCompleted` | Hook đa subscriber (Ads, AppsFlyer, game) |

---

## 11. Sample

Xem [Packages/com.jis.sdkads.samples/Samples~/IapIntegration/README.md](../Packages/com.jis.sdkads.samples/Samples~/IapIntegration/README.md).
