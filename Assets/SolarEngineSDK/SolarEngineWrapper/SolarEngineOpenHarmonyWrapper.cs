#if UNITY_OPENHARMONY&&!UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SolarEngine
{
    public partial class Analytics : MonoBehaviour
    {
        private static string SolorEnginopenharmony = SolorEngine + "openharmony not currently supported : ";

        private static readonly OpenHarmonyJSClass openHarmonyJSClass = new OpenHarmonyJSClass("SEOpenHarmonyProxy");


        private static Action<string> getVisitor_private;

        private static Action<string> getDistinct_private;
        private static Action<int> getRequestPermission_private;

        private static Action<Dictionary<string, object>> getProperties_private;


        private static void PreInitSeSdk(string appKey)
        {
            openHarmonyJSClass.CallStatic("preInit", appKey);
        }

        private static void setInitSDKListener()
        {
            OpenHarmonyJSCallback initsdkCalllback = new OpenHarmonyJSCallback(initSDKCallback);
            openHarmonyJSClass.CallStatic("setInitSDKListener", initsdkCalllback);
        }

        private static void setAttributionListener()
        {
            OpenHarmonyJSCallback attributionCalllback = new OpenHarmonyJSCallback(attributionCallback);
            openHarmonyJSClass.CallStatic("setAttributionListener", attributionCalllback);
        }


        private static void Init(string appKey, object userId, SEConfig config)
        {
            initCallBack(config);
            openHarmonyJSClass.CallStatic("initialize", appKey, initSeDict(config));
        }

        private static void Init(string appKey, string userId, SEConfig config, RCConfig rcConfig)
        {
            initCallBack(config);
            openHarmonyJSClass.CallStatic("initialize", appKey, initSeDict(config), initRcDict(rcConfig));
        }

        // private static void AttributionCompletedCallback(SEAttributionCallback callback)
        // {
        // }
        //
        // private static void InitCompletedCallback(SESDKInitCompletedCallback callback)
        // {
        // }

        private static void SetVisitorID(string visitorId)
        {
            openHarmonyJSClass.CallStatic("setVisitorID", visitorId);
        }


        private static void GetVisitorID(Action<string> callback)
        {
            getVisitor_private = callback;
            OpenHarmonyJSCallback getVisitorCalllback = new OpenHarmonyJSCallback(getVisitorIDCallback);
            openHarmonyJSClass.CallStatic("getVisitorID", getVisitorCalllback);
        }

        private static void GetPresetProperties(Action<Dictionary<string, object>> getProperties)
        {
            getProperties_private = getProperties;
            OpenHarmonyJSCallback getPropertiesCallback = new OpenHarmonyJSCallback(getPresetPropertiesCallBack);
            openHarmonyJSClass.CallStatic("getPresetProperties", getPropertiesCallback);
        }

        private static void Login(string accountId)
        {
            openHarmonyJSClass.CallStatic("login", accountId);
        }

        private static string GetAccountId()
        {
            return openHarmonyJSClass.CallStatic<string>("getAccountID");
        }


        private static void Logout()
        {
            openHarmonyJSClass.CallStatic("logout");
        }

        private static void SetGaid(string gaid)
        {
            Debug.Log($"{SolorEnginopenharmony}SetGaid");
        }

        private static void SetOaid(string oaid)
        {
            openHarmonyJSClass.CallStatic("setOaId", oaid);
        }

        private static void SetChannel(string channel)
        {
            openHarmonyJSClass.CallStatic("setChannel", channel);
        }

        private static void SetGDPRArea(bool isGDPRArea)
        {
            Debug.Log($"{SolorEnginopenharmony}SetGDPRArea");
        }


        private static void GetDistinctId(Action<string> dis)
        {
            getDistinct_private = dis;
            OpenHarmonyJSCallback getDisCalllback = new OpenHarmonyJSCallback(getDistinctIDCallback);
            openHarmonyJSClass.CallStatic("getDistinctId", getDisCalllback);
        }

        private static void SetSuperProperties(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            openHarmonyJSClass.CallStatic("setSuperProperties", userPropertiesJSONString);
        }

        private static void UnsetSuperProperty(string key)
        {
            openHarmonyJSClass.CallStatic("unsetSuperProperty", key);
        }

        private static void ClearSuperProperties()
        {
            openHarmonyJSClass.CallStatic("clearSuperProperties");
        }

        private static void EventStart(string timerEventName)
        {
            openHarmonyJSClass.CallStatic("eventStart", timerEventName);
        }

        private static void EventFinish(string timerEventName, Dictionary<string, object> attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(attributes);

            openHarmonyJSClass.CallStatic("eventFinish", timerEventName, attributesJSONString);
        }


        private static void UserUpdate(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            openHarmonyJSClass.CallStatic("userUpdate", userPropertiesJSONString);
        }

        private static void UserInit(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            openHarmonyJSClass.CallStatic("userInit", userPropertiesJSONString);
        }

        private static void UserAdd(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            openHarmonyJSClass.CallStatic("userAdd", userPropertiesJSONString);
        }

        private static void UserAppend(Dictionary<string, object> userProperties)
        {
            string userPropertiesJSONString = JsonConvert.SerializeObject(userProperties);

            openHarmonyJSClass.CallStatic("userAppend", userPropertiesJSONString);
        }

        private static void UserUnset(string[] keys)
        {
            string keysJSONStr = JsonConvert.SerializeObject(keys);

            openHarmonyJSClass.CallStatic("userUnset", keysJSONStr);
        }

        private static void UserDelete(UserDeleteType deleteType)
        {
            int seUserDeleteType = deleteType == UserDeleteType.ByAccountId ? 0 : 1;

            openHarmonyJSClass.CallStatic("userDelete", seUserDeleteType);
        }

        private static string GetAttribution()
        {
            return openHarmonyJSClass.CallStatic<string>("getAttribution");
        }

        private static void ReportIAPEvent(ProductsAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getIAPDic(attributes));
            openHarmonyJSClass.CallStatic("trackPurchaseEvent", attributesJSONString);
        }

        private static void ReportIAIEvent(ImpressionAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getIAIDic(attributes));
            openHarmonyJSClass.CallStatic("trackImpEvent", attributesJSONString);
        }

        private static void ReportAdClickEvent(AdClickAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getAdClickDic(attributes));
            openHarmonyJSClass.CallStatic("trackAdClickEvent", attributesJSONString);
        }

        private static void ReportRegisterEvent(RegisterAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getRegisterDic(attributes));
            openHarmonyJSClass.CallStatic("trackRegisterEvent", attributesJSONString);
        }

        private static void ReportLoginEvent(LoginAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getLoginDic(attributes));
            openHarmonyJSClass.CallStatic("trackLoginEvent", attributesJSONString);
        }

        private static void ReportOrderEvent(OrderAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getOrderDic(attributes));
            openHarmonyJSClass.CallStatic("trackOrderEvent", attributesJSONString);
        }

        private static void AppAttrEvent(AttAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getAttrDic(attributes));
            openHarmonyJSClass.CallStatic("trackAppAttrEvent", attributesJSONString);
        }

        private static void TrackAppReEngagement(Dictionary<string, object> attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(attributes);
            openHarmonyJSClass.CallStatic("trackAppReEngagement", attributesJSONString);
        }

        private static void TrackFirstEvent(SEBaseAttributes attributes)
        {
            string attributesJSONString = JsonConvert.SerializeObject(getFirstDic(attributes));

            openHarmonyJSClass.CallStatic("trackFirstEvent", attributesJSONString);
        }

        private static void SetPresetEvent(PresetEventType eventType, Dictionary<string, object> attributes)
        {
            string eventDataJSONString = "";
            if (attributes != null)
                eventDataJSONString = JsonConvert.SerializeObject(attributes);
            openHarmonyJSClass.CallStatic("setPresetEvent", getPresetEventName(eventType), eventDataJSONString);
        }

        private static void ReportCustomEvent(string customEventName, Dictionary<string, object> attributes)
        {
            string eventDataJSONString = JsonConvert.SerializeObject(attributes);
            openHarmonyJSClass.CallStatic("trackCustomEvent", customEventName, eventDataJSONString);
        }

        private static void ReportCustomEventWithPreAttributes(string customEventName,
            Dictionary<string, object> customAttributes, Dictionary<string, object> preAttributes)
        {
            string customDataJSONString = JsonConvert.SerializeObject(customAttributes);
            string preDataJSONString = JsonConvert.SerializeObject(preAttributes);

            openHarmonyJSClass.CallStatic("trackCustomEventWithPreEventData", customEventName,
                customDataJSONString, preDataJSONString);
        }

        private static void ReportEventImmediately()
        {
            openHarmonyJSClass.CallStatic("reportEventImmediately");
        }

        private static void HandleDeepLinkUrl(string url)
        {
            openHarmonyJSClass.CallStatic("appDeeplinkOpenURI", url);
        }


        private static void DeeplinkCompletionHandler(SESDKDeeplinkCallback callback)
        {
            Analytics.Instance.deeplinkCallback_private = callback;

            OpenHarmonyJSCallback deeplinkCalllback = new OpenHarmonyJSCallback(onDeepLinkCallback);
            openHarmonyJSClass.CallStatic("setDeepLinkListener", deeplinkCalllback);
        }

        private static void DelayDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
        {
            Analytics.Instance.delayDeeplinkCallback_private = callback;
            OpenHarmonyJSCallback delaydeeplinkCalllback = new OpenHarmonyJSCallback(onDelayDeepLinkCallback);
            openHarmonyJSClass.CallStatic("setDelayDeepLinkListener", delaydeeplinkCalllback);
        }

        private static void AuthorizationCompleted()
        {
            openHarmonyJSClass.CallStatic("authorizationCompleted");
        }
        private static void RequestPermissionsFromUser(Action<int> callback)
        {
            getRequestPermission_private = callback;

            OpenHarmonyJSCallback requestPermissionCalllback = new OpenHarmonyJSCallback(onRequestPermissionsCallback);
            openHarmonyJSClass.CallStatic("requestPermissionsFromUser", requestPermissionCalllback);
        }


        private static void SetReferrerTitle(string title)
        {
            Debug.Log($"{SolorEnginopenharmony} SetReferrerTitle ");
        }

        private static void SetXcxPageTitle(string title)
        {
            Debug.Log($"{SolorEnginopenharmony}: SetXcxPageTitle ");
        }


        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统requestTrackingAuthorizationWithCompletionHandler接口
        /// callback 回调用户授权状态: 0: Not Determined；1: Restricted；2: Denied；3: Authorized ；999: system error
        /// </summary>
        private static void RequestTrackingAuthorizationWithCompletionHandler(SESDKATTCompletedCallback callback)
        {
            Debug.Log($"{SolorEnginopenharmony}: RequestTrackingAuthorizationWithCompletionHandler");
        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updatePostbackConversionValue
        /// </summary>
        private static void UpdatePostbackConversionValue(int conversionValue, SKANUpdateCompletionHandler callback)
        {
            Debug.Log($"{SolorEnginopenharmony} UpdatePostbackConversionValue");
        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValue
        /// </summary>
        private static void UpdateConversionValueCoarseValue(int fineValue, String coarseValue,
            SKANUpdateCompletionHandler callback)
        {
            Debug.Log($"{SolorEnginopenharmony}UpdateConversionValueCoarseValue");
        }

        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValueLockWindow
        /// </summary>
        private static void UpdateConversionValueCoarseValueLockWindow(int fineValue, String coarseValue,
            bool lockWindow, SKANUpdateCompletionHandler callback)
        {
            Debug.Log($"{SolorEnginopenharmony} UpdateConversionValueCoarseValueLockWindow");
        }


        #region 腾讯回传

        private static void TrackReActive(ReActiveAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony} TrackReActive");
        }

        private static void TrackAddToWishlist(AddToWishlistAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony} TrackAddToWishlist");
        }

        private static void TrackShare(ShareAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony}TrackShare");
        }

        private static void TrackCreateRole(CreateRoleAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony}TrackCreateRole");
        }

        private static void TrackTutorialFinish(TutorialFinishAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony} TrackTutorialFinish");
        }

        private static void TrackUpdateLevel(UpdateLevelAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony}TrackUpdateLevel");
        }

        private static void TrackViewContentMall(ViewContentMallAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony}TrackViewContentMall");
        }

        private static void TrackViewContentActivity(ViewContentActivitAttributes attributes)
        {
            Debug.Log($"{SolorEnginopenharmony} TrackViewContentActivity");
        }

        #endregion


        private static object attributionCallback(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                int code = args[0].As<int>();
                string json = args[1].As<string>();
                if (islog)
                {
                    Debug.Log("attributionCallback  json: " + json);
                    Debug.Log("attributionCallback  Data: " + code);
                }

                OnAttributionHandler(code, json);
            }

            return true;
        }

        private static object onDeepLinkCallback(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                int code = args[0].As<int>();
                string json = args[1].As<string>();
                if (islog)
                {
                    Debug.Log("onDeepLinkCallback  json: " + json);
                    Debug.Log("onDeepLinkCallback  Data: " + code);
                }

                OnDeeplinkCompletionHandler(code, json);
            }

            return true;
        }

        private static object onDelayDeepLinkCallback(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                int code = args[0].As<int>();
                string json = args[1].As<string>();
                if (islog)
                {
                    Debug.Log("onDelayDeepLinkCallback  Data: " + code);
                    Debug.Log("onDelayDeepLinkCallback  json: " + json);
                }

                OnDelayDeeplinkCompletionHandler(code, json);
            }

            return true;
        }


        private static object onRequestPermissionsCallback(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                int code = args[0].As<int>();

                if (islog)
                {
                    Debug.Log("onRequestPermissionsCallback  code: " + code);
                }

                getRequestPermission_private?.Invoke(code);
            }

            return true;
        }

        private static object getPresetPropertiesCallBack(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                string value = args[0].As<string>();
                if (islog)
                {
                    Debug.Log("getPresetPropertiesCallBack  json: " + value);
                }

                Dictionary<string, object> data = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(value))
                {
                    data = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
                }

                getProperties_private?.Invoke(data);
            }

            return true;
        }


        private static object initSDKCallback(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                int code = args[0].As<int>();
                if (islog)
                {
                    Debug.Log("initSDKCallback  code: " + code);
                }

                Analytics.Instance.initCompletedCallback_private?.Invoke(code);
            }

            return true;
        }


        private static object getVisitorIDCallback(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                string VisitorID = args[0].As<string>();
                if (islog)
                {
                    Debug.Log("getVisitorIDCallback  VisitorID: " + VisitorID);
                }

                getVisitor_private?.Invoke(VisitorID);
            }

            return true;
        }

        private static object getDistinctIDCallback(params OpenHarmonyJSObject[] args)
        {
            if (args.Length > 0)
            {
                string DistinctID = args[0].As<string>();
                if (islog)
                {
                    Debug.Log("getDistinctIDCallback  DistinctID: " + DistinctID);
                }


                getDistinct_private?.Invoke(DistinctID);
            }

            return true;
        }
    }
}

#endif