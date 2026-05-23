#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using SolarEngineInner;

using UnityEngine;

namespace SolarEngine
{
 
        public partial class Analytics : MonoBehaviour
        {

            //private static SESDKInitCompletedCallback initCompletedCallback_win;
            private static string SolorEnginWin =SolorEngine+"windows not currently supported ";
            private static Dictionary<string, object> GetPresetProperties()
            {
              return WinSDKWrapper.Instance.getPresetProperties();
            }

            private static void PreInitSeSdk(string appKey)
            {
               WinSDKWrapper.Instance.preInitSeSdk(appKey);
                
            }

          
            private static PackageType MainLand()
            {
                PackageType packageType=PackageType.None;
                SolarEngineGlobalInfo.MainLand land = SolarEngineGlobalInfo.getMainLand();

                if (land == SolarEngineGlobalInfo.MainLand.None)
                {
                    Debug.LogError($"{Analytics.SolorEngine} please set mainLand first ");
                  
                }
                else
                {
                    if (land == SolarEngineGlobalInfo.MainLand.China)
                    {
                        packageType = PackageType.ChinaMainland;
                    }
                    else
                    {
                        packageType = PackageType.NonChinaMainland;
                    }
                }
                return packageType;

            }
            private static void Init(string appKey, object userId, SEConfig config)
            {
            PackageType packageType = MainLand();
            if (packageType == PackageType.None)
            {
                return;
            }
                WinSDKWrapper.Instance.init(appKey, packageType,winConfig(config));
            }

            private static WinSDKWrapper.WinConfig winConfig(SEConfig config)
            {
                // if(config.initCompletedCallback==null&& initCompletedCallback_win!=null)
                //     config.initCompletedCallback = initCompletedCallback_win;
                //
                WinSDKWrapper.WinConfig winConfig = new WinSDKWrapper.WinConfig();
                winConfig.isDebugModel = config.isDebugModel;
                winConfig.logEnabled = config.logEnabled;
                winConfig.subLibVersion = sdk_version;
#if TUANJIE_2022_3_OR_NEWER
                winConfig.sdkType = "tuanjie";
#else
                winConfig.sdkType = "unity";
          
#endif
              
              
                if (config.initCompletedCallback != null)
                {
                    WinSDKWrapper.SESDKInitCompletedCallback initCompletedCallback =
                        ( WinSDKWrapper.SESDKInitCompletedCallback)Delegate.CreateDelegate(
                            typeof( WinSDKWrapper.SESDKInitCompletedCallback),
                            config.initCompletedCallback.Target, config.initCompletedCallback.Method);
                    winConfig.initCompletedCallback = initCompletedCallback;
                }
                return winConfig;
            }
            
            // private static void AttributionCompletedCallback(SEAttributionCallback callback)
            // {
            // }
            //
            // private static void InitCompletedCallback(SESDKInitCompletedCallback callback)
            // {
            //     initCompletedCallback_win = callback;
            // }
            private static void Init(string appKey, string userId, SEConfig config, RCConfig rcConfig)
            {
                PackageType packageType = MainLand();
                if (packageType == PackageType.None)
                {
                    return;
                }
               WinSDKWrapper.Instance.init(appKey, packageType,winConfig(config));
            }

            private static void SetVisitorID(string visitorId)
            {
               WinSDKWrapper.Instance.setVisitorId(visitorId);
            }
          
            private static string GetVisitorID()
            {
                return WinSDKWrapper.Instance.getVisitorId();
            }

            private static void Login(string accountId)
            {
               WinSDKWrapper.Instance.login(accountId);
            }

            private static string GetAccountId()
            {
                return WinSDKWrapper.Instance.getAccountId();
            }

            private static void Logout()
            {
               WinSDKWrapper.Instance.logout();
            }

            private static void SetGaid(string gaid)
            {
                Debug.Log($"{SolorEnginWin} : SetGaid");
            }

            private static void SetOaid(string oaid)
            {
                Debug.Log($"{SolorEnginWin} : SetOaid");
            }

            private static void SetChannel(string channel)
            {
               WinSDKWrapper.Instance.setChannel(channel);
            }

            private static void SetGDPRArea(bool isGDPRArea)
            {
                Debug.Log($"{SolorEnginWin}: SetGDPRArea");
            }

            private static string GetDistinctId()
            {
                return WinSDKWrapper.Instance.getDistinctId();
            }

            // private static void GetDistinctId(Action<Distinct> dis)
            // {
            //     returnWinSDKWrapper.Instance.getDistinctId();
            // }

            private static void SetSuperProperties(Dictionary<string, object> userProperties)
            {
               WinSDKWrapper.Instance.setSuperProperties(userProperties);
            }

            private static void UnsetSuperProperty(string key)
            {
               WinSDKWrapper.Instance.unsetSuperProperty(key);
            }

