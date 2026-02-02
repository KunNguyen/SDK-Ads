using SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestingBanner : MonoBehaviour
{
    public Text m_TextStatus;
    public void OnLoadBanner() {
        AdsManager.Instance.RequestBannerAds();
    }
    public void OnShowBanner() {
        AdsManager.Instance.ShowBannerAds();
    }
    public void OnDestroyBanner() {
        AdsManager.Instance.DestroyBanner();
    }
    private void Update() {
        UpdateStatus();
    }
    public void UpdateStatus() {
        m_TextStatus.text = "Banner Load: " + AdsManager.Instance.CanShowBannerAd();
    }

}
