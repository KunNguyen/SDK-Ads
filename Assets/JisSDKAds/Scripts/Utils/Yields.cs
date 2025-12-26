using System.Collections.Generic;
using UnityEngine;

namespace ABIMaxSDKAds.Scripts.Utils
{
     public static class Yields {

          public static WaitForEndOfFrame EndOfFrame { get; } = new WaitForEndOfFrame();

          public static WaitForFixedUpdate FixedUpdate { get; } = new WaitForFixedUpdate();

          public static WaitForSeconds Get(float seconds)
          {
               if (!timeInterval.ContainsKey(seconds))
               {
                    timeInterval.Add(seconds, new WaitForSeconds(seconds));
               }
               return timeInterval[seconds];
          }

          private static Dictionary<float, WaitForSeconds> timeInterval = new Dictionary<float, WaitForSeconds>(100);
     }
}