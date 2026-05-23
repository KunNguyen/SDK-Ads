#if (UNITY_EDITOR&&!UNITY_STANDALONE_WIN)&&!SE_DEV&&!SOLORENGINE_DEVELOPEREDITOR
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
            Debug.Log($"{SolorEngine}Unity Editor: GetPresetProperties");
            return null;
        }

        private static void PreInitSeSdk(string appKey)
        {
            Debug.Log($"{SolorEngine}Unity Editor: PreInitSeSdk");
        }


        private static void Init(string appKey, object userId, SEConfig config)
        {
            Debug.Log($"{SolorEngine}Unity Editor: Init");
        }

        private static void Init(string appKey, string userId, SEConfig config, RCConfig rcConfig)
        {
            Debug.Log($"{SolorEngine}Unity Editor: Init");
        }

        private static void SetVisitorID(string visitorId)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetVisitorID");
        }

        private static string GetVisitorID()
        {
            Debug.Log($"{SolorEngine}Unity Editor: GetVisitorID");
            return null;
        }

        private static void Login(string accountId)
        {
            Debug.Log($"{SolorEngine}Unity Editor: Login");
        }

        private static string GetAccountId()
        {
            Debug.Log($"{SolorEngine}Unity Editor: GetAccountId");
            return null;
        }

        private static void Logout()
        {
            Debug.Log($"{SolorEngine}Unity Editor: Logout");
        }

        private static void SetGaid(string gaid)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetGaid");
        }

        private static void SetOaid(string oaid)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetOaid");
        }

        private static void SetChannel(string channel)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetChannel");
        }

        private static void SetGDPRArea(bool isGDPRArea)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetGDPRArea");
        }

        private static string GetDistinctId()
        {
            Debug.Log($"{SolorEngine}Unity Editor: GetDistinctId");
            return null;
        }

        private static void GetDistinctId(Action<Distinct> dis)
        {
            Debug.Log($"{SolorEngine}Unity Editor: GetDistinctId");
        }

        private static void SetSuperProperties(Dictionary<string, object> userProperties)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetSuperProperties");
        }

        private static void UnsetSuperProperty(string key)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UnsetSuperProperty");
        }

        private static void ClearSuperProperties()
        {
            Debug.Log($"{SolorEngine}Unity Editor: ClearSuperProperties");
        }

        private static void EventStart(string timerEventName)
        {
            Debug.Log($"{SolorEngine}Unity Editor: EventStart");
        }

        private static void EventFinish(string timerEventName, Dictionary<string, object> attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: EventFinish");
        }


        private static void UserUpdate(Dictionary<string, object> userProperties)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UserUpdate");
        }

        private static void UserInit(Dictionary<string, object> userProperties)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UserInit");
        }

        private static void UserAdd(Dictionary<string, object> userProperties)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UserAdd");
        }

        private static void UserAppend(Dictionary<string, object> userProperties)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UserAppend");
        }

        private static void UserUnset(string[] keys)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UserUnset");
        }

        private static void UserDelete(UserDeleteType deleteType)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UserDelete");
        }

        private static string GetAttribution()
        {
            Debug.Log($"{SolorEngine}Unity Editor: GetAttribution");
            return null;
        }

        private static void TrackFirstEvent(SEBaseAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackFirstEvent");
        }

        private static void ReportIAPEvent(ProductsAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportIAPEvent");
        }

        private static void ReportIAIEvent(ImpressionAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportIAIEvent");
        }

        private static void ReportAdClickEvent(AdClickAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportAdClickEvent");
        }

        private static void ReportRegisterEvent(RegisterAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportRegisterEvent");
        }

        // private static void AttributionCompletedCallback(SEAttributionCallback callback)
        // {
        // }
        //
        // private static void InitCompletedCallback(SESDKInitCompletedCallback callback)
        // {
        // }

        private static void ReportLoginEvent(LoginAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportLoginEvent");
        }

        private static void ReportOrderEvent(OrderAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportOrderEvent");
        }

        private static void AppAttrEvent(AttAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: AppAttrEvent");
        }


        private static void SetPresetEvent(PresetEventType eventType, Dictionary<string, object> attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetPresetEvent");
        }

        private static void ReportCustomEvent(string customEventName, Dictionary<string, object> attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportCustomEvent");
        }

        private static void ReportCustomEventWithPreAttributes(string customEventName,
            Dictionary<string, object> customAttributes, Dictionary<string, object> preAttributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportCustomEventWithPreAttributes");
        }

        private static void ReportEventImmediately()
        {
            Debug.Log($"{SolorEngine}Unity Editor: ReportEventImmediately");
        }

        private static void TrackAppReEngagement(Dictionary<string, object> attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackAppReEngagement");
        }

        private static void HandleDeepLinkUrl(string url)
        {
            Debug.Log($"{SolorEngine}Unity Editor: HandleDeepLinkUrl not found");
        }


        private static void DeeplinkCompletionHandler(SESDKDeeplinkCallback callback)
        {
            Debug.Log($"{SolorEngine}Unity Editor: DeeplinkCompletionHandler not found");
        }


        private static void DelayDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
        {
            Debug.Log($"{SolorEngine}Unity Editor: DelayDeeplinkCompletionHandler not found");
        }

        private static void SetReferrerTitle(string title)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetReferrerTitle ");
        }

        private static void SetXcxPageTitle(string title)
        {
            Debug.Log($"{SolorEngine}Unity Editor: SetXcxPageTitle ");
        }


        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统requestTrackingAuthorizationWithCompletionHandler接口
        /// callback 回调用户授权状态: 0: Not Determined；1: Restricted；2: Denied；3: Authorized ；999: system error
        /// </summary>
        private static void RequestTrackingAuthorizationWithCompletionHandler(SESDKATTCompletedCallback callback)
        {
            Debug.Log($"{SolorEngine}Unity Editor: RequestTrackingAuthorizationWithCompletionHandler");
        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updatePostbackConversionValue
        /// </summary>
        private static void UpdatePostbackConversionValue(int conversionValue, SKANUpdateCompletionHandler callback)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UpdatePostbackConversionValue");
        }

        /// <summary>
        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValue
        /// </summary>
        private static void UpdateConversionValueCoarseValue(int fineValue, String coarseValue,
            SKANUpdateCompletionHandler callback)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UpdateConversionValueCoarseValue");
        }

        /// 仅支持iOS
        /// SolarEngine 封装系统updateConversionValueCoarseValueLockWindow
        /// </summary>
        private static void UpdateConversionValueCoarseValueLockWindow(int fineValue, String coarseValue,
            bool lockWindow, SKANUpdateCompletionHandler callback)
        {
            Debug.Log($"{SolorEngine}Unity Editor: UpdateConversionValueCoarseValueLockWindow");
        }


        #region 腾讯回传

        private static void TrackReActive(ReActiveAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackReActive");
        }

        private static void TrackAddToWishlist(AddToWishlistAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackAddToWishlist");
        }

        private static void TrackShare(ShareAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackShare");
        }

        private static void TrackCreateRole(CreateRoleAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackCreateRole");
        }

        private static void TrackTutorialFinish(TutorialFinishAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackTutorialFinish");
        }

        private static void TrackUpdateLevel(UpdateLevelAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackUpdateLevel");
        }

        private static void TrackViewContentMall(ViewContentMallAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackViewContentMall");
        }

        private static void TrackViewContentActivity(ViewContentActivitAttributes attributes)
        {
            Debug.Log($"{SolorEngine}Unity Editor: TrackViewContentActivity");
        }

        #endregion
    }
}
#endif