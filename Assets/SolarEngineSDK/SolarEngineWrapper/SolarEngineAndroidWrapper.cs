#if UNITY_ANDROID&&!UNITY_EDITOR&&!SOLARENGINE_BYTEDANCE&&!SOLARENGINE_BYTEDANCE_CLOUD&&!SOLARENGINE_BYTEDANCE_STARK
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using Newtonsoft.Json;
using UnityEngine;

namespace SolarEngine
{
    public partial class Analytics : MonoBehaviour
    {
        protected static AndroidJavaClass SolarEngineAndroidSDK =
            new AndroidJavaClass("com.reyun.solar.engine.unity.bridge.UnityAndroidSeSDKManager");

        protected static AndroidJavaObject SolarEngineAndroidSDKObject =
            new AndroidJavaObject("com.reyun.solar.engine.unity.bridge.UnityAndroidSeSDKManager");

        protected static AndroidJavaObject Context =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");

        #region

        private static Dictionary<string, object> GetPresetProperties()
        {
            string presetProperties = SolarEngineAndroidSDK.CallStatic<string>("getPresetProperties");
              Debug.Log($"{SolorEngine}SEUunity-presetProperties: " + presetProperties);
            if (presetProperties != null)
            {
                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(presetProperties);
                }
                catch (Exception e)
                {
                }
            }

            return null;
        }


        private static void PreInitSeSdk(string appKey)
        {
            SolarEngineAndroidSDK.CallStatic("preInit", Context, appKey);
        }


        private static void Init(string appKey, string userId, SEConfig config)
        {
            initCallBack(config);
            SolarEngineAndroidSDK.CallStatic("initialize", Context, appKey, userId, initSeDict(config),
                Analytics.Instance.attributionCallback_private != null ? new OnAttributionReceivedData() : null,
                Analytics.Instance.initCompletedCallback_private != null ? new OnUnityInitCompletedCallback() : null);
        }

        private static void Init(string appKey, string userId, SEConfig config, RCConfig rcConfig)
        {
            initCallBack(config);
            SolarEngineAndroidSDK.CallStatic("initialize", Context, appKey, userId, initSeDict(config),
                initRcDict(rcConfig),
                Analytics.Instance.attributionCallback_private != null ? new OnAttributionReceivedData() : null,
                Analytics.Instance.initCompletedCallback_private != null ? new OnUnityInitCompletedCallback() : null);
        }

        private static void AttributionCompletedCallback(SEAttributionCallback  callback)
        {
         
        }
        private static void InitCompletedCallback( SESDKInitCompletedCallback  callback)
        {
           
        }

        private static void SetVisitorID(string visitorId)
        {
            SolarEngineAndroidSDK.CallStatic("setVisitorID", visitorId);
        }

        private static string GetVisitorID()
        {
            return SolarEngineAndroidSDK.CallStatic<string>("getVisitorID");
        }

        private static void Login(string accountId)
        {
            SolarEngineAndroidSDK.CallStatic("login", accountId);
        }

        private static string GetAccountId()
        {
            return SolarEngineAndroidSDK.CallStatic<string>("getAccountID");
        }

        private static void Logout()
        {
            SolarEngineAndroidSDK.CallStatic("logout");
        }

        private static void SetGaid(string gaid)
        {
            SolarEngineAndroidSDK.CallStatic("setGaid", gaid);
        }
        private  static void SetOaid(string oaid)
        {
            SolarEngineAndroidSDK.CallStatic("setOaid", oaid);
        }

        private static void SetChannel(string channel)
        {
            SolarEngineAndroidSDK.CallStatic("setChannel", channel);
        }

        private static void SetGDPRArea(bool isGDPRArea)
        {
            SolarEngineAndroidSDK.CallStatic("setGDPRArea", isGDPRArea);
        }

        private static string GetDistinctId()
        {
            return SolarEngineAndroidSDK.CallStatic<string>("getDistinctId");
        }

        private static void GetDistinctId(Action<Distinct>dis)
        {
            Distinct dist = new Distinct();
            dist.distinct_id = GetDistinctId();

            dist.distinct_id_type = -1;
            
            dis?.Invoke(dist);
            //  Debug.Log($"{SolorEngine}Only MiniGame can use , Android not support");
        }
        private static void SetSuperProperties(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);


