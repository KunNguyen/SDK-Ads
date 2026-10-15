# Remove the legacy AdsManager runtime — design

Date: 2026-10-15
Status: approved in conversation, pending written review
Scope: SDK-Ads (all `com.jis.sdkads.*` packages), JIS-CardShop consumer update

## 1. Goal

`JisAds` becomes the only ads entry point. Everything that existed solely to serve the
legacy `AdsManager` front (unit managers, mediation controllers, callbacks, reflection
bridges, editor tooling that builds the legacy hierarchy) is deleted. The few features
game code still reads from `AdsManager` move to `JisAds` or `JisSDKAdsSettings`.

Why now: on the Core routing path the legacy objects never initialize, so they are dead
weight (about 5,100 runtime lines plus 2,000 editor lines) that confuses Remote Config
tuning (`ads_interval` is read only by the dead path) and doubles the API surface.

Consumers:

- JIS-CardShop: already calls `JisAds` for every show/load; touches `AdsManager` at six
  places for flags and consent. Updated in the same change set.
- idlestickman: `AdsBridge` already calls `JisAds` only. Pinned to a July SDK hash; when
  it bumps it only needs the legacy prefab subtree removed (migration menu provided).

Breaking change: package major bump to 6.0.0.

## 2. Delete from SDK runtime

`com.jis.sdkads.ads/Runtime`:

- `Ads/AdsManager/` — `AdsManager.cs`, `AdsManager_BannerAd.cs`, `AdsManager_EditorSetup.cs`,
  `AdsManager_InterstitialAd.cs`, `AdsManager_JisAdsBridge.cs`, `AdsManager_RewardedAd.cs`,
  `AdsManager_RewardedInterstitial.cs`, `AdsStateMachine.cs`
- `Ads/UnitAdManager/` — `UnitAdManager.cs`, `BannerAdManager.cs`, `InterstitialAdManager.cs`,
  `RewardAdManager.cs`, `AdsHelper.cs`, `Interface/*`, `Service/*` (`AutoLoadSystem`,
  `CooldownSystem`, `UnitCoroutineBehaviour`)
- `Ads/MediationManager/` — `AdsMediationController.cs`, `IAdMobConsentHost.cs`,
  `IAdMobRewardedInterstitialHost.cs`, `Callbacks/*`
- `Ads/AdsConfig.cs`, `Ads/AdsRemoteConfigBootstrap.cs`, `Ads/Configs/AdsManagerSDKSetupContainer.cs`
- `Ads/Placement/WatchVideoRewardType.cs`, `Ads/Placement/RewardAdsPlacementConfig.cs`
- `Ads/InterstitialTier/InterstitialTierCompat.cs`
- `Integration/MaxMediationReflection.cs`, `Integration/AdMobMediationReflection.cs`

`com.jis.sdkads.providers.max/Runtime`:

- `MaxMediationController.cs`, `MaxMediationController_TieredInterstitial.cs`,
  `MaxMediationController_TieredRewarded.cs`, `MaxMediationConfigBridge.cs`

`com.jis.sdkads.providers.admob/Runtime`:

- `AdmobMediationController.cs`, `AdmobMediationController_TieredInterstitial.cs`,
  `AdmobMediationController_TieredRewarded.cs`, `AdmobMediationController_RewardedInterstitial.cs`,
  `AdmobMediationConfigBridge.cs`

Dropped features (no Core equivalent, no consumer uses them): AdMob Rewarded Interstitial,
`AdsManager.IsFirstOpen`, `AdsManager.isCheatAds`, legacy lifecycle evaluation
(`ScheduleLifecycleEvaluation`), legacy `RewardAdsPlacementConfig` asset.

Kept (used by Core): `AdsType`, `AdsMediationType`, `SDKSetup`, `MaxAdSetup`, `AdmobAdSetup`,
`AdScheduleUnitID`, `PlatformAdsProfile`, `ProviderConfigFactory`, `AdsTracker`,
`AppsflyerManager`, `AdmobUmpConsent`, `AdMobMobileAdsInitializer`, `MaxSdkInitializer`,
everything under `Ads/SequentialTier`, `Ads/AppOpen`, `Ads/Resume`, `Ads/SDKSetup`.

