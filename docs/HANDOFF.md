# JIS SDK v4 — Handoff / Tiếp tục công việc

> **Cập nhật:** sau Phase 4 + namespace migration. Dùng file này khi mở chat mới hoặc context đầy.
> **6.0:** legacy ads runtime đã bị gỡ — `JisAds` là entry point duy nhất ([MIGRATION_V6.md](MIGRATION_V6.md), kiến trúc hiện tại: [ADS_DESIGN.md](ADS_DESIGN.md)).

---

## 1. Mục tiêu dự án

- Repo **SDK-Ads** = monorepo UPM, **không phải game** — import vào từng game qua Git UPM.
- **Breaking change** OK: bỏ legacy `SDK` / `ABIMaxSDKAds`, chỉ MAX + AdMob, bỏ IronSource/LevelPlay/Unity Ads.
- **Firebase** bắt buộc (user tự cài Google packages); Analytics + Remote Config qua `com.jis.sdkads.firebase`.
- **Hub** (`JIS SDK → Hub`): import từng module → sửa `Packages/manifest.json` + scripting defines.
- **Odin** nhúng trong `com.jis.sdkads.core`.
- **MeldAppAds** giữ tại `Assets/MeldAppAds/` — **không** đưa vào UPM.

---

## 2. Quyết định đã chốt (không đổi trừ khi user nói)

| Chủ đề | Quyết định |
|--------|------------|
| Mediation | Chỉ **MAX** + **AdMob**; **1 mediation / 1 platform** (vd. AdMob Android, MAX iOS) |
| Fallback MAX↔AdMob | **Tắt** (`singleMediationOnly` / `ConfigureSingleMediation`) |
| Config | `JisSDKAdsSettings` + `SDKSetup` per platform + optional `MaxAdConfig`/`AdMobConfig` |
| IAP | Cùng monorepo, package `com.jis.sdkads.iap`, Hub import |
| AppsFlyer / Solar / Facebook | Optional packages |
| App Review | Optional, **Android only**, Hub import |
| UPM | Nhiều package, một Git repo, `?path=Packages/com.jis.sdkads.xxx#4.0.0` |
| Unity | Dev repo Unity 6; package.json khai báo `unity: "2022.3"` |

---

## 3. Trạng thái phase

| Phase | Mô tả | Trạng thái |
|-------|--------|------------|
| **0** | Xóa LevelPlay, IronSource, UnityAds, BanThanh/GiftCode | ✅ Done |
| **1** | UPM `Packages/com.jis.sdkads.*` | ✅ Done |
| **2** | Hub window | ✅ Done v1 |
| **3** | `JisSDKAdsSettings` + platform profiles | ✅ Done |
| **4** | `JisAds` facade + Core bridge | ✅ Done |
| **5** | Namespace `JisSDKAds.*` | ✅ Done |
| **6** | Samples, Hub auto-settings, Core App Open, migration doc | ✅ Done (MREC Core = backlog) |
| **6.0** | Gỡ legacy ads runtime, scene chỉ còn Firebase + JisAds | ✅ Done |

---

## 4. Cấu trúc repo hiện tại

```
SDK-Ads/
├── Packages/
│   ├── com.jis.sdkads.hub/          # Editor Hub
│   ├── com.jis.sdkads.core/         # AdManager, interfaces, Odin
│   ├── com.jis.sdkads.common/       # EventManager, Keys, Utils, ScriptOrder
│   ├── com.jis.sdkads.firebase/     # FirebaseManager, RC, Analytics
│   ├── com.jis.sdkads.ads/          # JisAds, SDKSetup, settings, App Open, Resume, tracking
│   ├── com.jis.sdkads.providers.max/
│   ├── com.jis.sdkads.providers.admob/
│   ├── com.jis.sdkads.iap/
│   ├── com.jis.sdkads.appreview/
│   ├── com.jis.sdkads.analytics.{appsflyer,solarengine,facebook}/
│   └── com.jis.sdkads.editor/
├── Assets/
│   └── MeldAppAds/                  # Dev tool ONLY — không UPM
└── docs/
    ├── HANDOFF.md                   # ← file này
    ├── REFACTOR_PLAN.md
    ├── GAME_SETUP.md
    ├── NAMESPACES.md
    └── PHASE4_JISADS.md
```

