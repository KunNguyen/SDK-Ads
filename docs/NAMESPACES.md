# JIS SDK v4 — Namespaces

Breaking change: legacy `SDK` and `ABIMaxSDKAds` namespaces are removed.

## Mapping (old → new)

| Old | New |
|-----|-----|
| `SDK` | `JisSDKAds.Ads` |
| `SDK.IAP` | `JisSDKAds.IAP` |
| `ABIMaxSDKAds.Scripts` | `JisSDKAds.Common` |
| `ABIMaxSDKAds.Scripts.Utils` | `JisSDKAds.Common` |
| `ABIMaxSDKAds.Scripts.IAPServices` | `JisSDKAds.IAP` |
| `ABIMaxSDKAds.Editor` | `JisSDKAds.Editor` |
| Firebase types (was `SDK`) | `JisSDKAds.Firebase` |
| AppsFlyer facade | `JisSDKAds.Ads.Tracking` |
| AppsFlyer implementation | `JisSDKAds.Analytics.AppsFlyer` |
| SolarEngine | `JisSDKAds.Analytics.SolarEngineIntegration` (not `...SolarEngine` — conflicts with vendor `SolarEngine` SDK namespace) |
| Facebook | `JisSDKAds.Analytics.Facebook` |
| Core | `JisSDKAds.Core` (unchanged) |
| Providers | `JisSDKAds.Providers.Max` / `.AdMob` |

## Game code migration

```csharp
// Before
using SDK;

// After
using JisSDKAds.Ads;
JisAds.Instance.ShowInterstitial(...);
```

The legacy unit-manager / mediation-callback namespaces (`JisSDKAds.Ads.UnitAdManagers*`, `JisSDKAds.Ads.Mediation.Callbacks`) and the legacy ads singleton were removed in 6.0 — API mapping: [MIGRATION_V6.md](MIGRATION_V6.md).

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
| **`JisAds`** (only ads entry point) | `JisSDKAds.Ads` |
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
