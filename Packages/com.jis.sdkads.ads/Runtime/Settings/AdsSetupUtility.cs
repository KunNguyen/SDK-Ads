using System.Collections.Generic;

namespace JisSDKAds.Ads.Settings
{
    /// <summary>
    /// Read-only helpers for SDKSetup format / platform status (runtime + editor).
    /// </summary>
    public static class AdsSetupUtility
    {
        public struct FormatStatus
        {
            public AdsType Type;
            public AdsMediationType Mediation;
            public bool IsActive;
            public string Label;

            public FormatStatus(AdsType type, AdsMediationType mediation, bool isActive)
            {
                Type = type;
                Mediation = mediation;
                IsActive = isActive;
                Label = type.ToString();
            }
        }

        static readonly AdsType[] ManagedFormats =
        {
            AdsType.BANNER,
            AdsType.INTERSTITIAL,
            AdsType.REWARDED,
            AdsType.APP_OPEN
        };

        public static IReadOnlyList<FormatStatus> GetFormatStatuses(SDKSetup setup)
        {
            var list = new List<FormatStatus>(ManagedFormats.Length);
            if (setup == null) return list;

            foreach (var type in ManagedFormats)
            {
                var mediation = setup.GetAdsMediationType(type);
                list.Add(new FormatStatus(type, mediation, setup.IsActiveAdsType(type)));
            }

            return list;
        }

        public static int CountActiveFormats(SDKSetup setup)
        {
            if (setup == null) return 0;
            var count = 0;
            foreach (var type in ManagedFormats)
            {
                if (setup.IsActiveAdsType(type))
                    count++;
            }

            return count;
        }

        public static bool HasValidPrimaryMediation(SDKSetup setup)
        {
            return setup != null && setup.adsMediationType != AdsMediationType.NONE;
        }
    }
}
