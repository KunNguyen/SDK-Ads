# Sequential 5-Tier Ads (AdMob) — Interstitial & Rewarded

One load at a time: **Premium → High → Mid → Low → Fill**.

## JIS SDK Ads Settings

Per platform (Android / iOS), for **Interstitial** and **Rewarded**:

| Mode | Behaviour |
|------|-----------|
| **Single unit** | One default Ad Unit ID (legacy rotation via `AdScheduleUnitID` on fail). |
| **Tiered** | Sequential 5-tier ladder (AdMob only). |

When **Tiered** is selected:

- Set mediation for that format to **ADMOB** in the same section.
- Fill **Fallback Android/iOS unit** and each tier (Premium … Fill).
- Optional: **Tier memory + cooldown**, **Premium retry cooldown (min)**, **Failures before downgrade**.

`Inventory mode` toolbar syncs `enableSequentialLadder` on `AdmobAdSetup.InterstitialTierConfig` / `RewardedTierConfig`.

Up to 5 IDs in the single-unit list can auto-fill empty tier slots when you **Apply to Scene**.

## Runtime (legacy AdsManager / AdmobMediationController)

- `RequestInterstitialAd` / `RequestRewardVideoAd` → ladder preload
- `IsInterstitialLoaded` / `IsRewardVideoLoaded` → cached ready ad
- `ShowInterstitialAd` / `ShowRewardVideoAd` → show cache, then preload again

Logs: `interstitial_*` / `rewarded_*` (`load_start`, `load_success`, `load_fail`, `load_timeout`, `show_*`, `paid_event`).

## Platform tab

Use **Android / iOS** at the top of JIS SDK Ads Settings. Only IDs for the selected platform are shown and saved (`AndroidID` / `IosID` per `SDKSetup` asset).

Choosing **Tiered** sets format mediation to **AdMob** and enables the 5-tier ladder (no legacy 3-tier UI).
