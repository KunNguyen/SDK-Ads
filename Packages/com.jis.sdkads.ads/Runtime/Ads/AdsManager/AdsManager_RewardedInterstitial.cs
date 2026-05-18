#if UNITY_AD_ADMOB
using System;
using JisSDKAds.Common;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads
{
    public partial class AdsManager
    {
        [Header("AdMob Rewarded Interstitial")]
        [SerializeField] private string admobRewardedInterstitialUnitIdAndroid;
        [SerializeField] private string admobRewardedInterstitialUnitIdIOS;

        private RewardedInterstitialAd admobRewardedInterstitial;
        private bool admobRewardedInterstitialLoading;

        private string GetAdmobRewardedInterstitialUnitId()
        {
            // NEW: ưu tiên lấy từ SDKSetup -> AdmobAdSetup
            try
            {
                var fromSetup = CurrentSDKSetup?.admobAdsSetup?.RewardedInterstitialAdUnitID?.ID;
                if (!string.IsNullOrEmpty(fromSetup))
                    return fromSetup;
            }
            catch
            {
                // ignore -> fallback xuống legacy fields bên dưới
            }

#if UNITY_ANDROID
            return admobRewardedInterstitialUnitIdAndroid;
#elif UNITY_IOS
            return admobRewardedInterstitialUnitIdIOS;
#else
            // Editor/other platforms: ưu tiên Android để dev test nhanh
            return string.IsNullOrEmpty(admobRewardedInterstitialUnitIdAndroid)
                ? admobRewardedInterstitialUnitIdIOS
                : admobRewardedInterstitialUnitIdAndroid;
#endif
        }

        public void LoadAdmobRewardedInterstitial()
        {
            if (admobRewardedInterstitialLoading) return;

            string unitId = GetAdmobRewardedInterstitialUnitId();
            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogError("[AdsManager][AdMob][RewardedInterstitial] Missing Ad Unit Id (Android/iOS).");
                return;
            }

            // Nếu ad cũ vẫn còn dùng được thì khỏi load lại
            if (admobRewardedInterstitial != null && admobRewardedInterstitial.CanShowAd())
                return;

            admobRewardedInterstitialLoading = true;

            var request = new AdRequest();

            RewardedInterstitialAd.Load(unitId, request, (RewardedInterstitialAd ad, LoadAdError error) =>
            {
                admobRewardedInterstitialLoading = false;

                if (error != null || ad == null)
                {
                    DebugAds.LogWarning($"[AdsManager][AdMob][RewardedInterstitial] Load failed: {error}");
                    admobRewardedInterstitial = null;
                    return;
                }

                admobRewardedInterstitial = ad;

                // (Optional) hook events để debug
                admobRewardedInterstitial.OnAdPaid += (AdValue adValue) =>
                {
                    DebugAds.Log($"[AdsManager][AdMob][RewardedInterstitial] Paid: {adValue.Value} {adValue.CurrencyCode}");
                };
                admobRewardedInterstitial.OnAdFullScreenContentClosed += () =>
                {
                    DebugAds.Log("[AdsManager][AdMob][RewardedInterstitial] Closed");
                    // Auto reload for next time
                    LoadAdmobRewardedInterstitial();
                };
                admobRewardedInterstitial.OnAdFullScreenContentFailed += (AdError adError) =>
                {
                    DebugAds.LogWarning($"[AdsManager][AdMob][RewardedInterstitial] FullScreen failed: {adError}");
                    admobRewardedInterstitial = null;
                    LoadAdmobRewardedInterstitial();
                };

                DebugAds.Log("[AdsManager][AdMob][RewardedInterstitial] Loaded successfully");
            });
        }

        public bool IsAdmobRewardedInterstitialLoaded()
        {
            return admobRewardedInterstitial != null && admobRewardedInterstitial.CanShowAd();
        }

        public void ShowAdmobRewardedInterstitial(
            UnityAction rewardCallback,
            UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null)
        {
            if (IsRemoveAds)
            {
                DebugAds.Log("[AdsManager][AdMob][RewardedInterstitial] Skip show because RemoveAds = true");
                failedCallback?.Invoke();
                closedCallback?.Invoke(false);
                return;
            }

            if (!IsAdmobRewardedInterstitialLoaded())
            {
                DebugAds.LogWarning("[AdsManager][AdMob][RewardedInterstitial] Not ready -> request load");
                LoadAdmobRewardedInterstitial();
                failedCallback?.Invoke();
                closedCallback?.Invoke(false);
                return;
            }

            bool rewarded = false;

            try
            {
                admobRewardedInterstitial.Show((Reward reward) =>
                {
                    rewarded = true;
                    rewardCallback?.Invoke();
                });
            }
            catch (Exception e)
            {
                DebugAds.LogException("[AdsManager][AdMob][RewardedInterstitial] Exception on Show", e);
                failedCallback?.Invoke();
                closedCallback?.Invoke(false);
                return;
            }

            // GoogleMobileAds gọi close event async; để đồng bộ kiểu API bạn đang dùng,
            // ta fire closedCallback trong event Close (đã hook ở Load).
            // Nhưng để đảm bảo closedCallback được gọi đúng instance, ta re-hook tạm thời:
            admobRewardedInterstitial.OnAdFullScreenContentClosed += () =>
            {
                closedCallback?.Invoke(rewarded);

                // Giống flow rewarded hiện tại của bạn: reset cooldown interstitial sau khi xem xong
                if (InterstitialAdManager != null)
                    InterstitialAdManager.ResetCooldown();
            };
        }
    }
}
#endif
