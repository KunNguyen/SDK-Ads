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

```
DontDestroyOnLoad
├── FirebaseManager   (your Firebase prefab)
├── AdsManager        (legacy — all formats + RC)
└── JisAds            (component)
    └── JisSDKAdsSettings assigned
    └── Use Core For Standard Formats: ON
```

## 4. Code

```csharp
using JisSDKAds.Ads;
using JisSDKAds.Firebase;

// After JisAds finished init (IsReady)
JisAds.Instance.ShowInterstitial(onClosed: () => { });
JisAds.Instance.ShowRewardVideo("shop", onSuccess: () => { });
JisAds.Instance.ShowAppOpenAd(); // Core on MAX when app open unit configured
```

## 5. Verify

- Console: `[JisAds] Core AdManager ready.` and legacy `IsReady`
- No duplicate init: only one `JisAds` + one `AdsManager` in scene

See `docs/MIGRATION_V4.md` when upgrading old projects.
