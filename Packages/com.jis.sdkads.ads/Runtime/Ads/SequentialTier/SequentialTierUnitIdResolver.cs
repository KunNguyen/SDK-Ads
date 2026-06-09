using System.Collections.Generic;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Firebase;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>Resolves sequential tier unit IDs from Firebase Remote Config only.</summary>
    public static class SequentialTierUnitIdResolver
    {
        static readonly AdTier[] TierOrder =
        {
            AdTier.Premium, AdTier.High, AdTier.Mid, AdTier.Low, AdTier.Fill
        };

        public static bool TryBuildTierIds(
            SequentialTierAdFormat format,
            PlatformAdsProfile profile,
            out Dictionary<AdTier, string> tierIds)
        {
            tierIds = new Dictionary<AdTier, string>();
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return false;

            var anyRc = false;
            string cascadeId = null;

            foreach (var tier in TierOrder)
            {
                var key = SequentialTierRemoteConfigResolver.GetRemoteConfigKey(format, tier);
                if (string.IsNullOrEmpty(key)) continue;

                var rcValue = FirebaseManager.Instance.GetConfigString(key);
                if (!string.IsNullOrWhiteSpace(rcValue))
                {
                    cascadeId = rcValue.Trim();
                    tierIds[tier] = cascadeId;
                    anyRc = true;
                }
                else if (!string.IsNullOrEmpty(cascadeId))
                {
                    tierIds[tier] = cascadeId;
                }
            }

            return anyRc && tierIds.Count > 0;
        }

        public static string ResolveDefaultFallbackUnitId(
            SequentialTierAdFormat format,
            PlatformAdsProfile profile)
        {
            if (profile == null) return null;

#if UNITY_AD_ADMOB
            var admob = profile.sdkSetup?.admobAdsSetup;
            if (admob != null)
            {
                var seqConfig = format == SequentialTierAdFormat.Interstitial
                    ? admob.InterstitialTierConfig
                    : admob.RewardedTierConfig;

                var fromDefault = seqConfig?.ResolveDefaultAdUnitId();
                if (!string.IsNullOrEmpty(fromDefault)) return fromDefault;

                var list = format == SequentialTierAdFormat.Interstitial
                    ? admob.InterstitialAdUnitIDList
                    : admob.RewardedAdUnitIDList;
                if (list != null && list.Count > 0 && !string.IsNullOrWhiteSpace(list[0]))
                    return list[0].Trim();
            }
#endif
#if UNITY_AD_MAX
            if (profile.mediation == AdsMediationType.MAX && profile.sdkSetup?.maxAdsSetup != null)
            {
                var max = profile.sdkSetup.maxAdsSetup;
                var primary = format == SequentialTierAdFormat.Interstitial
                    ? max.InterstitialAdUnitID
                    : max.RewardedAdUnitID;
                if (!string.IsNullOrEmpty(primary)) return primary;
            }
#endif
            return null;
        }
    }
}
