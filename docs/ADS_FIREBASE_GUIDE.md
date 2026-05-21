# Hướng dẫn sử dụng Ads + Firebase (JIS SDK v4)

Tài liệu này mô tả cách tích hợp **quảng cáo** và **Firebase** (Analytics, Remote Config, Auth tùy chọn) trong game Unity dùng JIS SDK.

**Tài liệu liên quan**

| Chủ đề | File |
|--------|------|
| Cài UPM / Hub | [UPM_INSTALL.md](UPM_INSTALL.md) |
| Cấu hình Ads trong Editor | [ADS_EDITOR_SETUP.md](ADS_EDITOR_SETUP.md) |
| Setup game tổng quát | [GAME_SETUP.md](GAME_SETUP.md) |
| API `JisAds` (kiến trúc) | [PHASE4_JISADS.md](PHASE4_JISADS.md) |
| Namespace | [NAMESPACES.md](NAMESPACES.md) |

---

## 1. Kiến trúc tổng quan

```
Scene
├── FirebaseManager          ← Firebase App, Analytics, Remote Config, Auth (tùy chọn)
├── AdsTracker               ← Gửi event ads lên Firebase Analytics
├── AdsManager (legacy)      ← App Open, MREC, Resume, RC-driven rules, cooldown
└── JisAds                   ← Entry point game code (ưu tiên dùng)
        ├── Legacy AdsManager
        └── Core AdManager (Interstitial / Rewarded / Banner, nếu bật)
```

**Luồng khởi tạo mặc định** (`JisAds.autoInitializeOnStart = true`):

1. `FirebaseManager.InitAsync()` — kiểm tra dependency, tạo `FirebaseApp`
2. (Tùy chọn) `FetchRemoteConfigAsync()` — nếu gọi `InitializeAsync(fetchRemoteConfig: true)`
3. `AdsManager.InitializeAdsFlow()` — init mediation theo `SDKSetup`
4. `AdManager` (Core) — nếu `useCoreForStandardFormats = true`

Remote Config được unit manager đọc khi init (banner refresh, interstitial capping, app open, resume ads, …).

---

## 2. Cài đặt package

### 2.1 Thứ tự qua Hub

**JIS SDK → Hub**

1. **Firebase (required)** — `com.jis.sdkads.firebase` + core + common + hub  
   Hub có thể thêm **External Dependency Manager** (OpenUPM) nếu thiếu.
2. Cài **Firebase Unity SDK từ Google** (ít nhất **Analytics** + **Remote Config**).
3. **Ads** — `com.jis.sdkads.ads`
4. Bật mediation: **Enable MAX** và/hoặc **Enable AdMob** (theo platform).
5. Tùy chọn: AppsFlyer, IAP, App Review, …

### 2.2 Scripting Define Symbols

| Define | Khi nào cần |
|--------|-------------|
| `UNITY_AD_MAX` | Dùng AppLovin MAX |
| `UNITY_AD_ADMOB` | Dùng Google AdMob |
| `UNITY_APPSFLYER` | Package `analytics.appsflyer` (cần trên **mọi platform** build, kể cả iOS) |
| `FIREBASE_AUTH` | Bật Firebase Auth trong `SDKSetup` |
| `UNITY_CRASHLYTICS` | Dùng Crashlytics (nếu cài package) |
| `Firebase_Database` | Dùng `FirebaseDataManager` (Realtime DB) |

Hub và **Apply Settings to Scene** (`SDKSetup.SetupSymbol()`) tự thêm/bớt define theo mediation và cờ trong `SDKSetup`.

> **Lưu ý iOS:** Nếu dùng AppsFlyer, thêm `UNITY_APPSFLYER` vào define của **iOS** (không chỉ Android), rồi Apply lại.

### 2.3 File Firebase của Google

- Tải `google-services.json` (Android) và `GoogleService-Info.plist` (iOS) từ Firebase Console.
- Đặt đúng theo hướng dẫn Firebase Unity (thường `Assets/`).
- Resolve dependencies: **Assets → External Dependency Manager → Android Resolver / iOS Resolver**.

---

## 3. Cấu hình Editor

### 3.1 Tạo settings

