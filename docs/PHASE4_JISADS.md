# Phase 4 — JisAds facade

## Architecture

```
FirebaseManager (scene)
        │
        ▼
    JisAds.Instance
    ├── Legacy AdsManager  → App Open, MREC, Collapsible, Resume, RC, cooldowns
    └── Core AdManager     → Interstitial, Rewarded, Banner (optional)
            └── Provider from MaxAdConfig / AdMobConfig / ProviderConfigFactory
```

## `ProviderConfigFactory`

If `PlatformAdsProfile.maxProviderConfig` / `admobProviderConfig` is empty, IDs are copied from `SDKSetup.maxAdsSetup` / `admobAdsSetup` at runtime.

## Flags

| Field | Default | Meaning |
|-------|---------|---------|
| `useCoreForStandardFormats` | true | Route inter/reward/banner through Core |
| `autoInitializeOnStart` | true | `InitializeAsync()` on Start |

## Migration

| Old | New |
|-----|-----|
| `AdsManager.Instance.ShowInterstitial(...)` | `JisAds.Instance.ShowInterstitial(...)` |
| `AdsManager.Instance.ShowRewardVideo(...)` | `JisAds.Instance.ShowRewardVideo(...)` |
| App open (private) | `JisAds.Instance.ShowAppOpenAd()` |

## Still legacy-only

- App Open, MREC, Collapsible banner, Resume ads
- Rewarded interstitial (AdMob)
- Remote Config driven rules inside unit managers
- Remove-ads / cheat flags on `AdsManager`

Future work: extend `IAdService` with App Open / MREC, then move those formats to Core.