**Đã xóa:** `Assets/JisSDKAds` (chuyển sang Packages).

---

## 5. Entry points quan trọng (code)

| Thành phần | File / class | Ghi chú |
|------------|--------------|---------|
| **Game API (duy nhất)** | `JisSDKAds.Ads.JisAds` | `Packages/.../ads/Runtime/JisAds.cs` — capping, level gate, remove ads, consent |
| Core ads | `JisSDKAds.Core.AdManager` | Inter / Reward / Banner / App Open |
| App Open / Resume | `AppOpenAdService`, `ResumeAdCoordinator` | `Packages/.../ads/Runtime/Ads/{AppOpen,Resume}/` |
| Settings | `JisSDKAds.Ads.Settings.JisSDKAdsSettings` | Android/iOS profiles |
| Provider từ SDKSetup | `ProviderConfigFactory` | `Packages/.../ads/Runtime/ProviderConfigFactory.cs` |
| Firebase | `JisSDKAds.Firebase.FirebaseManager` | |
| Hub | `JisSDKAds.Hub.JisSDKHubWindow` | Menu: **JIS SDK → Hub** |
| Bootstrap cũ | `SdkAdsBootstrap` | Gắn `JisAds` trên cùng GO |

### Init flow (`JisAds`)

1. `Awake`: `Instance`
2. `Start` → `InitializeAsync()` (khi `autoInitializeOnStart`): warm up SDK → Firebase RC → ad inventory từ RC → tạo provider + `AdManager.Initialize()`
3. Core sẵn sàng → `IsReady = true`, bootstrap App Open + Resume, preload

### Bug đã sửa gần đây

- `Keys` → `public` trong `JisSDKAds.Common`
- `JisSDKAds.IAP.asmdef` + `Unity.Services.Core`
- `ProviderConfigFactory`: `using JisSDKAds.Core.Interfaces` **ngoài** `#if UNITY_AD_MAX`
- AppsFlyer: facade `JisSDKAds.Ads.Tracking.AppsflyerManager` + package `analytics.appsflyer` registration
- Một số file bị hỏng khi migrate namespace (PowerShell) — đã sửa (các file legacy liên quan đã bị gỡ ở 6.0), `AdsTracker`, `SolarEngineManager`

---

## 6. Namespace map (tóm tắt)

Xem chi tiết: [NAMESPACES.md](NAMESPACES.md)

```
SDK                          → JisSDKAds.Ads
SDK.IAP                      → JisSDKAds.IAP
ABIMaxSDKAds.Scripts.Utils   → JisSDKAds.Common
Firebase (trước SDK)         → JisSDKAds.Firebase
AppsFlyer facade             → JisSDKAds.Ads.Tracking
```

**Game code mới:**

```csharp
using JisSDKAds.Ads;
using JisSDKAds.Firebase;

JisAds.Instance.ShowInterstitial(...);
JisAds.Instance.ShowAppOpenAd();
```

---

## 7. Hub — import modules

| Nút Hub | Packages + defines |
|---------|-------------------|
| Firebase | hub, core, common, firebase |
| Ads | providers.max, providers.admob, ads + `UNITY_AD_MAX`, `UNITY_AD_ADMOB` + AppLovin UPM |
| IAP | iap + Purchasing + `UNITY_IAP_ACTIVE` — guide [IAP_USAGE.md](IAP_USAGE.md), RemoveAds auto via `ProductKind` |
| App Review | appreview + `GOOGLE_REVIEW` (Android) |
| AppsFlyer / Solar / Facebook | package tương ứng |
| Editor | editor |

Dev repo: **Use embedded packages** = `file:com.jis.sdkads.xxx`  
Game project: Git URL + `?path=Packages/...#4.0.0`

---

## 8. Scene setup game (tóm tắt)

