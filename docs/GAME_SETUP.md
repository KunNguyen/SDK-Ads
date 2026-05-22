# Game project setup (JIS SDK v4)

> **Chưa cài UPM?** Làm trước: [UPM_INSTALL.md](UPM_INSTALL.md) — thêm Git package + Hub import module.  
> **Hướng dẫn chi tiết Ads + Firebase (API, RC, Analytics):** [ADS_FIREBASE_GUIDE.md](ADS_FIREBASE_GUIDE.md)

## 1. Install packages

Open **JIS SDK → Hub** and import:

1. **Firebase** (required) — Hub also adds [External Dependency Manager](https://openupm.com/packages/com.google.external-dependency-manager/) from OpenUPM if missing (`com.google.external-dependency-manager` **1.2.187**)
2. Install **Firebase** from Google (Analytics + Remote Config; add **Authentication** if using Firebase Auth — [FIREBASE_AUTH_SETUP.md](FIREBASE_AUTH_SETUP.md))
3. **Ads** (ads runtime only), then **Enable MAX** / **Enable AdMob** in Hub as needed (installs `providers.max` / `providers.admob` — define alone is not enough). AdMob CMP/UMP: cần `com.google.ads.mobile` bản có **User Messaging Platform**; nếu thiếu UMP, SDK vẫn compile và tự bỏ qua consent → init ads trực tiếp. Lỗi `GoogleMobileAds.Ump` / `FormError` trong `providers.admob` → cập nhật **providers.admob ≥ 4.0.3** + **common ≥ 4.0.3** và **Flush PackageCache**. Lỗi `CollapsibleBannerAdManager` static / `DebugAds` / `AddEventNextFrame` → cùng nguyên nhân cache cũ hoặc thiếu `JisSDKAds.Common` trong asmdef provider (đã có sẵn trong package).
4. Optional: IAP ([IAP_USAGE.md](IAP_USAGE.md)), **App Review** (Android + Hub adds Google Play Review UPM + `GOOGLE_REVIEW`), AppsFlyer, **SolarEngine** (Hub module + `UNITY_SOLAR_ENGINE`), …

## 2. Create settings

**JIS SDK → Ads → Create Settings Asset** → creates:

- `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`
- `AndroidSDKSetup.asset` / `IOSSDKSetup.asset` (linked automatically)

See [ADS_EDITOR_SETUP.md](ADS_EDITOR_SETUP.md) for the full editor workflow (platform tabs, active formats, Apply to Scene).

Per platform on `JisSDKAdsSettings`:

| Field | Example |
|-------|---------|
| Android → Mediation | AdMob |
| Android → SDK Setup | Your existing `SDKSetup` asset (formats, RC) |
| iOS → Mediation | MAX |
| iOS → SDK Setup | iOS `SDKSetup` asset |

Assign **MaxAdConfig** / **AdMobConfig** under provider configs if using Core `AdManager`.

## 3. Scene setup

1. `FirebaseManager` in scene
2. `AdsManager` prefab (required for App Open, MREC, Resume, RC rules, …)
3. GameObject + **JisAds** component
   - Assign `JisSDKAdsSettings`
   - **Use Core For Standard Formats**: on (Interstitial / Rewarded / Banner via Core)
   - Provider configs optional — auto-built from `SDKSetup` when empty

(`SdkAdsBootstrap` still works: it adds `JisAds` on the same object.)

4. **JIS SDK → Ads → Apply Settings to Scene** (syncs AdsManager + scripting defines for active build target)

## 4. API (Phase 4 — use `JisAds`)

```csharp
using JisSDKAds.Ads;
using JisSDKAds.Firebase;
```

```csharp
// Standard formats (Core when ready, else legacy)
JisAds.Instance.ShowInterstitial("level_end", onClosed: () => { });
JisAds.Instance.ShowRewardVideo("placement", onSuccess: () => { });
JisAds.Instance.ShowBannerAds();
JisAds.Instance.HideBannerAds();

// Legacy-only formats
JisAds.Instance.ShowAppOpenAd();
JisAds.Instance.ShowMRecAds();
JisAds.Instance.Legacy  // full AdsManager access
JisAds.Instance.Core    // Core AdManager when enabled
```

`AdsManager.Instance` is obsolete — migrate to `JisAds.Instance`.

See [NAMESPACES.md](NAMESPACES.md).

### `using JisSDKAds.Common` không hiện / không resolve

1. **Đúng namespace:** `using JisSDKAds.Common;` — **không** có `JisSDKAds.Scripts`.
2. **Script game có `.asmdef`:** mở asmdef của folder script (vd. `HoaEm`, `_Game`) → **Assembly Definition References** thêm:
   - `JisSDKAds.Common` (cho `EventManager`, `DebugAds`, `Keys`)
   - `JisSDKAds.Ads` / `JisSDKAds.Firebase` nếu dùng API tương ứng
3. **Không dùng asmdef:** script nằm `Assets/` gốc (Assembly-CSharp) — `Common` tự reference khi package compile OK.
4. Package **Common** lỗi compile → IDE chỉ thấy vài namespace (Core/Firebase). Console Unity xem lỗi `JisSDKAds.Common`; Hub → **Flush PackageCache** → Resolve. Cập nhật `common` ≥ **4.0.2** (bỏ phụ thuộc Sirenix thừa).

### Lỗi `Sirenix` / `Odin` trong `com.tw.utility`, `com.tw.gui`, `com.tw.ugui`, …

Các package **TW** dùng Odin Inspector (attributes + editor drawers). SDK cung cấp Odin qua **`com.jis.sdkads.odin`** (≥ **5.0.0**, tự cài khi có `core`). Runtime **`com.jis.sdkads.ads`** không phụ thuộc Odin.

1. **Manifest** phải có `com.jis.sdkads.core` (thường qua Ads / Hub import Firebase+Ads).
2. **Xóa** `Assets/Plugins/Sirenix` nếu còn bản cũ — trùng DLL trong package → Unity không load assembly.
3. Hub → **Fix com.jis.sdkads.\* revisions** → **Flush PackageCache** → Package Manager → **Resolve**.
4. Hub hiển thị cảnh báo **Odin assemblies missing** nếu vẫn lỗi — cập nhật core hoặc cài Odin từ Asset Store.
5. **Odin Module Manager:** tắt module **Unity Addressables** nếu không dùng (xem [MIGRATION_GUID_CONFLICT.md](MIGRATION_GUID_CONFLICT.md)).
6. **Package khác cũng nhúng Odin:** chỉ giữ **một** bản — xem [ODIN_CONFLICT.md](ODIN_CONFLICT.md).

Assertion `newChildren.size() == childrenArray.size()` thường là lỗi phụ khi Inspector/layout lỗi — thử sau khi compile sạch, hoặc tắt component `HorizontalAutoResizeFitter` / `VerticalAutoResizeFitter` trên object đang chọn.

## 5. One mediation per platform

`JisSDKAdsSettings.singleMediationOnly` disables MAX↔AdMob fallback in Core `AdManager`.

Legacy `AdsManager` uses the active profile’s `SDKSetup.adsMediationType` and per-format mediation fields.
