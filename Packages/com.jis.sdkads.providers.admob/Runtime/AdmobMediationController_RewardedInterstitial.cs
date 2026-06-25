#if UNITY_AD_ADMOB
using System;
using GoogleMobileAds.Api;
using JisSDKAds.Common;
using JisSDKAds.Providers.AdMob;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads
{
    public partial class AdmobMediationController : IAdMobRewardedInterstitialHost
    {
        private AdmobAdSetup _rewardedInterstitialSetup;
        private string _rewardedInterstitialAndroidUnitId;
        private string _rewardedInterstitialIosUnitId;
        private RewardedInterstitialAd _rewardedInterstitial;
        private bool _rewardedInterstitialLoading;

        public void ConfigureRewardedInterstitial(AdmobAdSetup setup, string androidUnitId, string iosUnitId)
        {
            _rewardedInterstitialSetup = setup;
            _rewardedInterstitialAndroidUnitId = androidUnitId;
            _rewardedInterstitialIosUnitId = iosUnitId;
        }

        public void LoadRewardedInterstitial()
        {
            if (_rewardedInterstitialLoading) return;

            var unitId = GetRewardedInterstitialUnitId();
            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogError("[AdMob][RewardedInterstitial] Missing Ad Unit Id (Android/iOS).");
                return;
            }

            if (_rewardedInterstitial != null && _rewardedInterstitial.CanShowAd())
                return;

            _rewardedInterstitialLoading = true;
            var request = new AdRequest();
            RewardedInterstitialAd.Load(unitId, request, (RewardedInterstitialAd ad, LoadAdError error) =>
            {
                var errorText = error?.ToString();
                AdMobMainThread.Run(() =>
                {
                    _rewardedInterstitialLoading = false;
                    if (error != null || ad == null)
                    {
                        DebugAds.LogWarning($"[AdMob][RewardedInterstitial] Load failed: {errorText}");
                        _rewardedInterstitial = null;
                        return;
                    }

                    _rewardedInterstitial = ad;
                    _rewardedInterstitial.OnAdFullScreenContentClosed += () =>
                        AdMobMainThread.Run(() => LoadRewardedInterstitial());
                    _rewardedInterstitial.OnAdFullScreenContentFailed += _ =>
                        AdMobMainThread.Run(() => LoadRewardedInterstitial());
                    DebugAds.Log("[AdMob][RewardedInterstitial] Loaded successfully");
                });
            });
        }

        public bool IsRewardedInterstitialLoaded() =>
            _rewardedInterstitial != null && _rewardedInterstitial.CanShowAd();

        public void ShowRewardedInterstitial(UnityAction rewardCallback, UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null)
        {
            if (!IsRewardedInterstitialLoaded())
            {
                LoadRewardedInterstitial();
                failedCallback?.Invoke();
                closedCallback?.Invoke(false);
                return;
            }

            JisAds.Instance?.HideBannerForFullscreenAd("rewarded_interstitial");

            var rewarded = false;
            void OnClosed() => AdMobMainThread.Run(() =>
            {
                closedCallback?.Invoke(rewarded);
                JisAds.Instance?.ScheduleBannerRestoreAfterFullscreenAd("rewarded_interstitial");
            });

            void OnFailed() => AdMobMainThread.Run(() =>
            {
                failedCallback?.Invoke();
                closedCallback?.Invoke(false);
                JisAds.Instance?.ScheduleBannerRestoreAfterFullscreenAd("rewarded_interstitial");
            });

            try
            {
                _rewardedInterstitial.Show(reward => AdMobMainThread.Run(() =>
                {
                    rewarded = true;
                    rewardCallback?.Invoke();
                }));
            }
            catch (Exception e)
            {
                DebugAds.LogException("[AdMob][RewardedInterstitial] Exception on Show", e);
                OnFailed();
                return;
            }

            _rewardedInterstitial.OnAdFullScreenContentClosed += OnClosed;
            _rewardedInterstitial.OnAdFullScreenContentFailed += _ => OnFailed();
        }

        private string GetRewardedInterstitialUnitId()
        {
            try
            {
                var fromSetup = _rewardedInterstitialSetup?.RewardedInterstitialAdUnitID?.ID;
                if (!string.IsNullOrEmpty(fromSetup)) return fromSetup;
            }
            catch
            {
                // ignored
            }

#if UNITY_ANDROID
            return _rewardedInterstitialAndroidUnitId;
#elif UNITY_IOS
            return _rewardedInterstitialIosUnitId;
#else
            return string.IsNullOrEmpty(_rewardedInterstitialAndroidUnitId)
                ? _rewardedInterstitialIosUnitId
                : _rewardedInterstitialAndroidUnitId;
#endif
        }
    }
}
#endif
