# Game project setup (JIS SDK v4)

> **Chưa cài UPM?** Làm trước: [UPM_INSTALL.md](UPM_INSTALL.md) — thêm Git package + Hub import module.

## 1. Install packages

Open **JIS SDK → Hub** and import:

1. **Firebase** (required) — Hub also adds [External Dependency Manager](https://openupm.com/packages/com.google.external-dependency-manager/) from OpenUPM if missing (`com.google.external-dependency-manager` **1.2.187**)
2. Install **Firebase** from Google (Analytics + Remote Config)
3. **Ads** (ads runtime only), then **Enable MAX** / **Enable AdMob** in Hub as needed
4. Optional: IAP, App Review, AppsFlyer, …

## 2. Create settings

**JIS SDK → Create Ads Settings Asset** → `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`

Per platform:

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

## 4. API (Phase 4 — use `JisAds`)

```csharp
using JisSDKAds.Ads;
using JisSDKAds.Firebase;
```

```csharp
// Standard formats (Core when ready, else legacy)
JisAds.Instance.ShowInterstitial(onClosed: () => { });
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

## 5. One mediation per platform

`JisSDKAdsSettings.singleMediationOnly` disables MAX↔AdMob fallback in Core `AdManager`.

Legacy `AdsManager` uses the active profile’s `SDKSetup.adsMediationType` and per-format mediation fields.
