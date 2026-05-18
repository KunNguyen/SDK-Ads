# SDK-Ads — JIS SDK (UPM)

Unity package monorepo for **Ads (MAX / AdMob)**, **Firebase**, **IAP**, and optional analytics modules.

## Requirements

- Unity **2022.3 LTS** or newer (dev repo tested on Unity 6)
- Firebase installed from Google in **game projects** (not bundled in this repo)
- Git UPM for distribution

## Repository layout

| Path | Purpose |
|------|---------|
| `Packages/com.jis.sdkads.*` | UPM packages (published via Git `?path=`) |
| `Assets/MeldAppAds/` | **Dev tool only** — not part of UPM |
| `Assets/Firebase`, `Assets/GoogleMobileAds`, … | Third-party SDKs for local development |

## Packages

| Package | Description |
|---------|-------------|
| `com.jis.sdkads.hub` | Editor Hub — import modules |
| `com.jis.sdkads.core` | AdManager, interfaces, Odin (embedded) |
| `com.jis.sdkads.common` | EventManager, InternetChecker, Keys |
| `com.jis.sdkads.firebase` | Analytics + Remote Config |
| `com.jis.sdkads.ads` | Full ads runtime (legacy stack, v4) |
| `com.jis.sdkads.providers.max` | MAX `IAdService` |
| `com.jis.sdkads.providers.admob` | AdMob `IAdService` |
| `com.jis.sdkads.iap` | In-app purchasing |
| `com.jis.sdkads.appreview` | Play In-App Review (Android) |
| `com.jis.sdkads.analytics.*` | Optional: AppsFlyer, SolarEngine, Facebook |
| `com.jis.sdkads.editor` | Setup & build tools |

## Game project setup

1. Add scoped registries (AppLovin, Google) — Hub can add these.
2. Install **Firebase** from Google.
3. Add Hub package:

```json
"com.jis.sdkads.hub": "https://github.com/YOUR_ORG/SDK-Ads.git?path=Packages/com.jis.sdkads.hub#4.0.0"
```

4. Open **JIS SDK → Hub** and import modules (Firebase → Ads → IAP → …).

Or add each package manually:

```json
"com.jis.sdkads.firebase": "https://github.com/YOUR_ORG/SDK-Ads.git?path=Packages/com.jis.sdkads.firebase#4.0.0"
```

## Continuing development

**Start here:** [docs/HANDOFF.md](docs/HANDOFF.md)

**Migrate games:** [docs/MIGRATION_V4.md](docs/MIGRATION_V4.md)

**Samples:** import package `com.jis.sdkads.samples` in Package Manager → Samples → Minimal integration

## Game setup

See [docs/GAME_SETUP.md](docs/GAME_SETUP.md).

## Dev workflow (this repo)

All `com.jis.sdkads.*` folders under `Packages/` are **embedded packages** — Unity loads them automatically.

Open **JIS SDK → Hub** to test manifest edits, or reference packages from another game via Git URL.

## Version

**4.0.0** — breaking refactor from `Assets/JisSDKAds` monolith. See `docs/REFACTOR_PLAN.md`.
