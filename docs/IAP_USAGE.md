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
| Scripting define | `UNITY_IAP_ACTIVE` (bắt buộc — Hub **Enable IAP** hoặc `SDKSetup.IsActiveIAP` + Apply Settings) |

> **Lỗi `Sirenix` / `TableList` trong IAP:** package IAP ≥ **4.0.2** không còn phụ thuộc Odin runtime. Nút tạo config: Inspector **InAppPurchaser** → **Create IAP Package Configs**.

> **Lỗi `IapProductKind` / `IapPurchaseNotification` not found:** IAP và `com.jis.sdkads.common` đang ở **hai commit Git khác nhau** trong `Library/PackageCache`. Các type nằm trong **common** (`Runtime/Iap/`). Hub → **Fix revisions** → **Flush PackageCache** → Resolve. Cập nhật `common` ≥ **4.0.1** (cùng revision với IAP).
| Store config | Google Play + App Store product IDs |

---

## 2. Setup nhanh

1. **JIS SDK → Hub** → import **IAP**
2. **JIS SDK → IAP → Enable IAP** (nếu menu IAP đầy đủ chưa hiện)
3. **JIS SDK → IAP → Create/Open Packages Config** → `IAPPackageConfigs.asset`
4. Thêm sản phẩm trong Inspector (Product ID, loại, Android/iOS ID nếu khác)
5. **Window → Unity IAP → IAP Receipt Validation Obfuscator** → Obfuscate Google Play + Apple (tạo `GooglePlayTangle.cs`, `AppleTangle.cs` trong project)
6. **JIS SDK → IAP → Validate Packages Config**
7. **JIS SDK → IAP → Scene → Add InApp Purchaser Prefab**
8. Gán `IAPPackageConfigs` vào `InAppPurchaser`

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

// Buy fail có payload (khuyến nghị)
EventManager.StartListening<IapPurchaseFailure>(IapEvents.BuyFail, f =>
    Debug.Log($"{f.ProductId}: {f.Reason}"));

// iOS Ask to Buy / deferred
EventManager.StartListening<string>(IapEvents.PurchaseDeferred, productId => { });
IapIntegration.PurchaseDeferred += productId => { };
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

// Đã mua entitlement (non-consumable / remove-ads / subscription)
bool owned = InAppPurchaser.Instance.HasPurchased("remove_ads");
// Consumable (coin pack) luôn trả false — dùng game state / Init callback
```

- **Transaction đã xử lý** lưu riêng → tránh cộng coin / analytics trùng khi fetch purchase cũ.
- **Restore** vẫn apply Remove Ads nhưng **không** gửi lại Firebase / AppsFlyer / SolarEngine (`IsRestore = true`).
- **`IsStoreReady`** chỉ true sau khi fetch existing purchases xong (hoặc fetch fail + cảnh báo).
- Receipt invalid → **không** confirm order (pending giữ lại để retry).
- Khi store ready, SDK tự apply Remove Ads nếu đã có trong persistence local.

---

## 7. Tích hợp Ads / Analytics (tự động)

| Tích hợp | Điều kiện | Hành vi |
|----------|-----------|---------|
| **Remove ads** | `ProductKind == RemoveAds` | `JisAds.SetRemoveAds(true)` hoặc `AdsManager` |
| **Firebase** | `FirebaseManager` ready | Event `iap_purchase` |
| **AppsFlyer** | `UNITY_APPSFLYER` + package analytics | `TrackAppflyerPurchase` (bỏ qua restore; revenue × 0.65) |
| **SolarEngine** | `UNITY_SOLAR_ENGINE` | `trackPurchase` (bỏ qua restore; revenue × 0.65) |

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
| `GooglePlayTangle` compile error (cũ) | Package ≥ bản có `IapTangleLoader` — không cần reference trực tiếp; vẫn nên generate Tangle cho production |
| Receipt invalid | Kiểm tra Tangle / bundle id / sandbox account |
| Thiếu Tangle | **Window → Unity IAP → IAP Receipt Validation Obfuscator**; chạy **Validate Packages Config** |

---

## 10. API tóm tắt

| API | Mô tả |
|-----|--------|
| `InAppPurchaser.Instance.InitializeAsync()` | Init + chờ store ready |
| `InAppPurchaser.Instance.BuyIapProduct(...)` | Mua |
| `InAppPurchaser.Instance.RestorePurchases()` | Restore |
| `InAppPurchaser.Instance.HasPurchased(id)` | Entitlement owned (không dùng cho consumable) |
| `IapEvents.PurchaseDeferred` | Ask to Buy / deferred |
| `InAppPurchaser.Instance.FindProduct(id)` | `Product` Unity IAP |
| `IapEvents.*` | Event bus |
| `IapIntegration.PurchaseCompleted` | Hook đa subscriber (Ads, AppsFlyer, game) |

---

## 11. Sample

Xem [Packages/com.jis.sdkads.samples/Samples~/IapIntegration/README.md](../Packages/com.jis.sdkads.samples/Samples~/IapIntegration/README.md).
