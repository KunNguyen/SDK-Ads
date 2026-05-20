using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Interfaces
{
    public interface ITierPersistenceService
    {
        void Save(TierInventory inventory);
        void Load(TierInventory inventory);
        void SaveAll(TierInventory interstitial, TierInventory rewarded);
        void LoadAll(TierInventory interstitial, TierInventory rewarded);
    }
}
