#if UNITY_AD_ADMOB
using System;
using GoogleMobileAds.Api;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal struct SequentialTierShowHooks
    {
        public Action onClosed;
        public Action onOpened;
        public Action<AdError> onFailed;
        public Action<AdValue> onPaid;
        public Action onRewardGranted;
    }

    internal interface ISequentialTierAdAdapter
    {
        bool IsReady { get; }
        void Destroy();
        void Load(string adUnitId, int loadGeneration, int expectedGeneration,
            Action onSuccess, Action<LoadAdError> onFail);
        void RegisterShowCallbacks(SequentialTierShowHooks hooks);
        bool TryShow();
    }
}
#endif
