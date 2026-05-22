using System.Collections.Generic;
using UnityEngine;

namespace JisSDKAds.Ads
{
    [CreateAssetMenu(fileName = "RewardAdsPlacementConfig", menuName = "SDK/RewardAdsPlacementConfig")]
    public class RewardAdsPlacementConfig : ScriptableObject
    {
        public List<string> placementIds = new List<string>();
    }
}
