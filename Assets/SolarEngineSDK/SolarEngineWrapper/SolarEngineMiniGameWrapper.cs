#if (SOLARENGINE_BYTEDANCE||SOLARENGINE_WECHAT||SOLARENGINE_KUAISHOU||SOLARENGINE_BYTEDANCE_CLOUD||SOLARENGINE_BYTEDANCE_STARK)&&(!UNITY_EDITOR||SE_DEV||SOLORENGINE_DEVELOPEREDITOR)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using Newtonsoft.Json;

using SolaEngine.MiniGames.Enum;
using SolarEngine.MiniGames;
using SolarEngine.MiniGames.info;
using SolarEngine.MiniGames.Wrapper.SDKWrapper;
using UnityEngine;

namespace SolarEngine
{
    public partial class Analytics : MonoBehaviour
    {
     
        // private static SESDKInitCompletedCallback initCompletedCallback_miniGame;
        // private static SEAttributionCallback attCompletedCallback_miniGame;
        
        private static Dictionary<string, object> GetPresetProperties()
        {
            return SolarEngineSDK4MiniGames.getPresetProperties();
        }

        private static void PreInitSeSdk(string appKey)
        {
            SolarEngineSDK4MiniGames.prevInit(appKey);
        }


        private static void Init(string appKey, string userId, SEConfig config)
        {
          
            SolarEngineSDK4MiniGames.init(appKey, initSDKInitParams(config));
        }
      
        private static void Init(string appKey, string userId, SEConfig config, RCConfig rcConfig)
        {
           
            MiniGameRCConfig minircConfig = new MiniGameRCConfig();
            minircConfig.enable = rcConfigEnable(rcConfig);
            minircConfig.mergeType = (MiniRCMergeType)(int)rcConfig.mergeType;
            minircConfig.customIDEventProperties = rcConfig.customIDEventProperties;
            minircConfig.customIDProperties = rcConfig.customIDProperties;
            minircConfig.customIDUserProperties = rcConfig.customIDUserProperties;

            SolarEngineSDK4MiniGames.init(appKey, initSDKInitParams(config), minircConfig);
        }

        // private static void InitCompletedCallback( SESDKInitCompletedCallback  callback)
        // {
        //     initCompletedCallback_miniGame=callback;
        // }
        // private static void AttributionCompletedCallback( SEAttributionCallback callback)
        // {
        //     attCompletedCallback_miniGame=callback;
       // }
        private static InitParams initSDKInitParams( SEConfig config)
        {
            // if(config.initCompletedCallback==null&& initCompletedCallback_miniGame!=null)
            //     config.initCompletedCallback = initCompletedCallback_miniGame;
            // if(config.attributionCallback==null&& attCompletedCallback_miniGame!=null)
            //     config.attributionCallback = attCompletedCallback_miniGame;
              InitParams initParams = new InitParams();

            if (config.initCompletedCallback != null)
            {
                SolarEngineSDK4MiniGames.MiniGameInitCompletedCallback initCompletedCallback =
                    (SolarEngineSDK4MiniGames.MiniGameInitCompletedCallback)Delegate.CreateDelegate(
                        typeof(SolarEngineSDK4MiniGames.MiniGameInitCompletedCallback),
                        config.initCompletedCallback.Target, config.initCompletedCallback.Method);
                initParams.miniGameInitCompletedCallback = initCompletedCallback;
            }
            if (config.attributionCallback != null)
            {
                SolarEngineSDK4MiniGames.MiniGameAttributionCallback attributionCallback =
                    (SolarEngineSDK4MiniGames.MiniGameAttributionCallback)Delegate.CreateDelegate(
                        typeof(SolarEngineSDK4MiniGames.MiniGameAttributionCallback), config.attributionCallback.Target,
                        config.attributionCallback.Method);
                initParams.miniGameAttributionCallback = attributionCallback;
            }

            if (config.miniGameInitParams != null)
            {
                initParams.miniGameInitParams = new MiniGames. MiniGameInitParams();

#if SOLARENGINE_WECHAT
                initParams.miniGameInitParams.anonymous_openid = "";
#else
                initParams.miniGameInitParams.anonymous_openid = config.miniGameInitParams.anonymous_openid;
#endif
              

                initParams.miniGameInitParams.openid = config.miniGameInitParams.openid;
                initParams.miniGameInitParams.unionid = config.miniGameInitParams.unionid;
                
                
#if  SOLARENGINE_WECHAT
                initParams.isInitTencentAdvertisingGameSDK=config.miniGameInitParams.isInitTencentAdvertisingGameSDK;
                initParams.reportingToTencentSdk=config.miniGameInitParams.reportingToTencentSdk;
                initParams.tencentAdvertisingGameSDKInitParams=new MiniGames.TencentAdvertisingGameSDKInitParams();
                initParams.tencentAdvertisingGameSDKInitParams.appid=config.miniGameInitParams.tencentAdvertisingGameSDKInitParams.appid;
                initParams.tencentAdvertisingGameSDKInitParams.secret_key=config.miniGameInitParams.tencentAdvertisingGameSDKInitParams.secret_key;
                initParams.tencentAdvertisingGameSDKInitParams.user_action_set_id=config.miniGameInitParams.tencentAdvertisingGameSDKInitParams.user_action_set_id;
                initParams.tencentAdvertisingGameSDKInitParams.tencentSdkIsAutoTrack=config.miniGameInitParams.tencentAdvertisingGameSDKInitParams.tencentSdkIsAutoTrack;
            
            
#endif

            }
            initParams.debugModel = config.isDebugModel;
            initParams.logEnabled = config.logEnabled;
            initParams.sublibVersion = sdk_version;
#if TUANJIE_2022_3_OR_NEWER
            initParams.sdktype = "tuanjie";
#else
            initParams.sdktype = "unity";
#endif

            return initParams;
        }
  


