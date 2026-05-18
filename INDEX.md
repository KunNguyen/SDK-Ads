# SDK-Ads — Project Index

**JIS SDK Ads v4** — Unity UPM monorepo for mobile ads (MAX, AdMob), Firebase, IAP, optional analytics.

**Continue development:** [docs/HANDOFF.md](docs/HANDOFF.md)

---

## Repository layout

| Path | Description |
|------|-------------|
| `Packages/com.jis.sdkads.*` | UPM packages (publish via Git `?path=`) |
| `Assets/MeldAppAds/` | Dev tool only — **not** in UPM |
| `Assets/Firebase`, `GoogleMobileAds`, … | Third-party SDKs for local dev |
| `docs/` | UPM_INSTALL, GAME_SETUP, HANDOFF, MIGRATION_V4, NAMESPACES |

---

## UPM packages

| Package | Role |
|---------|------|
| `com.jis.sdkads.hub` | Import modules → manifest + defines |
| `com.jis.sdkads.core` | AdManager, interfaces, Odin |
| `com.jis.sdkads.common` | EventManager, Keys, utils |
| `com.jis.sdkads.firebase` | Analytics + Remote Config |
| `com.jis.sdkads.ads` | AdsManager, **JisAds**, SDKSetup, settings |
| `com.jis.sdkads.providers.max` | MAX IAdService |
| `com.jis.sdkads.providers.admob` | AdMob IAdService |
| `com.jis.sdkads.iap` | In-app purchasing |
| `com.jis.sdkads.appreview` | Play In-App Review (Android) |
| `com.jis.sdkads.analytics.*` | AppsFlyer, SolarEngine, Facebook (optional) |
| `com.jis.sdkads.editor` | Setup & build tools |
| `com.jis.sdkads.samples` | Sample import + integration README |

---

## Game integration (short)

1. [docs/UPM_INSTALL.md](docs/UPM_INSTALL.md) — add `com.jis.sdkads.hub` via Git UPM
2. **JIS SDK → Hub** → Import Firebase, Ads, …
3. Configure `Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset`
4. Scene: `FirebaseManager` + `AdsManager` + `JisAds`
5. Code: `JisAds.Instance.ShowInterstitial(...)`

See [docs/UPM_INSTALL.md](docs/UPM_INSTALL.md), [docs/GAME_SETUP.md](docs/GAME_SETUP.md), [docs/MIGRATION_V4.md](docs/MIGRATION_V4.md).

---

## Removed in v4

- IronSource / LevelPlay / Unity Ads
- `Assets/JisSDKAds` monolith (moved to Packages)
- Namespaces `SDK`, `ABIMaxSDKAds`

---

*Updated for v4 UPM structure*
