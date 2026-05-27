using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Ads;
using UnityEngine;

[System.Serializable]
public class MaxAdSetup
{
    [SerializeField]private AdUnitID sdkKey;
    [SerializeField]private AdUnitID interstitialAdUnitID;
    [SerializeField]private AdUnitID rewardedAdUnitID;
    [SerializeField]private AdUnitID bannerAdUnitID;
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

    public string AppOpenAdUnitID
    {
        get => appOpenAdUnitID.ID;
        set => appOpenAdUnitID.ID = value;
    }
}