        private static void SetVisitorID(string visitorId)
        {
            SolarEngineSDK4MiniGames.setVisitorId(visitorId);
        }

        private static string GetVisitorID()
        {
            return SolarEngineSDK4MiniGames.getVisitorId();
        }

        private static void Login(string accountId)
        {
         
            SolarEngineSDK4MiniGames.login(accountId);
        }
        private static void SetReferrerTitle(string referrerTitle)
        {
            SolarEngineSDK4MiniGames.setReferrerTitle(referrerTitle);
        }

        public static void SetXcxPageTitle(string xcxPageTitle)
        {
            SolarEngineSDK4MiniGames.setXcxPageTitle(xcxPageTitle);
        }

        public static void setSASS(string receiverDomain, string ruleDomain, string openIdDomain)
        {
            SolarEngineSDK4MiniGames.setSASS(receiverDomain, ruleDomain, openIdDomain);
        }


        private static string GetAccountId()
        {
            return SolarEngineSDK4MiniGames.getAccountId();
        }

        private static void Logout()
        {
            SolarEngineSDK4MiniGames.logout();
        }

   
        private static void SetChannel(string channel)
        {
            SolarEngineSDK4MiniGames.setChannel(channel);
        }

      

        private static string GetDistinctId()
        {
            // Action<SolarEngine.MiniGames.info.Distinct> miniGamesDistinctAction = (arg) => distinct?.Invoke(ConvertToCustomDistinct(arg));

         return   SolarEngineSDK4MiniGames.getDistinct();
        }
        private static Distinct ConvertToCustomDistinct(SolarEngine.MiniGames.info.Distinct source)
        {
            Distinct target = new Distinct();
            target.distinct_id = source.distinct_id;
            target.distinct_id_type = source.distinct_id_type;
            return target;
        }
     
        private static void SetSuperProperties(Dictionary<string, object> userProperties)
        {
            SolarEngineSDK4MiniGames.setSuperProperties(userProperties);
        }

        private static void UnsetSuperProperty(string key)
        {
           

            SolarEngineSDK4MiniGames.unsetSuperProperty(key);
        }

        private static void ClearSuperProperties()
        {
            SolarEngineSDK4MiniGames.clearSuperProperties();
        }


        private static void EventStart(string timerEventName)
        {
         

            SolarEngineSDK4MiniGames.eventStart(timerEventName);
        }

        private static void EventFinish(string timerEventName, Dictionary<string, object> attributes)
        {
          

            SolarEngineSDK4MiniGames.eventFinish(timerEventName, attributes);
        }


        private static void UserUpdate(Dictionary<string, object> userProperties)
        {
            SolarEngineSDK4MiniGames.userUpdate(userProperties);
        }

