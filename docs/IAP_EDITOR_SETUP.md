# IAP Editor Setup — JIS SDK

> Runtime API: [IAP_USAGE.md](IAP_USAGE.md)

---

## Menu

```
JIS SDK/IAP/
├── Enable IAP
├── Create/Open Packages Config
├── Validate Packages Config
└── Scene/Add InApp Purchaser Prefab

GameObject/JIS SDK/IAP/
├── Enable IAP
└── Add InApp Purchaser
```

**Yêu cầu:** Import module **IAP** từ **JIS SDK → Hub** và scripting define `UNITY_IAP_ACTIVE`.

---

## Assets

| Asset | Path mặc định |
|-------|----------------|
| `IAPPackageConfigs` | `Assets/JisSDKAds/Settings/IAP/IAPPackageConfigs.asset` |

Tạo qua **JIS SDK → IAP → Create/Open Packages Config** hoặc **Assets → Create → JIS SDK/IAP/Packages Config**.

### Product kinds

| ProductKind | Dùng khi |
|-------------|----------|
| `Consumable` | Coin, gem, … |
| `NonConsumable` | Unlock vĩnh viễn (không phải ads) |
| `Subscription` | Sub |
| `RemoveAds` | Tắt quảng cáo — SDK tự gọi `JisAds.SetRemoveAds(true)` |

**Validate:** **JIS SDK → IAP → Validate Packages Config** trước khi build.

---

## Scene setup

1. **JIS SDK → IAP → Scene → Add InApp Purchaser Prefab**
2. Gán `IAPPackageConfigs` trên `InAppPurchaser`
3. `InitializationMode`: `Manual` nếu game init qua `InitializeAsync()` trong loading

---

## Liên kết Ads

- `SDKSetup.IsActiveIAP` → bật `UNITY_IAP_ACTIVE` khi **Apply Settings to Scene**
- Remove ads: đặt `ProductKind = RemoveAds` — **không cần** code `SetRemoveAds` thủ công
- Menu IAP tách khỏi `JIS SDK/Ads/`

---

## File tham chiếu

| File | Vai trò |
|------|---------|
| `iap/Runtime/IAPServices/InAppPurchaser.cs` | Runtime IAP |
| `iap/Runtime/IAPServices/IAPPackageConfigs.cs` | Config + validate |
| `iap/Runtime/IAPServices/IapPurchasePersistence.cs` | PlayerPrefs persistence |
| `ads/Runtime/Integration/IapPurchaseIntegration.cs` | Remove ads + Firebase |
| `analytics.appsflyer/.../IapAppsflyerIntegration.cs` | AppsFlyer revenue |
| `common/Runtime/Iap/IapIntegration.cs` | Event hooks |
| `Editor/IapConfigValidatorMenu.cs` | Validate menu |
