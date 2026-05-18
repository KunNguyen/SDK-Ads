using JisSDKAds.Ads.UnitAdManagers;

namespace JisSDKAds.Ads.UnitAdManagers
{
     public static class AdsHelper
     {
          public static bool IsAdLoaded(UnitAdManager unitAdManager)
          {
               return unitAdManager?.IsLoaded() ?? false;
          }

          public static bool IsRemoveAds(UnitAdManager unitAdManager)
          {
               return unitAdManager?.IsRemoveAds() ?? false;
          }

          public static bool IsCheatAds(UnitAdManager unitAdManager)
          {
               return unitAdManager?.IsCheatAds() ?? false;
          }
     }
}