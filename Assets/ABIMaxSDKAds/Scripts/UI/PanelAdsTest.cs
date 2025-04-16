using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SDK;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PanelAdsTest : MonoBehaviour
{
    [Header("Banner")]
    private bool IsShowingBanner;
    public TextMeshProUGUI m_TextBannerCountTime;
    public TextMeshProUGUI m_TextBannerStatus;
    public Button m_ButtonShowBanner, m_ButtonHideBanner, m_ButtonDestroyBanner;
    
    [Header("Banner")]
    private bool IsShowingCollapsibleBanner;
    public TextMeshProUGUI m_TextCollapsibleBannerStatus;
    public Button m_ButtonShowCollapsibleBanner, m_ButtonHideCollapsibleBanner, m_ButtonDestroyCollapsibleBanner;
    
    public Button m_ButtonMREC;
    
    public TextMeshProUGUI m_TextInterstitialStatus;
    public TextMeshProUGUI m_TextRewardedStatus;
    
    public TextMeshProUGUI m_TextMRECStatus;
    
    private bool IsShowingMREC;
    
    private void Start()
    {
        m_ButtonMREC.onClick.AddListener(() =>
        {
            if (IsShowingMREC)
            {
                HideMrec();
            }
            else
            {
                ShowMrec();
            }
            IsShowingMREC = !IsShowingMREC;
        });
        m_ButtonShowBanner.onClick.AddListener(() =>
        {
            ShowBanner();
            IsShowingBanner = true;
        });
        m_ButtonHideBanner.onClick.AddListener(() =>
        {
            HideBanner();
            IsShowingBanner = false;
        });
        m_ButtonDestroyBanner.onClick.AddListener(() =>
        {
            AdsManager.Instance.DestroyBanner();
            IsShowingBanner = false;
        });
        
        m_ButtonShowCollapsibleBanner.onClick.AddListener(() =>
        {
            ShowCollapsibleBanner();
            IsShowingBanner = true;
        });
        m_ButtonHideCollapsibleBanner.onClick.AddListener(() =>
        {
            HideCollapsibleBanner();
            IsShowingCollapsibleBanner = false;
        });
        m_ButtonDestroyCollapsibleBanner.onClick.AddListener(() =>
        {
            AdsManager.Instance.DestroyCollapsibleBanner();
            IsShowingCollapsibleBanner = false;
        });
    }

    private void Update()
    {
        if (AdsManager.Instance.BannerAdManager.IsShowingAd)
        {
            m_TextBannerStatus.text = "Banner Ad Showing";
        }
        else
        {
            m_TextBannerStatus.text = AdsManager.Instance.IsBannerLoaded() ? "Banner Ad Loaded" : "Banner Ad Not Loaded";
        }
        if (AdsManager.Instance.IsCollapsibleBannerExpended())
        {
            m_TextCollapsibleBannerStatus.text = "Collapsible Banner Ad Showing";
        }
        else
        {
            m_TextCollapsibleBannerStatus.text = AdsManager.Instance.IsBannerLoaded() ? "Collapsible Banner Ad Loaded" : "Collapsible Banner Ad Not Loaded";
        }
        if (AdsManager.Instance.IsMRecShowing())
        {
            m_TextMRECStatus.text = "MREC Ad Showing";
        }
        else
        {
            m_TextMRECStatus.text = AdsManager.Instance.IsMRecLoaded() ? "MREC Ad Loaded" : "MREC Ad Not Loaded";
        }
        m_TextInterstitialStatus.text = AdsManager.Instance.IsInterstitialAdLoaded() ? "Interstitial Ad Loaded" : "Interstitial Ad Not Loaded";
        m_TextRewardedStatus.text = AdsManager.Instance.IsRewardVideoLoaded() ? "Rewarded Ad Loaded" : "Rewarded Ad Not Loaded";
        m_TextBannerCountTime.text = AdsManager.Instance.BannerAdManager.BannerAutoResetTime.ToString(CultureInfo.InvariantCulture);
    }

    public void ShowBanner()
    {
        AdsManager.Instance.ShowBannerAds();
    }
    public void HideBanner()
    {
        AdsManager.Instance.HideBannerAds();
    }
    
    public void ShowCollapsibleBanner()
    {
        AdsManager.Instance.HideBannerAds();
        AdsManager.Instance.ShowCollapsibleBannerAds(null);
    }
    public void HideCollapsibleBanner()
    {
        AdsManager.Instance.HideCollapsibleBannerAds();
    }
    
    public void ShowInter()
    {
        AdsManager.Instance.ShowInterstitial(null,null,null);
    }

    public void ShowReward()
    {
        AdsManager.Instance.ShowRewardVideo(null,null);
    }
    public void ShowMrec()
    {
        AdsManager.Instance.ShowMRecAds();
    }
    public void HideMrec()
    {
        AdsManager.Instance.HideMRecAds();
    }
}
