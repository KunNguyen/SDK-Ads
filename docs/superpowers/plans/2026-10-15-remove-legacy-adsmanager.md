# Remove Legacy AdsManager Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Delete the legacy `AdsManager` runtime and tooling so `JisAds` is the only ads entry point, then migrate JIS-CardShop to it.

**Architecture:** The Core path (`JisAds` → `AdManager` → providers) already owns every load/show. We delete the parallel legacy stack (unit managers, mediation controllers, callbacks, reflection bridges, editor scene builder branches), relocate three small features game code still reads (`AdsInitializationMode`, interstitial level gate, privacy-options form) onto `JisAds`/`JisSDKAdsSettings`, and ship an editor menu that strips the legacy prefab subtree from existing projects.

**Tech Stack:** Unity 2022.3 UPM packages (C#), Unity Editor API, `dotnet build` against Unity-generated csproj files, Python 3 for one prefab YAML prune script.

**Spec:** `docs/superpowers/specs/2026-10-15-remove-legacy-adsmanager-design.md`

## Global Constraints

- Package versions after this change: `com.jis.sdkads.ads`, `providers.max`, `providers.admob`, `editor`, `hub` → `6.0.0`.
- `JisSDKAdsSettings.adsInitializationMode` keeps its field name; enum values stay `AutoOnStart = 0`, `Manual = 1`.
- PlayerPrefs key `key_local_remove_ads` stays the single remove-ads source of truth.
- No new compile-time reference from `JisSDKAds.Ads` to `JisSDKAds.Providers.*`; cross-assembly calls go through `Type.GetType` reflection like `AdMobSdkEarlyInitBridge`.
- Every deleted `.cs` is deleted together with its `.meta`.
- Verification per spec §7: `dotnet build` of the SDK csproj files must report zero errors; no runtime code may still reference `AdsManager`.
- Commits end with `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.

## Review Focus

1. A project whose `JisSDKAdsSettings.asset` was saved with `adsInitializationMode: 1` must still deserialize as `Manual` after the enum moves (Task 1 Step 7 greps the asset and the applier mapping).
2. `IsPassLevelToShowInterstitial` before Remote Config is ready must use `interstitialLevelFallback`, not 0 (Task 2 Step 3 code; Task 5 Step 2 replaces the game fallback).
3. `ShowPrivacyOptionsForm` on a build without the MAX assembly must fall through to AdMob UMP, and with neither must call `onError`, never throw (Task 2 Step 4 code).
4. `Remove legacy Ads_Runtime` run on a prefab asset must save the asset, not only a scene copy (Task 3 Step 6 code handles asset, stage and scene).
5. The pruned `JisSDK_Manager.prefab` must leave the root transform's `m_Children` without a dangling fileID (Task 5 Step 5 script removes it and Step 6 greps for it).

---

### Task 1: Delete the legacy runtime and relocate `AdsInitializationMode`

**Files:**
- Delete (with `.meta`): everything listed in spec §2.
- Create: `Packages/com.jis.sdkads.ads/Runtime/Settings/AdsInitializationMode.cs`
- Modify: `Packages/com.jis.sdkads.ads/Runtime/Settings/JisSDKAdsSettings.cs` (enum field, remove `ApplyToAdsManager`)
- Modify: `Packages/com.jis.sdkads.ads/Runtime/JisAds.cs` (lines 334, 1313, 2134-2136)
- Modify: `Packages/com.jis.sdkads.ads/Runtime/Integration/IapPurchaseIntegration.cs:36-48`
- Modify: `Packages/com.jis.sdkads.ads/Runtime/Ads/SDKSetup/SDKSetup.cs` (remove `Setup()` editor method)
- Modify: comments in `AdInventoryRemoteConfigResolver.cs:89`, `SequentialTierRemoteConfigResolver.cs:71`

**Interfaces:**
- Produces: `JisSDKAds.Ads.Settings.AdsInitializationMode { AutoOnStart = 0, Manual = 1 }`; `JisSDKAdsSettings.adsInitializationMode` of that type.

- [ ] **Step 1: Delete legacy files**

```bash
cd /f/JustIdleGames/SDK-Ads
A=Packages/com.jis.sdkads.ads/Runtime
git rm -rq $A/Ads/AdsManager $A/Ads/UnitAdManager $A/Ads/MediationManager $A/Ads/Placement $A/Ads/InterstitialTier
git rm -q $A/Ads/AdsConfig.cs $A/Ads/AdsConfig.cs.meta $A/Ads/AdsRemoteConfigBootstrap.cs $A/Ads/AdsRemoteConfigBootstrap.cs.meta
git rm -rq $A/Ads/Configs
git rm -q $A/Integration/MaxMediationReflection.cs $A/Integration/MaxMediationReflection.cs.meta $A/Integration/AdMobMediationReflection.cs $A/Integration/AdMobMediationReflection.cs.meta
M=Packages/com.jis.sdkads.providers.max/Runtime
git rm -q $M/MaxMediationController.cs $M/MaxMediationController.cs.meta $M/MaxMediationController_TieredInterstitial.cs $M/MaxMediationController_TieredInterstitial.cs.meta $M/MaxMediationController_TieredRewarded.cs $M/MaxMediationController_TieredRewarded.cs.meta $M/MaxMediationConfigBridge.cs $M/MaxMediationConfigBridge.cs.meta
G=Packages/com.jis.sdkads.providers.admob/Runtime
git rm -q $G/AdmobMediationController.cs $G/AdmobMediationController.cs.meta $G/AdmobMediationController_TieredInterstitial.cs $G/AdmobMediationController_TieredInterstitial.cs.meta $G/AdmobMediationController_TieredRewarded.cs $G/AdmobMediationController_TieredRewarded.cs.meta $G/AdmobMediationController_RewardedInterstitial.cs $G/AdmobMediationController_RewardedInterstitial.cs.meta $G/AdmobMediationConfigBridge.cs $G/AdmobMediationConfigBridge.cs.meta
```

Before running, confirm `Ads/Configs` holds only `AdsManagerSDKSetupContainer.cs` and `Ads/Placement` only the two placement files (`ls`); if another file lives there, delete the listed files individually instead of the folder. Leave the now-empty folder `.meta` files to `git rm` as well (`git status` shows them).

- [ ] **Step 2: Create the enum file**

```csharp
// Packages/com.jis.sdkads.ads/Runtime/Settings/AdsInitializationMode.cs
namespace JisSDKAds.Ads.Settings
{
    /// <summary>
    /// How ads start. Manual: the game calls JisAds.InitializeAsync() from its loading flow.
    /// AutoOnStart: JisAds initializes itself in Start (prototypes only).
    /// Values are serialized as ints; keep the order.
    /// </summary>
    public enum AdsInitializationMode
    {
        AutoOnStart = 0,
        Manual = 1
    }
}
```

Create its `.meta` by copying a sibling meta and replacing the guid with a fresh one: `python -c "import uuid;print(uuid.uuid4().hex)"`.

- [ ] **Step 3: Update `JisSDKAdsSettings.cs`**

Replace the field (line 18-19):

```csharp
        [Tooltip("Manual (recommended): call JisAds.InitializeAsync() from loading — fetches Remote Config then inits ads. AutoOnStart: JisAds initializes itself on Start (prototypes only).")]
        public AdsInitializationMode adsInitializationMode = AdsInitializationMode.Manual;
```

Delete the whole `ApplyToAdsManager(AdsManager adsManager)` method (lines 192-217) and its summary comment. Keep `SyncAllProfileMediationToSdkSetups`.

- [ ] **Step 4: Remove legacy calls in `JisAds.cs`**

Line 334 block: delete the three lines

```csharp
            if (!_legacyBridgeNotified)
            {
                _legacyBridgeNotified = true;
                AdsManager.Instance?.NotifyJisAdsCoreReady();
            }
```

and the field `private bool _legacyBridgeNotified;`.

Line 1313: delete `AdsManager.Instance?.RefreshRemoteConfigDrivenSettings(notifyLegacySubscribers: false);` (inventory is already applied by `RecoverFullscreenPreloadsAfterRemoteConfigRefresh` → `ApplyRemoteAdInventoryFromConfig`).

Lines 2134-2136 in `SetRemoveAds`: delete

```csharp
            var legacy = AdsManager.Instance;
            if (legacy != null && legacy.IsRemoveAds != isRemove)
                legacy.SetRemoveAds(isRemove);
```

- [ ] **Step 5: Remove legacy fallbacks in `IapPurchaseIntegration.cs` and `SDKSetup.cs`**

`ApplyRemoveAds` becomes:

```csharp
        static void ApplyRemoveAds()
        {
            if (JisAds.Instance == null)
            {
                Debug.LogWarning("[JIS SDK] RemoveAds IAP purchased but no JisAds in scene.");
                return;
            }

            JisAds.Instance.SetRemoveAds(true);
        }
```

In `SDKSetup.cs` delete the `#if UNITY_EDITOR public void Setup() { ... } #endif` block (lines 56 to its closing `#endif`). Keep `ApplyScriptingDefines` and the AppLovin settings sync if they live outside `Setup()`; if the AppLovin settings sync is inside `Setup()`, move that `#if UNITY_AD_MAX` block into a new `public void SyncMaxSdkKeyToAppLovinSettings()` method that `JisSDKAdsSettingsApplier.Apply` calls (Task 3 Step 3).

Update the two comments:
- `AdInventoryRemoteConfigResolver.cs:89` → `/// Applies RC inventory mode to <see cref="SDKSetup"/> before Core init (JisAds).`
- `SequentialTierRemoteConfigResolver.cs:71` → `/// <summary>Applies RC + settings fallback to AdMob sequential tier configs (JisAds Core path).</summary>`

- [ ] **Step 6: Build the runtime projects**

Run: `cd /f/JustIdleGames/SDK-Ads && dotnet build JisSDKAds.Providers.Max.csproj -nologo -v q 2>&1 | grep -E "error|Build succeeded" ; dotnet build JisSDKAds.Providers.AdMob.csproj -nologo -v q 2>&1 | grep -E "error|Build succeeded"`
Expected: `Build succeeded.` twice. Any `error CS` naming a deleted type means a reference was missed; fix it in the file reported.

Note: the Unity-generated csproj still lists deleted files? No — Unity csproj files enumerate files explicitly. If `dotnet build` reports `CS2001 Source file ... could not be found`, remove those `<Compile Include=...>` lines from the csproj (they regenerate on the next Unity refresh).

- [ ] **Step 7: Check serialization and leftovers**

Run: `grep -rn "AdsManager" Packages --include=*.cs | grep -v "/Editor/" `
Expected: no output (editor is Task 3).
Run: `grep -n "adsInitializationMode" Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`
Expected: `adsInitializationMode: 1` (Manual) unchanged.

- [ ] **Step 8: Commit**

```bash
git add -A Packages/com.jis.sdkads.ads Packages/com.jis.sdkads.providers.max Packages/com.jis.sdkads.providers.admob
git commit -m "Ads: remove legacy AdsManager runtime, unit managers and mediation controllers

JisAds Core is the only ads path. Drops AdsManager (+partials), UnitAdManager
family, AdsMediationController + callbacks, Max/Admob mediation controllers and
config bridges, reflection bridges, AdsConfig, AdsRemoteConfigBootstrap,
AdsManagerSDKSetupContainer, reward placement config, rewarded interstitial.
AdsInitializationMode moves to JisSDKAds.Ads.Settings (same values).

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

### Task 2: Interstitial level gate and privacy-options form on `JisAds`

**Files:**
- Modify: `Packages/com.jis.sdkads.ads/Runtime/Settings/JisSDKAdsSettings.cs` (new field)
- Modify: `Packages/com.jis.sdkads.ads/Runtime/JisAds.cs` (two public methods, in `#region Standard formats` after `CanShowBannerAd`)
- Create: `Packages/com.jis.sdkads.ads/Runtime/Integration/ConsentFormBridge.cs`
- Create: `Packages/com.jis.sdkads.providers.max/Runtime/MaxConsentForm.cs`

**Interfaces:**
- Produces: `bool JisAds.IsPassLevelToShowInterstitial(int level)`; `void JisAds.ShowPrivacyOptionsForm(Action<string> onError = null, Action onDismiss = null)`; `int JisSDKAdsSettings.interstitialLevelFallback`.
- Consumes: `AdmobUmpConsent.ShowPrivacyOptionsForm(Action<string>, Action)` (exists); `MaxSdk.CmpService.ShowCmpForExistingUser(Action<MaxCmpError>)`.

- [ ] **Step 1: Add the settings field**

After `skipStartupAdLoadWhenRemoveAds` in `JisSDKAdsSettings.cs`:

```csharp
        [Tooltip("Store level required before gameplay interstitials, used until Remote Config key level_show_inter is available.")]
        [Min(0)] public int interstitialLevelFallback = 3;
```

- [ ] **Step 2: Add `MaxConsentForm` in the MAX provider assembly**

Check the MAX plugin API first: `grep -n "ShowCmpForExistingUser\|class MaxCmpError\|CmpService" Assets/MaxSdk/Scripts/*.cs Assets/MaxSdk/Scripts/**/*.cs | head`. Expect `MaxSdk.CmpService` (static) and `MaxCmpService.ShowCmpForExistingUser(Action<MaxCmpError>)` with `MaxCmpError.Message`.

```csharp
// Packages/com.jis.sdkads.providers.max/Runtime/MaxConsentForm.cs
#if UNITY_AD_MAX
using System;

namespace JisSDKAds.Providers.Max
{
    /// <summary>Re-opens the MAX CMP (Google UMP via MAX) for a user who already answered it.</summary>
    public static class MaxConsentForm
    {
        public static void ShowForExistingUser(Action<string> onError, Action onDismiss)
        {
            if (!MaxSdk.IsInitialized())
            {
                onError?.Invoke("MAX SDK not initialized.");
                return;
            }

            MaxSdk.CmpService.ShowCmpForExistingUser(error =>
            {
                if (error != null)
                    onError?.Invoke(error.Message);
                else
                    onDismiss?.Invoke();
            });
        }
    }
}
#endif
```

If the grep shows a different member name, use that name; the bridge only needs the two-argument static signature.

- [ ] **Step 3: Add `ConsentFormBridge` in the Ads assembly**

Confirm the AdMob namespace: `grep -n "^namespace" Packages/com.jis.sdkads.providers.admob/Runtime/AdmobUmpConsent.cs`. Use that namespace in `AdMobTypeName`.

```csharp
// Packages/com.jis.sdkads.ads/Runtime/Integration/ConsentFormBridge.cs
using System;
using System.Reflection;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;

namespace JisSDKAds.Ads.Integration
{
    /// <summary>
    /// Opens the privacy-options (consent) form of the active mediation without a compile-time
    /// reference to the provider assemblies. MAX profile → MAX CMP; otherwise AdMob UMP.
    /// </summary>
    internal static class ConsentFormBridge
    {
        const string MaxTypeName = "JisSDKAds.Providers.Max.MaxConsentForm, JisSDKAds.Providers.Max";
        const string AdMobTypeName = "<AdmobUmpConsent namespace>.AdmobUmpConsent, JisSDKAds.Providers.AdMob";

        public static void Show(JisSDKAdsSettings settings, Action<string> onError, Action onDismiss)
        {
            var useMax = settings?.GetActiveProfile()?.mediation == AdsMediationType.MAX;
            if (useMax && TryInvoke(MaxTypeName, "ShowForExistingUser", onError, onDismiss))
                return;

            if (TryInvoke(AdMobTypeName, "ShowPrivacyOptionsForm", onError, onDismiss))
                return;

            DebugAds.LogWarning("[JisAds] No consent form provider available (MAX / AdMob assembly not loaded).");
            onError?.Invoke("No consent form provider available.");
        }

        static bool TryInvoke(string typeName, string methodName, Action<string> onError, Action onDismiss)
        {
            var method = Type.GetType(typeName)?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return false;

            try
            {
                method.Invoke(null, new object[] { onError, onDismiss });
                return true;
            }
            catch (Exception e)
            {
                DebugAds.LogWarning($"[JisAds] Consent form call failed: {e.GetBaseException().Message}");
                onError?.Invoke(e.GetBaseException().Message);
                return true;
            }
        }
    }
}
```

- [ ] **Step 4: Add the two methods to `JisAds.cs`**

Insert after `public bool CanShowBannerAd() => IsBannerAdLoaded();` (inside `#region Standard formats`):

```csharp
        /// <summary>Gameplay interstitial level gate: RC key level_show_inter when ready, else settings fallback.</summary>
        public bool IsPassLevelToShowInterstitial(int level)
        {
            var required = settings != null ? settings.interstitialLevelFallback : 0;
            var firebase = FirebaseManager.Instance;
            if (firebase != null && firebase.IsRemoteConfigReady)
                required = (int)firebase.GetConfigDouble(Keys.key_remote_interstitial_level);
            return level >= required;
        }

        /// <summary>Re-opens the consent / privacy options form of the active mediation (MAX CMP or AdMob UMP).</summary>
        public void ShowPrivacyOptionsForm(Action<string> onError = null, Action onDismiss = null) =>
            ConsentFormBridge.Show(settings, onError, onDismiss);
```

`JisAds.cs` already imports `JisSDKAds.Ads.Integration`, `JisSDKAds.Common`, `JisSDKAds.Firebase` and `System`.

- [ ] **Step 5: Build**

Run: `dotnet build JisSDKAds.Providers.Max.csproj -nologo -v q 2>&1 | grep -E "error|Build succeeded"`
Expected: `Build succeeded.` If `MaxConsentForm.cs` is not in the csproj (Unity has not regenerated it), add `<Compile Include="Packages\com.jis.sdkads.providers.max\Runtime\MaxConsentForm.cs" />` next to the other Runtime entries so the build covers it, and likewise for `ConsentFormBridge.cs` in `JisSDKAds.Ads.csproj`.

- [ ] **Step 6: Commit**

```bash
git add Packages/com.jis.sdkads.ads/Runtime/Settings/JisSDKAdsSettings.cs Packages/com.jis.sdkads.ads/Runtime/JisAds.cs Packages/com.jis.sdkads.ads/Runtime/Integration/ConsentFormBridge.cs* Packages/com.jis.sdkads.providers.max/Runtime/MaxConsentForm.cs*
git commit -m "Ads: JisAds owns the interstitial level gate and the privacy-options form

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

### Task 3: Editor and Hub tooling

**Files:**
- Delete (with `.meta`): `Editor/AdsManager/AdsManagerEditor.cs`, `Editor/AdsManager/AdsManagerSDKSetupUtility.cs`, `Editor/AdsManager/AdsManagerSDKSetupContainerEditor.cs`, `Editor/Ads/SDKSetupEditor.cs`, `Editor/Ads/RewardAdsPlacementConfigEditor.cs` (all under `Packages/com.jis.sdkads.editor/`)
- Modify: `Editor/AdsManager/JISEditorConfig.cs` (remove `OpenRewardAdsPlacementConfig`)
- Modify: `Editor/JisSDKMenuPaths.cs`
- Rename+modify: `Editor/BuildHandler/AdsManagerAutoPlatformSetup.cs` → `JisAdsAutoPlatformSetup.cs` (keep the `.meta` guid by `git mv` on both files)
- Modify: `Editor/Settings/JisSDKAdsSettingsApplier.cs:60-70, 100`
- Rewrite: `Editor/JisSDKSceneSetupBuilder.cs`
- Modify: `Editor/JisSDKScenePrefabUtility.cs:90-104`
- Modify: `Packages/com.jis.sdkads.hub/Editor/JisSDKHubWindow.cs:315`

**Interfaces:**
- Produces: menu `JIS SDK/Ads/Scene/Remove legacy Ads_Runtime` (`JisSDKMenuPaths.AdsSceneRemoveLegacy`); `JisSDKSceneSetupBuilder.RemoveLegacyAdsRuntime()`.
- Consumes: `AdsInitializationMode` (Task 1), `JisSDKAdsSettingsApplier.Apply`.

- [ ] **Step 1: Delete editor files**

```bash
E=Packages/com.jis.sdkads.editor/Editor
git rm -q $E/AdsManager/AdsManagerEditor.cs $E/AdsManager/AdsManagerEditor.cs.meta $E/AdsManager/AdsManagerSDKSetupUtility.cs $E/AdsManager/AdsManagerSDKSetupUtility.cs.meta $E/AdsManager/AdsManagerSDKSetupContainerEditor.cs $E/AdsManager/AdsManagerSDKSetupContainerEditor.cs.meta $E/Ads/SDKSetupEditor.cs $E/Ads/SDKSetupEditor.cs.meta $E/Ads/RewardAdsPlacementConfigEditor.cs $E/Ads/RewardAdsPlacementConfigEditor.cs.meta
git mv $E/BuildHandler/AdsManagerAutoPlatformSetup.cs $E/BuildHandler/JisAdsAutoPlatformSetup.cs
git mv $E/BuildHandler/AdsManagerAutoPlatformSetup.cs.meta $E/BuildHandler/JisAdsAutoPlatformSetup.cs.meta
```

`AdsManagerSDKSetupUtility.cs` also hosted the `AdsSceneAddManager` / `GameObjectAddManager` menu items (lines 39-72). Re-home them before deleting: copy the two `[MenuItem]` methods `AddManagerPrefabToCurrentScene` and `AddManagerPrefabFromHierarchyContext` into `JisSDKScenePrefabUtility.cs` as-is (they call `JisSDKScenePrefabUtility.AddPrefabToActiveScene(JisSDKSceneSetupBuilder.PrefabAssetName)` or equivalent; keep their bodies verbatim).

- [ ] **Step 2: Menu paths and `JISEditorConfig`**

In `JisSDKMenuPaths.cs` delete `AdsCreateRewardPlacements`, `AdsSceneReorganizeManager`, `AdsLegacyRoot`, `AdsLegacyCreateContainer`; add:

```csharp
        public const string AdsSceneRemoveLegacy = AdsSceneRoot + "Remove legacy Ads_Runtime";
```

In `JISEditorConfig.cs` delete the `OpenRewardAdsPlacementConfig` method and its `[MenuItem]` attribute (lines 89-103).

- [ ] **Step 3: `JisSDKAdsSettingsApplier.cs`**

Replace lines 60-70 with:

```csharp
            activeSetup.SyncMaxSdkKeyToAppLovinSettings();
```

only if Task 1 Step 5 created that method; otherwise delete lines 60-70 outright. Replace line 100 with:

```csharp
                    settings.adsInitializationMode == AdsInitializationMode.AutoOnStart;
```

Remove `using JisSDKAds.Ads;` only if nothing else in the file needs it (`SDKSetup` and `AdsSetupUtility` live in `JisSDKAds.Ads`, so keep it).

- [ ] **Step 4: `JisAdsAutoPlatformSetup.cs`**

Replace the whole file:

```csharp
#if UNITY_EDITOR
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JisSDKAds.Editor
{
    /// <summary>Applies JisSDKAdsSettings (scripting defines + JisAds wiring) on platform switch, play and build.</summary>
    [InitializeOnLoad]
    public static class JisAdsAutoPlatformSetup
    {
        const string PrefSwitch = "SDK_ADS_AUTO_SETUP_ON_PLATFORM_SWITCH";
        const string PrefPlay = "SDK_ADS_AUTO_SETUP_ON_PLAY";
        const string PrefBuild = "SDK_ADS_AUTO_SETUP_ON_BUILD";

        const string MenuSwitch = JisSDKMenuPaths.AdsAutoApplyPlatformSwitch;
        const string MenuPlay = JisSDKMenuPaths.AdsAutoApplyOnPlay;
        const string MenuBuild = JisSDKMenuPaths.AdsAutoApplyOnBuild;
        const string MenuApplyNow = JisSDKMenuPaths.AdsAutoApplyNow;

        static JisAdsAutoPlatformSetup()
        {
            EnsureDefaultPrefs();
            EditorUserBuildSettings.activeBuildTargetChanged += OnActiveBuildTargetChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void EnsureDefaultPrefs()
        {
            if (!EditorPrefs.HasKey(PrefSwitch)) EditorPrefs.SetBool(PrefSwitch, true);
            if (!EditorPrefs.HasKey(PrefBuild)) EditorPrefs.SetBool(PrefBuild, true);
            if (!EditorPrefs.HasKey(PrefPlay)) EditorPrefs.SetBool(PrefPlay, true);
        }

        public static bool TryApplyForActiveBuildTarget(string reason, bool requireJisAdsInScene)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var settings = JisSDKAdsSettingsApplier.TryLoadDefaultSettings();
            if (settings == null)
            {
                Debug.LogWarning($"[JIS SDK] {reason}: No JisSDKAdsSettings asset found.");
                return false;
            }

            if (requireJisAdsInScene && Object.FindFirstObjectByType<JisSDKAds.Ads.JisAds>() == null)
            {
                Debug.Log($"[JIS SDK] {reason}: No JisAds in scene, skipped.");
                return false;
            }

            return JisSDKAdsSettingsApplier.Apply(settings, reason);
        }

        public static bool TryApplyForBuildTarget(BuildTarget target, string reason)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var settings = JisSDKAdsSettingsApplier.TryLoadDefaultSettings();
            if (settings == null)
                return false;

            var previous = EditorUserBuildSettings.activeBuildTarget;
            try
            {
                if (previous != target)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
                return JisSDKAdsSettingsApplier.Apply(settings, reason);
            }
            finally
            {
                if (previous != target)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(previous), previous);
            }
        }

        static void OnActiveBuildTargetChanged()
        {
            if (!EditorPrefs.GetBool(PrefSwitch, true)) return;
            EditorApplication.delayCall += () =>
                TryApplyForActiveBuildTarget("OnPlatformSwitch", requireJisAdsInScene: false);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(PrefPlay, true)) return;
            if (state == PlayModeStateChange.ExitingEditMode)
                TryApplyForActiveBuildTarget("OnPlay", requireJisAdsInScene: true);
        }

        [MenuItem(MenuSwitch, false, 300)]
        static void ToggleOnSwitch() => TogglePref(PrefSwitch, "Auto apply on platform switch");

        [MenuItem(MenuPlay, false, 301)]
        static void ToggleOnPlay() => TogglePref(PrefPlay, "Auto apply on play");

        [MenuItem(MenuBuild, false, 302)]
        static void ToggleOnBuild() => TogglePref(PrefBuild, "Auto apply on build");

        [MenuItem(MenuSwitch, true)]
        static bool ToggleOnSwitch_Validate()
        {
            Menu.SetChecked(MenuSwitch, EditorPrefs.GetBool(PrefSwitch, true));
            return true;
        }

        [MenuItem(MenuPlay, true)]
        static bool ToggleOnPlay_Validate()
        {
            Menu.SetChecked(MenuPlay, EditorPrefs.GetBool(PrefPlay, true));
            return true;
        }

        [MenuItem(MenuBuild, true)]
        static bool ToggleOnBuild_Validate()
        {
            Menu.SetChecked(MenuBuild, EditorPrefs.GetBool(PrefBuild, true));
            return true;
        }

        [MenuItem(MenuApplyNow, false, 303)]
        static void ApplyNow() =>
            TryApplyForActiveBuildTarget("ManualApplyNow", requireJisAdsInScene: false);

        static void TogglePref(string key, string label)
        {
            var enabled = !EditorPrefs.GetBool(key, true);
            EditorPrefs.SetBool(key, enabled);
            Debug.Log($"[JIS SDK] {label}: {(enabled ? "ON" : "OFF")}");
        }

        internal static bool IsBuildAutoApplyEnabled() => EditorPrefs.GetBool(PrefBuild, true);
    }

    class JisAdsAutoPlatformSetupPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!JisAdsAutoPlatformSetup.IsBuildAutoApplyEnabled()) return;
            JisAdsAutoPlatformSetup.TryApplyForBuildTarget(report.summary.platform, "PreBuild");
        }
    }
}
#endif
```

Then `grep -rn "AdsManagerAutoPlatformSetup" Packages --include=*.cs` and rename any remaining caller (the Hub may call `TryApplyForActiveBuildTarget`; the second parameter name changed but positional calls keep compiling).

- [ ] **Step 5: `JisSDKScenePrefabUtility.cs` and Hub text**

Replace lines 93-94:

```csharp
            var existingAds = UnityEngine.Object.FindFirstObjectByType<JisAds>(
                FindObjectsInactive.Include);