        private static void UserInit(Dictionary<string, object> userProperties)
        {
          
            SolarEngineSDK4MiniGames.userInit(userProperties);
        }

        private static void UserAdd(Dictionary<string, object> userProperties)
        {
            SolarEngineSDK4MiniGames.userAdd(userProperties);
        }

        private static void UserAppend(Dictionary<string, object> userProperties)
        {
            SolarEngineSDK4MiniGames.userAppend(userProperties);
        }

        private static void UserUnset(string[] keys)
        {
        
            SolarEngineSDK4MiniGames.userUnset(keys);
        }

        private static void UserDelete(UserDeleteType deleteType)
        {
            miniGameUserDeleteType miniGameUserDeleteType = (miniGameUserDeleteType)(int)deleteType;
            SolarEngineSDK4MiniGames.userDelete(miniGameUserDeleteType);
        }

        private static string  GetAttribution()
        {
            return SolarEngineSDK4MiniGames.getAttribution();
        
        }
        private static void TrackFirstEvent(SEBaseAttributes attributes)
        {
              Debug.Log($"{SolorEngine}minigame not support");
        }

        private static void ReportIAPEvent(ProductsAttributes attributes)
        {
        
            SolarEngineSDK4MiniGames.trackIAP(getIAPDic(attributes,false), attributes.customProperties,attributes.reportingToTencentSdk);
        }

        private static void ReportIAIEvent(ImpressionAttributes attributes)
        {
           
            SolarEngineSDK4MiniGames.trackAdImpression(getIAIDic(attributes,false), attributes.customProperties);
        }

        private static void ReportAdClickEvent(AdClickAttributes attributes)
        {
         
            SolarEngineSDK4MiniGames.trackAdClick(getAdClickDic(attributes,false), attributes.customProperties);
        }

        private static void ReportRegisterEvent(RegisterAttributes attributes)
        {
          
            SolarEngineSDK4MiniGames.trackRegister(getRegisterDic(attributes,false), attributes.customProperties,attributes.reportingToTencentSdk);
        }

        private static void ReportLoginEvent(LoginAttributes attributes)
        {
            SolarEngineSDK4MiniGames.trackLogin(getLoginDic(attributes,false), attributes.customProperties);
        }

        private static void ReportOrderEvent(OrderAttributes attributes)
        {
        
            SolarEngineSDK4MiniGames.trackOrder(getOrderDic(attributes,false), attributes.customProperties);
        }

        private static void AppAttrEvent(AttAttributes attributes)
        {
            SolarEngineSDK4MiniGames.trackAppAttr(getAttrDic(attributes,false), attributes.customProperties);
        }


           #region 腾讯回传
           private static void TrackReActive(ReActiveAttributes attributes)
        {
#if SOLARENGINE_WECHAT
            SolarEngineSDK4MiniGames.trackReActive(getReDic(attributes),attributes.customProperties,attributes.reportingToTencentSdk);
#endif
        }
           private static void TrackAddToWishlist(AddToWishlistAttributes attributes)
        {  
#if SOLARENGINE_WECHAT
            SolarEngineSDK4MiniGames.trackAddToWishlist(getWishlistDic(attributes),attributes.customProperties,attributes.reportingToTencentSdk);
#endif
        }
           private static void TrackShare(ShareAttributes attributes)
        {
#if SOLARENGINE_WECHAT
            SolarEngineSDK4MiniGames.trackShare(getShareDic(attributes),attributes.customProperties,attributes.reportingToTencentSdk);
#endif
        }
           private static void TrackCreateRole(CreateRoleAttributes attributes)
        {
#if SOLARENGINE_WECHAT
            SolarEngineSDK4MiniGames.trackCreateRole(getCreateRoleDic(attributes),attributes.customProperties,attributes.reportingToTencentSdk);
 #endif
        }

           private static void TrackTutorialFinish(TutorialFinishAttributes attributes)
        {
#if SOLARENGINE_WECHAT
            SolarEngineSDK4MiniGames.trackTutorialFinish(getTutorialFinishDic(attributes),attributes.customProperties,attributes.reportingToTencentSdk );
#endif            
        }

           private static void TrackUpdateLevel( UpdateLevelAttributes attributes){
#if SOLARENGINE_WECHAT
            SolarEngineSDK4MiniGames.trackUpdateLevel(getUpdateLevel(attributes),attributes.customProperties,attributes.reportingToTencentSdk);
#endif
           }