Every deleted `.cs` takes its `.meta` with it.

## 3. Move into JisAds / JisSDKAdsSettings

- `AdsInitializationMode` enum: new file `Runtime/Settings/AdsInitializationMode.cs`,
  namespace `JisSDKAds.Ads.Settings`, same member order and values (`AutoOnStart = 0`,
  `Manual = 1`). `JisSDKAdsSettings.adsInitializationMode` keeps its field name so existing
  `.asset` files deserialize unchanged.
- `JisAds.IsPassLevelToShowInterstitial(int level)`: returns
  `level >= RC level_show_inter` when Remote Config is ready, else
  `level >= settings.interstitialLevelFallback`. New serialized field
  `JisSDKAdsSettings.interstitialLevelFallback` (int, default 3) replaces the value that
  lived on the `AdsManager` prefab component.
- `JisAds.ShowPrivacyOptionsForm()`: new reflection bridge
  `Runtime/Integration/ConsentFormBridge.cs` following `AdMobSdkEarlyInitBridge`. Routing:
  when the active profile mediation is MAX, or `JisSDKAdsSettings.ShouldDelegateConsentToMax()`
  is true (same rule as init-time consent), and `MaxSdk.CmpService` is available, call
  `MaxSdk.CmpService.ShowCmpForExistingUser`; otherwise call
  `AdmobUmpConsent.ShowPrivacyOptionsForm`. Errors are logged, never thrown.
- `JisAds.SetRemoveAds`: remove the mirror into `AdsManager`; the PlayerPrefs key
  `key_local_remove_ads` stays the single source of truth.
- `IapPurchaseIntegration`: remove the `AdsManager` fallback; `JisAds` only.
- `JisSDKAdsSettings.ApplyToAdsManager` and `SDKSetup.Setup()` (editor-only) removed.
- Comments in `AdInventoryRemoteConfigResolver` and `SequentialTierRemoteConfigResolver`
  that mention `AdsManager` updated.

## 4. Editor and Hub

Delete: `Editor/AdsManager/AdsManagerEditor.cs`, `Editor/AdsManager/AdsManagerSDKSetupUtility.cs`,
`Editor/AdsManager/AdsManagerSDKSetupContainerEditor.cs`, `Editor/Ads/SDKSetupEditor.cs`,
`Editor/Ads/RewardAdsPlacementConfigEditor.cs`, the `OpenRewardAdsPlacementConfig` command
in `JISEditorConfig.cs`, and the `JisSDKMenuPaths` entries `AdsLegacyCreateContainer`,
`AdsSceneReorganizeManager`, `AdsCreateRewardPlacements`.

Rework:

- `Editor/BuildHandler/AdsManagerAutoPlatformSetup.cs` → `JisAdsAutoPlatformSetup.cs`:
  keep the switch/play/build hooks and prefs, drop `TryApplyLegacyContainer*`; "apply"
  means `JisSDKAdsSettingsApplier.Apply` for the active build target.
- `Editor/Settings/JisSDKAdsSettingsApplier.cs`: drop the `AdsManager` branch; `Apply`
  = scripting defines + `SyncJisAdsInScene` + validation.
- `Editor/JisSDKSceneSetupBuilder.cs`: builds `JisSDK_Manager` with children `Firebase`
  (`FirebaseManager`, `AdsTracker`) and `JisAds` (`JisAds`, `ResumeAdCoordinator`). Drop
  `Ads_Runtime`, mediation controllers, unit managers and `WireAdsManager`. Add menu
  **JIS SDK → Ads → Scene → Remove legacy Ads_Runtime**: finds `Ads_Runtime` (or any object
  carrying a missing script where `AdsManager` used to be) under the selected or scene
  `JisSDK_Manager`, deletes the subtree, marks the scene/prefab dirty. Works on an open
  prefab stage too.
- `Editor/JisSDKScenePrefabUtility.cs`: `TryFocusExistingManager` looks for `JisAds`.
- `Hub/Editor/JisSDKHubWindow.cs`: description text only.

## 5. JIS-CardShop