```

(`using JisSDKAds.Ads;` is already present because `AdsManager` was used.)

Hub line 315: `const string desc = "Ads runtime (JisAds Core). Enable MAX or AdMob below.";`

- [ ] **Step 6: Rewrite `JisSDKSceneSetupBuilder.cs`**

```csharp
#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Resume;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Builds the JIS SDK scene hierarchy: JisSDK_Manager → Firebase (FirebaseManager, AdsTracker)
    /// and JisAds (JisAds, ResumeAdCoordinator). Also strips the pre-6.0 legacy Ads_Runtime subtree.
    /// </summary>
    static class JisSDKSceneSetupBuilder
    {
        const string PrefabFolder = "Assets/JisSDKAds/Prefabs";
        public const string RootName = "JisSDK_Manager";
        public const string PrefabAssetName = "JisSDK_Manager";

        public const string IapRootName = "JisSDK_InAppPurchaser";
        public const string IapPrefabAssetName = "JisSDK_InAppPurchaser";
        const string FirebaseChildName = "Firebase";
        const string JisAdsChildName = "JisAds";
        public const string LegacyAdsRuntimeChildName = "Ads_Runtime";

        public static GameObject CreateManagerInScene()
        {
            if (JisSDKScenePrefabUtility.TryFocusExistingManager(out var existingRoot))
            {
                if (FindLegacyAdsRuntime(existingRoot) != null
                    && EditorUtility.DisplayDialog(
                        "JIS SDK Manager",
                        "This manager still carries the legacy Ads_Runtime subtree (AdsManager, mediation controllers, unit managers). Remove it?",
                        "Remove",
                        "Keep"))
                {
                    RemoveLegacyAdsRuntime(existingRoot);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }

                TryApplySceneSettings("Scene Setup Builder (existing)");
                return existingRoot;
            }

            var hierarchyRoot = BuildHierarchy();
            TryApplySceneSettings("Scene Setup Builder");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = hierarchyRoot;
            EditorGUIUtility.PingObject(hierarchyRoot);

            TrySavePrefabAsset(hierarchyRoot, PrefabAssetName);
            Debug.Log("[JIS SDK] Created JisSDK_Manager hierarchy (Firebase + JisAds).");
            return hierarchyRoot;
        }

        static GameObject BuildHierarchy()
        {
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create JIS SDK Manager");
            EnsurePersistentRootComponent(root);

            var firebaseGo = CreateChild(root.transform, FirebaseChildName);
            var firebase = firebaseGo.AddComponent<FirebaseManager>();
            firebaseGo.AddComponent<AdsTracker>();
            ConfigureFirebaseManualInit(firebase);

            var jisAdsGo = CreateChild(root.transform, JisAdsChildName);
            var jisAds = jisAdsGo.AddComponent<JisAds>();
            WireJisAds(jisAds);

            return root;
        }

        /// <summary>Removes the legacy Ads_Runtime subtree from a scene object, an open prefab stage, or a prefab asset.</summary>
        [MenuItem(JisSDKMenuPaths.AdsSceneRemoveLegacy, false, 202)]
        public static void RemoveLegacyAdsRuntimeMenu()
        {
            // 1) Prefab asset selected in the Project window.
            if (Selection.activeObject is GameObject selectedAsset
                && PrefabUtility.GetPrefabAssetType(selectedAsset) != PrefabAssetType.NotAPrefab
                && !selectedAsset.scene.IsValid())
            {
                var path = AssetDatabase.GetAssetPath(selectedAsset);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (RemoveLegacyAdsRuntime(contents))
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        Debug.Log($"[JIS SDK] Removed legacy Ads_Runtime from prefab {path}.");
                    }
                    else
                    {
                        Debug.Log($"[JIS SDK] No legacy Ads_Runtime found in {path}.");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                return;
            }

            // 2) Open prefab stage, or 3) the scene.
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            var root = stage != null
                ? stage.prefabContentsRoot
                : Selection.activeGameObject != null
                    ? Selection.activeGameObject.transform.root.gameObject
                    : Object.FindFirstObjectByType<JisAds>(FindObjectsInactive.Include)?.transform.root.gameObject;

            if (root == null)
            {
                EditorUtility.DisplayDialog("JIS SDK", "Select the JisSDK_Manager object or prefab first.", "OK");
                return;
            }

            if (!RemoveLegacyAdsRuntime(root))
            {
                Debug.Log($"[JIS SDK] No legacy Ads_Runtime found under {root.name}.");
                return;
            }

            if (stage != null)
                EditorSceneManager.MarkSceneDirty(stage.scene);
            else
                EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[JIS SDK] Removed legacy Ads_Runtime under {root.name}.");
        }

        public static bool RemoveLegacyAdsRuntime(GameObject root)
        {
            var legacy = FindLegacyAdsRuntime(root);
            if (legacy == null)
                return false;

            Undo.DestroyObjectImmediate(legacy);
            return true;
        }

        static GameObject FindLegacyAdsRuntime(GameObject root)
        {
            if (root == null)
                return null;

            foreach (Transform child in root.transform)
            {
                if (child.name == LegacyAdsRuntimeChildName)
                    return child.gameObject;
            }

            return null;
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

#if UNITY_IAP_ACTIVE
        public static GameObject CreateInAppPurchaserInScene()
        {
            if (JisSDKScenePrefabUtility.TryFocusExistingInAppPurchaser(out var existingGo))
                return existingGo;

            var root = new GameObject(IapRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create JisSDK InApp Purchaser");
            var purchaser = root.AddComponent<JisSDKAds.IAP.InAppPurchaser>();

            var config = AssetDatabase.LoadAssetAtPath<JisSDKAds.IAP.IAPPackageConfigs>(
                JisSDKIapSettingsMenu.DefaultPackagesConfigPath);
            if (config != null)
            {
                var so = new SerializedObject(purchaser);
                var prop = so.FindProperty("IapProductConfigs");
                if (prop != null)
                {
                    prop.objectReferenceValue = config;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = root;
            TrySavePrefabAsset(root, IapPrefabAssetName);
            Debug.Log($"[JIS SDK] Created {IapRootName} in scene.");
            return root;
        }
#endif

        static void WireJisAds(JisAds jisAds)
        {
            var settings = LoadSettingsAsset();

            if (jisAds.GetComponent<ResumeAdCoordinator>() == null)
                jisAds.gameObject.AddComponent<ResumeAdCoordinator>();

            var so = new SerializedObject(jisAds);
            var prop = so.FindProperty("settings");
            if (prop != null && settings != null)
                prop.objectReferenceValue = settings;

            var resumeProp = so.FindProperty("resumeCoordinator");
            if (resumeProp != null)
                resumeProp.objectReferenceValue = jisAds.GetComponent<ResumeAdCoordinator>();

            var autoInitProp = so.FindProperty("autoInitializeOnStart");
            if (autoInitProp != null && settings != null)
                autoInitProp.boolValue = settings.adsInitializationMode == AdsInitializationMode.AutoOnStart;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(jisAds);
        }

        static void ConfigureFirebaseManualInit(FirebaseManager firebase)
        {
            var firebaseSo = new SerializedObject(firebase);
            var firebaseInit = FindProperty(firebaseSo, "InitializationMode");
            if (firebaseInit != null)
            {
                firebaseInit.enumValueIndex = (int)FirebaseManager.FirebaseInitializationMode.Manual;
                firebaseSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static SerializedProperty FindProperty(SerializedObject so, string propertyName) =>
            so.FindProperty(propertyName) ?? so.FindProperty($"<{propertyName}>k__BackingField");

        static JisSDKAdsSettings LoadSettingsAsset() => JisSDKAdsSettingsApplier.TryLoadDefaultSettings();

        static void TryApplySceneSettings(string reason)
        {
            var adsSettings = LoadSettingsAsset();
            if (adsSettings != null)
                JisSDKAdsSettingsApplier.Apply(adsSettings, reason);
        }

        public static void EnsurePersistentRootComponent(GameObject root)
        {
            if (root == null) return;
            if (root.GetComponent<JisSDKPersistentRoot>() != null
                || root.GetComponent<Manager>() != null)
                return;

#if UNITY_IAP_ACTIVE
            if (root.GetComponent<JisSDKAds.IAP.InAppPurchaser>() != null)
                return;
#endif

            Undo.AddComponent<JisSDKPersistentRoot>(root);
        }

        static void TrySavePrefabAsset(GameObject root, string prefabName)
        {
            if (root == null)
                return;

            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds"))
                AssetDatabase.CreateFolder("Assets", "JisSDKAds");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/JisSDKAds", "Prefabs");

            var path = $"{PrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            // Save a scene-detached clone so the template prefab is not tied to the scene instance.
            var clone = Object.Instantiate(root);
            clone.name = prefabName;
            clone.transform.SetParent(null, false);

            try
            {
                using (new EditorLogNoiseFilter())
                {
                    PrefabUtility.SaveAsPrefabAsset(clone, path);
                    AssetDatabase.SaveAssets();
                }

                Debug.Log($"[JIS SDK] Saved prefab template to {path}");
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }
    }
}
#endif
```

`IsFlatLayout` and `ReorganizeFlatHierarchy` are gone; grep for callers (`grep -rn "IsFlatLayout\|ReorganizeFlatHierarchy" Packages --include=*.cs`) and delete the `AdsSceneReorganizeManager` menu method that called them (it lived in `AdsManagerSDKSetupUtility.cs`, already deleted).

- [ ] **Step 7: Build editor and hub**

Run: `dotnet build JisSDKAds.Editor.csproj -nologo -v q 2>&1 | grep -E "error|Build succeeded"; dotnet build JisSDKAds.Hub.csproj -nologo -v q 2>&1 | grep -E "error|Build succeeded"`
Expected: `Build succeeded.` for both. Fix csproj `<Compile Include>` entries for deleted/renamed/new files if `CS2001` appears.

Run: `grep -rn "AdsManager\|MediationController\|UnitAdManager" Packages --include=*.cs`
Expected: no output.

- [ ] **Step 8: Commit**

```bash
git add -A Packages/com.jis.sdkads.editor Packages/com.jis.sdkads.hub
git commit -m "Editor: scene builder creates Firebase + JisAds only; add Remove legacy Ads_Runtime menu

Drops the AdsManager inspector, legacy setup container tooling, reward placement
config menu and SDKSetup apply button. Auto platform setup now applies
JisSDKAdsSettings to JisAds.

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

### Task 4: Docs, versions, SDK test project prefab

**Files:**
- Modify: `docs/SCENE_SETUP.md`, `docs/GAME_SETUP.md`, `docs/ADS_USAGE_GUIDE.md`
- Create: `docs/MIGRATION_V6.md`
- Modify: `Packages/com.jis.sdkads.{ads,providers.max,providers.admob,editor,hub}/package.json`
- Modify: `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab` (SDK-Ads test project, untracked)

- [ ] **Step 1: Docs**

`docs/SCENE_SETUP.md`: replace the hierarchy tree (around line 14) with

```
JisSDK_Manager (JisSDKPersistentRoot)
├── Firebase   (FirebaseManager, AdsTracker)
└── JisAds     (JisAds, ResumeAdCoordinator)
```

Replace the row at line 30 with `| **JIS SDK → Ads → Scene → Add JisSDK Manager Prefab** | Create the hierarchy above and wire JisAds to JisSDKAdsSettings |`. Delete the init-mode table rows that mention `AdsManager` (lines 44-48) and keep only the Manual row: `| **Manual (default)** | Game calls JisAds.InitializeAsync() from loading | Remote Config fetched by the game, applied via JisAds.OnRemoteConfigFetched |`. Lines 52-58: replace `or AdsManager.Instance.IsReady` with nothing, delete the "serialized references" and "hierarchy is for clarity" bullets, add `- **JIS SDK → Ads → Scene → Remove legacy Ads_Runtime** strips the pre-6.0 subtree from an existing manager (scene, prefab stage or prefab asset).`

`docs/GAME_SETUP.md`: line 38 → `2. JisSDK_Manager prefab (Firebase + JisAds)`; line 46 → `4. **JIS SDK → Ads → Apply Settings to Scene** (syncs JisAds + scripting defines for active build target)`; delete lines 65-69 (`JisAds.Instance.Legacy` block and the obsolete note) and add `See MIGRATION_V6.md if the project still references AdsManager.`

`docs/ADS_USAGE_GUIDE.md`: open it, replace every `AdsManager.Instance.X(...)` call with the `JisAds.Instance` equivalent listed in `MIGRATION_V6.md`; delete sections that document unit managers, mediation controllers or `AdsConfig`.

`docs/MIGRATION_V6.md`:

```markdown
# Migrating to JIS SDK Ads 6.0

6.0 removes the legacy `AdsManager` runtime. `JisAds` is the only entry point.

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
```

- [ ] **Step 2: Versions**

In each listed `package.json` set `"version": "6.0.0"`. In `providers.max`, `providers.admob`, `editor`, `hub` set any `"com.jis.sdkads.ads": "5.x.y"` dependency to `"6.0.0"`; leave `core`, `common`, `firebase` dependencies as they are. Verify: `grep -n '"version"\|sdkads.ads"' Packages/com.jis.sdkads.{ads,providers.max,providers.admob,editor,hub}/package.json`.

- [ ] **Step 3: SDK test project prefab**

Run the prune script from Task 5 Step 5 against `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab` in SDK-Ads (same prefab layout). Verify with `grep -c "Ads_Runtime\|AdsManager\|3d568bc3f02e48c08fa6180b05a5f070" Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab` → `0`.

- [ ] **Step 4: Commit**

```bash
git add docs/SCENE_SETUP.md docs/GAME_SETUP.md docs/ADS_USAGE_GUIDE.md docs/MIGRATION_V6.md Packages/com.jis.sdkads.*/package.json
git commit -m "Docs + versions: 6.0.0, migration guide for the AdsManager removal

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

The test-project prefab stays uncommitted together with the other untracked test-project files (not part of the package).

### Task 5: JIS-CardShop migration

**Files (all under `F:\JustIdleGames\JIS-CardShop`):**
- Modify: `Assets/_Game/Scripts/Loading/Scenes/InitSdkSceneController.cs:88-102, 156-168`
- Rewrite: `Assets/_Game/Scripts/Utility/JisSdkAdsGameCompat.cs`
- Modify: `Assets/_Game/Scripts/Manager/GameplayController/Services/AdService.cs:113-125`
- Modify: `Assets/_Game/Scripts/Manager/GameplayController/GameplayManager.cs:629-630`
- Modify: `Assets/_Game/Scripts/Manager/GameplayController/UseCase/ShowRewardedAdsUseCase.cs:38`
- Modify: `Assets/_Game/TanThanh/Scripts/ShopFeature/IAPFeature/IAPController.cs:114`
- Modify: `Assets/_Game/TanThanh/Scripts/SettingFeature/ModalSettingContext.cs:478`
- Modify: `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab`, `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`
- Modify: `Packages/packages-lock.json` (after SDK push)

**Interfaces:**
- Consumes: `JisAds.IsPassLevelToShowInterstitial`, `JisAds.ShowPrivacyOptionsForm` (Task 2).

- [ ] **Step 1: `InitSdkSceneController.cs`**

`BootJisAdsCoreAsync` becomes:

```csharp
        static async UniTask BootJisAdsCoreAsync()
        {
            if (JisAds.Instance == null)
            {
                Debug.LogWarning("[InitSdk] JisAds missing in scene — ads boot skipped.");
                return;
            }

            Debug.Log("[InitSdk] JisAds InitializeAsync (background, no RC)");
            await JisAds.Instance.InitializeAsync(fetchRemoteConfig: false).AsUniTask();
        }
```

`BootRemoteConfigAndApplyAsync`: delete the legacy branch (lines 130-139: `EnsureFirebaseManager(); await FirebaseManager.Instance.InitAsync(); ... ApplyRemoteConfigToAds();`) so after the `if (JisAds.Instance != null) { ... return; }` block the method only logs `Debug.LogWarning("[InitSdk] JisAds missing — Remote Config not applied to ads.");`. `ApplyRemoteConfigToAds` keeps only the `JisAds.Instance.OnRemoteConfigFetched()` call (delete `AdsManager.Instance?.RefreshRemoteConfigDrivenSettings();`). Delete `EnsureFirebaseManager` if nothing else calls it.

- [ ] **Step 2: Rewrite `JisSdkAdsGameCompat.cs`**

```csharp
using System;
using JisSDKAds.Ads;
using JisSDKAds.Common;
using UnityEngine;

namespace _Game.Scripts.Utility
{
    /// <summary>Game-side helpers around JisAds (remove-ads sync, banner bootstrap, rearm).</summary>
    public static class JisSdkAdsGameCompat
    {
        public const string KeyLocalRemoveAds = Keys.key_local_remove_ads;

        /// <summary>True sau lần sync remove-ads đầu tiên (IAP save đã load).</summary>
        public static bool IsRemoveAdsSynced { get; private set; }

        /// <summary>Trạng thái remove-ads thực tế (Remove Ads/VIP, đã tính debug force-ads) ở lần sync gần nhất.</summary>
        public static bool IsRemoveAdsEffective { get; private set; }

        public static event Action RemoveAdsStateSynced;

        // Project tắt domain reload khi vào Play Mode -> reset static mỗi lần Play
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsRemoveAdsSynced = false;
            IsRemoveAdsEffective = false;
            RemoveAdsStateSynced = null;
        }

        /// <summary>Debug override: ads stay active even when Remove Ads/VIP is owned.</summary>
        public static bool BypassRemoveAdsGate() => DebugManager.ForceAdsActiveIgnoreRemoveAds;

        /// <summary>Sync remove-ads flag to JisAds.</summary>
        public static void SyncRemoveAds(bool isRemove) => JisAds.Instance?.SetRemoveAds(isRemove);

        /// <summary>
        /// Re-apply the effective remove-ads state to JisAds, honoring the debug force-ads override.
        /// When the override is on, ads stay active even if Remove Ads/VIP is owned (the saved IAP
        /// entitlement is not modified).
        /// </summary>
        public static void SyncRemoveAdsStateToAdsManager()
        {
            var isRemoveAds = BanThanh.IAPDataSave.Function.IsRemoveAds()
                              && !DebugManager.ForceAdsActiveIgnoreRemoveAds;
            SyncRemoveAds(isRemoveAds);

            IsRemoveAdsEffective = isRemoveAds;
            IsRemoveAdsSynced = true;
            RemoveAdsStateSynced?.Invoke();
        }

        /// <summary>After IAP save is loaded, request banner ads if the user has not removed ads and has finished the tutorial.</summary>
        public static void TryInitializeBannerFormatsAfterUserSaveLoaded()
        {
            if (JisAds.Instance == null || JisAds.Instance.IsRemoveAds)
                return;

            // User mới: không hiện banner cho tới khi xong tutorial (SetPassAllTutorial(true) sẽ gọi ShowBannerAds).
            if (!BanThanh.TutorialDataSave.Function.IsPassedAllTutorial())
                return;

            JisAds.Instance.ShowBannerAds();
        }

        public static void HideBannerAds() => JisAds.Instance?.HideBannerAds();

        /// <summary>Re-arm interstitial/rewarded after offline play or tier ladder give-up.</summary>
        public static void TryRearmFullscreenAdsAfterGameplayReady()
        {
            if (JisAds.Instance == null || !JisAds.Instance.CanShowAds())
                return;

            JisAds.Instance.ForceRearmInterstitialLoad();
            JisAds.Instance.ForceRearmRewardedLoad();
        }
    }
}
```

- [ ] **Step 3: Call sites**

- `AdService.cs:115` → `var bypassRemoveAdsGate = JisSdkAdsGameCompat.BypassRemoveAdsGate();`
- `AdService.cs:119-124` → replace the Firebase lookup with
  ```csharp
            if (JisAds.Instance == null)
                return false;
            return JisAds.Instance.IsPassLevelToShowInterstitial(storeLevel);
  ```
  and drop the now-unused `using JisSDKAds.Common;` / `using JisSDKAds.Firebase;` if nothing else in the file uses them.
- `GameplayManager.cs:629` → `JisSdkAdsGameCompat.TryInitializeBannerFormatsAfterUserSaveLoaded();`; `:630` → `var bypassRemoveAdsGate = JisSdkAdsGameCompat.BypassRemoveAdsGate();`
- `ShowRewardedAdsUseCase.cs:38` → `var bypassRemoveAdsGate = JisSdkAdsGameCompat.BypassRemoveAdsGate();`
- `IAPController.cs:114` → `JisSdkAdsGameCompat.TryInitializeBannerFormatsAfterUserSaveLoaded();`
- `ModalSettingContext.cs:478` → `JisAds.Instance?.ShowPrivacyOptionsForm();` (add `using JisSDKAds.Ads;` if missing).

Run: `grep -rn "AdsManager" Assets/_Game --include=*.cs` → expected: no output.

- [ ] **Step 4: Settings asset**

In `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset` add the line `  interstitialLevelFallback: 3` right after `  skipStartupAdLoadWhenRemoveAds: 1`.

- [ ] **Step 5: Prune the prefab**

Save as `E:\Temp\claude\...\scratchpad\prune_prefab.py` (outside the repo) and run with `python -I prune_prefab.py <prefab path> Ads_Runtime`:

```python
import re, sys

path, target_name = sys.argv[1], sys.argv[2]
text = open(path, encoding="utf-8").read()
parts = re.split(r"(?m)^(?=--- !u!)", text)
header, blocks = parts[0], parts[1:]

def block_id(b):
    return re.match(r"--- !u!\d+ &(\d+)", b).group(1)

by_id = {block_id(b): b for b in blocks}

def ids_in(section_text):
    return re.findall(r"\{fileID: (\d+)\}", section_text)

def field(b, name):
    m = re.search(rf"(?m)^  {name}:(.*?)(?=^  \w|\Z)", b, re.S)
    return m.group(1) if m else ""

# find the GameObject block with the target name
go_id = next(i for i, b in by_id.items() if b.startswith("--- !u!1 ") and f"m_Name: {target_name}\n" in b)

doomed = set()
def visit_go(gid):
    doomed.add(gid)
    go = by_id[gid]
    for cid in ids_in(field(go, "m_Component")):
        doomed.add(cid)
        comp = by_id.get(cid, "")
        if comp.startswith("--- !u!4 "):
            for child_tr in ids_in(field(comp, "m_Children")):
                child_go = ids_in(field(by_id[child_tr], "m_GameObject"))[0]
                visit_go(child_go)

visit_go(go_id)

target_tr = next(cid for cid in ids_in(field(by_id[go_id], "m_Component")) if by_id[cid].startswith("--- !u!4 "))
parent_tr = ids_in(field(by_id[target_tr], "m_Father"))[0]

kept = []
for b in blocks:
    bid = block_id(b)
    if bid in doomed:
        continue
    if bid == parent_tr:
        b = re.sub(rf"(?m)^  - \{{fileID: {target_tr}\}}\n", "", b)
    kept.append(b)

open(path, "w", encoding="utf-8", newline="\n").write(header + "".join(kept))
print(f"removed {len(doomed)} blocks, unlinked transform {target_tr} from {parent_tr}")
```

Run it on `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab`.

- [ ] **Step 6: Verify the prefab**

Run: `grep -c "Ads_Runtime\|UnitAdManagers\|MaxMediation\|AdmobMediation\|3d568bc3f02e48c08fa6180b05a5f070" Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab` → `0`.
Run: `python -I -c "import re,sys;t=open(sys.argv[1],encoding='utf-8').read();ids=set(re.findall(r'--- !u!\d+ &(\d+)',t));refs=set(re.findall(r'\{fileID: (\d+)\}',t))-{'0'};print('dangling',sorted(refs-ids))" Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab` → `dangling []` (ignore ids that also appear as `m_PrefabInstance`/`m_CorrespondingSourceObject` targets if the prefab is a variant; this one is not).

- [ ] **Step 7: Build the game assembly**

Run: `cd /f/JustIdleGames/JIS-CardShop && dotnet build Assembly-CSharp.csproj -nologo -v q 2>&1 | grep -E "error|Build succeeded"`
Expected: `Build succeeded.` The game csproj references the SDK package sources from `Library/PackageCache` (git packages); until the lock bump (Step 9) it still compiles against the old SDK, so `JisAds.IsPassLevelToShowInterstitial` is unresolved at this point. If so, finish Step 9 first and rerun; otherwise temporarily point `JisSDKAds.Ads.csproj` references to the SDK-Ads folder for the check.

- [ ] **Step 8: Commit game code (without lock bump)**

```bash
git add Assets/_Game Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset
git commit -m "Ads: migrate to JisAds-only SDK 6.0 (drop AdsManager calls, prune legacy prefab subtree)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

- [ ] **Step 9: Push SDK-Ads and bump the lock (needs user go-ahead for the push)**

After `git push origin main` in SDK-Ads, take `git rev-parse HEAD` and in JIS-CardShop `Packages/packages-lock.json` set the `"hash"` of every `com.jis.sdkads.*` entry whose `"version"` points at `SDK-Ads.git` to that commit. Reopen the project in Unity (or `Library/PackageCache` refresh) so the package cache updates, then rerun Step 7 and commit `Packages/packages-lock.json`:

```bash
git add Packages/packages-lock.json
git commit -m "Ads: bump JIS SDK to 6.0.0 (<hash>)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

## Self-review notes

- Spec §2 deletions: Task 1 Step 1 and Task 3 Step 1 cover every listed file.
- Spec §3 moves: enum (Task 1), level gate + consent + settings field (Task 2), remove-ads mirror, IAP fallback, `ApplyToAdsManager`, `SDKSetup.Setup` (Task 1), comments (Task 1).
- Spec §4: Task 3; `AdsSceneAddManager`/`GameObjectAddManager` menu re-homing added because deleting `AdsManagerSDKSetupUtility.cs` would otherwise remove the "Add JisSDK Manager Prefab" menu.
- Spec §5: Task 5. Spec §6: Task 4. Spec §7 verification: build steps in each task plus the greps.
- Names used consistently: `IsPassLevelToShowInterstitial`, `ShowPrivacyOptionsForm`, `interstitialLevelFallback`, `AdsInitializationMode`, `RemoveLegacyAdsRuntime`, `JisAdsAutoPlatformSetup`, `BypassRemoveAdsGate()`, `TryInitializeBannerFormatsAfterUserSaveLoaded()`.