           private  static void TrackViewContentMall(ViewContentMallAttributes attributes)
        {
#if SOLARENGINE_WECHAT            
            SolarEngineSDK4MiniGames.trackViewContentMall(getViewContentMallDic(attributes),attributes.customProperties,attributes.reportingToTencentSdk);
#endif             
        }

           private static void TrackViewContentActivity(ViewContentActivitAttributes attributes)
        {
#if SOLARENGINE_WECHAT 
            SolarEngineSDK4MiniGames.trackViewContentActivity(getViewContentActivitDic(attributes),attributes.customProperties,attributes.reportingToTencentSdk);
#endif           
        }
        

        #endregion

     
 
        
        private static void SetPresetEvent(PresetEventType eventType, Dictionary<string, object> attributes)
        {
            miniGamePreset_EventType miniGamePresetEvent = (miniGamePreset_EventType)(int)eventType;

            SolarEngineSDK4MiniGames.setPresetEvent(miniGamePresetEvent, attributes);
        }

        private static void ReportCustomEvent(string customEventName, Dictionary<string, object> attributes)
        {
            SolarEngineSDK4MiniGames.track(customEventName, null, attributes);
        }

        private static void ReportCustomEventWithPreAttributes(string customEventName,
            Dictionary<string, object> customAttributes, Dictionary<string, object> preAttributes)
        {
            SolarEngineSDK4MiniGames.track(customEventName, preAttributes, customAttributes);
        }

        private static void ReportEventImmediately()
        {
            SolarEngineSDK4MiniGames.reportEventImmediately();
        }

        private static void HandleDeepLinkUrl(string url)
        {
              Debug.Log($"{SolorEngine}Only Android can use , minigame not support");
        }
        private  static void SetOaid(string oaid)
        {
            Debug.Log($"{SolorEngine}Only Android can use , minigame not support");
        }
        private static void TrackAppReEngagement(Dictionary<string, object> attributes)
         {
             Debug.Log($"{SolorEngine}miniGame not support  TrackAppReEngagement");
         }

        #region  not support function

        private static void DeeplinkCompletionHandler(SESDKDeeplinkCallback callback)
        {
              Debug.Log($"{SolorEngine}MiniGame not support");
        }

        private static void DelayDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
        {
              Debug.Log($"{SolorEngine}MiniGame not support");
        }

   
        private static void RequestTrackingAuthorizationWithCompletionHandler(SESDKATTCompletedCallback callback) {
               Debug.Log($"{SolorEngine}Current on MiniGame,requestTrackingAuthorizationWithCompletionHandler only iOS");
         }

         /// <summary>
         /// 仅支持iOS
         /// SolarEngine 封装系统updatePostbackConversionValue
         /// </summary>
         private static void UpdatePostbackConversionValue(int conversionValue, SKANUpdateCompletionHandler callback)
         {

               Debug.Log($"{SolorEngine}Current on MiniGame,requestTrackingAuthorizationWithCompletionHandler only iOS");


         }
         /// <summary>
         /// 仅支持iOS
         /// SolarEngine 封装系统updateConversionValueCoarseValue
         /// </summary>
         private static void UpdateConversionValueCoarseValue(int fineValue, String coarseValue, SKANUpdateCompletionHandler callback)
         {
               Debug.Log($"{SolorEngine}Current on MiniGame,requestTrackingAuthorizationWithCompletionHandler only iOS");

         }
         /// 仅支持iOS
         /// SolarEngine 封装系统updateConversionValueCoarseValueLockWindow
         /// </summary>
         private static void UpdateConversionValueCoarseValueLockWindow(int fineValue, String coarseValue, bool lockWindow, SKANUpdateCompletionHandler callback)
         {
               Debug.Log($"{SolorEngine}Current on MiniGame,requestTrackingAuthorizationWithCompletionHandler only iOS");

         }
         
         private static void SetGaid(string gaid)
         {
               Debug.Log($"{SolorEngine}Current on MiniGame，Only Android can set gaid");
         }
         
         private static void SetGDPRArea(bool isGDPRArea)
         {
             Debug.Log($"{SolorEngine}minigame not support");
         }

        #endregion
    }
}
#endif