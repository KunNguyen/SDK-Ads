# JIS SDK v4 — Namespaces

Breaking change: legacy `SDK` and `ABIMaxSDKAds` namespaces are removed.

## Mapping (old → new)

| Old | New |
|-----|-----|
| `SDK` | `JisSDKAds.Ads` |
| `SDK.AdsManagers` | `JisSDKAds.Ads.UnitAdManagers` |
| `SDK.AdsManagers.Interface` | `JisSDKAds.Ads.UnitAdManagers.Interface` |
| `SDK.Struct` | `JisSDKAds.Ads.Mediation.Callbacks` |
| `SDK.IAP` | `JisSDKAds.IAP` |
| `ABIMaxSDKAds.Scripts` | `JisSDKAds.Common` |
| `ABIMaxSDKAds.Scripts.Utils` | `JisSDKAds.Common` |
| `ABIMaxSDKAds.Scripts.Ads.AdsManagers.Service` | `JisSDKAds.Ads.UnitAdManagers.Service` |
| `ABIMaxSDKAds.Scripts.IAPServices` | `JisSDKAds.IAP` |
| `ABIMaxSDKAds.Editor` | `JisSDKAds.Editor` |
| Firebase types (was `SDK`) | `JisSDKAds.Firebase` |
| AppsFlyer facade | `JisSDKAds.Ads.Tracking` |
| AppsFlyer implementation | `JisSDKAds.Analytics.AppsFlyer` |
| SolarEngine | `JisSDKAds.Analytics.SolarEngine` |
| Facebook | `JisSDKAds.Analytics.Facebook` |
| Core | `JisSDKAds.Core` (unchanged) |
| Providers | `JisSDKAds.Providers.Max` / `.AdMob` |

## Game code migration

```csharp
// Before
using SDK;
AdsManager.Instance.ShowInterstitial(...);

// After
using JisSDKAds.Ads;
AdsManager.Instance.ShowInterstitial(...);
```

```csharp
// Before
using SDK.IAP;
InAppPurchaser.Instance...

// After
using JisSDKAds.IAP;
InAppPurchaser.Instance...
```

```csharp
// Before
FirebaseManager.Instance...

// After
using JisSDKAds.Firebase;
FirebaseManager.Instance...
```

## Entry types

| Type | Namespace |
|------|-----------|
| **`JisAds`** (preferred) | `JisSDKAds.Ads` |
| `AdsManager` (legacy, obsolete) | `JisSDKAds.Ads` |
| `SDKSetup` | `JisSDKAds.Ads` |
| `JisSDKAdsSettings` | `JisSDKAds.Ads.Settings` |
| `SdkAdsBootstrap` | `JisSDKAds.Ads` |
| `AdManager` | `JisSDKAds.Core` |
| `FirebaseManager` | `JisSDKAds.Firebase` |
| `InAppPurchaser` | `JisSDKAds.IAP` |
| `EventManager` | `JisSDKAds.Common` |
| `DebugAds` | `JisSDKAds.Common` |
| `Keys` | `JisSDKAds.Common` |

Game scripts in a custom **Assembly Definition** must reference asmdef `JisSDKAds.Common` (package `com.jis.sdkads.common`). There is no `JisSDKAds.Scripts` namespace.