**JIS SDK → Ads → Create/Open Settings Asset**

Tạo (hoặc mở):

- `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`
- `AndroidSDKSetup.asset` / `IOSSDKSetup.asset` (gắn vào profile từng platform)

Trên `JisSDKAdsSettings`, mỗi platform (`PlatformAdsProfile`):

| Field | Ý nghĩa |
|-------|---------|
| `mediation` | MAX hoặc AdMob cho platform đó |
| `sdkSetup` | `SDKSetup` — ID ad unit, bật/tắt từng format |
| `maxProviderConfig` / `admobProviderConfig` | Core provider (để trống → tự lấy ID từ `SDKSetup`) |

Trong `SDKSetup`, mỗi format có `xxxAdsMediationType`:

| Giá trị | Ý nghĩa |
|---------|---------|
| `NONE` | Tắt format — không init/load |
| `MAX` | Dùng MAX |
| `ADMOB` | Dùng AdMob |

Các tuỳ chọn khác trên `SDKSetup` / inspector:

- **Firebase Auth** → thêm define `FIREBASE_AUTH`
- **AppsFlyer** → thêm `UNITY_APPSFLYER` (cần package AppsFlyer)

### 3.2 Áp dụng vào scene

**JIS SDK → Ads → Apply Settings to Scene**

- Gán `SDKSetup` Android/iOS vào `AdsManager`
- Cập nhật scripting defines theo build target đang chọn
- Đồng bộ `JisAds.settings` trong scene

Chi tiết inspector / validate: [ADS_EDITOR_SETUP.md](ADS_EDITOR_SETUP.md).

---

## 4. Setup scene

### Cách nhanh (khuyến nghị)

**JIS SDK → Ads → Scene → Add Manager Prefab**  
hoặc **GameObject → JIS SDK → Ads → Add Manager**

Editor tạo object `JisSDK_Manager` gồm:

- `FirebaseManager`
- `AdsTracker`
- `JisAds`
- `AdsManager` + các `*AdManager` (Banner, Interstitial, Rewarded, MREC, App Open, …)
- Mediation controller (MAX/AdMob nếu package đã cài)

Sau đó gán **`JisSDKAdsSettings`** vào component `JisAds` (và `SdkAdsBootstrap` nếu dùng).

### Thủ công

1. GameObject với `FirebaseManager` (singleton, `DontDestroyOnLoad`)
2. Cùng object hoặc con: `AdsManager` + unit managers đã wire
3. `JisAds` + assign `JisSDKAdsSettings`
4. `AdsTracker` (thường cùng hierarchy với ads)

**Chế độ init khuyến nghị khi dùng `JisAds`:**

| Component | `InitializationMode` |
|-----------|----------------------|
| `FirebaseManager` | `Manual` (để `JisAds` gọi `InitAsync`) |
| `AdsManager` | `Manual` (SDK tự set trong `JisAds.Awake`) |

Nếu để `AutoOnAwake` / `AutoOnStart`, có thể init trùng — nên dùng Manual + `JisAds`.

### `SdkAdsBootstrap` (tuỳ chọn)

Component legacy: tự thêm/cấu hình `JisAds` trên cùng GameObject. Project mới nên gắn `JisAds` trực tiếp.

---

## 5. Khởi tạo trong code

```csharp
using JisSDKAds.Ads;
using JisSDKAds.Firebase;
using JisSDKAds.Common;
```

### 5.1 Tự động (mặc định)

`JisAds` với `autoInitializeOnStart = true` gọi `InitializeAsync()` trong `Start()`:

- Init Firebase
- Init ads legacy (+ Core nếu bật)
- Chờ `AdsManager.IsReady`

### 5.2 Thủ công + Remote Config

```csharp
// Chỉ Firebase
await JisAds.Instance.InitializeFirebaseAsync(fetchRemoteConfig: true);

// Hoặc toàn bộ pipeline ads
await JisAds.Instance.InitializeAsync(fetchRemoteConfig: true);
```

`fetchRemoteConfig: true` → `FirebaseManager.FetchRemoteConfigAsync()`:

- Set default values (xem mục 7)
- Fetch + activate Remote Config
- `IsRemoteConfigReady = true`

