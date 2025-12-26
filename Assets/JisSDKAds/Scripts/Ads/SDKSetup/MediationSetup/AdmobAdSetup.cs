using System.Collections;
using System.Collections.Generic;
using SDK;
using UnityEngine;

[System.Serializable]
public class AdmobAdSetup
{
    [SerializeField]private AdScheduleUnitID interstitialAdUnitID;
    [SerializeField]private AdScheduleUnitID rewardedAdUnitID;
    [SerializeField]private AdScheduleUnitID bannerAdUnitID;
    [SerializeField]private AdScheduleUnitID collapsibleBannerAdUnitID;
    [SerializeField]private AdScheduleUnitID mrecAdUnitID;
    [SerializeField]private AdScheduleUnitID appOpenAdUnitID;

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

    public AdScheduleUnitID CollapsibleBannerAdUnitID
    {
        get => collapsibleBannerAdUnitID;
        set => collapsibleBannerAdUnitID = value;
    }
    
    public AdScheduleUnitID MrecAdUnitID
    {
        get => mrecAdUnitID;
        set => mrecAdUnitID = value;
    }

    public AdScheduleUnitID AppOpenAdUnitID
    {
        get => appOpenAdUnitID;
        set => appOpenAdUnitID = value;
    }
    
    public List<string> InterstitialAdUnitIDList
    {
        get => interstitialAdUnitID.CurrentPlatformID;
        set => interstitialAdUnitID.CurrentPlatformID = value;
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
    public List<string> CollapsibleBannerAdUnitIDList
    {
        get => collapsibleBannerAdUnitID.CurrentPlatformID;
        set => collapsibleBannerAdUnitID.CurrentPlatformID = value;
    }
    public List<string> MrecAdUnitIDList
    {
        get => mrecAdUnitID.CurrentPlatformID;
        set => mrecAdUnitID.CurrentPlatformID = value;
    }
    public List<string> AppOpenAdUnitIDList {
        get => appOpenAdUnitID.CurrentPlatformID;
        set => appOpenAdUnitID.CurrentPlatformID = value;
    }
}
