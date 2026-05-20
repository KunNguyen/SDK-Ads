# IAP Editor Setup — JIS SDK

> In-App Purchase setup tách riêng khỏi Ads trong Menu Bar.

---

## Menu

```
JIS SDK/IAP/
├── Create Packages Config
└── Scene/Add InApp Purchaser Prefab

GameObject/JIS SDK/IAP/Add InApp Purchaser
```

**Yêu cầu:** Import module **IAP** từ **JIS SDK → Hub** (bật scripting define `UNITY_IAP_ACTIVE`).

---

## Assets

| Asset | Path mặc định |
|-------|----------------|
| `IAPPackageConfigs` | `Assets/JisSDKAds/Settings/IAP/IAPPackageConfigs.asset` |

Tạo qua menu **JIS SDK → IAP → Create Packages Config** hoặc **Assets → Create → JIS SDK/IAP/Packages Config**.

---

## Scene setup

1. **JIS SDK → IAP → Scene → Add InApp Purchaser Prefab** (tự tạo nếu chưa có prefab)
2. Gán `IAPPackageConfigs` trên component `InAppPurchaser`
3. (Optional) Link remove-ads với `JisAds.SetRemoveAds()` sau khi mua IAP thành công

---

## Liên kết với Ads

- `SDKSetup.IsActiveIAP` vẫn bật define `UNITY_IAP_ACTIVE` khi Setup Ads (scripting symbols)
- Menu IAP **không** nằm dưới `JIS SDK/Ads/` — cấu hình IAP độc lập

---

## File tham chiếu

| File | Vai trò |
|------|---------|
| `Editor/Settings/JisSDKIapSettingsMenu.cs` | Menu IAP |
| `Editor/JisSDKMenuPaths.cs` | Đường dẫn menu |
| `iap/Runtime/IAPServices/IAPPackageConfigs.cs` | Config asset |
| `iap/Runtime/IAPServices/InAppPurchaser.cs` | Runtime IAP |
