using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDK
{
     [System.Serializable]
     public class AdsStateMachine
     {
          public enum AdsState
          {
               NotInitialized,
               Initializing,
               Ready,
               ShowingAds,
               RemoteConfigUpdating,
          }
          
          [field: SerializeField] private AdsState CurrentState{ get; set; }
          private readonly Dictionary<AdsState, Action> m_StateActions = new Dictionary<AdsState, Action>();

          public AdsStateMachine()
          {
               m_StateActions[AdsState.Initializing] = OnInitializing;
               m_StateActions[AdsState.Ready] = OnReady;
               m_StateActions[AdsState.ShowingAds] = OnShowingAds;
               m_StateActions[AdsState.RemoteConfigUpdating] = OnRemoteConfigUpdating;
               
               CurrentState = AdsState.NotInitialized;
          }
          
          public void ChangeState(AdsState newState)
          {
               if (CurrentState == newState) return;
               
               CurrentState = newState;
               
               if (m_StateActions.TryGetValue(CurrentState, out var action))
               {
                    action.Invoke();
               }
          }
          
          private void OnInitializing()
          {
               // Initialize ads SDK here
               Debug.Log("Ads SDK is initializing...");
          }
          private void OnShowingAds()
          {
               // Show ads here
               Debug.Log("Showing ads...");
          }

          private void OnRemoteConfigUpdating()
          {
               // Update remote config here
               Debug.Log("Updating remote config...");
          }
          private void OnReady()
          {
               // Ads SDK is ready to show ads
               Debug.Log("Ads SDK is ready.");
          }

          public AdsState GetCurrentState()
          {
               return CurrentState;
          }
     }
}