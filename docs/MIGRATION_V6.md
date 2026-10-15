# Migrating to JIS SDK Ads 6.0

6.0 removes the legacy `AdsManager` runtime. `JisAds` is the only ads entry point.

## Removed types

`AdsManager` (all partials), `UnitAdManager`, `BannerAdManager`, `InterstitialAdManager`,
`RewardAdManager`, `AutoLoadSystem`, `CooldownSystem`, `AdsMediationController` and the
`*Callbacks` classes, `MaxMediationController`, `AdmobMediationController` (incl. tiered and
rewarded-interstitial partials), `MaxMediationConfigBridge`, `AdmobMediationConfigBridge`,
`MaxMediationReflection`, `AdMobMediationReflection`, `AdsConfig`, `AdsRemoteConfigBootstrap`,
`AdsManagerSDKSetupContainer`, `RewardAdsPlacementConfig`, `WatchVideoRewardType`.

## API mapping

| Before | After |
|---|---|
| `AdsManager.Instance.ShowInterstitial(...)` | `JisAds.Instance.ShowInterstitial(...)` / `ShowInterstitialAuto(...)` |
| `AdsManager.Instance.ShowRewardVideo(...)` | `JisAds.Instance.ShowRewardVideo(...)` |
| `AdsManager.Instance.RequestBannerAds()` / `ShowBannerAds()` / `HideBannerAds()` | `JisAds.Instance.ShowBannerAds()` / `HideBannerAds()` |
| `AdsManager.Instance.IsInterstitialAdLoaded()` | `JisAds.Instance.IsInterstitialAdLoaded()` |
| `AdsManager.Instance.IsPassLevelToShowInterstitial(level)` | `JisAds.Instance.IsPassLevelToShowInterstitial(level)` (RC `level_show_inter`, fallback `JisSDKAdsSettings.interstitialLevelFallback`) |
| `AdsManager.Instance.ShowConsentForm()` | `JisAds.Instance.ShowPrivacyOptionsForm()` (MAX CMP on MAX profiles, AdMob UMP otherwise) |
| `AdsManager.Instance.SetRemoveAds(bool)` / `IsRemoveAds` | `JisAds.Instance.SetRemoveAds(bool)` / `IsRemoveAds` |
| `AdsManager.Instance.InitializeAllAsync(...)` | `JisAds.Instance.InitializeAsync(fetchRemoteConfig)` + `FetchAndApplyRemoteConfigAsync()` |
| `AdsManager.Instance.isCheatAds` | removed; keep a game-side debug flag |
| `AdsManager.AdsInitializationMode` | `JisSDKAds.Ads.Settings.AdsInitializationMode` (same values) |
| `SDKSetup.Setup()` (editor) | `SDKSetup.SyncMaxSdkKeyToAppLovinSettings()`, called by **Apply Settings to Scene** |
| Rewarded Interstitial (AdMob) | removed |

## Remote Config

`ads_interval` is no longer read. Interstitial frequency is controlled by
`inter_capping_from_app_open_seconds`, `inter_capping_between_shows_seconds` and
`inter_capping_after_load_fail_seconds`.

## Prefab

Existing `JisSDK_Manager` prefabs carry an `Ads_Runtime` subtree whose scripts no longer exist.
Select the prefab (or the scene object) and run **JIS SDK → Ads → Scene → Remove legacy Ads_Runtime**.
Set `JisSDKAdsSettings.interstitialLevelFallback` to the value previously stored in
`AdsManager.LevelPassToShowInterstitial`.
