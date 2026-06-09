# JIS SDK Ads — Hướng dẫn sử dụng

Tài liệu này tóm tắt **tất cả những gì cần setup để chạy quảng cáo** và **cách gọi API** cho 3 format chính: Interstitial, Rewarded, Banner (kèm App Open / Resume).

> Liên quan:
> - Cài UPM / Hub modules: [UPM_INSTALL.md](UPM_INSTALL.md)
> - Workflow editor (platform tabs, Apply to Scene): [ADS_EDITOR_SETUP.md](ADS_EDITOR_SETUP.md)
> - Firebase + Remote Config chi tiết: [ADS_FIREBASE_GUIDE.md](ADS_FIREBASE_GUIDE.md)
> - Kiến trúc tổng thể: [ADS_DESIGN.md](ADS_DESIGN.md)

---

## 0. TL;DR — Checklist tối thiểu

1. Import **Firebase** + **Ads** qua **JIS SDK → Hub**, bật **Enable AdMob** và/hoặc **Enable MAX**.
2. **JIS SDK → Ads → Create Settings Asset** → có `JisSDKAdsSettings` + `AndroidSDKSetup` + `IOSSDKSetup`.
3. Gán **mediation + ad unit IDs** cho từng platform (xem [mục 3](#3-cấu-hình-mediation--ad-unit-id-quan-trọng-nhất)).
4. Scene có: `FirebaseManager`, prefab `AdsManager`/`Manager`, và GameObject gắn **`JisAds`** (gán `JisSDKAdsSettings`, bật **Use Core For Standard Formats**).
5. **JIS SDK → Ads → Apply Settings to Scene**.
6. Gọi `await JisAds.Instance.InitializeAsync()` từ màn loading.
7. Dùng API: `ShowInterstitial` / `ShowRewardVideo` / `ShowBannerAds`.

> ⚠️ **Nếu dùng Tiered:** bắt buộc cấu hình unit ID ở Remote Config **hoặc** điền fallback local, nếu không format đó **không bao giờ load được**. Xem [mục 3.2](#32-tiered-sequential-ladder).

---

## 1. Kiến trúc ngắn gọn

```
Game code
   │  JisAds.Instance.ShowRewardVideo(...)
   ▼
JisAds (Core, điều phối)
   │  → AdManager (Core)  → Provider (AdMob / MAX)
   │  → tiered decorator (SequentialTier) nếu bật
   └→ AppOpenAdService / ResumeAdCoordinator
```

- **`JisAds`** là entry point khuyến nghị. `AdsManager.Instance` (legacy) đã obsolete — dùng `JisAds.Instance`.
- **Một mediation cho mỗi platform** (mặc định `singleMediationOnly = true`). Cấu hình hiện tại: Android = **AdMob**, iOS = **MAX**.

---

## 2. Cài đặt package & define symbols

1. **Hub → import Firebase + Ads.** Bật **Enable AdMob** (cài `providers.admob`) và/hoặc **Enable MAX** (cài `providers.max`).
   - Chỉ thêm define `UNITY_AD_ADMOB` / `UNITY_AD_MAX` là **chưa đủ** — phải import package provider tương ứng.
2. Cài **Firebase** (Analytics + Remote Config) từ Google.
3. **Apply Settings to Scene** sẽ tự set scripting defines cho build target đang chọn (qua `ApplyScriptingDefinesForAllPlatforms`).

> Nếu đổi platform (Android↔iOS) trong Editor, chạy lại **Apply Settings to Scene** để define + mediation đồng bộ.

---

## 3. Cấu hình mediation & ad unit ID (QUAN TRỌNG NHẤT)

Mở `JisSDKAdsSettings.asset`. Mỗi platform có 1 `PlatformAdsProfile`:

| Field | Ý nghĩa |
|-------|---------|
| `mediation` | `ADMOB` hoặc `MAX` cho platform đó |
| `sdkSetup` | Trỏ tới `AndroidSDKSetup` / `IOSSDKSetup` chứa unit ID + tier config |

Trong `SDKSetup` (vd. `AndroidSDKSetup.asset`):
- `admobAdsSetup` (khi mediation = AdMob): `interstitialAdUnitID`, `rewardedAdUnitID`, `bannerAdUnitID`, `appOpenAdUnitID` (mỗi cái có list `AndroidID` / `IosID`), và `interstitialTierConfig` / `rewardedTierConfig`.
- `maxAdsSetup` (khi mediation = MAX): `sdkKey`, `interstitialAdUnitID`, `rewardedAdUnitID`, `bannerAdUnitID`, `appOpenAdUnitID`.

### 3.1 Single inventory (đơn giản, khuyến nghị để bắt đầu)

Để `enableSequentialLadder = false` trong `interstitialTierConfig` / `rewardedTierConfig`, rồi điền:
- AdMob: `admobAdsSetup.rewardedAdUnitID.AndroidID` (và interstitial/banner/appopen tương ứng).
- MAX: `maxAdsSetup.rewardedAdUnitID.AndroidID` (…)

→ SDK dùng đúng 1 unit ID cho mỗi format.

### 3.2 Tiered (sequential ladder)

Bật `enableSequentialLadder = true`. SDK sẽ load lần lượt **Premium → High → Mid → Low → Fill** (tier nào load được trước thì dùng).

**Nguồn unit ID cho mỗi tier (theo thứ tự ưu tiên):**
1. **Firebase Remote Config** theo key tier (xem bảng [mục 4](#4-remote-config-keys)).
2. `tierConfig.tiers[].androidAdUnitId` (điền tay trong asset).
3. `tierConfig.defaultAndroidAdUnitId`.
4. `admobAdsSetup.rewardedAdUnitID` (list unit chuẩn) — fallback cuối.

> ⚠️ **Bẫy cấu hình:** nếu cả 4 nguồn đều trống thì ladder fail với `no_ad_unit_configured` và **format đó không bao giờ load** (preload tiered chỉ retry vài lần rồi dừng). Console sẽ in cảnh báo:
> `[SequentialTier] rewarded ladder has NO ad unit id …`
>
> **Khuyến nghị:** luôn điền tối thiểu **1 fallback local** (`defaultAndroidAdUnitId` hoặc list unit chuẩn) để kể cả khi Remote Config trống vẫn load được.

> SDK đã có cơ chế tự hồi phục: khi Remote Config refresh, unit ID tier mới được áp lại và preload được re-arm tự động.

---

## 4. Remote Config keys

Tạo các tham số này trong Firebase Remote Config (chỉ cần khi dùng tính năng tương ứng):

### Inventory mode (tùy chọn — bật/tắt tiered từ xa)
| Key | Giá trị | Mô tả |
|-----|---------|-------|
| `interstitial_inventory_mode` | `single` \| `tiered` | Chế độ inventory interstitial |
| `rewarded_inventory_mode` | `single` \| `tiered` | Chế độ inventory rewarded |

### Tiered unit IDs (bắt buộc khi tiered + không có fallback local)
| Interstitial | Rewarded |
|--------------|----------|
| `inter_premium_id` | `reward_premium_id` |
| `inter_high_id` | `reward_high_id` |
| `inter_mid_id` | `reward_mid_id` |
| `inter_low_id` | `reward_low_id` |
| `inter_fill_id` | `reward_fill_id` |

> Có thể chỉ set vài tier; các tier dưới sẽ **cascade** dùng id của tier trên gần nhất.

### Capping / Interstitial
| Key | Mô tả |
|-----|-------|
| `inter_capping_from_app_open_seconds` | Chặn interstitial X giây sau khi mở app |
| `inter_capping_between_shows_seconds` | Khoảng cách tối thiểu giữa 2 lần interstitial (xem rewarded cũng reset timer này) |
| `level_show_inter` | Level bắt đầu cho phép interstitial |
| `ads_interval` | Capping interval (legacy) |

### Banner
| Key | Mô tả |
|-----|-------|
| `banner_auto_refresh` | Bật auto-refresh banner (mặc định false) |
| `banner_auto_refresh_time` | Chu kỳ refresh (giây) |

### App Open / Resume
| Key | Mô tả |
|-----|-------|
| `show_open_ads` | Bật App Open |
| `show_open_ads_first_open` | Cho phép App Open ở lần mở đầu |
| `ads_resume_active` | Bật Resume ad |
| `ads_resume_capping_time` | Capping giữa các Resume ad |
| `ads_resume_pause_time` | Thời gian pause tối thiểu để tính resume |
| `ads_resume_type` | Loại ad khi resume |

### Remove Ads / Free Ads
| Key | Mô tả |
|-----|-------|
| `time_free_ads` | Thời gian free ads |
| (local) `key_local_remove_ads` | Cờ Remove Ads lưu trong PlayerPrefs |

---

## 5. Khởi tạo

```csharp
using JisSDKAds.Ads;

// Gọi 1 lần từ màn loading. Tự fetch Remote Config → init mediation → preload.
await JisAds.Instance.InitializeAsync();
```

- `JisSDKAdsSettings.adsInitializationMode`:
  - **Manual** (khuyến nghị): bạn tự gọi `InitializeAsync()`.
  - **AutoOnStart**: `AdsManager` tự bootstrap lúc `Start` (chỉ cho prototype; **không** dùng chung với JisAds auto-init).
- `JisAds` có `autoInitializeOnStart` (serialized): nếu bật, `JisAds` tự gọi `InitializeAsync()` ở `Start`.
- `preloadAdsOnGameStart` (settings): sau init sẽ tự preload banner/interstitial/rewarded/app-open.

> Trong **Unity Editor**, startup preload bị bỏ qua (chỉ load on-demand). Test fill/tier thật cần build lên device.

---

## 6. API sử dụng

```csharp
using JisSDKAds.Ads;
using UnityEngine.Events;
```

### Interstitial
```csharp
// Có placement + callback
JisAds.Instance.ShowInterstitial(
    "level_end",
    closedCallback:      () => Debug.Log("inter closed"),
    showSuccessCallback: () => Debug.Log("inter shown"),
    showFailCallback:    () => Debug.Log("inter failed/capped"));

// Ngắn gọn
JisAds.Instance.ShowInterstitial();

bool ready = JisAds.Instance.IsInterstitialAdLoaded();
```

### Rewarded
```csharp
JisAds.Instance.ShowRewardVideo(
    "double_coins",
    successCallback: () => GrantReward(),          // chỉ gọi khi user xem đủ
    closedCallback:  (watched) => Resume(),         // luôn gọi khi đóng
    failedCallback:  () => ShowNoAdToast());         // chưa có ad / lỗi

bool ready = JisAds.Instance.IsRewardedVideoLoaded();
```

> Quan trọng: cấp thưởng trong `successCallback`, **không** cấp trong `closedCallback` (closed gọi cả khi user bỏ ngang).

### Banner
```csharp
JisAds.Instance.ShowBannerAds();   // hiện + giữ trạng thái "muốn hiển thị"
JisAds.Instance.HideBannerAds();   // ẩn + dừng auto-refresh
```
- Banner tự được **ẩn khi có fullscreen ad** và **hiện lại** sau khi đóng (có retry).

### App Open
```csharp
JisAds.Instance.ShowAppOpenAd();
JisAds.Instance.PreloadAppOpenAd();
bool ready = JisAds.Instance.IsAppOpenAdLoaded();
```
App Open lúc cold-start và Resume ad khi quay lại app được điều khiển bằng Remote Config (mục 4) + cấu hình trên component `JisAds`.

### Remove Ads
```csharp
JisAds.Instance.SetRemoveAds(true);   // tắt banner/interstitial/app-open
bool removed = JisAds.Instance.IsRemoveAds;
```
(Rewarded vẫn cho xem khi Remove Ads — tùy cách bạn gọi.)

---

## 7. Cấu hình nâng cao trên component `JisAds`

Các field serialized (chỉnh trong Inspector của GameObject gắn `JisAds`):

| Field | Mặc định | Ý nghĩa |
|-------|----------|---------|
| `useCoreForStandardFormats` | on | Inter/Rewarded/Banner đi qua Core AdManager |
| `autoInitializeOnStart` | on | Tự `InitializeAsync()` ở `Start` |
| **Banner restore** | | |
| `restoreBannerAfterFullscreenAds` | true | Ẩn banner khi fullscreen ad, hiện lại sau khi đóng |
| `bannerRestoreDelaySec` | 0.35 | Delay trước khi restore |
| `bannerRestoreMaxRetries` | 4 | Số lần thử lại nếu restore load fail |
| `bannerRestoreRetryDelaySec` | 3 | Giãn cách giữa các lần retry |
| **Watchdog fullscreen** | | |
| `enableFullscreenInFlightWatchdog` | true | Tự gỡ kẹt khi callback close của inter/rewarded không về |
| `fullscreenInFlightWatchdogSec` | 60 | Thời gian chờ trước khi watchdog kích hoạt |
| **App Open** | | |
| `showAppOpenOnColdStart` | off | App Open ở cold start |
| `appOpenFirstShowDelayMs` / `appOpenMinIntervalBetweenShowsSec` | 600ms / 20s | Tinh chỉnh App Open |

---

## 8. Troubleshooting nhanh

| Triệu chứng | Nguyên nhân thường gặp |
|-------------|------------------------|
| Rewarded/Inter "load 1 lần rồi hết" (MAX/iOS) | Provider phải warm-reload sau close — đã fix trong SDK. Build lại với `UNITY_AD_MAX`. |
| Tiered rewarded **không bao giờ load** | Không có unit ID ở RC lẫn local. Xem cảnh báo `[SequentialTier] … NO ad unit id`. Điền RC key hoặc fallback (mục 3.2). |
| Banner mất sau khi đóng ad | Bật `restoreBannerAfterFullscreenAds`; kiểm tra log `[JisAds][BannerRestore]`. |
| `using JisSDKAds.Common` không resolve | Thêm asmdef reference (xem [GAME_SETUP.md](GAME_SETUP.md) mục 4). |
| Không có ad trong Editor | Bình thường — startup preload bị skip trong Editor; test trên device. |
| Init không xong | Bật `enableAdsDebugLogging` trong settings để xem log init/mediation. |

Bật log chi tiết: `JisSDKAdsSettings.enableAdsDebugLogging = true` (hoặc qua editor).