### 5.3 Chỉ Firebase (không qua JisAds)

```csharp
await FirebaseManager.Instance.InitAsync();
await FirebaseManager.Instance.FetchRemoteConfigAsync();

// Analytics
FirebaseManager.Instance.LogEvent("level_complete");
FirebaseManager.Instance.SetUserProperty("player_tier", "gold");

// Đọc RC
bool showAoa = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_aoa_active);
double capping = FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_interstitial_capping_time);
```

Callback sau init Firebase:

```csharp
FirebaseManager.Instance.OnInitedSuccessCallback = () => { /* ... */ };
```

---

## 6. API quảng cáo (`JisAds`)

Game code **ưu tiên** `JisAds.Instance` thay cho `AdsManager.Instance` (obsolete).

### 6.1 Format chuẩn (Interstitial / Rewarded / Banner)

```csharp
// Interstitial
JisAds.Instance.ShowInterstitial(
    closedCallback: () => { },
    showSuccessCallback: null,
    showFailCallback: null,
    isTracking: true,
    isSkipCapping: false);

// Rewarded
JisAds.Instance.ShowRewardVideo(
    rewardedPlacement: "shop_coins",
    successCallback: () => { /* thưởng */ },
    closedCallback: success => { },
    failedCallback: () => { });

// Banner
JisAds.Instance.ShowBannerAds();
JisAds.Instance.HideBannerAds();

// Kiểm tra sẵn sàng
bool loaded = JisAds.Instance.IsInterstitialAdLoaded();
bool canShow = JisAds.Instance.CanShowInterstitialAd();
bool rewardReady = JisAds.Instance.IsRewardedVideoLoaded();
```

Routing:

- `useCoreForStandardFormats = true` → **Core `AdManager`** (MAX/AdMob provider)
- `false` → **legacy `AdsManager`**
- Tiered inventory (nếu bật config) → có nhánh riêng — xem [TIERED_INVENTORY.md](TIERED_INVENTORY.md)

### 6.2 Format chỉ legacy

```csharp
JisAds.Instance.ShowAppOpenAd();
JisAds.Instance.ShowMRecAds();
JisAds.Instance.HideMRecAds();
JisAds.Instance.ShowCollapsibleBannerAds(closeCallback: null);
JisAds.Instance.HideCollapsibleBannerAds();
JisAds.Instance.InitResumeAdManager();

// Remove ads (thường gắn IAP)
JisAds.Instance.SetRemoveAds(true);
bool noAds = JisAds.Instance.IsRemoveAds;
```

### 6.3 Truy cập tầng thấp hơn

```csharp
JisAds.Instance.Legacy;   // AdsManager đầy đủ
JisAds.Instance.Core;     // AdManager (Core), null nếu không init
JisAds.Instance.Settings; // JisSDKAdsSettings
bool ready = JisAds.Instance.IsReady;
```

Ví dụ init có điều kiện:

```csharp
async void Start()
{
    await JisAds.Instance.InitializeAsync(fetchRemoteConfig: true);
    if (!JisAds.Instance.IsReady)
        Debug.LogWarning("Ads chưa ready — kiểm tra SDKSetup / mediation define");
}
```

---

## 7. Remote Config (ads)

Key constants: `JisSDKAds.Common.Keys`  
Default khi fetch: `FirebaseRemoteConfigManager` (package firebase).

