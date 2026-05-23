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
        private SEAttributionCallback attributionCallback_private = null;


        public delegate void SEAttributionCallback(int code, Dictionary<string, object> attribution);

        private SESDKInitCompletedCallback initCompletedCallback_private = null;


        public delegate void SESDKInitCompletedCallback(int code);

        private SESDKDeeplinkCallback deeplinkCallback_private = null;

        public delegate void SESDKDeeplinkCallback(int code, Dictionary<string, object> data);

        // 延迟deeplink
        private SESDKDeferredDeeplinkCallback delayDeeplinkCallback_private = null;

        public delegate void SESDKDeferredDeeplinkCallback(int code, Dictionary<string, object> data);

        private SESDKATTCompletedCallback attCompletedCallback_private = null;

        public delegate void SESDKATTCompletedCallback(int code);

        private static bool islog = false;

        // only ios
        public delegate void SKANUpdateCompletionHandler(int errorCode, String errorMsg);

        private SKANUpdateCompletionHandler iosSKANUpdateCVCompletionHandler_private = null;
        private SKANUpdateCompletionHandler iosSKANUpdateCVCoarseValueCompletionHandler_private = null;
        private SKANUpdateCompletionHandler iosSKANUpdateCVCoarseValueLockWindowCompletionHandler_private = null;


        public delegate void SEiOSStringCallback(int code, string dataString);

        private static List<Action> waitingTaskList = new List<Action>();
        private static List<Action> executingTaskList = new List<Action>();

        private static Analytics _instance = null;


        public static Analytics Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType(typeof(Analytics)) as Analytics;
                    if (!_instance)
                    {
                        GameObject am = new GameObject("Analytics");
                        _instance = am.AddComponent(typeof(Analytics)) as Analytics;
                        DontDestroyOnLoad(am);
                    }
                }

                return _instance;
            }
        }

        public static void PostTask(Action task)
        {
            lock (waitingTaskList)
            {
                waitingTaskList.Add(task);
            }
        }

        private void Update()
        {
            lock (waitingTaskList)
            {
                if (waitingTaskList.Count > 0)
                {
                    if (islog)
                    {
                        Debug.Log($"{SolorEngine} waitingTaskList.Count: {waitingTaskList.Count}");
                    }

                    executingTaskList.AddRange(waitingTaskList);

                    waitingTaskList.Clear();
                }
            }

            for (int i = 0; i < executingTaskList.Count; ++i)
            {
                Action task = executingTaskList[i];
                try
                {
                    task();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message, this);
                }
            }

            executingTaskList.Clear();
        }


        #region OpenAPI

        /// <summary>
        /// 预初始化 SDK。
        /// </summary>
        /// <param name="appKey">应用 appKey，请联系商务人员获取。</param>
        public static void preInitSeSdk(string appKey)
        {
            PreInitSeSdk(appKey);
        }


        /// <summary>
        /// 用于应用启动后，初始化 SDK。
        /// </summary>
        /// <param name="appKey">应用 appKey，请联系商务人员获取。</param>
        /// <param name="userID">用户 ID ，请联系商务人员获取。</param>
        /// <param name="SEConfig">见SEConfig 说明</param>
        public static void initSeSdk(string appKey, string userID, SEConfig config = new SEConfig())
        {
            islog = config.logEnabled;
            Init(appKey, userID, config);
        }

        /// <summary>
        /// 用于应用启动后，初始化 SDK。
        /// </summary>
        /// <param name="appKey">应用 appKey，请联系商务人员获取。</param>
        /// <param name="SEConfig">见SEConfig 说明</param>
        public static void initSeSdk(string appKey, SEConfig config = new SEConfig())
        {
            islog = config.logEnabled;
            Init(appKey, null, config);
        }

        /// <summary>
        /// 用于应用启动后，初始化 SDK。
        /// </summary>
        /// <param name="appKey">应用 appKey，请联系商务人员获取。</param>
        /// <param name="userID">用户 ID ，请联系商务人员获取。</param>
        /// <param name="SEConfig">见SEConfig 说明</param>
        public static void initSeSdk(string appKey, string userID, SEConfig config = new SEConfig(),
            RCConfig rcConfig = new RCConfig())
        {
            Init(appKey, userID, config, rcConfig);
        }

        /// <summary>
        /// 用于应用启动后，初始化 SDK。
        /// </summary>
        /// <param name="appKey">应用 appKey，请联系商务人员获取。</param>
        /// <param name="SEConfig">见SEConfig 说明</param>
        public static void initSeSdk(string appKey, SEConfig config, RCConfig rcConfig)
        {
            islog = config.logEnabled;
            Init(appKey, null, config, rcConfig);
        }

