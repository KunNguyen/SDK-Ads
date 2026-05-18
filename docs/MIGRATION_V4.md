# Migration guide — SDK v3 → v4

Breaking changes. No backward compatibility with `SDK` / `ABIMaxSDKAds` namespaces.

## 1. Install via UPM + Hub

Cài package Git UPM trước: [UPM_INSTALL.md](UPM_INSTALL.md).  
Sau đó dùng **JIS SDK → Hub** thay vì copy `Assets/JisSDKAds`.

Remove old folder:

- Delete `Assets/JisSDKAds` (monolith) from game project if present
- Remove `com.unity.services.levelplay` from manifest

## 2. Namespace changes

| Old | New |
|-----|-----|
| `using SDK;` | `using JisSDKAds.Ads;` |
| `using SDK.IAP;` | `using JisSDKAds.IAP;` |
| `FirebaseManager` in `SDK` | `using JisSDKAds.Firebase;` |
| `AdsManager.Instance` | Prefer `JisAds.Instance` |

Full table: [NAMESPACES.md](NAMESPACES.md)

## 3. Entry point

**Before:**

```csharp
AdsManager.Instance.ShowInterstitial(closed, success, fail);
AdsManager.Instance.ShowRewardVideo(placement, onSuccess, onClosed, onFail);
```

**After:**

```csharp
using JisSDKAds.Ads;

JisAds.Instance.ShowInterstitial(closed, success, fail);
JisAds.Instance.ShowRewardVideo(placement, onSuccess, onClosed, onFail);
```

`JisAds` routes **Interstitial / Rewarded / Banner** through Core when enabled.  
**App Open, MREC, Collapsible, Resume** still use legacy `AdsManager` (or Core App Open on MAX).

## 4. Scene setup

| Component | Required |
|-----------|----------|
| `FirebaseManager` | Yes |
| `AdsManager` | Yes (legacy formats + RC) |
| `JisAds` | Yes (recommended) |
| `JisSDKAdsSettings` | Assign on `JisAds` |

Remove old `SdkAdsBootstrap` only if you add `JisAds` directly (bootstrap adds `JisAds` for you).

## 5. Config assets

**Before:** `Assets/JisSDKConfigs/`, `SDKSetup` per platform on `AdsManager`

**After:**

- `JisSDKAdsSettings` — platform profiles (mediation + link to `SDKSetup`)
- `SDKSetup` — still used for all format IDs and RC behaviour
- Optional: `MaxAdConfig` / `AdMobConfig` for Core (auto-filled from `SDKSetup` if empty)

## 6. Mediation

- IronSource / LevelPlay / Unity Ads **removed**
- One mediation per platform in `JisSDKAdsSettings` (e.g. AdMob Android, MAX iOS)

## 7. Scripting defines

Hub sets defines on import. Verify:

- `UNITY_AD_MAX`, `UNITY_AD_ADMOB` — Ads
- `UNITY_IAP_ACTIVE` — IAP
- `UNITY_APPSFLYER` — AppsFlyer package
- `GOOGLE_REVIEW` — App Review (Android)

## 8. IAP

```csharp
// Before
using SDK.IAP;
InAppPurchaser.Instance...

// After
using JisSDKAds.IAP;
InAppPurchaser.Instance...
```

## 9. AppsFlyer

Install **AppsFlyer** optional package via Hub. Tracking facade: `JisSDKAds.Ads.Tracking.AppsflyerManager` (same static API).

## 10. Checklist

- [ ] Hub: Firebase + Ads imported
- [ ] Firebase Google packages installed
- [ ] `JisSDKAdsSettings` created and assigned
- [ ] Android/iOS `SDKSetup` filled
- [ ] Scene: Firebase + AdsManager + JisAds
- [ ] Replace `AdsManager.Instance` calls with `JisAds.Instance` where possible
- [ ] Build Android/iOS and test ads + RC