            SolarEngineAndroidSDK.CallStatic("setSuperProperties", Context, userPropertiesJSONString);
        }

        private static void UnsetSuperProperty(string key)
        {
            SolarEngineAndroidSDK.CallStatic("unsetSuperProperty", Context, key);
        }

        private static void ClearSuperProperties()
        {
            SolarEngineAndroidSDK.CallStatic("clearSuperProperties", Context);
        }

        private static void EventStart(string timerEventName)
        {
            SolarEngineAndroidSDK.CallStatic("eventStart", timerEventName);
        }

        private static void EventFinish(string timerEventName, Dictionary<string, object> attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(attributes);
            SolarEngineAndroidSDK.CallStatic("eventFinish", timerEventName, attributesJSONString);
        }

       

        private static void UserUpdate(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            SolarEngineAndroidSDK.CallStatic("userUpdate", userPropertiesJSONString);
        }

        private static void UserInit(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            SolarEngineAndroidSDK.CallStatic("userInit", userPropertiesJSONString);
        }

        private static void UserAdd(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            SolarEngineAndroidSDK.CallStatic("userAdd", userPropertiesJSONString);
        }

        private static void UserAppend(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            SolarEngineAndroidSDK.CallStatic("userAppend", userPropertiesJSONString);
        }
        private static void TrackAppReEngagement(Dictionary<string, object> customAttributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(customAttributes);
            SolarEngineAndroidSDK.CallStatic("trackAppReEngagement", attributesJSONString);
            
                
        }

        private static void UserUnset(string[] keys)
        {
            string keysJSONStr = JsonConvert.SerializeObject(keys);

            SolarEngineAndroidSDK.CallStatic("userUnset", keysJSONStr);
        }

        private static void UserDelete(UserDeleteType deleteType)
        {
            int seUserDeleteType = deleteType == UserDeleteType.ByAccountId ? 0 : 1;

            SolarEngineAndroidSDK.CallStatic("userDelete", seUserDeleteType);
        }

        private static string GetAttribution()
        {
            return SolarEngineAndroidSDK.CallStatic<string>("getAttribution");
        }

        private static void TrackFirstEvent(SEBaseAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getFirstDic(attributes));

            SolarEngineAndroidSDKObject.CallStatic("trackFirstEvent", attributesJSONString);
        }

        private static void ReportIAPEvent(ProductsAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getIAPDic(attributes));

            SolarEngineAndroidSDKObject.CallStatic("trackPurchaseEvent", attributesJSONString);
        }

        private static void ReportIAIEvent(ImpressionAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getIAIDic(attributes));

            SolarEngineAndroidSDKObject.CallStatic("trackImpEvent", attributesJSONString);
        }

        private static void ReportAdClickEvent(AdClickAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getAdClickDic(attributes));


            SolarEngineAndroidSDKObject.CallStatic("trackAdClickEvent", attributesJSONString);
        }

        private static void ReportRegisterEvent(RegisterAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getRegisterDic(attributes));

            SolarEngineAndroidSDKObject.CallStatic("trackRegisterEvent", attributesJSONString);
        }

        private static void ReportLoginEvent(LoginAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getLoginDic(attributes));


            SolarEngineAndroidSDKObject.CallStatic("trackLoginEvent", attributesJSONString);
        }

        private static void ReportOrderEvent(OrderAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getOrderDic(attributes));

            SolarEngineAndroidSDKObject.CallStatic("trackOrderEvent", attributesJSONString);
        }

        private static void AppAttrEvent(AttAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getAttrDic(attributes));

            SolarEngineAndroidSDKObject.CallStatic("trackAppAttrEvent", attributesJSONString);
        }


        private static void SetPresetEvent(PresetEventType eventType, Dictionary<string, object> attributes)
        {
            string eventDataJSONString = JsonConvert.SerializeObject(attributes);


            SolarEngineAndroidSDKObject.CallStatic("setPresetEvent", getPresetEventName(eventType),
                eventDataJSONString);
        }

        private static void ReportCustomEvent(string customEventName, Dictionary<string, object> attributes)
        {
            string eventDataJSONString = JsonConvert.SerializeObject(attributes);

            SolarEngineAndroidSDKObject.CallStatic("trackCustomEvent", customEventName, eventDataJSONString);
        }

        private static void ReportCustomEventWithPreAttributes(string customEventName,
            Dictionary<string, object> customAttributes, Dictionary<string, object> preAttributes)
        {
            string customDataJSONString = JsonConvert.SerializeObject(customAttributes);
            string preDataJSONString = JsonConvert.SerializeObject(preAttributes);


            SolarEngineAndroidSDKObject.CallStatic("trackCustomEventWithPreEventData", customEventName,
                customDataJSONString, preDataJSONString);
        }

        private static void ReportEventImmediately()
        {
            SolarEngineAndroidSDKObject.CallStatic("reportEventImmediately");
        }

        private static void HandleDeepLinkUrl(string url)
        {
            if (url != null)
            {
                SolarEngineAndroidSDK.CallStatic("handleSchemeUrl", url);
            }
            else
            {
                  Debug.Log($"{SolorEngine}url is invalid");
            }
        }


        private static void DeeplinkCompletionHandler(SESDKDeeplinkCallback callback)
        {
            Analytics.Instance.deeplinkCallback_private = callback;

            SolarEngineAndroidSDK.CallStatic("setDeepLinkCallback", callback != null ? new OnDeepLinkCallBack() : null);
        }


        private static void DelayDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
        {
            Analytics.Instance.delayDeeplinkCallback_private = callback;

            SolarEngineAndroidSDK.CallStatic("setDelayDeepLinkCallback",
                callback != null ? new OnDelayDeepLinkCallBack() : null);
        }


        private sealed class OnAttributionReceivedData : AndroidJavaProxy
        {
            public OnAttributionReceivedData() : base(
                "com.reyun.solar.engine.unity.bridge.OnAttributionReceivedDataForUnity")
            {
            }

            public void onResultForUnity(int code, String result)
            {
                if (islog)
                {
                    Debug.Log($"{SolorEngine}OnAttributionReceivedData");
                }
                OnAttributionHandler(code, result);
            }
        }


        private sealed class OnDeepLinkCallBack : AndroidJavaProxy
        {
            public OnDeepLinkCallBack() : base("com.reyun.solar.engine.unity.bridge.OnDeepLinkCallBack")
            {
            }

            public void onReceived(int code, String result)
            {
                OnDeeplinkCompletionHandler(code, result);
            }
        }


        private sealed class OnDelayDeepLinkCallBack : AndroidJavaProxy
        {
            public OnDelayDeepLinkCallBack() : base("com.reyun.solar.engine.unity.bridge.OnDelayDeepLinkCallBack")
            {
            }

            public void onReceived(int code, String result)
            {
                OnDelayDeeplinkCompletionHandler(code, result);
            }
        }


        private sealed class OnUnityInitCompletedCallback : AndroidJavaProxy
        {
            public OnUnityInitCompletedCallback() : base(
                "com.reyun.solar.engine.unity.bridge.OnUnityInitCompletedCallback")
            {
            }

            public void onInitializationCompleted(int code)
            {
                if (islog)
                {
                    Debug.Log($"{SolorEngine}OnUnityInitCompletedCallback");
                }
                OnInitCompletedHandler(code);
            }
        }
        
        
        

        #endregion

        #region not support

 

        private static  void SetReferrerTitle(string title)
        {
              Debug.Log($"{SolorEngine}Current on Android,Only MiniGame can SetReferrerTitle ");
        }

        private static void SetXcxPageTitle(string title)
        {
              Debug.Log($"{SolorEngine}Current on Android,Only MiniGame can SetXcxPageTitle ");
        } 
        private static void RequestTrackingAuthorizationWithCompletionHandler(SESDKATTCompletedCallback callback) {
              Debug.Log($"{SolorEngine}Current on Android,requestTrackingAuthorizationWithCompletionHandler only iOS");
        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updatePostbackConversionValue
        /// </summary>
        private static void UpdatePostbackConversionValue(int conversionValue, SKANUpdateCompletionHandler callback)
        {

              Debug.Log($"{SolorEngine}Current on Android,requestTrackingAuthorizationWithCompletionHandler only iOS");


        }
        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValue
        /// </summary>
        private static void UpdateConversionValueCoarseValue(int fineValue, String coarseValue, SKANUpdateCompletionHandler callback)
        {
              Debug.Log($"{SolorEngine}Current on Android,requestTrackingAuthorizationWithCompletionHandler only iOS");


        }
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValueLockWindow
        /// </summary>
        private static void UpdateConversionValueCoarseValueLockWindow(int fineValue, String coarseValue, bool lockWindow, SKANUpdateCompletionHandler callback)
        {
              Debug.Log($"{SolorEngine}Current on Android,requestTrackingAuthorizationWithCompletionHandler only iOS");

        }
        
           private static readonly string _platform = "Android";
        #region 腾讯回传
        private static void TrackReActive(ReActiveAttributes attributes)
        {
    
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackReActive ");
        }
        private static void TrackAddToWishlist(AddToWishlistAttributes attributes)
        {  
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackAddToWishlist ");    
        }
        private static void TrackShare(ShareAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackShare ");    
    
        }
        private static void TrackCreateRole(CreateRoleAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackCreateRole ");    
        }

        private static void TrackTutorialFinish(TutorialFinishAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackTutorialFinish ");    
        }

        private static void TrackUpdateLevel( UpdateLevelAttributes attributes){
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackUpdateLevel ");    
        }

        private  static void TrackViewContentMall(ViewContentMallAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackViewContentMall ");    
        }

        private static void TrackViewContentActivity(ViewContentActivitAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Current on {_platform},Only MiniGame can TrackViewContentActivity ");    
        }
        

        #endregion
        
        #endregion


      
    }
}
#endif