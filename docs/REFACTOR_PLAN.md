# JIS SDK v4 Refactor Plan

> **Tiếp tục công việc:** đọc [HANDOFF.md](HANDOFF.md) (đầy đủ context, quyết định, file paths, Phase 6).

## Status

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Remove LevelPlay, IronSource, UnityAds, BanThanh/GiftCode | Done |
| 1 | UPM package split under `Packages/com.jis.sdkads.*` | Done |
| 2 | JIS SDK Hub window | Done (v1) |
| 3 | `JisSDKAdsSettings` + single mediation per platform | Done |
| 4 | `JisAds` facade + Core `AdManager` bridge | Done |
| 5 | Namespace migration `SDK` → `JisSDKAds.*` | Done |
| 6 | Samples, Hub auto-settings, Core App Open, migration | Done |

## Architecture (current)

```
Firebase (user) + com.jis.sdkads.firebase
        │
JisAds.Instance ──┬── Legacy AdsManager (all formats, RC, rules)
                  └── Core AdManager (Inter/Reward/Banner via providers)
```

- Mediation: **MAX** or **AdMob** per platform only
- **MeldAppAds**: `Assets/MeldAppAds/` only (not UPM)

## Backlog (post–Phase 6)

- Core MREC (`IMrecAd`)
- AdMob App Open on Core
- Sample Unity scene + prefabs
- Further legacy removal

See [HANDOFF.md](HANDOFF.md) §9.
