# JisSDKAds — Refactored Class Structure

## Class Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           JisSDKAds.Core                                │
├─────────────────────────────────────────────────────────────────────────┤
│  IAdService                                                             │
│  ├── ProviderId, IsInitialized                                          │
│  ├── Initialize(onSuccess, onFailure)                                   │
│  ├── Interstitial : IInterstitialAd                                     │
│  ├── Rewarded : IRewardedAd                                             │
│  └── Banner : IBannerAd                                                │
├─────────────────────────────────────────────────────────────────────────┤
│  IInterstitialAd          IRewardedAd           IBannerAd               │
│  ├── IsLoaded             ├── IsLoaded          ├── IsLoaded            │
│  ├── Load(...)            ├── Load(...)         ├── IsVisible           │
│  └── Show(...)            └── Show(...)         ├── Load(...)           │
│                                                  ├── Show(...)          │
│                                                  ├── Hide()             │
│                                                  └── Destroy()          │
├─────────────────────────────────────────────────────────────────────────┤
│  AdManager (MonoBehaviour)                                              │
│  ├── RegisterProvider(id, IAdService)                                   │
│  ├── Initialize(onSuccess, onFailure)                                   │
│  ├── ShowInterstitial(onClosed, onFailed)                               │
│  ├── ShowRewarded(onRewardEarned, onClosed, onFailed)                   │
│  ├── ShowBanner(onShown, onFailed)                                     │
│  └── HideBanner()                                                       │
├─────────────────────────────────────────────────────────────────────────┤
│  AdEvents (static)                                                      │
│  ├── OnInterstitialLoaded/Failed/Shown/Closed                           │
│  ├── OnRewardedLoaded/Failed/Shown/OnRewardEarned/Closed                │
│  └── OnBannerLoaded/Failed/Shown/Hidden                                │
└─────────────────────────────────────────────────────────────────────────┘
         │
         │ implements
         ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ MaxAdProvider   │  │ AdMobProvider   │  │ UnityAdsProvider│
│ (Providers.Max) │  │ (Providers.AdMob)│  │ (Providers.UA)  │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

---

## Key Code Snippets

### 1. IAdService

```csharp
public interface IAdService
{
    string ProviderId { get; }
    bool IsInitialized { get; }
    void Initialize(Action onSuccess, Action<string> onFailure);
    void SetConsent(bool hasConsent);
    IInterstitialAd Interstitial { get; }
    IRewardedAd Rewarded { get; }
    IBannerAd Banner { get; }
}
```

### 2. IInterstitialAd

```csharp
public interface IInterstitialAd
{
    bool IsLoaded { get; }
    void Load(Action onLoaded = null, Action<string> onFailed = null);
    void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null);
}
```

### 3. IRewardedAd

```csharp
public interface IRewardedAd
{
    bool IsLoaded { get; }
    void Load(Action onLoaded = null, Action<string> onFailed = null);
    void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null);
}
```

### 4. IBannerAd

```csharp
public interface IBannerAd
{
    bool IsLoaded { get; }
    bool IsVisible { get; }
    void Load(Action onLoaded = null, Action<string> onFailed = null);
    void Show(Action onShown = null, Action<string> onFailed = null);
    void Hide();
    void Destroy();
}
```

### 5. AdManager — Show Interstitial (with fallback)

```csharp
public void ShowInterstitial(Action onClosed = null, Action<string> onFailed = null)
{
    if (!_isInitialized) { onFailed?.Invoke("AdManager not initialized"); return; }
    TryShowInterstitialWithFallback(primaryProvider, fallbackProvider, 0, onClosed, onFailed);
}
```

### 6. AdManager — Show Rewarded

```csharp
public void ShowRewarded(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
{
    if (!_isInitialized) { onFailed?.Invoke("AdManager not initialized"); return; }
    TryShowRewardedWithFallback(primaryProvider, fallbackProvider, 0, onRewardEarned, onClosed, onFailed);
}
```

### 7. Provider Registration (Bootstrap)

```csharp
foreach (var obj in providerConfigs)
{
    if (obj is IAdProviderConfig config)
    {
        adManager.RegisterProvider(config.ProviderId, config.CreateProvider());
    }
}
adManager.Initialize(onSuccess, onFailure);
```

### 8. Game Developer Usage

```csharp
// Interstitial
AdManager.Instance.ShowInterstitial(
    onClosed: () => LoadNextLevel(),
    onFailed: err => Debug.LogWarning(err)
);

// Rewarded
AdManager.Instance.ShowRewarded(
    onRewardEarned: () => GrantReward(),
    onClosed: () => { },
    onFailed: err => Debug.LogWarning(err)
);

// Banner
AdManager.Instance.ShowBanner();
AdManager.Instance.HideBanner();
```

---

## File Layout

```
Assets/JisSDKAds/
├── Core/
│   ├── JisSDKAds.Core.asmdef
│   ├── AdManager.cs
│   ├── AdManagerBootstrap.cs
│   ├── Interfaces/
│   │   ├── IAdService.cs
│   │   ├── IInterstitialAd.cs
│   │   ├── IRewardedAd.cs
│   │   ├── IBannerAd.cs
│   │   └── IAdProviderConfig.cs
│   ├── Models/
│   │   ├── AdFormat.cs
│   │   └── AdLoadResult.cs
│   └── Events/
│       └── AdEvents.cs
│
├── Providers/
│   ├── Max/
│   │   ├── JisSDKAds.Providers.Max.asmdef
│   │   └── MaxAdProvider.cs
│   ├── AdMob/
│   │   ├── JisSDKAds.Providers.AdMob.asmdef
│   │   └── AdMobProvider.cs
│   └── UnityAds/
│       ├── JisSDKAds.Providers.UnityAds.asmdef
│       └── UnityAdsProvider.cs
│
├── MIGRATION_GUIDE.md
└── REFACTORED_STRUCTURE.md
```

---

## Design Principles

1. **Core has zero ad network dependencies** — only interfaces and models.
2. **Providers are swappable** — register any `IAdService` via config.
3. **Fallback & retry built-in** — AdManager handles primary → fallback → retry.
4. **Events for analytics** — subscribe to `AdEvents` without coupling.
5. **Config-driven** — ScriptableObject configs per provider, assigned in Bootstrap.
