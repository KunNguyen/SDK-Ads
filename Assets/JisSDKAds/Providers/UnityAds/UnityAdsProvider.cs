using System;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Providers.UnityAds
{
    [CreateAssetMenu(fileName = "UnityAdsConfig", menuName = "JisSDKAds/Providers/Unity Ads Config", order = 3)]
    public class UnityAdsConfig : ScriptableObject, IAdProviderConfig
    {
        public string gameId;
        public string interstitialPlacementId = "video";
        public string rewardedPlacementId = "rewardedVideo";
        public string bannerPlacementId = "banner";

        public AdProviderId ProviderId => AdProviderId.UnityAds;
        public IAdService CreateProvider() => new UnityAdsProvider(gameId, interstitialPlacementId, rewardedPlacementId, bannerPlacementId);
    }

    /// <summary>
    /// Unity Ads implementation of IAdService.
    /// Stub implementation — add Unity Ads SDK and implement when Unity Ads package is available.
    /// </summary>
    public class UnityAdsProvider : IAdService
    {
        public string ProviderId => "UnityAds";
        public bool IsInitialized { get; private set; }

        public IInterstitialAd Interstitial { get; }
        public IRewardedAd Rewarded { get; }
        public IBannerAd Banner { get; }

        public UnityAdsProvider(string gameId, string interstitialPlacementId, string rewardedPlacementId, string bannerPlacementId)
        {
            Interstitial = new UnityAdsInterstitialStub(interstitialPlacementId);
            Rewarded = new UnityAdsRewardedStub(rewardedPlacementId);
            Banner = new UnityAdsBannerStub(bannerPlacementId);
        }

        public void Initialize(Action onSuccess, Action<string> onFailure)
        {
            // TODO: UnityAds.Initialize(gameId);
            IsInitialized = true;
            onSuccess?.Invoke();
        }

        public void SetConsent(bool hasConsent)
        {
            // TODO: UnityAds consent flow
        }
    }

    internal class UnityAdsInterstitialStub : IInterstitialAd
    {
        private readonly string _placementId;

        public UnityAdsInterstitialStub(string placementId) => _placementId = placementId;
        public bool IsLoaded => false;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            onFailed?.Invoke("Unity Ads provider not implemented. Add Unity Ads SDK and implement.");
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            onFailed?.Invoke("Unity Ads provider not implemented.");
        }
    }

    internal class UnityAdsRewardedStub : IRewardedAd
    {
        private readonly string _placementId;

        public UnityAdsRewardedStub(string placementId) => _placementId = placementId;
        public bool IsLoaded => false;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            onFailed?.Invoke("Unity Ads provider not implemented.");
        }

        public void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            onFailed?.Invoke("Unity Ads provider not implemented.");
        }
    }

    internal class UnityAdsBannerStub : IBannerAd
    {
        private readonly string _placementId;

        public UnityAdsBannerStub(string placementId) => _placementId = placementId;
        public bool IsLoaded => false;
        public bool IsVisible => false;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            onFailed?.Invoke("Unity Ads provider not implemented.");
        }

        public void Show(Action onShown = null, Action<string> onFailed = null)
        {
            onFailed?.Invoke("Unity Ads provider not implemented.");
        }

        public void Hide() { }
        public void Destroy() { }
    }
}