//         public static void attributionCallback(SEAttributionCallback callback)
//         {
//             if (callback!= null)
//             {
//                 Analytics.Instance.attributionCallback_private = callback;
// #if UNITY_OPENHARMONY&&!UNITY_EDITOR
//                 setAttributionListener();
// #endif
//             }
//
//             AttributionCompletedCallback(callback);
//         }
//
//         public static void initCompletedCallback(SESDKInitCompletedCallback callback)
//         { 
//             if (callback != null)
//             {
//                 Analytics.Instance.initCompletedCallback_private = callback;
// #if UNITY_OPENHARMONY&&!UNITY_EDITOR
//                 setInitSDKListener();
// #endif
//             }
//
//             InitCompletedCallback(callback);
//         }

        public static Dictionary<string, object> getAttribution()
        {
            string attributionString = GetAttribution();

            if (attributionString == null)
            {
                return null;
            }

            Dictionary<string, object> attribution = null;

            try
            {
                attribution = JsonConvert.DeserializeObject<Dictionary<string, object>>(attributionString);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            return attribution;
        }

        /// <summary>
        /// 设置访客 ID
        /// </summary>
        /// <param name="visitorId">访客 ID</param>
        public static void setVisitorId(string visitorId)
        {
            if (visitorId == null)
            {
                Debug.Log("visitorId must not be null");
                return;
            }

            SetVisitorID(visitorId);
        }


#if UNITY_OPENHARMONY&&!UNITY_EDITOR
        public static void getVisitorId(Action<string> callback)
        {
            GetVisitorID(callback);
        }

        public static void getDistinctId(Action<string> distinct)
        {
            GetDistinctId(distinct);
        }

        /// <summary>
        /// 获取设备、用户相关信息'
        /// <returns>设备、用户相关信息</returns>
        /// </summary>
        public static void getPresetProperties(Action<Dictionary<string, object>> properties)
        {
            GetPresetProperties(properties);
        }

        public static void authorizationCompleted()
        {
             AuthorizationCompleted();
        }

        public static void requestPermissionsFromUser(Action<int> callback)
        {
            RequestPermissionsFromUser(callback);
        }

#else

        /// <summary>
        /// 获取访客 ID
        /// </summary>
        /// <returns></returns>
        public static string getVisitorId()
        {
            return GetVisitorID();
        }


        /// <summary>
        /// 获取distinct_id
        /// </summary>
        /// <returns>distinct_id</returns>
        public static string getDistinctId()
        {
            return GetDistinctId();
        }

        /// <summary>
        /// 获取设备、用户相关信息'
        /// <returns>设备、用户相关信息</returns>
        /// </summary>
        public static Dictionary<string, object> getPresetProperties()
        {
            return GetPresetProperties();
        }

#endif

        /// <summary>
        /// 设置账户 ID
        /// </summary>
        /// <param name="accountId">账户 ID</param>
        public static void login(string accountId)
        {
            if (accountId == null)
            {
                Debug.Log("accountId must not be null");
                return;
            }

            Login(accountId);
        }

        /// <summary>
        /// 获取账户 ID
        /// </summary>
        /// <returns>账户 ID</returns>
        public static string getAccountId()
        {
            return GetAccountId();
        }

        /// <summary>
        /// 清除账号 ID
        /// </summary>
        public static void logout()
        {
            Logout();
        }

        /// <summary>
        /// 设置谷歌Gaid
        /// </summary>
        /// <param name="gaid">谷歌 gaid，此方法只支持Android系统，IOS不支持</param>
        public static void setGaid(string gaid)
        {
            SetGaid(gaid);
        }

        /// <summary>
        /// 设置渠道名
        /// </summary>
        /// <param name="channel">渠道名，此方法只支持Android系统，IOS不支持</param>
        public static void setChannel(string channel)
        {
            SetChannel(channel);
        }

        /// <summary>
        /// 设置GDPR地区
        /// </summary>
        /// <param name="isGDPRArea">是否属于GDPR地区</param>
        public static void setGDPRArea(bool isGDPRArea)
        {
            SetGDPRArea(isGDPRArea);
        }

        public static void setOaid(string oaid)
        {
            SetOaid(oaid);
        }

        public static void setReferrerTitle(string title)
        {
            SetReferrerTitle(title);
        }

        public static void setXcxPageTitle(string title)
        {
            SetXcxPageTitle(title);
        }


        /// <summary>
        /// 设置公共事件属性
        /// </summary>
        /// <param name="properties">公共事件属性</param>
        public static void setSuperProperties(Dictionary<string, object> properties)
        {
            if (properties == null)
            {
                Debug.Log("userProperties must not be null");
                return;
            }

            SetSuperProperties(properties);
        }

        /// <summary>
        /// 清除某个 key 对应公共事件属性
        /// </summary>
        /// <param name="key">键</param>
        public static void unsetSuperProperty(string key)
        {
            if (key == null)
            {
                Debug.Log("key must not be null");
                return;
            }

            UnsetSuperProperty(key);
        }

        /// <summary>
        /// 清除公共事件属性
        /// </summary>
        public static void clearSuperProperties()
        {
            ClearSuperProperties();
        }

        /// <summary>
        /// 上报首次事件
        /// </summary>
        /// <param name="attributes">SEBaseAttributes 实例</param>
        public static void trackFirstEvent(SEBaseAttributes attributes)
        {
            if (attributes == null)
            {
                Debug.Log("attributes must not be null");
                return;
            }

            TrackFirstEvent(attributes);
        }

        /// <summary>
        /// 上报应用内购买事件
        /// </summary>
        /// <param name="attributes">ProductsAttributes 实例</param>
        [Obsolete("This method is obsolete. Please use trackPurchase instead.")]
        public static void trackIAP(ProductsAttributes attributes)
        {
            ReportIAPEvent(attributes);
        }


        /// <summary>
        /// 上报应用内购买事件
        /// </summary>
        /// <param name="attributes">ProductsAttributes 实例</param>
        public static void trackPurchase(ProductsAttributes attributes)
        {
            ReportIAPEvent(attributes);
        }

        public static void trackAppReEngagement(Dictionary<string, object> customAttributes)
        {
            TrackAppReEngagement(customAttributes);
        }

        /// <summary>
        /// 上报变现广告展示事件
        /// </summary>
        /// <param name="attributes">AppImpressionAttributes 实例</param>
        [Obsolete("This method is obsolete. Please use trackAdImpression instead.")]
        public static void trackIAI(ImpressionAttributes attributes)
        {
            ReportIAIEvent(attributes);
        }

        /// <summary>
        /// 上报变现广告展示事件
        /// </summary>
        /// <param name="attributes">AppImpressionAttributes 实例</param>
        /// 
        public static void trackAdImpression(ImpressionAttributes attributes)
        {
            ReportIAIEvent(attributes);
        }

        /// <summary>
        /// 上报变现广告点击事件
        /// </summary>
        /// <param name="attributes">AdClickAttributes 实例</param>
        public static void trackAdClick(AdClickAttributes attributes)
        {
            ReportAdClickEvent(attributes);
        }

        /// <summary>
        /// 上报注册事件
        /// </summary>
        /// <param name="attributes">RegisterAttributes 实例</param>
        public static void trackRegister(RegisterAttributes attributes)
        {
            ReportRegisterEvent(attributes);
        }

        /// <summary>
        /// 上报登录事件
        /// </summary>
        /// <param name="attributes">LoginAttributes 实例</param>
        public static void trackLogin(LoginAttributes attributes)
        {
            ReportLoginEvent(attributes);
        }

        /// <summary>
        /// 上报订单事件
        /// </summary>
        /// <param name="attributes">OrderAttributes 实例</param>
        public static void trackOrder(OrderAttributes attributes)
        {
            ReportOrderEvent(attributes);
        }

        /// <summary>
        /// 上报自定义归因安装事件
        /// </summary>
        /// <param name="attributes">AppAttributes 实例</param>
        [Obsolete("This method is obsolete. Please use trackAttribution instead.")]
        public static void trackAppAttr(AttAttributes attributes)
        {
            AppAttrEvent(attributes);
        }

        /// <summary>
        /// 上报自定义归因安装事件
        /// </summary>
        /// <param name="attributes">AppAttributes 实例</param>
        public static void trackAttribution(AttAttributes attributes)
        {
            AppAttrEvent(attributes);
        }


        #region 腾讯回传

        public static void trackReActive(ReActiveAttributes attributes)
        {
            TrackReActive(attributes);
        }

        public static void trackAddToWishlist(AddToWishlistAttributes attributes)
        {
            TrackAddToWishlist(attributes);
        }

        public static void trackShare(ShareAttributes attributes)
        {
            TrackShare(attributes);
        }

        public static void trackCreateRole(CreateRoleAttributes attributes)
        {
            TrackCreateRole(attributes);
        }

        public static void trackTutorialFinish(TutorialFinishAttributes attributes)
        {
            TrackTutorialFinish(attributes);
        }

        public static void trackUpdateLevel(UpdateLevelAttributes attributes)
        {
            TrackUpdateLevel(attributes);
        }

        public static void trackViewContentMall(ViewContentMallAttributes attributes)
        {
            TrackViewContentMall(attributes);
        }

        public static void trackViewContentActivity(ViewContentActivitAttributes attributes)
        {
            TrackViewContentActivity(attributes);
        }

        #endregion


        /// <summary>
        /// 上报自定义事件
        /// </summary>
        /// <param name="customEventName">自定义事件名称</param>
        /// <param name="customAttributes">自定义事件属性</param>
        [Obsolete("This method is obsolete. Please use track instead.")]
        public static void trackCustom(string customEventName, Dictionary<string, object> customAttributes)
        {
            ReportCustomEvent(customEventName, customAttributes);
        }


        /// <summary>
        /// 上报自定义事件
        /// </summary>
        /// <param name="customEventName">自定义事件名称</param>
        /// <param name="customAttributes">自定义事件属性</param>
        public static void track(string customEventName, Dictionary<string, object> customAttributes)
        {
            ReportCustomEvent(customEventName, customAttributes);
        }

        /// <summary>
        /// 上报自定义事件
        /// </summary>
        /// <param name="customEventName">自定义事件名称</param>
        /// <param name="customAttributes">自定义事件属性</param>
        /// <param name="preAttributes">SDK预置属性</param>
        [Obsolete("This method is obsolete. Please use track instead.")]
        public static void trackCustom(string customEventName, Dictionary<string, object> customAttributes,
            Dictionary<string, object> preAttributes)
        {
            ReportCustomEventWithPreAttributes(customEventName, customAttributes, preAttributes);
        }

        /// <summary>
        /// 上报自定义事件
        /// </summary>
        /// <param name="customEventName">自定义事件名称</param>
        /// <param name="customAttributes">自定义事件属性</param>
        /// <param name="preAttributes">SDK预置属性</param>
        public static void track(string customEventName, Dictionary<string, object> customAttributes,
            Dictionary<string, object> preAttributes)
        {
            ReportCustomEventWithPreAttributes(customEventName, customAttributes, preAttributes);
        }

        /// <summary>
        /// 创建时长事件
        /// </summary>
        /// <param name="timerEventName">时长事件名称</param>
        public static void eventStart(string timerEventName)
        {
            if (timerEventName == null)
            {
                Debug.Log("timerEventName must not be null");
                return;
            }

            EventStart(timerEventName);
        }

        /// <summary>
        /// 上报时长事件
        /// </summary>
        /// <param name="timerEventName">时长事件名称</param>
        /// <param name="attributes">时长事件自定义属性</param>
        public static void eventFinish(string timerEventName, Dictionary<string, object> attributes)
        {
            if (timerEventName == null)
            {
                Debug.Log("timerEventName must not be null");
                return;
            }

            EventFinish(timerEventName, attributes);
        }


        /// <summary>
        /// 设置预置事件属性
        /// </summary>
        /// <param name="setPresetEvent">事件类型</param>
        /// <param name="properties">事件属性</param>
        public static void setPresetEvent(PresetEventType eventType, Dictionary<string, object> properties)
        {
            SetPresetEvent(eventType, properties);
        }

        /// <summary>
        /// 用户属性初始化设置。使用本方法上传的属性如果已经存在时不修改原有属性值，如果不存在则会新建。
        /// </summary>
        /// <param name="userProperties">用户属性</param>
        public static void userInit(Dictionary<string, object> userProperties)
        {
            UserInit(userProperties);
        }

        /// <summary>
        /// 用户属性更新设置。使用本方法上传的属性如果已经存在时将会覆盖原有的属性值，如果不存在则会新建
        /// </summary>
        /// <param name="userProperties">用户属性<</param>
        public static void userUpdate(Dictionary<string, object> userProperties)
        {
            UserUpdate(userProperties);
        }

        /// <summary>
        /// 用户属性累加操作
        /// </summary>
        /// <param name="userProperties">自定义属性（仅对数值类型的 key 进行累加操作）</param>
        public static void userAdd(Dictionary<string, object> userProperties)
        {
            UserAdd(userProperties);
        }

        /// <summary>
        /// 追加用户属性
        /// </summary>
        /// <param name="userProperties">用户属性</param>
        public static void userAppend(Dictionary<string, object> userProperties)
        {
            UserAppend(userProperties);
        }

        /// <summary>
        /// 重置用户属性。对指定属性进行清空操作
        /// </summary>
        /// <param name="keys">自定义属性 key 数组</param>
        public static void userUnset(string[] keys)
        {
            if (keys == null)
            {
                Debug.Log("keys must not be null");
                return;
            }

            if (keys.Length <= 0)
            {
                Debug.Log("keys length must be > 0");
                return;
            }

            UserUnset(keys);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public static void userDelete(UserDeleteType deleteType)
        {
            UserDelete(deleteType);
        }


        /// <summary>
        /// 立即上报事件，不再等上报策略
        /// </summary>
        public static void reportEventImmediately()
        {
            ReportEventImmediately();
        }

        /// <summary>
        /// 设置deeplink回调
        /// <param name="callback">deeplink回调</param>
        /// </summary>
        public static void deeplinkCompletionHandler(SESDKDeeplinkCallback callback)
        {
            DeeplinkCompletionHandler(callback);
        }

        /// <summary>
        /// 设置深度deeplink回调
        /// <param name="callback">delayDeeplink回调</param>
        /// </summary>
        [Obsolete("This method is obsolete. Please use deferredDeeplinkCompletionHandler.")]
        public static void delayDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
        {
            DelayDeeplinkCompletionHandler(callback);
        }


        public static void deferredDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
        {
            DelayDeeplinkCompletionHandler(callback);
        }


        // <summary>
        /// 设置urlScheme
        /// <param name="url">deeplink url,此方法仅支持Android系统</param>
        /// </summary>
        public static void handleDeepLinkUrl(string url)
        {
            HandleDeepLinkUrl(url);
        }


        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统requestTrackingAuthorizationWithCompletionHandler接口
        /// callback 回调用户授权状态: 0: Not Determined；1: Restricted；2: Denied；3: Authorized ；999: system error
        /// </summary>
        public static void requestTrackingAuthorizationWithCompletionHandler(SESDKATTCompletedCallback callback)
        {
            RequestTrackingAuthorizationWithCompletionHandler(callback);
        }


        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updatePostbackConversionValue
        /// </summary>
        public static void updatePostbackConversionValue(int conversionValue, SKANUpdateCompletionHandler callback)
        {
            UpdatePostbackConversionValue(conversionValue, callback);
        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValue
        /// </summary>
        public static void updateConversionValueCoarseValue(int fineValue, String coarseValue,
            SKANUpdateCompletionHandler callback)
        {
            UpdateConversionValueCoarseValue(fineValue, coarseValue, callback);
        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValueLockWindow
        /// </summary>
        public static void updateConversionValueCoarseValueLockWindow(int fineValue, String coarseValue,
            bool lockWindow, SKANUpdateCompletionHandler callback)
        {
            UpdateConversionValueCoarseValueLockWindow(fineValue, coarseValue, lockWindow, callback);
        }

        #region

        #endregion


        private static void OnAttributionHandler(int code, String attributionString)
        {
            Dictionary<string, object> attribution = null;

            try
            {
                if (attributionString != null)
                {
                    attribution = JsonConvert.DeserializeObject<Dictionary<string, object>>(attributionString);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }


            Analytics.PostTask(() =>
            {
                if (Analytics.Instance.attributionCallback_private != null)
                {
                    Analytics.Instance.attributionCallback_private.Invoke(code, attribution);
                }
                else
                {
                    Debug.Log("Unity Editor: attributionCallback_private not found ");
                }
            });
        }

        private static void OnInitCompletedHandler(int code)
        {
            if (islog)
            {
                Debug.Log($"{SolorEngine}OnInitCompletedHandler");
            }

            Analytics.PostTask(() =>
            {
                if (islog)
                {
                    Debug.Log($"{SolorEngine}initCompletedCallback_private");
                }

                if (Analytics.Instance.initCompletedCallback_private != null)
                {
                    Analytics.Instance.initCompletedCallback_private.Invoke(code);
                }
                else
                {
                    Debug.Log("Unity Editor: initCompletedCallback_private not found ");
                }
            });
        }

        private static void OnRequestTrackingAuthorizationCompletedHandler(int code)
        {
            Analytics.PostTask(() =>
            {
                if (Analytics.Instance.attCompletedCallback_private != null)
                {
                    Analytics.Instance.attCompletedCallback_private.Invoke(code);
                }
                else
                {
                    Debug.Log("Unity Editor: attCompletedCallback_private not found ");
                }
            });
        }

        private static void OnDeeplinkCompletionHandler(int code, String jsonString)
        {
            Dictionary<string, object> deeplinkData = null;

            try
            {
                if (jsonString != null)
                {
                    deeplinkData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            Analytics.PostTask(() =>
            {
                if (Analytics.Instance.deeplinkCallback_private != null)
                {
                    Analytics.Instance.deeplinkCallback_private.Invoke(code, deeplinkData);
                }
                else
                {
                    Debug.Log("Unity Editor: OnDeeplinkCompletionHandler not found ");
                }
            });
        }


        private static void OnDelayDeeplinkCompletionHandler(int code, String jsonString)
        {
            Dictionary<string, object> delayDeeplinkData = null;

            try
            {
                if (jsonString != null)
                {
                    delayDeeplinkData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            Analytics.PostTask(() =>
            {
                if (Analytics.Instance.delayDeeplinkCallback_private != null)
                {
                    Analytics.Instance.delayDeeplinkCallback_private.Invoke(code, delayDeeplinkData);
                }
                else
                {
                    Debug.Log("Unity Editor: OnDelayDeeplinkCompletionHandler not found ");
                }
            });
        }


        private static void OnSKANUpdateCVCompletionHandler(int errorCode, String errorMsg)
        {
            Analytics.PostTask(() =>
            {
                if (Analytics.Instance.iosSKANUpdateCVCompletionHandler_private != null)
                {
                    Analytics.Instance.iosSKANUpdateCVCompletionHandler_private.Invoke(errorCode, errorMsg);
                }
                else
                {
                    Debug.Log("Unity Editor: iosSKANUpdateCVCompletionHandler_private not found ");
                }
            });
        }

        private static void OnSKANUpdateCVCoarseValueCompletionHandler(int errorCode, String errorMsg)
        {
            Analytics.PostTask(() =>
            {
                if (Analytics.Instance.iosSKANUpdateCVCoarseValueCompletionHandler_private != null)
                {
                    Analytics.Instance.iosSKANUpdateCVCoarseValueCompletionHandler_private.Invoke(errorCode, errorMsg);
                }
                else
                {
                    Debug.Log("Unity Editor: iosSKANUpdateCVCoarseValueCompletionHandler_private not found ");
                }
            });
        }


        private static void OnSKANUpdateCVCoarseValueLockWindowCompletionHandler(int errorCode, String errorMsg)
        {
            Analytics.PostTask(() =>
            {
                if (Analytics.Instance.iosSKANUpdateCVCoarseValueLockWindowCompletionHandler_private != null)
                {
                    Analytics.Instance.iosSKANUpdateCVCoarseValueLockWindowCompletionHandler_private.Invoke(errorCode,
                        errorMsg);
                }
                else
                {
                    Debug.Log("Unity Editor: iosSKANUpdateCVCoarseValueLockWindowCompletionHandler_private not found ");
                }
            });
        }

        #endregion
    }
}