            private static void ClearSuperProperties()
            {
               WinSDKWrapper.Instance.clearSuperProperties();
            }

            private static void EventStart(string timerEventName)
            {
               WinSDKWrapper.Instance.eventStart(timerEventName);
            }

            private static void EventFinish(string timerEventName, Dictionary<string, object> attributes)
            {
               WinSDKWrapper.Instance.eventFinish(timerEventName, attributes);
            
            }



            private static void UserUpdate(Dictionary<string, object> userProperties)
            {
               WinSDKWrapper.Instance.userUpdate(userProperties);
            }

            private static void UserInit(Dictionary<string, object> userProperties)
            {
               WinSDKWrapper.Instance.userInit(userProperties);
            }

            private static void UserAdd(Dictionary<string, object> userProperties)
            {
               WinSDKWrapper.Instance.userAdd(userProperties);
            }

            private static void UserAppend(Dictionary<string, object> userProperties)
            {
               WinSDKWrapper.Instance.userAppend(userProperties);
            }

            private static void UserUnset(string[] keys)
            {
               WinSDKWrapper.Instance.userUnset(keys);
            }

            private static void UserDelete(UserDeleteType deleteType)
            {
                WinSDKWrapper.WinUserDeleteType _UserDeleteType = (WinSDKWrapper.WinUserDeleteType)(int)deleteType;
               WinSDKWrapper.Instance.userDelete(_UserDeleteType);
            }

            private static string GetAttribution()
            {
                Debug.Log($"{SolorEnginWin} : GetAttribution");
                return null;

            }

            private static void TrackFirstEvent(SEBaseAttributes attributes)
            {
                
                Dictionary<string, object> attributesDict = new Dictionary<string, object>();
                string eventName = "";
                WinSDKWrapper.FirstEventType eventType = WinSDKWrapper.FirstEventType.None;
                Dictionary<string, object> customProperties = new Dictionary<string, object>();
                if (attributes is RegisterAttributes registerAttributes)
                {
                    attributesDict.Add(SolarEngine.Analytics.SEConstant_EVENT_TYPE,
                        SolarEngine.Analytics.SEConstant_Register);
                    attributesDict.Add(SolarEngine.Analytics.SEConstant_Register_Type, registerAttributes.register_type);
                    attributesDict.Add(SolarEngine.Analytics.SEConstant_Register_Status,
                        registerAttributes.register_status);
                    eventName = SEConstant_Register;
                    eventType= WinSDKWrapper.FirstEventType.Reg;
                    customProperties=registerAttributes.customProperties;

                }
                else  if (attributes is CustomAttributes customAttributes)
                {
                    attributesDict = customAttributes.preProperties;
                    eventName= customAttributes.custom_event_name;
                    eventType= WinSDKWrapper.FirstEventType.Custom;
                    customProperties=customAttributes.customProperties;
                }
                else
                {
                    Debug.Log($"{SolorEnginWin} firstevent other than register events and custom events ");
                    return;
                }
                LogTool.DebugLog($"attributes.checkId: {attributes.checkId}");
            
                WinSDKWrapper.Instance. trackFirstEvent( eventName, eventType, attributes.checkId, attributesDict, customProperties);
            }

            private static void ReportIAPEvent(ProductsAttributes attributes)
            {
                WinSDKWrapper.Instance.trackPurchase(getIAPDic(attributes, false), attributes.customProperties);

            }

            private static void ReportIAIEvent(ImpressionAttributes attributes)
            {
               WinSDKWrapper.Instance.trackAdImpression(getIAIDic(attributes,false), attributes.customProperties);
            }

            private static void ReportAdClickEvent(AdClickAttributes attributes)
            {
               WinSDKWrapper.Instance.trackAdClick(getAdClickDic(attributes,false), attributes.customProperties);
            }

            private static void ReportRegisterEvent(RegisterAttributes attributes)
            {
                WinSDKWrapper.Instance.trackRegister(getRegisterDic(attributes, false), attributes.customProperties);
            }

            private static void ReportLoginEvent(LoginAttributes attributes)
            {
                WinSDKWrapper.Instance.trackLogin(getLoginDic(attributes, false), attributes.customProperties);
            }

            private static void ReportOrderEvent(OrderAttributes attributes)
            {
                WinSDKWrapper.Instance.trackOrder(getOrderDic(attributes, false), attributes.customProperties);
            }

            private static void AppAttrEvent(AttAttributes attributes)
            {
                WinSDKWrapper.Instance.trackAppAttr(getAttrDic(attributes, false), attributes.customProperties);
            }


            private static void SetPresetEvent(PresetEventType eventType, Dictionary<string, object> attributes)
            {
                WinSDKWrapper.WinEventType _eventType = (WinSDKWrapper.WinEventType)(int)eventType;
                
               WinSDKWrapper.Instance.setPresetEvent(_eventType, attributes);
            }