Xem [GAME_SETUP.md](GAME_SETUP.md), [ADS_EDITOR_SETUP.md](ADS_EDITOR_SETUP.md)

1. User cài Firebase (Google)
2. Hub → Import Firebase, Ads, …
3. **JIS SDK → Ads → Create/Open Settings Asset**
4. Scene: **JIS SDK → Ads → Scene → Add JisSDK Manager Prefab** → `JisSDK_Manager` (Firebase + JisAds)
5. **JIS SDK → Ads → Apply Settings to Scene**

---

## 9. Phase 6 — Đã làm / backlog

**Done:**

- `com.jis.sdkads.samples` + `Samples~/MinimalIntegration/README.md`
- Hub Import Ads → `JisSDKAdsSettings` + stub `AndroidSDKSetup` / `IOSSDKSetup`
- `IAppOpenAd` + MAX implementation; AdMob uses `NullAppOpenAd`
- `AdManager.ShowAppOpen` / `JisAds.ShowAppOpenAd` (Core when MAX has unit id)
- Hub: OpenUPM + optional `com.google.ads.mobile` 9.4.0 on Ads import
- [MIGRATION_V4.md](MIGRATION_V4.md), [INDEX.md](../INDEX.md) updated

**Backlog:**

- Core **MREC** (`IMrecAd`)
- Unity **.unity** sample scene + prefabs in samples package

**Done sau Phase 6:** AdMob App Open trên Core; gỡ legacy runtime (6.0).

---

## 10. Lệnh / path hữu ích

- Hub menu: `JIS SDK/Hub`
- Tạo settings: `JIS SDK/Ads/Create/Open Settings Asset` → `Assets/JisSDKAds/Settings/`
- Manifest: `Packages/manifest.json`
- Version package: **4.0.0** (tất cả `package.json` con)

### Git UPM (game)

Hướng dẫn đầy đủ: **[UPM_INSTALL.md](UPM_INSTALL.md)** (Hub, manifest thủ công, private repo, nâng version).

```json
"com.jis.sdkads.hub": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.hub#main"
```

Hub: `JisSDKAds.Hub.GitBaseUrl`, `JisSDKAds.Hub.GitRevision` (mặc định `main`). Tag Git `4.0.0` chưa publish → đừng dùng `#4.0.0` trong manifest.

---

## 11. Rủi ro / lưu ý kỹ thuật

- **Prefab cũ:** `JisSDK_Manager` từ SDK < 6.0 còn subtree `Ads_Runtime` (missing script) → **JIS SDK → Ads → Scene → Remove legacy Ads_Runtime**.
- **AdMob `appId`** trong `ProviderConfigFactory` đang để `""` — có thể cần lấy từ `SDKSetup` hoặc GMA settings asset.
- **Firebase DLLs** trong asmdef firebase — user phải có Firebase trong project.
- **Không commit** secrets / `google-services.json` nhầm vào SDK repo nếu sensitive.

---

## 12. Cách tiếp tục trong chat mới

Gửi cho agent:

> Đọc `docs/HANDOFF.md` và tiếp tục Phase 6 (hoặc task cụ thể: …).

Hoặc:

> Tiếp tục JIS SDK v4 — bắt đầu từ HANDOFF.md

---

## 13. Tài liệu liên quan

| File | Nội dung |
|------|----------|
| [HANDOFF.md](HANDOFF.md) | File này — snapshot tổng thể |
| [REFACTOR_PLAN.md](REFACTOR_PLAN.md) | Bảng phase ngắn |
| [GAME_SETUP.md](GAME_SETUP.md) | Hướng dẫn game project |
| [NAMESPACES.md](NAMESPACES.md) | Map namespace |
| [ADS_DESIGN.md](ADS_DESIGN.md) | Kiến trúc ads v6 |
| [MIGRATION_V6.md](MIGRATION_V6.md) | Gỡ legacy runtime (6.0) |
| [PHASE4_JISADS.md](PHASE4_JISADS.md) | Kiến trúc JisAds (lịch sử, trước 6.0) |
| [README.md](../README.md) | Overview repo |
