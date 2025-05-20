using System;
using System.Collections;
using ABIMaxSDKAds.Scripts.Utils;
using SDK.AdsManagers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ABIMaxSDKAds.Scripts.Ads.AdsManagers.Service
{
     [System.Serializable]
     public class CooldownSystem
     {
          public enum CooldownState
          {
               Waiting,
               CoolingDown,
               Finished
          }
          [field: SerializeField] public UnitAdManager Owner { get; set; }
          [field: SerializeField] public float CooldownTime { get; private set; }
          [field: SerializeField] private float MaxCooldownTime { get; set; }
          [field: SerializeField] public CooldownState State { get; private set; }
          public bool IsCooldownFinished => State == CooldownState.Finished;
          
          public void Init(UnitAdManager owner, float maxCooldownTime, bool isActiveFromStart = false)
          {
               Owner = owner;
               MaxCooldownTime = maxCooldownTime;
               State = CooldownState.Waiting;
               CooldownTime = 0;
               if (isActiveFromStart)
               { 
                    StartCooldown();
               }
          }
          
          public void StartCooldown()
          {
               if (Owner != null && Owner.gameObject != null)
               {
                    var cooldownBehaviour = Owner.GetComponent<UnitCoroutineBehaviour>();
                    if (cooldownBehaviour == null)
                    {
                         cooldownBehaviour = Owner.gameObject.AddComponent<UnitCoroutineBehaviour>();
                    }
                    CooldownTime = MaxCooldownTime;
                    cooldownBehaviour.StartCoroutine(CooldownCoroutine());
               }
               State = CooldownState.CoolingDown;
          }

          public void ResetCooldown()
          {
               CooldownTime = MaxCooldownTime;
               State = CooldownState.CoolingDown;
          }
          public void StopCooldown()
          {
               DebugAds.Log(Owner.gameObject.name +  " Stop Cooldown");
               if (Owner != null && Owner.gameObject != null)
               {
                    var cooldownBehaviour = Owner.GetComponent<UnitCoroutineBehaviour>();
                    if (cooldownBehaviour != null)
                    {
                         cooldownBehaviour.StopCoroutine(CooldownCoroutine());
                         Object.DestroyImmediate(cooldownBehaviour);
                    }
               }
               State = CooldownState.Waiting;
               CooldownTime = 0;
          }

          private IEnumerator CooldownCoroutine()
          {
               while (true)
               {
                    switch (State)
                    {
                         case CooldownState.Waiting:
                         {
                              break;
                         }
                         case CooldownState.CoolingDown:
                         {
                              CooldownTime -= Time.deltaTime;
                              if (CooldownTime <= 0)
                              {
                                   State = CooldownState.Finished;
                                   CooldownTime = 0;
                              }
                              break;
                         }
                         case CooldownState.Finished:
                         {
                              break;
                         }
                    }
                    
                    yield return Yields.EndOfFrame;
               }
          }

          public void SetMaxCooldown(float value)
          {
               MaxCooldownTime = value;
          }
     }
}