            private static void ReportCustomEvent(string customEventName, Dictionary<string, object> attributes)
            {
               WinSDKWrapper.Instance.track(customEventName, attributes);
            }

            private static void ReportCustomEventWithPreAttributes(string customEventName,
                Dictionary<string, object> customAttributes, Dictionary<string, object> preAttributes)
            {
               WinSDKWrapper.Instance.track(customEventName, customAttributes, preAttributes);
            }

            private static void ReportEventImmediately()
            {
               WinSDKWrapper.Instance.reportEventImmediately();
            }

            private static void HandleDeepLinkUrl(string url)
            {
                Debug.Log($"{SolorEnginWin}: HandleDeepLinkUrl not found");
            }


            private static void DeeplinkCompletionHandler(SESDKDeeplinkCallback callback)
            {
                Debug.Log($"{SolorEnginWin}: DeeplinkCompletionHandler not found");

            }


            private static void DelayDeeplinkCompletionHandler(SESDKDeferredDeeplinkCallback callback)
            {
                Debug.Log($"{SolorEnginWin}: DelayDeeplinkCompletionHandler not found");
            }

            private static void SetReferrerTitle(string title)
            {
                Debug.Log($"{SolorEnginWin}: SetReferrerTitle ");
            }

            private static void SetXcxPageTitle(string title)
            {
                Debug.Log($"{SolorEnginWin}: SetXcxPageTitle ");
            }



            /// <summary>
            /// 仅支持iOS
            /// SolarEngine 封装系统requestTrackingAuthorizationWithCompletionHandler接口
            /// callback 回调用户授权状态: 0: Not Determined；1: Restricted；2: Denied；3: Authorized ；999: system error
            /// </summary>
            private static void RequestTrackingAuthorizationWithCompletionHandler(SESDKATTCompletedCallback callback)
            {
                Debug.Log($"{SolorEnginWin}: RequestTrackingAuthorizationWithCompletionHandler");

            }

            /// <summary>
            /// 仅支持iOS
            /// SolarEngine 封装系统updatePostbackConversionValue
            /// </summary>
            private static void UpdatePostbackConversionValue(int conversionValue, SKANUpdateCompletionHandler callback)
            {

                Debug.Log($"{SolorEnginWin}: UpdatePostbackConversionValue");

            }

            /// <summary>
            /// 仅支持iOS
            /// SolarEngine 封装系统updateConversionValueCoarseValue
            /// </summary>
            private static void UpdateConversionValueCoarseValue(int fineValue, string  coarseValue,
                SKANUpdateCompletionHandler callback)
            {

                Debug.Log($"{SolorEnginWin}: UpdateConversionValueCoarseValue");


            }

            /// 仅支持iOS
            /// SolarEngine 封装系统updateConversionValueCoarseValueLockWindow
            /// </summary>
            private static void UpdateConversionValueCoarseValueLockWindow(int fineValue, string coarseValue,
                bool lockWindow, SKANUpdateCompletionHandler callback)
            {

                Debug.Log($"{SolorEnginWin}: UpdateConversionValueCoarseValueLockWindow");

            }


            #region 腾讯回传
            private static void GetDistinctId(Action<Distinct>distinct)
            {
                Debug.Log($"{SolorEnginWin}: GetDistinctId with Action<Distinct>");
            }   
            private static void TrackReActive(ReActiveAttributes attributes)
            {

                Debug.Log($"{SolorEnginWin}: TrackReActive");
            }

            private static void TrackAddToWishlist(AddToWishlistAttributes attributes)
            {
                Debug.Log($"{SolorEnginWin}: TrackAddToWishlist");

            }

            private static void TrackShare(ShareAttributes attributes)
            {
                Debug.Log($"{SolorEnginWin}: TrackShare");
            }

            private static void TrackCreateRole(CreateRoleAttributes attributes)
            {
                Debug.Log($"{SolorEnginWin}: TrackCreateRole");
            }

            private static void TrackTutorialFinish(TutorialFinishAttributes attributes)
            {
                Debug.Log($"{SolorEnginWin}: TrackTutorialFinish");
            }

            private static void TrackUpdateLevel(UpdateLevelAttributes attributes)
            {
                Debug.Log($"{SolorEnginWin}: TrackUpdateLevel");
            }

            private static void TrackViewContentMall(ViewContentMallAttributes attributes)
            {
                Debug.Log($"{SolorEnginWin}: TrackViewContentMall");
            }

            private static void TrackViewContentActivity(ViewContentActivitAttributes attributes)
            {
                Debug.Log($"{SolorEnginWin}: TrackViewContentActivity");
            }

      private static void TrackAppReEngagement(Dictionary<string, object> customAttributes)
        {
                Debug.Log($"{SolorEnginWin}: TrackAppReEngagement");
        }

            #endregion

        }


    

}
#endif