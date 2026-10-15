# Sequential 5-Tier Ads (AdMob) — Interstitial & Rewarded

One load at a time: **Premium → High → Mid → Low → Fill**.

## JIS SDK Ads Settings

Per platform (Android / iOS), for **Interstitial** and **Rewarded**:

| Mode | Behaviour |
|------|-----------|
| **Single unit** | One default Ad Unit ID from SDK Settings (legacy rotation via `AdScheduleUnitID` on fail). No tier Remote Config keys. |
| **Tiered** | Sequential 5-tier ladder (AdMob only). Unit IDs are loaded from **Firebase Remote Config** before the first ad request. |

### Firebase Remote Config — inventory mode (default: **single**)

| Key | Values | Default |
|-----|--------|---------|
| `interstitial_inventory_mode` | `single` \| `tiered` | `single` |
| `rewarded_inventory_mode` | `single` \| `tiered` | `single` |

At runtime, RC **overrides** the editor toolbar. Editor settings are the fallback before fetch.

### Bootstrap order (JisAds)

`JisAds.InitializeAsync(fetchRemoteConfig: true)` (called from `Start` when `adsInitializationMode = AutoOnStart`):

1. Warm up the MAX / AdMob SDKs
2. **Fetch & activate Remote Config** (`FirebaseManager.FetchRemoteConfigAsync()`)
3. Apply mediation modes, inventory mode and resolved tier unit IDs to the active profile's `SDKSetup`
4. Create the providers for the active profile, register them with Core `AdManager`, `Initialize()`
5. Preloads start only after Core is ready (`JisAds.Instance.IsReady`)

`JisAds.Instance.FetchAndApplyRemoteConfigAsync()` re-applies the inventory mode after a later fetch.

When RC (or editor) is **Tiered**:

- Mediation for that format is forced to **ADMOB**.
- Configure tier unit ID keys (platform-specific string per key, use Firebase Android/iOS conditions):
  - Interstitial IDs: `inter_premium_id`, `inter_high_id`, `inter_mid_id`, `inter_low_id`, `inter_fill_id`
  - Rewarded IDs: `reward_premium_id`, `reward_high_id`, `reward_mid_id`, `reward_low_id`, `reward_fill_id`
- **JIS SDK Ads Settings (Tiered tab)** does not edit per-tier unit IDs — configure them in Firebase. Only optional **fallback unit ID**, tier timeouts, and memory/cooldown are stored locally.
- Optional: **Tier memory + cooldown**, **Premium retry cooldown (min)**, **Failures before downgrade**.

`Inventory mode` toolbar syncs `enableSequentialLadder` on `AdmobAdSetup.InterstitialTierConfig` / `RewardedTierConfig`.

Up to 5 IDs in the single-unit list can auto-fill empty tier slots when you **Apply to Scene**.

## Runtime (Core providers)

- AdMob: `AdMobSequentialTierBridge` wraps `AdMobProvider` in `SequentialTierAdServiceDecorator` when the tier config is enabled
- MAX: `MaxAdProvider` drives the same `SequentialTierLoader` for its tier config
- `JisAds.Instance.RequestInterstitialLoadIfNeeded()` / `RequestRewardedLoadIfNeeded()` → ladder preload (also run automatically after Core is ready and after each close)
- `JisAds.Instance.IsInterstitialAdLoaded()` / `IsRewardedVideoLoaded()` → cached ready ad
- `JisAds.Instance.ShowInterstitial(...)` / `ShowRewardVideo(...)` → show cache, then preload again

Logs: `interstitial_*` / `rewarded_*` (`load_start`, `load_success`, `load_fail`, `load_timeout`, `show_*`, `paid_event`).

## Platform tab

Use **Android / iOS** at the top of JIS SDK Ads Settings. Only IDs for the selected platform are shown and saved (`AndroidID` / `IosID` per `SDKSetup` asset).

Choosing **Tiered** sets format mediation to **AdMob** and enables the 5-tier ladder (no legacy 3-tier UI).
