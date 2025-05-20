using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace SDK
{
     public partial class SDKSetup
     {
          [BoxGroup("REWARDED"), PropertyOrder(3)]
          public AdsMediationType rewardedAdsMediationType;

          [BoxGroup("REWARDED"), PropertyOrder(3)]
          [ShowInInspector, ShowIf("@rewardedAdsMediationType != AdsMediationType.NONE")]
          public bool IsLinkToRemoveAds = true;

          [BoxGroup("REWARDED"), PropertyOrder(3)]
          [ShowInInspector, ShowIf("@rewardedAdsMediationType == AdsMediationType.MAX")]
          public string rewardedAdUnitID_MAX
          {
               get => maxAdsSetup.RewardedAdUnitID;
               set => maxAdsSetup.RewardedAdUnitID = value;
          }

          [BoxGroup("REWARDED"), PropertyOrder(3)]
          [ShowInInspector, ShowIf("@rewardedAdsMediationType == AdsMediationType.ADMOB")]
          public List<string> rewardedAdUnitID_ADMOB
          {
               get => admobAdsSetup.RewardedAdUnitIDList;
               set => admobAdsSetup.RewardedAdUnitIDList = value;
          }

          [BoxGroup("REWARDED"), PropertyOrder(3)]
          [ShowInInspector, ShowIf("@rewardedAdsMediationType == AdsMediationType.IRONSOURCE")]
          public string rewardedAdUnitID_IRONSOURCE
          {
               get => ironSourceAdSetup.rewardedID;
               set => ironSourceAdSetup.rewardedID = value;
          }

     }
}