using System.Collections;
using System.Collections.Generic;
using SDK;
using UnityEngine;

[System.Serializable]
public class MaxAdSetup
{
    [SerializeField]private AdUnitID sdkKey;
    [SerializeField]private AdUnitID interstitialAdUnitID;
    [SerializeField]private AdUnitID rewardedAdUnitID;
    [SerializeField]private AdUnitID bannerAdUnitID;
    [SerializeField]private AdUnitID collapsibleBannerAdUnitID;
    [SerializeField]private AdUnitID mrecAdUnitID;
    [SerializeField]private AdUnitID appOpenAdUnitID;

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
    
    public string CollapsibleBannerAdUnitID
    {
        get => collapsibleBannerAdUnitID.ID;
        set => collapsibleBannerAdUnitID.ID = value;
    }
    
    public string MrecAdUnitID
    {
        get => mrecAdUnitID.ID;
        set => mrecAdUnitID.ID = value;
    }

    public string AppOpenAdUnitID
    {
        get => appOpenAdUnitID.ID;
        set => appOpenAdUnitID.ID = value;
    }
}