- `InitSdkSceneController.cs`: `BootJisAdsCoreAsync` and `ApplyRemoteConfigToAds` keep only
  the `JisAds` branch.
- `JisSdkAdsGameCompat.cs`: remove every `AdsManager` parameter and call.
  `BypassRemoveAdsGate()` = `DebugManager.ForceAdsActiveIgnoreRemoveAds`;
  `SyncRemoveAds`, `HideBannerAds`, `TryInitializeBannerFormatsAfterUserSaveLoaded()` call
  `JisAds` only. Call sites in `GameplayManager`, `AdService`, `ShowRewardedAdsUseCase`,
  `IAPController` drop the argument.
- `AdService.CanShowInterstitial` → `JisAds.Instance.IsPassLevelToShowInterstitial(storeLevel)`.
- `ModalSettingContext` → `JisAds.Instance.ShowPrivacyOptionsForm()`.
- `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab`: remove the `Ads_Runtime` subtree
  (`AdsManager`, `Mediation/MaxMediation`, `Mediation/AdmobMediation`,
  `UnitAdManagers/Banner|Interstitial|Rewarded`). Keep `Firebase` and `JisAds`.
- `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`: `interstitialLevelFallback: 3`.
- `Packages/packages-lock.json`: bump the `com.jis.sdkads.*` hashes to the new SDK commit
  after SDK-Ads is pushed.
- The SDK-Ads test project's own `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab` gets the
  same subtree removal.

## 6. Docs and versions

- `docs/SCENE_SETUP.md`, `docs/GAME_SETUP.md`, `docs/ADS_USAGE_GUIDE.md`: remove the
  `AdsManager` hierarchy, modes table and "legacy access" notes; describe the two-child
  prefab and the `JisAds` APIs.
- New `docs/MIGRATION_V6.md`: removed types, renamed APIs
  (`AdsManager.IsPassLevelToShowInterstitial` → `JisAds.IsPassLevelToShowInterstitial`,
  `AdsManager.ShowConsentForm` → `JisAds.ShowPrivacyOptionsForm`, `isCheatAds` → game-side
  flag), the `Remove legacy Ads_Runtime` menu, and the `interstitialLevelFallback` setting.
- `package.json` versions: `ads`, `providers.max`, `providers.admob`, `editor`, `hub` → 6.0.0;
  their inter-package dependency ranges updated to match.

## 7. Verification

No automated test harness covers these classes, so verification is compile plus editor
smoke:

1. `dotnet build` for `JisSDKAds.Ads`, `JisSDKAds.Providers.Max`, `JisSDKAds.Providers.AdMob`,
   `JisSDKAds.Editor`, `JisSDKAds.Hub`, `JisSDKAds.IAP` in SDK-Ads: zero errors.
2. `grep -r "AdsManager" Packages --include=*.cs` returns only comments in docs/migration
   notes and nothing in code.
3. Unity (via MCP when bound to SDK-Ads): open `JisSDK_Manager.prefab`, confirm no missing
   scripts, run the `Remove legacy Ads_Runtime` menu on a copy of the old prefab and confirm
   the subtree disappears.
4. In JIS-CardShop after the lock bump: `dotnet build Assembly-CSharp.csproj` zero errors;
   open the prefab in Unity, no missing scripts; Play in editor reaches gameplay and
   `ShowInterAds` still routes to `JisAds` (editor path returns callbacks immediately).

Order of work: SDK runtime deletions and moves → editor/hub → docs/versions → commit and
push SDK-Ads → Card Shop code, prefab, settings, lock bump → commit.

## 8. Risks and mitigations

- Projects whose prefab still holds the legacy subtree see "missing script" components
  after the bump. Mitigation: `Remove legacy Ads_Runtime` menu + `MIGRATION_V6.md`.
- `adsInitializationMode` enum relocation: Unity serializes enums as ints under the field
  name, so assets keep their value; only C# references need the new type.
- `ShowPrivacyOptionsForm` on a MAX-consent game now opens the MAX CMP instead of AdMob UMP.
  This matches the consent flow the game actually runs; call it out in the migration note.
- `isCheatAds` removal changes debug-only behavior; Card Shop already has its own flag.
