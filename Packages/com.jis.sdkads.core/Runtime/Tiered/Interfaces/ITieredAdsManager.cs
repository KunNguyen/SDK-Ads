using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Interfaces
{
    public interface ITieredAdsManager
    {
        bool IsInitialized { get; }
        TierInventory GetInventory(AdsFormatType format);
        void Initialize();
        void OnApplicationPause(bool paused);
        void EnqueueReload(AdsFormatType format, AdTier tier);
        bool IsAnyLoaded(AdsFormatType format);
    }
}
