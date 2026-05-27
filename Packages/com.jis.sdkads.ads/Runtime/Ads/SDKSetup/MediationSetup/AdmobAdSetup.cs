using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Ads.SequentialTier;
using UnityEngine;

[System.Serializable]
public class AdmobAdSetup
{
    [SerializeField]private AdScheduleUnitID interstitialAdUnitID;
    [SerializeField]private AdScheduleUnitID rewardedAdUnitID;
    [SerializeField]private AdScheduleUnitID bannerAdUnitID;
    [SerializeField]private AdScheduleUnitID appOpenAdUnitID;
    [SerializeField] private AdScheduleUnitID rewardedInterstitialAdUnitID;

    [SerializeField] private SequentialTierConfig interstitialTierConfig;
    [SerializeField] private SequentialTierConfig rewardedTierConfig;

    public AdScheduleUnitID InterstitialAdUnitID
    {
        get => interstitialAdUnitID;
        set => interstitialAdUnitID = value;
    }

    public AdScheduleUnitID RewardedAdUnitID
    {
        get => rewardedAdUnitID;
        set => rewardedAdUnitID = value;
    }

    public AdScheduleUnitID BannerAdUnitID
    {
        get => bannerAdUnitID;
        set => bannerAdUnitID = value;
    }

    public AdScheduleUnitID AppOpenAdUnitID
    {
        get => appOpenAdUnitID;
        set => appOpenAdUnitID = value;
    }

    public AdScheduleUnitID RewardedInterstitialAdUnitID
    {
        get => rewardedInterstitialAdUnitID;
        set => rewardedInterstitialAdUnitID = value;
    }
    
    public List<string> InterstitialAdUnitIDList
    {
        get => interstitialAdUnitID.CurrentPlatformID;
        set => interstitialAdUnitID.CurrentPlatformID = value;
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

    public List<string> RewardedAdUnitIDList
    {
        get => rewardedAdUnitID.CurrentPlatformID;
        set => rewardedAdUnitID.CurrentPlatformID = value;
    }

    public List<string> BannerAdUnitIDList
    {
        get => bannerAdUnitID.CurrentPlatformID;
        set => bannerAdUnitID.CurrentPlatformID = value;
    }

    public List<string> AppOpenAdUnitIDList {
        get => appOpenAdUnitID.CurrentPlatformID;
        set => appOpenAdUnitID.CurrentPlatformID = value;
    }

    public List<string> RewardedInterstitialAdUnitIDList
    {
        get => rewardedInterstitialAdUnitID.CurrentPlatformID;
        set => rewardedInterstitialAdUnitID.CurrentPlatformID = value;
    }

    /// <summary>
    /// Ensures nested unit-id schedules exist (fresh ScriptableObjects / scene controllers often have null fields).
    /// </summary>
    public void EnsureInitialized()
    {
        interstitialAdUnitID ??= new AdScheduleUnitID();
        rewardedAdUnitID ??= new AdScheduleUnitID();
        bannerAdUnitID ??= new AdScheduleUnitID();
        appOpenAdUnitID ??= new AdScheduleUnitID();
        rewardedInterstitialAdUnitID ??= new AdScheduleUnitID();
        _ = InterstitialTierConfig;
        _ = RewardedTierConfig;
    }
}
