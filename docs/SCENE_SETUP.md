# JIS SDK scene setup

## Recommended hierarchy

```
JisSDK_Manager
├── Firebase
│   ├── FirebaseManager
│   └── AdsTracker
├── JisAds
│   └── JisAds
└── Ads_Runtime
    ├── AdsManager
    ├── UnitAdManagers/
    │   ├── Banner              → BannerAdManager
    │   ├── Interstitial        → InterstitialAdManager
    │   ├── Rewarded            → RewardAdManager
    │   ├── MRec                  → MRecAdManager
    │   ├── AppOpen               → AppOpenAdManager
    │   ├── CollapsibleBanner     → CollapsibleBannerAdManager
    │   └── Resume                → ResumeAdManager
    └── Mediation/
        ├── MaxMediation          → MaxMediationController (optional)
        └── AdmobMediation        → AdmobMediationController (optional)
```

## Editor menus

| Menu | Action |
|------|--------|
| **JIS SDK → Ads → Scene → Add JisSDK Manager Prefab** | Create `JisSDK_Manager` hierarchy + wire `AdsManager` |
| **JIS SDK → Ads → Scene → Reorganize JisSDK Manager Hierarchy** | Move components from flat root into children |
| **JIS SDK → IAP → Scene → Add JisSDK InApp Purchaser Prefab** | Create `JisSDK_InAppPurchaser` (separate from ads manager) |

Prefab templates (if missing):

- Ads: `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab` (legacy: `Manager.prefab`)
- IAP: `Assets/JisSDKAds/Prefabs/JisSDK_InAppPurchaser.prefab` (legacy: `InAppPurchaser.prefab`)

`JisSDK_Manager` uses **Jis SDK Persistent Root** (`JisSDKPersistentRoot`) → **DontDestroyOnLoad** for the full ads hierarchy.  
`JisSDK_InAppPurchaser` is a **separate** object with `InAppPurchaser` only (do not mix prefabs).

## Init modes vs Remote Config

| Pattern | AdsManager mode | Who starts ads | Remote Config |
|--------|-----------------|----------------|---------------|
| **Production (recommended)** | `Manual` | `await JisAds.Instance.InitializeAsync(fetchRemoteConfig: true)` in loading | Fetched before mediation / requests |
| **Prototype / no JisAds** | `AutoOnStart` | `AdsManager` on `Start` → `CoBootstrapAdsWithRemoteConfig` | Same gate, no loading-screen control |
| **Avoid** | `AutoOnStart` on AdsManager **and** `JisAds` auto-init on `Start` | Two bootstraps | Duplicate init |

`FirebaseManager`: prefer **Manual** when using `JisAds.InitializeAsync`; `AutoOnAwake` is OK — ads bootstrap still waits for Firebase + RC.

Do not call `ShowInterstitial` / rely on autoload until `JisAds.Instance.IsReady` or `AdsManager.Instance.IsReady`.

## Notes

- `AdsManager` serialized references must point to child objects (auto-wired by scene builder).
- `JisAds` and `FirebaseManager` each use `DontDestroyOnLoad` on their own GameObject.
- Game code still uses `JisAds.Instance` / `AdsManager.Instance` — hierarchy is for clarity only.
