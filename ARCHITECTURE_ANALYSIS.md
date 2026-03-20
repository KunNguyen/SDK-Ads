# SDK-Ads — UPM Packaging Readiness Analysis

**Analyst:** Senior Unity SDK Architect  
**Date:** March 20, 2025  
**Scope:** Full codebase analysis for UPM package conversion

---

## 1. Architecture Overview

### 1.1 High-Level Module Map

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         JisSDKAds (Monolithic Core)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  AdsManager (Singleton)                                                     │
│  ├── Depends on: FirebaseManager, AdsStateMachine, EventManager             │
│  ├── Blocks init until: FirebaseManager.IsFirebaseReady                      │
│  └── Tracks via: AdsTracker, ABIAppsflyerManager, SolarEngineManager        │
├─────────────────────────────────────────────────────────────────────────────┤
│  Ads (Mediation)          │  Analytics/Tracking   │  IAP                    │
│  ├── AdmobMediationCtrl   │  ├── FirebaseManager  │  ├── InAppPurchaser     │
│  ├── MaxMediationCtrl     │  ├── AdsTracker       │  ├── IAPService         │
│  ├── IronSourceMediation  │  ├── AppsflyerManager │  └── IapCallbacks        │
│  └── UnitAdManagers       │  └── SolarEngine      │  (Unity.Purchasing)      │
└─────────────────────────────────────────────────────────────────────────────┘
         │                            │                        │
         ▼                            ▼                        ▼
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────────┐
│ Integration     │    │ Firebase (mandatory)   │    │ BanThanh (game-      │
│ Assemblies      │    │ - Analytics           │    │  specific namespace) │
│ (empty, bridge) │    │ - RemoteConfig        │    │ - PurchasedData      │
│ - Admob         │    │ - Crashlytics (opt)   │    │ - InternetChecker   │
│ - Max           │    │ - Messaging (opt)     │    └─────────────────────┘
│ - Appsflyer     │    └──────────────────────┘
│ - IAP           │
└─────────────────┘
```

### 1.2 Core Modules & Responsibilities

| Module | Location | Responsibility | Dependencies |
|--------|----------|----------------|--------------|
| **AdsManager** | Runtime/Ads/AdsManager/ | Central ads orchestration, lifecycle, mediation routing | FirebaseManager, EventManager, AdsTracker, all UnitAdManagers |
| **Mediation Controllers** | Runtime/Ads/MediationManager/ | Ad network abstraction (AdMob, MAX, IronSource) | GoogleMobileAds, MaxSdk, IronSource |
| **UnitAdManagers** | Runtime/Ads/UnitAdManager/ | Per-format ad logic (Banner, Interstitial, Rewarded, etc.) | Mediation controllers, FirebaseManager (RemoteConfig) |
| **AdsTracker** | Runtime/Analystic/ | Ad impression & event tracking | FirebaseManager, ABIAppsflyerManager |
| **FirebaseManager** | Runtime/Analystic/ | Analytics, RemoteConfig, init gate | Firebase SDK, EventManager |
| **InAppPurchaser** | Runtime/IAPServices/ | Unity IAP wrapper | Unity.Purchasing, BanThanh.PurchasedData |
| **EventManager** | Runtime/Manager/ | Global event bus | None (core) |

### 1.3 Dependency Graph (Critical Paths)

- **Ads → Analytics:** AdsManager blocks on `FirebaseManager.IsFirebaseReady` before any ad init. AdsTracker, AppsflyerManager called directly from AdsManager/UnitAdManagers.
- **Ads → IAP:** Loose (RemoveAds flag, no direct calls).
- **IAP → Analytics:** IAPLogger; no Firebase/AppsFlyer coupling in IAP.
- **Analytics → Ads:** None (one-way: Ads → Analytics).

### 1.4 Assembly Definition Layout

| Assembly | Purpose | References | Notes |
|----------|---------|------------|-------|
| **JisSDKAds.Runtime** | Core SDK | Integration asmdefs (GUID), Odin | Contains ALL runtime logic |
| **JisSDKAds.Editor** | Editor tools | Runtime | Build handlers, setup UI |
| **JisSDKAds.Runtime.AdmobIntegration** | Dependency bridge | Runtime, **GoogleMobileAds.Editor** | **Empty** — no .cs files |
| **JisSDKAds.Runtime.MaxIntegration** | Dependency bridge | Runtime, MaxSdk.Scripts | Empty |
| **JisSDKAds.Runtime.Appsflyer** | Dependency bridge | Runtime | Empty |
| **JisSDKAds.Runtime.IapIntegration** | Dependency bridge | Runtime, Unity.Purchasing.* | Empty |
| **JisSDKAds.Editor.Admob** | AdMob editor | Editor, GoogleMobileAds.Editor | Has code |

---

## 2. Problems / Risks

### 2.1 Critical (Blockers for UPM)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| 1 | **Runtime references Firebase.Editor.dll** | JisSDKAds.Runtime.asmdef | Runtime must not reference Editor DLLs — causes build failures on device |
| 2 | **Runtime.AdmobIntegration references GoogleMobileAds.Editor** | AdmobIntegration.asmdef | Runtime assembly depending on Editor assembly — invalid |
| 3 | **Game-specific namespace `BanThanh`** | InAppPurchaser.cs, InternetChecker.cs, PurchasedData.cs | Package cannot depend on consumer project types |
| 4 | **Hardcoded asset paths** | RewardAdsPlacementConfig.cs, SDKSetup.cs | `Assets/ABIMaxSDKAds/...` — wrong for UPM (package path differs) |
| 5 | **FirebaseDataManager → GiftCodeManager** | FirebaseDataManager.cs | References non-existent/game-specific type |
| 6 | **No package.json at package root** | — | JisSDKAds has package.json but unity: "6000.2.12f" — incompatible with 2021.3 project |
| 7 | **Third-party SDKs in Assets** | AppsFlyer, GoogleMobileAds, Firebase, LevelPlay, etc. | UPM package cannot bundle these; must be optional dependencies |

### 2.2 High (Architecture / Quality)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| 8 | **Firebase mandatory for Ads** | AdsManager.CoWaitForFirebaseInitialization | Ads cannot run without Firebase — no abstraction |
| 9 | **Inconsistent namespaces** | Throughout | Mix of `SDK`, `ABIMaxSDKAds.Scripts`, `SDK.AdsManagers`, `SDK.IAP`, `BanThanh` |
| 10 | **Tight coupling Ads ↔ Tracking** | AdsManager, AdsTracker, AppsflyerManager | Direct static calls; no IAnalyticsProvider interface |
| 11 | **Integration assemblies empty** | Runtime/Integrations/*/ | All mediation code in main Runtime; integrations only pull DLLs |
| 12 | **Odin Inspector bundled** | Plugins/Sirenix/ | Commercial dependency; problematic for public UPM |
| 13 | **IAPService uses Unity.Services.Core** | IAPService.cs | Different from Unity.Purchasing; unclear purpose |

### 2.3 Medium (UPM Structure)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| 14 | **No Samples folder** | — | UPM best practice; no demo scene/scripts |
| 15 | **Prefabs reference wrong paths** | Manager.prefab, AdsManager.prefab | May break when moved to Packages/ |
| 16 | **Scenes in package** | JisSDKAds/Scenes/ | FirstScene.unity — demo or required? |
| 17 | **com.opeious.pokemon3dstounity in manifest** | Packages/manifest.json | Unrelated game package in project |
| 18 | **package.json unity: 6000.2.12f** | JisSDKAds/package.json | README says 2021.3+ — version mismatch |

### 2.4 Low (Maintainability)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| 19 | **Typo: Analystic** | Runtime/Analystic/ | Should be Analytics |
| 20 | **Vietnamese comments** | README, some code | Minor for localization |
| 21 | **Duplicate Release/package.json** | Release/package.json | Different name (com.abi.sdk.Package) |

---

## 3. Refactoring Suggestions (Prioritized)

### P0 — Must Fix Before UPM

1. **Remove Firebase.Editor.dll from Runtime asmdef**  
   - Delete from precompiledReferences in JisSDKAds.Runtime.asmdef.

2. **Fix AdmobIntegration assembly**  
   - Remove `GoogleMobileAds.Editor` reference from Runtime.AdmobIntegration.  
   - Ensure only runtime GoogleMobileAds DLLs are referenced.

3. **Extract BanThanh to SDK namespace**  
   - Move `PurchasedData`, `PurchasedDataList` to `SDK.IAP` (or similar).  
   - Remove `using BanThanh` from InAppPurchaser, InternetChecker.

4. **Fix or remove FirebaseDataManager**  
   - Remove `GiftCodeManager.Instance.Test()` call or gate behind optional define.  
   - Consider moving Firebase Database to optional integration.

5. **Replace hardcoded paths**  
   - Use `AssetDatabase.FindAssets` or package-relative paths.  
   - RewardAdsPlacementConfig: generate to package-relative path.  
   - SDKSetup error: reference correct Manager prefab path.

### P1 — Architecture for Reusability

6. **Introduce IAnalyticsProvider / ITrackingService**  
   - AdsManager, AdsTracker should depend on interface, not FirebaseManager/AppsflyerManager directly.  
   - Allow consumers to inject custom analytics.

7. **Make Firebase optional**  
   - Extract Firebase init/tracking to `JisSDKAds.Runtime.FirebaseIntegration`.  
   - AdsManager: support init without Firebase (or with stub).

8. **Unify namespaces**  
   - Standardize on `JisSDKAds` or `SDK` (single root).  
   - Remove `ABIMaxSDKAds.Scripts` references.

9. **Document optional dependencies**  
   - List in package.json: Firebase, AppsFlyer, AdMob, MAX, Unity IAP as optional.  
   - Use asmdef defineConstraints correctly for each.

### P2 — UPM Package Structure

10. **Restructure folders for UPM**  
    ```
    com.jis.sdkads/
    ├── package.json
    ├── Runtime/
    │   ├── JisSDKAds.Runtime.asmdef
    │   └── ...
    ├── Editor/
    │   ├── JisSDKAds.Editor.asmdef
    │   └── ...
    ├── Plugins/           (or move Odin to optional)
    └── Samples~
        └── Demo/
            ├── Scenes/
            └── Scripts/
    ```

11. **Add Samples**  
    - Minimal demo scene showing Ads + IAP + basic tracking.  
    - README with setup steps.

12. **Fix package.json**  
    - Set `unity` to `2021.3` (or range).  
    - Add `dependencies` for packages that can be UPM (e.g. `com.unity.purchasing`).

### P3 — Polish

13. **Rename Analystic → Analytics**  
14. **Clarify IAPService vs InAppPurchaser** — consolidate or document.  
15. **Remove or isolate Odin Inspector** — optional add-on or replace with PropertyDrawers.

---

## 4. UPM Readiness Score

| Category | Score | Notes |
|----------|-------|-------|
| **Folder structure** | 5/10 | Has Runtime/Editor/Integrations but mixed with third-party, no Samples |
| **Assembly definitions** | 4/10 | Present but Runtime has Editor refs; integration asmdefs misconfigured |
| **Dependency management** | 3/10 | Firebase/Odin hardcoded; game-specific (BanThanh); third-party in Assets |
| **Decoupling** | 3/10 | Ads ↔ Firebase ↔ Tracking tightly coupled; no interfaces |
| **Namespace/organization** | 4/10 | Inconsistent; legacy ABIMaxSDKAds mixed with SDK |
| **Documentation** | 5/10 | README has build notes; no API docs, no Samples guide |
| **Reusability** | 3/10 | Game-specific code; hardcoded paths; mandatory Firebase |

### **Overall UPM Readiness: 4/10**

**Summary:** The project has a solid feature set (Ads, IAP, Analytics) and some modularization (integration asmdefs, define symbols). However, **critical asmdef errors** (Editor DLLs in Runtime), **game-specific code** (BanThanh, GiftCodeManager), **mandatory Firebase**, and **third-party SDKs embedded in Assets** block a clean UPM package. With P0 + P1 refactoring (estimated 2–4 weeks), readiness could reach **7–8/10**.

---

*End of analysis*
