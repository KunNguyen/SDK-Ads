using System;
using System.Collections.Generic;

namespace SDK
{
     [Serializable]
     public class AdScheduleUnitID
     {
#if UNITY_ANDROID
          public List<string> AndroidID = new List<string>();
#elif UNITY_IOS
        public List<string> IosID = new List<string>();
#endif

          private int CurrentID { get; set; } = 0;

          public void ChangeID()
          {
               CurrentID++;
               if (CurrentID >= CurrentPlatformID.Count)
               {
                    CurrentID = 0;
               }
          }

          public void Refresh()
          {
               CurrentID = 0;
          }

          public string ID
          {
               get
               {
                    if (CurrentPlatformID == null || CurrentPlatformID.Count == 0)
                    {
                         return string.Empty;
                    }

                    if (CurrentID < 0 || CurrentID >= CurrentPlatformID.Count)
                    {
                         CurrentID = 0;
                    }

                    return CurrentPlatformID[CurrentID];
               }
          }

          public List<string> CurrentPlatformID
          {
               get
               {
#if UNITY_ANDROID
                    return AndroidID;
#elif UNITY_IOS
                return IosID;
#else
                return null;
#endif
               }
               set
               {
#if UNITY_ANDROID
                    AndroidID = value;
#elif UNITY_IOS
                IosID = value;
#endif
               }
          }

          public bool IsActive()
          {
               return CurrentPlatformID.Count > 0;
          }
     }
}