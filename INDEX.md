# SDK-Ads — Project Index

**Unity Ads SDK** for mobile games. Unity 2021.3+, targeting Android & iOS with mediation (AdMob, AppLovin MAX, IronSource/LevelPlay).

---

## Root Structure

| Path | Description |
|------|--------------|
| `Assets/` | Main Unity assets, scripts, plugins |
| `Build/` | Build output |
| `Keystore/` | Android signing keys |
| `Library/` | Unity cache (generated) |
| `Logs/` | Build/log output |
| `Packages/` | Unity Package Manager |
| `ProjectSettings/` | Unity project config |
| `Release/` | Release builds |
| `UserSettings/` | User-specific settings |
| `.idea/` | JetBrains Rider/IDE config |

---

## Assets Overview

### Core SDK — `Assets/JisSDKAds/`

Main custom SDK for ads, IAP, analytics, and app review.

#### Editor (`JisSDKAds/Editor/`)
| File | Purpose |
|------|---------|
| `AppAds/AppAdsTxtMerger.cs` | Merges app-ads.txt files |
| `AdsManager/AdsManagerSDKSetupContainerEditor.cs` | Ads setup UI |
| `AdsManager/AdsManagerSDKSetupUtility.cs` | Ads setup helpers |
| `AdsManager/JISEditorConfig.cs` | Editor config |
| `BuildHandler/JISBuildHandler.cs` | Build pipeline |
| `BuildHandler/GradlePostProcessor.cs` | Android Gradle post-processing |
| `BuildHandler/AdsManagerAutoPlatformSetup.cs` | Platform-specific setup |

#### Runtime (`JisSDKAds/Runtime/`)

**Ads**
- `AdsManager/` — Ad type managers (Banner, Interstitial, Rewarded, App Open, MRec, Collapsible, Resume)
- `UnitAdManager/` — Unit managers (BannerAdManager, RewardAdManager, AppOpenAdManager, etc.)
- `MediationManager/` — AdMob, MAX, IronSource mediation
- `SDKSetup/` — Ad setup (RewardedAdSetup, InterstitialAdSetup, AppOpenAdSetup, etc.)
- `AdsConfig.cs`, `AdsHelper.cs`, `AdScheduleUnitID.cs`

**Analytics**
- `Analystic/FirebaseManager.cs` — Firebase integration
- `Analystic/FirebaseAnalyticsManager.cs`
- `Analystic/FirebaseRemoteConfigManager.cs`
- `Analystic/AdsTracker.cs`
- `Analystic/SolarEngine/SolarEngineManager.cs`

**IAP**
- `IAPServices/InAppPurchaser.cs`, `IAPService.cs`
- `IAPServices/IapCallbacks.cs`, `IAPLogger.cs`
- `IAPServices/Setup/IAPSetup.cs`

**Other**
- `AppReview/AppReviewManager.cs` — In-app review
- `Data/FirebaseDataManager.cs`
- `Manager/EventManager.cs`
- `Config/Keys.cs`
- `Utils/` — Yields, DebugAds, ScriptOrder, SymbolHelper
- `UI/PanelAdsTest.cs`

#### Plugins
- `Plugins/Sirenix/` — Odin Inspector

---

### Third-Party SDKs

| Path | SDK | Purpose |
|------|-----|---------|
| `Assets/AppsFlyer/` | AppsFlyer 6.15.3 | Attribution, ad revenue |
| `Assets/GoogleMobileAds/` | AdMob 9.4.0 | Google ads |
| `Assets/LevelPlay/` | AppLovin MAX 8.0.1 | Mediation |
| `Assets/Firebase/` | Firebase 12.4.1 | Analytics, Remote Config |
| `Assets/GooglePlayPlugins/` | Google Play | In-app review, App Bundle |
| `Assets/ExternalDependencyManager/` | EDM 1.2.185 | Dependency resolution |
| `Assets/Parse/` | Parse | Backend (if used) |
| `Assets/MeldAppAds/` | Meld | App ads config |
| `Assets/TextMesh Pro/` | TextMesh Pro | UI text |

---

### App Ads Config — `Assets/AppAds/`

| File | Purpose |
|------|---------|
| `app-ads.txt` | App-ads.txt (production) |
| `app-ads-d.txt` | Debug variant |
| `app-ads-m.txt` | Merged/alternate variant |
| `app-ads-merged.report.txt` | Merge report |

---

### Platform Plugins — `Assets/Plugins/`

- `Android/` — Gradle, manifest, Firebase
- `iOS/` — Firebase, native libs

---

### Other Assets

| Path | Contents |
|------|----------|
| `Assets/Editor/` | DisableBitcode, MobileDependencyResolver |
| `Assets/Editor Default Resources/` | Firebase editor icons |
| `Assets/GeneratedLocalRepo/` | Local Maven/Firebase repo |
| `Assets/JisSDKConfigs/` | SDK config assets |
| `Assets/Resources/` | Runtime resources |
| `Assets/Scenes/` | Unity scenes |
| `Assets/StreamingAssets/` | Streaming assets |

---

## Solution Projects (`SDK-Ads.sln`)

| Project | Role |
|---------|------|
| `JisSDKAds.Runtime` | Core SDK runtime |
| `JisSDKAds.Editor` | SDK editor tools |
| `JisSDKAds.Runtime.AdmobIntegration` | AdMob integration |
| `JisSDKAds.Runtime.MaxIntegration` | AppLovin MAX integration |
| `JisSDKAds.Runtime.Appsflyer` | AppsFlyer integration |
| `JisSDKAds.Runtime.IapIntegration` | IAP integration |
| `JisSDKAds.Editor.Admob` | AdMob editor setup |
| `AppsFlyer`, `AppsFlyer.Editor` | AppsFlyer SDK |
| `GoogleMobileAds.Editor` | AdMob editor |
| `Google.Android.AppBundle.Editor` | AAB build |
| `Google.Play.Common`, `Google.Play.Core`, `Google.Play.Review` | Play services |
| `MobileDependencyResolverLP.Installer.Editor` | LevelPlay resolver |
| `Unity.LevelPlay`, `Unity.LevelPlay.Editor` | LevelPlay SDK |
| `Sirenix.OdinInspector.Modules.UnityMathematics` | Odin Inspector |
| `Assembly-CSharp*` | Unity assemblies |

---

## Key Config Files

- `Assets/google-services.json` — Firebase config
- `ProjectSettings/ProjectSettings.asset` — Unity project settings
- `Packages/manifest.json` — UPM packages

---

## Build Notes (from README)

- Disable Auto Graphics API and Vulkan
- Enable x86 and x86-64 in Target Architectures
- Import FirebaseMessaging

---

*Generated index — SDK-Ads project*
