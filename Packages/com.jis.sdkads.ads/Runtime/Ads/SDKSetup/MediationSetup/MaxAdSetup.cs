using JisSDKAds.Ads;
using JisSDKAds.Ads.SequentialTier;
using UnityEngine;

[System.Serializable]
public class MaxAdSetup
{
    [SerializeField] private AdUnitID sdkKey;
    [SerializeField] private AdUnitID interstitialAdUnitID;
    [SerializeField] private AdUnitID rewardedAdUnitID;
    [SerializeField] private AdUnitID bannerAdUnitID;
    [SerializeField] private AdUnitID appOpenAdUnitID;
    [SerializeField] private AdUnitID rewardedInterstitialAdUnitID;

    [SerializeField] private SequentialTierConfig interstitialTierConfig;
    [SerializeField] private SequentialTierConfig rewardedTierConfig;

    public string SDKKey
    {
        get => SdkKey.ID;
        set => SdkKey.ID = value;
    }

    public string InterstitialAdUnitID
    {
        get => InterstitialUnit.ID;
        set => InterstitialUnit.ID = value;
    }

    public string RewardedAdUnitID
    {
        get => RewardedUnit.ID;
        set => RewardedUnit.ID = value;
    }

    public string BannerAdUnitID
    {
        get => BannerUnit.ID;
        set => BannerUnit.ID = value;
    }

    public string AppOpenAdUnitID
    {
        get => AppOpenUnit.ID;
        set => AppOpenUnit.ID = value;
    }

    public string RewardedInterstitialAdUnitID
    {
        get => RewardedInterstitialUnit.ID;
        set => RewardedInterstitialUnit.ID = value;
    }

    public SequentialTierConfig InterstitialTierConfig
    {
        get
        {
            interstitialTierConfig ??= new SequentialTierConfig();
            interstitialTierConfig.EnsureDefaultTierSlots();
            return interstitialTierConfig;
        }
        set => interstitialTierConfig = value;
    }

    public SequentialTierConfig RewardedTierConfig
    {
        get
        {
            rewardedTierConfig ??= new SequentialTierConfig();
            rewardedTierConfig.EnsureDefaultTierSlots();
            return rewardedTierConfig;
        }
        set => rewardedTierConfig = value;
    }

    AdUnitID SdkKey => sdkKey ??= new AdUnitID();
    AdUnitID InterstitialUnit => interstitialAdUnitID ??= new AdUnitID();
    AdUnitID RewardedUnit => rewardedAdUnitID ??= new AdUnitID();
    AdUnitID BannerUnit => bannerAdUnitID ??= new AdUnitID();
    AdUnitID AppOpenUnit => appOpenAdUnitID ??= new AdUnitID();
    AdUnitID RewardedInterstitialUnit => rewardedInterstitialAdUnitID ??= new AdUnitID();

    public void EnsureInitialized()
    {
        _ = SdkKey;
        _ = InterstitialUnit;
        _ = RewardedUnit;
        _ = BannerUnit;
        _ = AppOpenUnit;
        _ = RewardedInterstitialUnit;
        _ = InterstitialTierConfig;
        _ = RewardedTierConfig;
    }
}