| Key (`Keys.*`) | Firebase key | Kiểu | Default | Dùng cho |
|----------------|--------------|------|---------|----------|
| `key_remote_aoa_active` | `show_open_ads` | bool | `true` | Bật App Open |
| `key_remote_aoa_show_first_time_active` | `show_open_ads_first_open` | bool | `true` | App Open lần đầu |
| `key_remote_ads_resume_ads_active` | `ads_resume_active` | bool | `true` | Resume ads |
| `key_remote_ads_resume_ads_type` | `ads_resume_type` | string | `"APP_OPEN"` | Loại resume ad |
| `key_remote_ads_resume_pause_time` | `ads_resume_pause_time` | double | `5` | Thời gian pause |
| `key_remote_ads_resume_capping_time` | `ads_resume_capping_time` | double | `10` | Capping resume |
| `key_remote_interstitial_level` | `level_show_inter` | double | `3` | Level bắt đầu show inter |
| `key_remote_interstitial_capping_time` | `ads_interval` | double | `30` | Giãn cách inter (giây) |
| `key_remote_inter_reward_interspersed` | `inter_reward_interspersed` | bool | `false` | Xen kẽ reward/inter |
| `key_remote_inter_reward_interspersed_time` | `inter_reward_interspersed_time` | double | `10` | Thời gian xen kẽ |
| `key_remote_free_ads` | `time_free_ads` | double | `1` | Thời gian “free ads” |
| `key_remote_mrec_active` | `show_mrec_admob` | bool | `false` | MREC AdMob |
| `key_remote_banner_auto_refresh` | `banner_auto_refresh` | bool | `false` | Tự refresh banner |
| `key_remote_banner_auto_refresh_time` | `banner_auto_refresh_time` | double | `15` | Chu kỳ refresh (giây) |

**Đọc trong game** (sau khi fetch RC):

```csharp
using JisSDKAds.Common;
using JisSDKAds.Firebase;

float interval = (float)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_interstitial_capping_time);
```

Các `*AdManager` đọc RC trong `Init`/`Setup` — đổi RC trên Firebase Console, fetch lại khi cần (ví dụ sau `FetchRemoteConfigAsync()`).

---

## 8. Firebase Analytics (ads)

`AdsTracker` tự gửi event qua `FirebaseManager.LogEvent` khi ads chạy (nếu `FirebaseManager` đã ready).

### 8.1 Impression / revenue

```csharp
using JisSDKAds.Ads;

var data = new ImpressionData { /* ad_revenue, ad_unit_name, ... */ };
AdsTracker.TrackAdImpression(data);
```

Event mặc định: `ad_impression` (parameters: platform, source, unit, format, value, currency).

### 8.2 Event funnel (tự động từ AdsTracker)

**Rewarded**

| Event | Khi nào |
|-------|---------|
| `ads_reward_click` | Click nút xem reward |
| `ads_reward_show` | Bắt đầu show |
| `ads_reward_fail` | Show fail |
| `ads_reward_complete` | Hoàn thành (+ `placement`) |
| `ads_reward_load_success` | Load thành công |
| `ads_reward_first_show` | Lần đầu xem (local count) |
| `ad_rewarded_show_count_{5,10,20,50,100}` | Cột mốc số lần xem |

**Interstitial**

| Event | Khi nào |
|-------|---------|
| `ad_inter_click` | Click show inter |
| `ad_inter_show` | Show thành công |
| `ad_inter_fail` | Show fail |
| `ad_inter_load` | Load OK (+ count, request_time) |
| `ad_inter_load_fail` | Load fail |
| `ad_inter_show_fail_by_load` | Show fail vì chưa load |
| `ad_inter_first_show` | Lần đầu |
| `ad_inters_show_count_{5,10,20,50,100}` | Cột mốc |

### 8.3 Event tuỳ chỉnh

```csharp
FirebaseManager.Instance.LogEvent("custom_event");
FirebaseManager.Instance.LogEvent("purchase", new Parameter[] {
    new Parameter("item_id", "coin_pack_1"),
    new Parameter("value", 0.99)
});
```

---

## 9. Firebase Auth (tuỳ chọn)

1. Cài Firebase Auth package (Google).
2. Trong `SDKSetup`, bật **Firebase Auth** → define `FIREBASE_AUTH`.
3. **Apply Settings to Scene**.

API trên `FirebaseManager` (khi `FIREBASE_AUTH`):

```csharp
// Google
await FirebaseManager.Instance.SignInWithGoogle(ct);

// Platform: Play Games (Android) / Game Center (iOS)
await FirebaseManager.Instance.SignInWithPlatform(ct);

#if GOOGLE_PLAY_GAMES
await FirebaseManager.Instance.SignInWithPlayGames(ct);
#endif

await FirebaseManager.Instance.SignInAnonymously(ct);
await FirebaseManager.Instance.SignOut();

FirebaseManager.Instance.SignedInWithUserId += userId => { };
FirebaseManager.Instance.FirebaseAuth.SignedIn += user => { }; // FirebaseUser
FirebaseManager.Instance.SignedInFailed += msg => Debug.LogWarning(msg);
```

