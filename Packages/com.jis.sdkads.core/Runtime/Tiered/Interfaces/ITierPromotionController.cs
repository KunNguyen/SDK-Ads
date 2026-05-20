using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Interfaces
{
    public interface ITierPromotionController
    {
        void EvaluatePromotions(TierInventory inventory);
        void EvaluateRecovery(TierInventory inventory);
        bool TryPromote(TierInventory inventory, AdTier from, AdTier to, string reason);
        bool TryRestore(TierInventory inventory, AdTier tier, string reason);
    }
}
