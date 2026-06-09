using System;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    [Serializable]
    public class SequentialTierEntry
    {
        public AdTier tier = AdTier.Premium;

        [Tooltip("Android AdMob unit id for this tier.")]
        public string androidAdUnitId;

        [Tooltip("iOS AdMob unit id for this tier.")]
        public string iosAdUnitId;

        [Tooltip("Load timeout in seconds. -1 = package default; 0 on Fill = no timeout.")]
        public float timeoutSeconds = -1f;

        [NonSerialized] string _remoteAdUnitId;

        public bool HasUnitId => HasRemoteAdUnitId;

        public bool HasRemoteAdUnitId => !string.IsNullOrWhiteSpace(_remoteAdUnitId);

        public void SetRemoteAdUnitId(string adUnitId)
        {
            _remoteAdUnitId = string.IsNullOrWhiteSpace(adUnitId) ? null : adUnitId.Trim();
        }

        public void ClearRemoteAdUnitId() => _remoteAdUnitId = null;

        public string ResolveAdUnitId()
        {
            return ResolveRemoteAdUnitId();
        }

        public string ResolveRemoteAdUnitId()
        {
            if (!string.IsNullOrWhiteSpace(_remoteAdUnitId))
                return _remoteAdUnitId.Trim();

            return null;
        }
    }
}
