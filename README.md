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

## Add SDK to another Unity project (UPM)

**Full guide:** [docs/UPM_INSTALL.md](docs/UPM_INSTALL.md) (Git URL, Hub, scoped registries, private repo, troubleshooting).

Quick start:

1. Add to `Packages/manifest.json`:

```json
"com.jis.sdkads.hub": "https://github.com/KunNguyen/SDK-Ads.git?path=Packages/com.jis.sdkads.hub#4.0.0"
```

2. Open **JIS SDK → Hub** → disable *embedded packages* → set Git URL → Import **Firebase**, then **Ads**.
3. Install **Firebase** from Google in the game project.
4. Scene & API: [docs/GAME_SETUP.md](docs/GAME_SETUP.md).

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
