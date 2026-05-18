using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using JisSDKAds.Ads;
using JisSDKAds.Common;
using UnityEngine;

#if UNITY_APPSFLYER
using AppsFlyerSDK;
using AppsFlyerSdk = AppsFlyerSDK.AppsFlyer;
#endif

namespace JisSDKAds.Analytics.AppsFlyer
{
#if !UNITY_APPSFLYER
    public class ABIAppsflyerManager : MonoBehaviour
    {
    }
#else
    [ScriptOrder(-150)]
    public class AppsflyerManagerBehaviour : MonoBehaviour, IAppsFlyerConversionData
    {
        private static AppsflyerManagerBehaviour instance;
        public static AppsflyerManagerBehaviour Instance => instance;

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            instance = this;
        }

        public void onConversionDataSuccess(string conversionData)
        {
            Debug.Log("ABIAppsflyer onConversionDataSuccess " + conversionData);
        }

        public void onConversionDataFail(string error)
        {
            AppsFlyerSdk.AFLog("onConversionDataFail", error);
            Debug.Log("ABIAppsflyer onConversionDataFail " + error);
        }

        public void onAppOpenAttribution(string attributionData)
        {
            AppsFlyerSdk.AFLog("onAppOpenAttribution", attributionData);
            Debug.Log("ABIAppsflyer onAppOpenAttribution " + attributionData);
        }

        public void onAppOpenAttributionFailure(string error)
        {
            AppsFlyerSdk.AFLog("onAppOpenAttributionFailure", error);
            Debug.Log("ABIAppsflyer onAppOpenAttributionFailure " + error);
        }
    }

    static class AppsflyerTracking
    {
        public const string af_inters_logicgame = "af_inters_logicgame";
        public const string af_inters_successfullyloaded = "af_inters_successfullyloaded";
        public const string af_inters_displayed = "af_inters_displayed";
        public const string af_inters_show_count = "af_inters_show_count_";
        public const string af_rewarded_logicgame = "af_rewarded_logicgame";
        public const string af_rewarded_successfullyloaded = "af_rewarded_successfullyloaded";
        public const string af_rewarded_displayed = "af_rewarded_displayed";
        public const string af_rewarded_show_count = "af_rewarded_show_count_";
        public const string af_level_achieved = "af_level_achieved";
        public const string af_completed_level = "completed_level_";
        public const string af_first_open = "first_open";

        public static void SendEvent(string eventName, Dictionary<string, string> pairs)
        {
            AppsFlyerSdk.sendEvent(eventName, pairs);
        }

        public static void SendEvent(string eventName)
        {
            Debug.Log("ABIAppsflyer call send event " + eventName);
            AppsFlyerSdk.sendEvent(eventName, new Dictionary<string, string>());
        }

        public static void onFirstOpen()
        {
            SendEvent(af_first_open);
        }

        public static void TrackInterstitial_ClickShowButton()
        {
            SendEvent(af_inters_logicgame);
        }

        public static void TrackInterstitial_LoadedSuccess()
        {
            SendEvent(af_inters_successfullyloaded);
        }

        public static void TrackInterstitial_Displayed()
        {
            SendEvent(af_inters_displayed);
        }

        public static void TrackInterstitial_ShowCount(int total)
        {
            Debug.Log("TrackInterstitial_ShowCount " + total);
            if (total == 0) return;
            if (total <= 20)
            {
                string eventName = string.Format(af_inters_show_count, total);
                SendEvent(eventName);
            }
        }

        public static void TrackRewarded_ClickShowButton()
        {
            SendEvent(af_rewarded_logicgame);
        }

        public static void TrackRewarded_LoadedSuccess()
        {
            SendEvent(af_rewarded_successfullyloaded);
        }

        public static void TrackRewarded_Displayed()
        {
            SendEvent(af_rewarded_displayed);
        }

        public static void TrackRewarded_ShowCount(int total)
        {
            if (total == 0) return;
            bool isTracking = total % 5 == 0;
            if (!isTracking) return;
            string eventName = af_rewarded_show_count + total;
            SendEvent(eventName);
        }

        public static void TrackAppflyerPurchase(string purchaseId, decimal cost, string currency)
        {
            float fCost = (float)cost;
            fCost *= 0.65f;
            Dictionary<string, string> eventValue = new Dictionary<string, string>();
            eventValue.Add(AFInAppEvents.REVENUE, fCost.ToString(CultureInfo.InvariantCulture));
            eventValue.Add(AFInAppEvents.CURRENCY, currency);
            eventValue.Add(AFInAppEvents.QUANTITY, "1");
            AppsFlyerSdk.sendEvent(AFInAppEvents.PURCHASE, eventValue);
        }

        public static void TrackAppsflyerAdRevenue(ImpressionData impressionData)
        {
            Debug.Log("TrackAppsflyerAdRevenue " + impressionData.ad_revenue + " " + impressionData.ad_source + " " +
                      impressionData.ad_mediation + " " + impressionData.ad_unit_name + " " + impressionData.ad_format);
            MediationNetwork mediationNetwork = MediationNetwork.ApplovinMax;
            switch (impressionData.ad_mediation)
            {
                case AdsMediationType.MAX:
                    mediationNetwork = MediationNetwork.ApplovinMax;
                    break;
                case AdsMediationType.ADMOB:
                    mediationNetwork = MediationNetwork.GoogleAdMob;
                    break;
            }

            Dictionary<string, string> additionalParams = new Dictionary<string, string>();
            additionalParams.Add(AdRevenueScheme.COUNTRY, "USA");
            additionalParams.Add(AdRevenueScheme.AD_UNIT, impressionData.ad_unit_name);
            additionalParams.Add(AdRevenueScheme.AD_TYPE, impressionData.ad_format);
            additionalParams.Add(AdRevenueScheme.PLACEMENT, "");
            var logRevenue = new AFAdRevenueData(impressionData.ad_source, mediationNetwork, "USD",
                impressionData.ad_revenue);
            AppsFlyerSdk.logAdRevenue(logRevenue, additionalParams);
        }

        public static void TrackCompleteLevel(int level)
        {
            if (level > 200) return;
            string eventName = af_completed_level + level;
            SendEvent(eventName);
        }
    }
#endif
}
