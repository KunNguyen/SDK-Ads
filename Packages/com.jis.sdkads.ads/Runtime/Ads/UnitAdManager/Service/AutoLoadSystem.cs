using System;
using JisSDKAds.Common;
using JisSDKAds.Ads.UnitAdManagers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JisSDKAds.Ads.UnitAdManagers.Service
{
     [System.Serializable]
     public class AutoLoadSystem
     {
          public enum LoadState
          {
               None,
               ReadyToLoad,
               Loading,
               Loaded,
               Failed
          }
          private const int max_interval_time = 6;
          private const float min_delay_after_close = 2f;
          [field: SerializeField] private UnitAdManager Owner { get; set; }
          [field: SerializeField] private int IntervalTime { get; set; }
          [field: SerializeField] public LoadState State { get; private set; } = LoadState.None;
          [field: SerializeField] public bool IsActiveLogging { get; set; } = false;
          private float _nextLoadAllowedRealtime;

          public Action OnReload { get; set; } = null;
          
          public void Init(UnitAdManager owner, Action onReload)
          {
               Owner = owner;
               OnReload = onReload;
               IntervalTime = 0;
               State = LoadState.ReadyToLoad;
          }
          
          public void StartAutoLoad()
          {
               if (Owner != null)
               {
                    var autoReloadBehaviour = Owner.GetComponent<UnitCoroutineBehaviour>();
                    if (autoReloadBehaviour == null)
                    {
                         autoReloadBehaviour = Owner.gameObject.AddComponent<UnitCoroutineBehaviour>();
                    }
                    autoReloadBehaviour.StartCoroutine(AutoLoadTask());
               }
          }
          public void StopAutoLoad()
          {
               if (Owner != null)
               {
                    var autoReloadBehaviour = Owner.GetComponent<UnitCoroutineBehaviour>();
                    if (autoReloadBehaviour != null)
                    {
                         autoReloadBehaviour.StopAllCoroutines();
                         Object.DestroyImmediate(autoReloadBehaviour);
                    }
               }
          }
          private System.Collections.IEnumerator AutoLoadTask()
          {
               while (true)
               {
                    switch (State)
                    {
                         case LoadState.ReadyToLoad:
                         {
                              // After an ad is closed, wait out the minimum gap before requesting a new load.
                              var wait = _nextLoadAllowedRealtime - Time.realtimeSinceStartup;
                              if (wait > 0f)
                              {
                                   yield return Yields.Get(wait);
                                   break;
                              }
                              OnReload?.Invoke();
                              State = LoadState.Loading;
                              break;
                         }
                         case LoadState.Loading:
                         {
                              break;
                         }
                         case LoadState.Loaded:
                         {
                              break;
                         }
                         case LoadState.Failed:
                         {
                              State = LoadState.ReadyToLoad;
                              double delayTime = Math.Pow(2f, Math.Min(IntervalTime, max_interval_time));
                              DebugAds.Log(Owner.gameObject.name + " Start Delay Time = " + delayTime);
                              yield return Yields.Get((float)delayTime);
                              break;
                         }
                    }
                    yield return Yields.Get(1f);
               }
          }

          public void OnRemoveAds()
          {
               State = LoadState.None;
          }
          public void OnAdClosed()
          {
               State = LoadState.ReadyToLoad;
               _nextLoadAllowedRealtime = Time.realtimeSinceStartup + min_delay_after_close;
          }
          public void OnLoadSuccess()
          {
               IntervalTime = 0;
               State = LoadState.Loaded;
          }
          public void OnLoadFailed()
          {
               IntervalTime++;
               State = LoadState.Failed;
          }
     }
}