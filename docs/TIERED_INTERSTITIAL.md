# 5-Tier Interstitial (AdMob)

Sequential ladder: **Premium → High → Mid → Low → Fill** (one load at a time).

## Inspector

1. Open your **AdsManager** scene object → **Admob Mediation Controller** component.
2. Expand **m_Admob Ad Setup → Interstitial Tier Config**.
3. **Enable Tiered Interstitial** — turn on 5-tier ladder; off = legacy single unit.
4. **Default Android / iOS Ad Unit Id** — fallback when tier system is off or a tier entry is empty.
5. **Tiers** array (5 slots): per tier set `Android Ad Unit Id`, `Ios Ad Unit Id`, optional `Timeout Seconds`.
6. **Enable Tier Memory Cooldown** / **Premium Retry Cooldown Minutes** (default 45) / **Consecutive Failures Before Downgrade**.

You can also map up to 5 IDs from **Interstitial Ad Unit ID List** (SDK Setup) into tiers automatically when applying settings (index 0 = Premium … 4 = Fill).

## Public API (unchanged names)

| Method | Behavior |
|--------|----------|
| `LoadInterstitial()` | Alias → `RequestInterstitialAd()` |
| `ShowInterstitial()` | Alias → `ShowInterstitialAd()` |
| `IsInterstitialReady()` | Alias → `IsInterstitialLoaded()` |

Game code via `InterstitialAdManager` / `AdsManager` / `JisAds` continues to use existing show/load paths.

## Flow

**Load:** Start tier from memory (last success within cooldown, else Premium) → load one unit → on fail or timeout → next tier → Fill last. No parallel loads.

**Show:** Show cached ready ad → on close/fail → clear cache → preload again.

**Off (`enableTieredInterstitial = false`):** Uses `default*AdUnitId` or legacy `InterstitialAdUnitID` schedule.

## Logs (DebugAds)

`interstitial_load_start`, `_success`, `_fail`, `_timeout`, `interstitial_show_*`, `interstitial_paid_event` with `adUnitId`, `tier`, `loadDurationMs`, errors, revenue fields.
