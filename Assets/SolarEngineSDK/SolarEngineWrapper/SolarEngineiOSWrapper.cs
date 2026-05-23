#if (UNITY_5 && (UNITY_IOS||UNITY_STANDALONE_OSX)) || (UNITY_IPHONE||UNITY_STANDALONE_OSX)&&!UNITY_EDITOR
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
        private static Dictionary<string, object> GetPresetProperties()
        {

            string presetProperties = __iOSSolarEngineSDKGetPresetProperties();
            if(presetProperties != null){
                try{
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(presetProperties);
                } catch (Exception e) {

                }
            }
            return null;

        }


        private static void PreInitSeSdk(string appKey)
        {
            __iOSSolarEngineSDKPreInit(appKey);
        }


        private static void Init(string appKey, string userId, SEConfig config)
        {
            initCallBack(config);
            if (config.initCompletedCallback != null) {
                __iOSSESDKSetInitCompletedCallback(OnInitCompletedCallback);
            }

            if (config.attributionCallback != null) {
                __iOSSESDKSetAttributionDataCallback(OnAttributionCallback);
            }
            __iOSSolarEngineSDKInit(appKey, userId, initSeDict(config), null);
            
          
        }

        private static void Init(string appKey, string userId, SEConfig config, RCConfig rcConfig)
        {
            initCallBack(config);
            if (config.initCompletedCallback != null) {
                __iOSSESDKSetInitCompletedCallback(OnInitCompletedCallback);
            }

            if (config.attributionCallback != null) {
                __iOSSESDKSetAttributionDataCallback(OnAttributionCallback);
            }
            __iOSSolarEngineSDKInit(appKey, userId, initSeDict(config), initRcDict(rcConfig));
        }

        private static void AttributionCompletedCallback(SEAttributionCallback  callback)
        {
                __iOSSESDKSetAttributionDataCallback(OnAttributionCallback);
        }
        private static void InitCompletedCallback( SESDKInitCompletedCallback  callback)
        {
                __iOSSESDKSetInitCompletedCallback(OnInitCompletedCallback);
        }

        private static void SetVisitorID(string visitorId)
        {
            __iOSSolarEngineSDKSetVisitorID(visitorId);
        }

        private static string GetVisitorID()
        {
            return __iOSSolarEngineSDKVisitorID();
        }

        private static void Login(string accountId)
        {
            __iOSSolarEngineSDKLoginWithAccountID(accountId);
        }

        private static string GetAccountId()
        {
            return __iOSSolarEngineSDKAccountID();
        }

        private static void Logout()
        {
            __iOSSolarEngineSDKLogout();
        }

      

        private static void SetChannel(string channel)
        {
            Debug.Log($"{SolorEngine}iOS not support SetChannel");
        }

        private static void SetGDPRArea(bool isGDPRArea)
        {
            __iOSSolarEngineSDKSetGDPRArea(isGDPRArea);
        }

        private static string GetDistinctId()
        {
            return __iOSSolarEngineSDKGetDistinctId();
        }
        private static void GetDistinctId(Action<Distinct>dis)
        {
            Distinct dist = new Distinct();
            dist.distinct_id = GetDistinctId();
            dist.distinct_id_type = -1;
            dis?.Invoke(dist);
            // Debug.Log($"{SolorEngine}Only MiniGame can use , iOS not support");
        }

        private static void SetSuperProperties(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            __iOSSolarEngineSDKSetSuperProperties(userPropertiesJSONString);
        }

        private static void UnsetSuperProperty(string key)
        {
            __iOSSolarEngineSDKUnsetSuperProperty(key);
        }

        private static void ClearSuperProperties()
        {
            __iOSSolarEngineSDKClearSuperProperties();
        }

        private static void EventStart(string timerEventName)
        {
            __iOSSolarEngineSDKEventStart(timerEventName);
        }

        private static void EventFinish(string timerEventName, Dictionary<string, object> attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(attributes);

            __iOSSolarEngineSDKEventFinishNew(timerEventName, attributesJSONString);
        }

     
        private static void UserUpdate(Dictionary<string, object> userProperties)
        {
          

            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

                __iOSSolarEngineSDKUserUpdate(userPropertiesJSONString);
        }

        private static void UserInit(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            __iOSSolarEngineSDKUserInit(userPropertiesJSONString);
        }

        private static void UserAdd(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

                __iOSSolarEngineSDKUserAdd(userPropertiesJSONString);
        }

        private static void UserAppend(Dictionary<string, object> userProperties)
        {
          
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

                __iOSSolarEngineSDKUserAppend(userPropertiesJSONString);
        }

        private static void UserUnset(string[] keys)
        {
        
            string keysJSONStr = JsonConvert.SerializeObject(keys);

                __iOSSolarEngineSDKUserUnset(keysJSONStr);
        }

        private static void UserDelete(UserDeleteType deleteType)
        {
            int seUserDeleteType = deleteType == UserDeleteType.ByAccountId ? 0 : 1;

                __iOSSolarEngineSDKUserDelete(seUserDeleteType);

        }

        private static string GetAttribution()
        {

               return __iOSSolarEngineSDKGetAttribution();

        }

        private static void TrackFirstEvent(SEBaseAttributes attributes)
        {
           
          
          
            string attributesJSONString = JsonConvert.SerializeObject(getFirstDic(attributes));

            __iOSSolarEngineSDKTrackFirstEventWithAttributes(attributesJSONString);
        }
        
        private static void TrackAppReEngagement(Dictionary<string, object> attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(attributes);
            __iOSSolarEngineSDKTrackAppReEngagement(attributesJSONString);
        }

        private static void ReportIAPEvent(ProductsAttributes attributes)
        {

            string attributesJSONString = JsonConvert.SerializeObject(getIAPDic(attributes));

            __iOSSolarEngineSDKTrackIAPWithAttributes(attributesJSONString);
        }

        private static void ReportIAIEvent(ImpressionAttributes attributes)
        {
           

            string attributesJSONString = JsonConvert.SerializeObject(getIAIDic(attributes));
            __iOSSolarEngineSDKTrackAdImpressionWithAttributes(attributesJSONString);
        }

        private static void ReportAdClickEvent(AdClickAttributes attributes)
        {
          
            string attributesJSONString = JsonConvert.SerializeObject(getAdClickDic(attributes));

            __iOSSolarEngineSDKTrackAdClickWithAttributes(attributesJSONString);
        }

        private static void ReportRegisterEvent(RegisterAttributes attributes)
        {
           

            string attributesJSONString = JsonConvert.SerializeObject(getRegisterDic(attributes));


            __iOSSolarEngineSDKTrackRegisterWithAttributes(attributesJSONString);
        }

        private static void ReportLoginEvent(LoginAttributes attributes)
        {
         

            string attributesJSONString = JsonConvert.SerializeObject(getLoginDic(attributes));

            __iOSSolarEngineSDKTrackLoginWithAttributes(attributesJSONString);
        }

        private static void ReportOrderEvent(OrderAttributes attributes)
        {
           

            string attributesJSONString = JsonConvert.SerializeObject(getOrderDic(attributes));

            __iOSSolarEngineSDKTrackOrderWithAttributes(attributesJSONString);
        }

        private static void AppAttrEvent(AttAttributes attributes)
        {
          
            string attributesJSONString = JsonConvert.SerializeObject(getAttrDic(attributes));

            __iOSSolarEngineSDKTrackAppAttrWithAttributes(attributesJSONString);
        }


        private static void SetPresetEvent(PresetEventType eventType, Dictionary<string, object> attributes)
        {
            string eventDataJSONString = JsonConvert.SerializeObject(attributes);


            __iOSSolarEngineSDKSetPresetEvent(getPresetEventName(eventType), eventDataJSONString);
        }

        private static void ReportCustomEvent(string customEventName, Dictionary<string, object> attributes)
        {
            string eventDataJSONString = JsonConvert.SerializeObject(attributes);

            __iOSSolarEngineSDKTrack(customEventName, eventDataJSONString);
        }

        private static void ReportCustomEventWithPreAttributes(string customEventName,
            Dictionary<string, object> customAttributes, Dictionary<string, object> preAttributes)
        {
            string customDataJSONString = JsonConvert.SerializeObject(customAttributes);
            string preDataJSONString = JsonConvert.SerializeObject(preAttributes);

            __iOSSolarEngineSDKTrackCustomEventWithPreAttributes(customEventName, customDataJSONString,
                preDataJSONString);
        }

        private static void ReportEventImmediately()
        {

                __iOSSolarEngineSDKReportEventImmediately();

        }

        private static void HandleDeepLinkUrl(string url)
        {

                 Debug.Log($"{SolorEngine}Only Android can use , iOS not support");

        }


        private static void DeeplinkCompletionHandler(SESDKDeeplinkCallback callback)
        {
            Analytics.Instance.deeplinkCallback_private = callback;

                __iOSSolarEngineSDKDeeplinkParseCallback(OnDeeplinkParseCallback);

        }


        private static void DelayDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
        {
            Analytics.Instance.delayDeeplinkCallback_private = callback;

                __iOSSolarEngineSDKDelayDeeplinkParseCallback(OnDelayDeeplinkParseCallback);

        }
        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统requestTrackingAuthorizationWithCompletionHandler接口
        /// callback 回调用户授权状态: 0: Not Determined；1: Restricted；2: Denied；3: Authorized ；999: system error
        /// </summary>
        private static void RequestTrackingAuthorizationWithCompletionHandler(SESDKATTCompletedCallback callback) {
            Analytics.Instance.attCompletedCallback_private = callback;
            __iOSSESDKRequestTrackingAuthorizationWithCompletionHandler(OnRequestTrackingAuthorizationCompletedCallback);

        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updatePostbackConversionValue
        /// </summary>
        private static void UpdatePostbackConversionValue(int conversionValue, SKANUpdateCompletionHandler callback)
        {

            Instance.iosSKANUpdateCVCompletionHandler_private = callback;
            __iOSSESDKupdatePostbackConversionValue(conversionValue, OnSKANUpdateCVCallback);

        }
        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValue
        /// </summary>
        private static void UpdateConversionValueCoarseValue(int fineValue, String coarseValue, SKANUpdateCompletionHandler callback)
        {

            Analytics.Instance.iosSKANUpdateCVCoarseValueCompletionHandler_private = callback;
            __iOSSESDKupdateConversionValueCoarseValue(fineValue, coarseValue, OnSKANUpdateCVCoarseValueCallback);

        }
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValueLockWindow
        /// </summary>
        private static void UpdateConversionValueCoarseValueLockWindow(int fineValue, String coarseValue, bool lockWindow, SKANUpdateCompletionHandler callback)
        {

            Analytics.Instance.iosSKANUpdateCVCoarseValueLockWindowCompletionHandler_private = callback;
            __iOSSESDKupdateConversionValueCoarseValueLockWindow(fineValue, coarseValue, lockWindow, OnSKANUpdateCVCoarseValueLockWindowCallback);

        }

        //回调函数，必须MonoPInvokeCallback并且是static
        [MonoPInvokeCallback(typeof(SEiOSStringCallback))]
        private static void OnAttributionCallback(int code, string attribution)
        {
            OnAttributionHandler(code, attribution);
        }

        [MonoPInvokeCallback(typeof(SESDKInitCompletedCallback))]
        private static void OnInitCompletedCallback(int code)
        {
            OnInitCompletedHandler(code);
        }

        [MonoPInvokeCallback(typeof(SESDKATTCompletedCallback))]
        private static void OnRequestTrackingAuthorizationCompletedCallback(int code)
        {
            OnRequestTrackingAuthorizationCompletedHandler(code);
        }

        //回调函数，必须MonoPInvokeCallback并且是static
        [MonoPInvokeCallback(typeof(SEiOSStringCallback))]
        private static void OnSKANUpdateCVCallback(int errorCode, string errorMsg)
        {
            OnSKANUpdateCVCompletionHandler(errorCode, errorMsg);
        }

        //回调函数，必须MonoPInvokeCallback并且是static
        [MonoPInvokeCallback(typeof(SEiOSStringCallback))]
        private static void OnSKANUpdateCVCoarseValueCallback(int errorCode, string errorMsg)
        {
            OnSKANUpdateCVCoarseValueCompletionHandler(errorCode, errorMsg);
        }

        //回调函数，必须MonoPInvokeCallback并且是static
        [MonoPInvokeCallback(typeof(SEiOSStringCallback))]
        private static void OnSKANUpdateCVCoarseValueLockWindowCallback(int errorCode, string errorMsg)
        {
            OnSKANUpdateCVCoarseValueLockWindowCompletionHandler(errorCode, errorMsg);
        }

        //回调函数，必须MonoPInvokeCallback并且是static
        [MonoPInvokeCallback(typeof(SEiOSStringCallback))]
        private static void OnDeeplinkParseCallback(int code, string jsonString)
        {
            OnDeeplinkCompletionHandler(code, jsonString);
        }

        //回调函数，必须MonoPInvokeCallback并且是static
        [MonoPInvokeCallback(typeof(SEiOSStringCallback))]
        private static void OnDelayDeeplinkParseCallback(int code, string jsonString)
        {
            OnDelayDeeplinkCompletionHandler(code, jsonString);
        }


           #region DllImport

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKPreInit(string appKey);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKInit(string appKey, string SEUserId, string seConfig,
            string rcConfig);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKSetGDPRArea(bool isGDPRArea);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrack(string eventName, string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackCustomEventWithPreAttributes(string eventName,
            string customAttributes, string preAttributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackIAPWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackAdImpressionWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackAdClickWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackRegisterWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackLoginWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackOrderWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackAppAttrWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKTrackFirstEventWithAttributes(string attributes);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKSetVisitorID(string visitorID);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern string __iOSSolarEngineSDKVisitorID();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKLoginWithAccountID(string accountID);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern string __iOSSolarEngineSDKAccountID();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern string __iOSSolarEngineSDKGetDistinctId();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern string __iOSSolarEngineSDKGetPresetProperties();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKLogout();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKReportEventImmediately();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKSetSuperProperties(string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKUnsetSuperProperty(string property);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        
        private static extern void __iOSSolarEngineSDKTrackAppReEngagement(string properties);
#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKClearSuperProperties();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKUserInit(string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKUserUpdate(string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKUserAdd(string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKUserUnset(string keys);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKUserAppend(string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKUserDelete(int deleteType);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKEventStart(string eventName);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKEventFinish(string eventJSONStr);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKEventFinishNew(string eventName, string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKSetPresetEvent(string eventName, string eventDataJSONString);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern string __iOSSolarEngineSDKGetAttribution();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSESDKSetAttributionDataCallback(SEiOSStringCallback callback);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSESDKSetInitCompletedCallback(SESDKInitCompletedCallback callback);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSESDKRequestTrackingAuthorizationWithCompletionHandler(
            SESDKATTCompletedCallback callback);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSESDKupdatePostbackConversionValue(int conversionValue,
            SEiOSStringCallback callback);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern void __iOSSESDKupdateConversionValueCoarseValue(int fineValue, String coarseValue,
            SEiOSStringCallback callback);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSESDKupdateConversionValueCoarseValueLockWindow(int fineValue,
            String coarseValue, bool lockWindow, SEiOSStringCallback callback);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKDeeplinkParseCallback(SEiOSStringCallback callback);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif

        private static extern void __iOSSolarEngineSDKDelayDeeplinkParseCallback(SEiOSStringCallback callback);

        #endregion


        
        #region notsupport
       private  static void SetOaid(string oaid)
        {
           Debug.Log($"{SolorEngine}Current on iOS，Only Android can set oaid");
        }

        private static void SetGaid(string gaid)
        {
              Debug.Log($"{SolorEngine}Current on iOS，Only Android can set gaid");
        }
        private static  void SetReferrerTitle(string title)
        {
              Debug.Log($"{SolorEngine}Current on iOS,Only MiniGame can SetReferrerTitle ");
        }

        private static void SetXcxPageTitle(string title)
        {
              Debug.Log($"{SolorEngine}Current on iOS,Only MiniGame can SetXcxPageTitle ");
        }



   private static readonly string _platform = "iOS";
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
