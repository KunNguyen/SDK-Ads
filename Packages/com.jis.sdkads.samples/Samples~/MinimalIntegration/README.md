# Minimal JIS SDK integration

## 1. Packages (Hub)

1. Open **JIS SDK → Hub**
2. Import **Firebase** → install Firebase from Google if prompted
3. Import **Ads**
4. (Optional) Import **Editor Tools**, **IAP**, **AppsFlyer**, …

Hub creates:

- `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`
- `Assets/JisSDKAds/Settings/AndroidSDKSetup.asset` (stub)
- `Assets/JisSDKAds/Settings/IOSSDKSetup.asset` (stub)

## 2. Configure settings

1. Select `JisSDKAdsSettings`
2. **Android profile:** mediation (MAX or AdMob), assign `AndroidSDKSetup`
3. **iOS profile:** mediation, assign `IOSSDKSetup`
4. Fill ad unit IDs in each `SDKSetup` (or assign `MaxAdConfig` / `AdMobConfig` on profile)

## 3. Scene hierarchy

Use **JIS SDK → Ads → Scene → Add JisSDK Manager Prefab**:

```
JisSDK_Manager
├── Firebase              FirebaseManager, AdsTracker
└── JisAds                JisAds (+ JisSDKAdsSettings), ResumeAdCoordinator
```

`JisAds` drives every format (banner, interstitial, rewarded, app open) through the mediation
selected in the active `JisSDKAdsSettings` profile.

Managers from SDK < 6.0 still carry an `Ads_Runtime` subtree with missing scripts:
**JIS SDK → Ads → Scene → Remove legacy Ads_Runtime** (see `docs/MIGRATION_V6.md`).

## 4. Code

```csharp
using JisSDKAds.Ads;
using JisSDKAds.Firebase;

// After JisAds finished init (IsReady)
JisAds.Instance.ShowInterstitial(onClosed: () => { });
JisAds.Instance.ShowRewardVideo("shop", onSuccess: () => { });
JisAds.Instance.ShowAppOpenAd(); // when an app open unit is configured
```

## 5. Verify

- Console: `[JisAds] Core AdManager ready` and `JisAds.Instance.IsReady`
- No duplicate init: only one `JisAds` in scene

See `docs/MIGRATION_V6.md` when upgrading projects from SDK < 6.0.
