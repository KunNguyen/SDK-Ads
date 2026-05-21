# IAP integration sample

Requires Hub modules: **Firebase**, **Ads**, **IAP** (+ AppsFlyer optional).

## Scene checklist

1. `JisSDK_Manager` (Firebase + JisAds + AdsManager) — see MinimalIntegration
2. `InAppPurchaser` with `IAPPackageConfigs` assigned
3. Product example in config:

| ProductID | ProductKind | ProductType |
|-----------|-------------|-------------|
| `remove_ads` | RemoveAds | NonConsumable |
| `coin_pack_1` | Consumable | Consumable |

## Minimal code

```csharp
using JisSDKAds.Ads;
using JisSDKAds.IAP;

async void Start()
{
    await JisAds.Instance.InitializeAsync(fetchRemoteConfig: true);
    await InAppPurchaser.Instance.InitializeAsync();
}

public void OnBuyRemoveAds() =>
    InAppPurchaser.Instance.BuyIapProduct("remove_ads", () => { }, () => { });

public void OnRestore() =>
    InAppPurchaser.Instance.RestorePurchases();
```

Full guide: [docs/IAP_USAGE.md](../../../../docs/IAP_USAGE.md)
