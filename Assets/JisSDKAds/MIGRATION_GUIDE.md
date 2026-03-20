# JisSDKAds — Migration Guide (Old → New Architecture)

This guide helps you migrate from the legacy `AdsManager` / `AdsMediationController` design to the new interface-based Core + Providers architecture.

---

## 1. New Architecture Overview

```
JisSDKAds/
├── Core/                    # No ad network dependencies
│   ├── Interfaces/          # IAdService, IInterstitialAd, IRewardedAd, IBannerAd
│   ├── Models/              # AdFormat, AdProviderId
│   ├── Events/              # AdEvents (OnAdLoaded, OnAdFailed, etc.)
│   └── AdManager.cs         # Unified entry point
│
├── Providers/
│   ├── Max/                 # AppLovin MAX
│   ├── AdMob/               # Google AdMob
│   └── UnityAds/            # Unity Ads (stub)
│
├── Runtime/                 # Legacy code (to be phased out)
└── Editor/
```

---

## 2. API Mapping: Old → New

| Old API | New API |
|---------|---------|
| `AdsManager.Instance` | `AdManager.Instance` (from Core) |
| `AdsManager.Instance.ShowInterstitial()` | `AdManager.Instance.ShowInterstitial(onClosed, onFailed)` |
| `AdsManager.Instance.ShowRewarded(onSuccess, onClosed)` | `AdManager.Instance.ShowRewarded(onRewardEarned, onClosed, onFailed)` |
| `AdsManager.Instance.ShowBanner()` | `AdManager.Instance.ShowBanner(onShown, onFailed)` |
| `AdsManager.Instance.HideBanner()` | `AdManager.Instance.HideBanner()` |
| `InterstitialAdManager.CallToShowAd(...)` | `AdManager.Instance.ShowInterstitial(...)` |
| `RewardAdManager.CallToShowRewardAd(...)` | `AdManager.Instance.ShowRewarded(...)` |
| `BannerAdManager.Show()` | `AdManager.Instance.ShowBanner(...)` |

---

## 3. Setup (New Architecture)

### Step 1: Create Provider Configs

1. **Max:** Right-click in Project → Create → JisSDKAds → Providers → Max Config  
   - Fill: SDK Key, Interstitial Ad Unit ID, Rewarded Ad Unit ID, Banner Ad Unit ID

2. **AdMob:** Right-click → Create → JisSDKAds → Providers → AdMob Config  
   - Fill: App ID, Interstitial/Rewarded/Banner Ad Unit IDs

3. **Unity Ads:** Right-click → Create → JisSDKAds → Providers → Unity Ads Config  
   - Fill: Game ID, placement IDs (stub until Unity Ads SDK is integrated)

### Step 2: Add Bootstrap to Scene

1. Create an empty GameObject (e.g. `AdsBootstrap`)
2. Add component: **AdManagerBootstrap** (from JisSDKAds.Core)
3. In the Inspector:
   - Add your provider configs to the **Provider Configs** list (MaxAdConfig, AdMobConfig, etc.)
   - Set **Primary Provider** (e.g. Max)
   - Set **Fallback Provider** (e.g. AdMob)

### Step 3: Define Symbols

Ensure these are in **Player Settings → Scripting Define Symbols**:

- `UNITY_AD_MAX` — if using AppLovin MAX
- `UNITY_AD_ADMOB` — if using Google AdMob

---

## 4. Code Migration Examples

### Interstitial

**Old:**
```csharp
AdsManager.Instance.InterstitialAdManager.CallToShowAd(
    placementName: "level_complete",
    closedCallback: () => LoadNextLevel(),
    showSuccessCallback: null,
    showFailCallback: () => LoadNextLevel()
);
```

**New:**
```csharp
AdManager.Instance.ShowInterstitial(
    onClosed: () => LoadNextLevel(),
    onFailed: err => { Debug.LogWarning(err); LoadNextLevel(); }
);
```

### Rewarded

**Old:**
```csharp
AdsManager.Instance.RewardAdManager.CallToShowRewardAd(
    placementName: "double_coins",
    closedCallback: (rewarded) => { if (rewarded) GiveCoins(); },
    showSuccessCallback: null,
    showFailCallback: () => { }
);
```

**New:**
```csharp
AdManager.Instance.ShowRewarded(
    onRewardEarned: () => GiveCoins(),
    onClosed: () => { },
    onFailed: err => Debug.LogWarning(err)
);
```

### Banner

**Old:**
```csharp
AdsManager.Instance.BannerAdManager.Show();
```

**New:**
```csharp
AdManager.Instance.ShowBanner(
    onShown: () => { },
    onFailed: err => Debug.LogWarning(err)
);
AdManager.Instance.HideBanner();  // when needed
```

### Events (Optional)

**New:** Subscribe to centralized events:

```csharp
void OnEnable()
{
    AdEvents.OnInterstitialShown += HandleInterstitialShown;
    AdEvents.OnRewardEarned += HandleRewardEarned;
}

void OnDisable()
{
    AdEvents.OnInterstitialShown -= HandleInterstitialShown;
    AdEvents.OnRewardEarned -= HandleRewardEarned;
}
```

---

## 5. What Stays the Same (Legacy)

- **FirebaseManager, AdsTracker, EventManager** — still used by legacy code
- **SDKSetup, AdsConfig** — legacy setup assets
- **AdsMediationController, UnitAdManager** — old implementation

You can run **both** systems during migration: keep the old AdsManager for some flows and use the new AdManager for new features, then switch fully when ready.

---

## 6. Clean API for Game Developers

```csharp
// Init (handled by AdManagerBootstrap)
// No manual init needed if Bootstrap is in scene.

// Show interstitial
AdManager.Instance.ShowInterstitial(
    onClosed: () => OnInterstitialClosed(),
    onFailed: err => OnAdFailed(err)
);

// Show rewarded
AdManager.Instance.ShowRewarded(
    onRewardEarned: () => GrantReward(),
    onClosed: () => { },
    onFailed: err => OnAdFailed(err)
);

// Banner
AdManager.Instance.ShowBanner();
AdManager.Instance.HideBanner();

// Check init
if (AdManager.Instance.IsInitialized) { ... }
```

---

## 7. Fallback & Retry

The new AdManager automatically:

- Tries the **primary** provider first
- Falls back to the **fallback** provider on failure
- Retries up to **maxRetries** (default 3) with **retryDelaySeconds** (default 2s) between attempts

Configure in the AdManager component or via code.

---

## 8. Adding a New Provider

1. Create `JisSDKAds/Providers/MyNetwork/`
2. Implement `IAdService` and `IInterstitialAd`, `IRewardedAd`, `IBannerAd`
3. Create `MyNetworkConfig : ScriptableObject, IAdProviderConfig`
4. Add `AdProviderId.MyNetwork` to `AdFormat.cs`
5. Create config asset and add to Bootstrap’s Provider Configs list

---

*End of Migration Guide*