Chi tiết implementation: `FirebaseAuthManager` trong package firebase.

---

## 10. Firebase Realtime Database (tuỳ chọn)

`FirebaseDataManager` chỉ biên dịch khi có define `Firebase_Database` và package Realtime Database.

Game project mở rộng listener trong `HandleValueChanged`, v.v.

---

## 11. Một mediation mỗi platform

`JisSDKAdsSettings.singleMediationOnly = true` → Core không fallback MAX ↔ AdMob.

Legacy `AdsManager` dùng `SDKSetup.adsMediationType` và mediation từng format.

Ví dụ thường gặp:

| Platform | Mediation |
|----------|-----------|
| Android | AdMob |
| iOS | MAX |

Cấu hình trong `JisSDKAdsSettings` → tab Android / iOS.

---

## 12. Checklist & xử lý lỗi

### Checklist trước build

- [ ] Hub: Firebase + Ads + mediation đúng platform
- [ ] `google-services.json` / `GoogleService-Info.plist` đã import
- [ ] EDM resolve xong (Android / iOS)
- [ ] `JisSDKAdsSettings` + `SDKSetup` mỗi platform có ít nhất một format active
- [ ] Scene có `FirebaseManager`, `AdsManager`, `JisAds` (+ `AdsTracker`)
- [ ] **Apply Settings to Scene** cho build target hiện tại
- [ ] Define: `UNITY_AD_MAX` / `UNITY_AD_ADMOB` khớp mediation
- [ ] Nếu dùng AppsFlyer: `UNITY_APPSFLYER` trên **cả Android và iOS**

### Lỗi thường gặp

| Triệu chứng | Hướng xử lý |
|-------------|-------------|
| `[JisAds] FirebaseManager not found` | Thêm `FirebaseManager` vào scene |
| `[JisAds] AdsManager not found` | Add Manager / prefab ads |
| Ads không load | Kiểm tra define mediation, ad unit ID trong `SDKSetup`, internet |
| RC không đổi hành vi | Gọi `FetchRemoteConfigAsync()`; kiểm tra key trên Firebase Console |
| `Tracking` / AppsFlyer CS0234 trên iOS | Thêm `UNITY_APPSFLYER` cho iOS; cập nhật package ads + analytics.appsflyer |
| Firebase dependency error | EDM → Force Resolve; đúng phiên bản Firebase Unity |

---

## 13. Ví dụ tích hợp game (rút gọn)

```csharp
using System.Threading.Tasks;
using JisSDKAds.Ads;
using UnityEngine;

public class GameAdsBootstrap : MonoBehaviour
{
    async void Start()
    {
        await JisAds.Instance.InitializeAsync(fetchRemoteConfig: true);

        if (!JisAds.Instance.IsReady)
            return;

        // Banner sau menu chính
        JisAds.Instance.ShowBannerAds();
    }

    public void OnLevelComplete(int level)
    {
        if (JisAds.Instance.CanShowInterstitialAd())
            JisAds.Instance.ShowInterstitial(closedCallback: () => GrantLevelReward());
    }

    public void OnClickFreeCoins()
    {
        JisAds.Instance.ShowRewardVideo("free_coins", successCallback: () => AddCoins(100));
    }
}
```

---

## 14. Tóm tắt API

| Mục đích | API |
|----------|-----|
| Entry ads | `JisAds.Instance` |
| Init đầy đủ | `await JisAds.Instance.InitializeAsync(fetchRemoteConfig)` |
| Init Firebase | `await JisAds.Instance.InitializeFirebaseAsync(fetchRemoteConfig)` |
| Firebase trực tiếp | `FirebaseManager.Instance` |
| RC keys | `JisSDKAds.Common.Keys` |
| Track impression | `AdsTracker.TrackAdImpression(...)` |
| Settings Editor | `JisSDKAdsSettings` — menu **JIS SDK → Ads** |

Phiên bản tài liệu: **SDK 4.0.0** (`com.jis.sdkads.*`).
