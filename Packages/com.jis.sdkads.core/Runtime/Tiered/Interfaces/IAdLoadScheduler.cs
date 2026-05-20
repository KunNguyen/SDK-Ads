using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Interfaces
{
    public interface IAdLoadScheduler
    {
        void Start();
        void Stop();
        void Enqueue(AdsFormatType format, AdTier tier, bool force = false);
        void EnqueueFullInventoryRefresh();
        bool IsProcessing { get; }
    }
}
