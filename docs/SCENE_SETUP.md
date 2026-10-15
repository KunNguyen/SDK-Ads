# JIS SDK scene setup

## Hierarchy

```
JisSDK_Manager                    → JisSDKPersistentRoot (DontDestroyOnLoad)
├── Firebase
│   ├── FirebaseManager
│   └── AdsTracker
└── JisAds
    ├── JisAds                    → Core ads entry point, App Open, Resume
    ├── ResumeAdCoordinator
    └── (Core AdManager child created at runtime)
```

## Editor menus

| Menu | Action |
|------|--------|
| **JIS SDK → Ads → Scene → Add JisSDK Manager Prefab** | Create the hierarchy above and wire `JisAds` to `JisSDKAdsSettings` |
| **JIS SDK → Ads → Scene → Remove legacy Ads_Runtime** | Strip the pre-6.0 `Ads_Runtime` subtree (AdsManager, mediation controllers, unit managers) from a scene object, an open prefab stage or a selected prefab asset |
| **JIS SDK → Ads → Apply Settings to Scene** | Sync `JisAds` + scripting defines for the active build target |
| **JIS SDK → IAP → Scene → Add JisSDK InApp Purchaser Prefab** | Create `JisSDK_InAppPurchaser` (separate from the ads manager) |

Prefab templates (if missing):

- Ads: `Assets/JisSDKAds/Prefabs/JisSDK_Manager.prefab`
- IAP: `Assets/JisSDKAds/Prefabs/JisSDK_InAppPurchaser.prefab`

`JisSDK_InAppPurchaser` is a **separate** object with `InAppPurchaser` only (do not mix prefabs).

## Init mode vs Remote Config

| Pattern | `adsInitializationMode` | Who starts ads | Remote Config |
|--------|-------------------------|----------------|---------------|
| **Production (recommended)** | `Manual` | `await JisAds.Instance.InitializeAsync(fetchRemoteConfig: ...)` from the loading flow | Fetched by the game (`FetchAndApplyRemoteConfigAsync`) or by `InitializeAsync(fetchRemoteConfig: true)` |
| **Prototype** | `AutoOnStart` | `JisAds` on `Start` | Fetched inside `InitializeAsync` |

`FirebaseManager`: prefer **Manual** when using `JisAds.InitializeAsync`; `AutoOnAwake` is OK — ads bootstrap still waits for Firebase + RC.

Do not call `ShowInterstitial` / rely on preload until `JisAds.Instance.IsReady`.

## Notes

- `JisAds` and `FirebaseManager` each use `DontDestroyOnLoad` on their own GameObject.
- Projects created before 6.0 still carry an `Ads_Runtime` child whose scripts no longer exist. Run **Remove legacy Ads_Runtime** once; see [MIGRATION_V6.md](MIGRATION_V6.md).
