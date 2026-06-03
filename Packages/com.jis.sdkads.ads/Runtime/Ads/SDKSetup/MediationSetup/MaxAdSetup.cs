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
        get => sdkKey.ID;
        set => sdkKey.ID = value;
    }

    public string InterstitialAdUnitID
    {
        get => interstitialAdUnitID.ID;
        set => interstitialAdUnitID.ID = value;
    }

    public string RewardedAdUnitID
    {
        get => rewardedAdUnitID.ID;
        set => rewardedAdUnitID.ID = value;
    }

    public string BannerAdUnitID
    {
        get => bannerAdUnitID.ID;
        set => bannerAdUnitID.ID = value;
    }

    public string AppOpenAdUnitID
    {
        get => appOpenAdUnitID.ID;
        set => appOpenAdUnitID.ID = value;
    }

    public string RewardedInterstitialAdUnitID
    {
        get => rewardedInterstitialAdUnitID.ID;
        set => rewardedInterstitialAdUnitID.ID = value;
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
}
