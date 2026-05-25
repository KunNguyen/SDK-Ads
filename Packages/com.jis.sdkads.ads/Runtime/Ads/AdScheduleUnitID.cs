using System;
using System.Collections.Generic;
using JisSDKAds.Ads.Settings;

namespace JisSDKAds.Ads
{
     [Serializable]
     public class AdScheduleUnitID
     {
          public List<string> AndroidID = new List<string>();
          public List<string> IosID = new List<string>();

          private int CurrentID { get; set; }

          public void ChangeID()
          {
               CurrentID++;
               var list = CurrentPlatformID;
               if (list == null || list.Count == 0)
               {
                    CurrentID = 0;
                    return;
               }

               if (CurrentID >= list.Count)
                    CurrentID = 0;
          }

          public void Refresh() => CurrentID = 0;

          public string ID
          {
               get
               {
                    var list = CurrentPlatformID;
                    if (list == null || list.Count == 0)
                         return string.Empty;

                    if (CurrentID < 0 || CurrentID >= list.Count)
                         CurrentID = 0;

                    return list[CurrentID];
               }
          }

          /// <summary>Runtime active platform list (build target).</summary>
          public List<string> CurrentPlatformID
          {
               get
               {
#if UNITY_IOS
                    return IosID;
#else
                    return AndroidID;
#endif
               }
               set
               {
#if UNITY_IOS
                    IosID = value ?? new List<string>();
#else
                    AndroidID = value ?? new List<string>();
#endif
               }
          }

          public List<string> GetPlatformList(BuildTargetPlatform platform) =>
               platform == BuildTargetPlatform.iOS ? IosID : AndroidID;

          public void SetPlatformList(BuildTargetPlatform platform, List<string> ids)
          {
               if (platform == BuildTargetPlatform.iOS)
                    IosID = ids ?? new List<string>();
               else
                    AndroidID = ids ?? new List<string>();
          }

          public bool IsActive()
          {
               var list = CurrentPlatformID;
               return list != null && list.Count > 0;
          }
     }
}
