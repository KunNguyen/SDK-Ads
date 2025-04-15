using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Firebase.Analytics;

namespace SDK {
    [ScriptOrder(-98)]
    public class ABIAnalyticsManager : MonoBehaviour {
        private static ABIAnalyticsManager instance;
        public static ABIAnalyticsManager Instance => instance;

        void Awake() {
            if (instance) {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            instance = this;
        }

        #region Ads Tracking

        #region Revenue
        public const string key_ad_rewarded_revenue = "ad_rewarded_revenue";
        public const string key_ad_rewarded_count = "ad_rewarded_count";
        public const string key_ad_inters_revenue = "ad_inters_revenue";
        public const string key_ad_inters_count = "ad_inters_count";

        public const string ad_inters_show_count = "ad_inters_show_count_";
        public const string ad_rewarded_show_count = "ad_rewarded_show_count_";

        public static void TrackAdImpression(ImpressionData impressionData) {
            double revenue = impressionData.ad_revenue;
            string ad_platform = impressionData.ad_mediation.ToString();
            switch (impressionData.ad_mediation)
            {
                case AdsMediationType.MAX:
                    ad_platform = "Applovin";
                    break;
                case AdsMediationType.ADMOB:
                    ad_platform = "Admob";
                    break;
                case AdsMediationType.IRONSOURCE:
                    ad_platform = "Ironsource";
                    break;
            }
            Parameter[] impressionParameters = new[] {
                new Parameter("ad_platform", ad_platform),
                new Parameter("ad_source", impressionData.ad_source),
                new Parameter("ad_unit_name", impressionData.ad_unit_name),
                new Parameter("ad_format", impressionData.ad_format),
                new Parameter("value", revenue),
                new Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            ABIFirebaseManager.Instance.LogFirebaseEvent("ad_impression", impressionParameters);
        }
        public static void TrackLocalAdImpression(AdsType adsType) {
            Debug.Log("TrackLocalAdImpression " + adsType);
            switch (adsType) {
                case AdsType.REWARDED: {
                        int totalWatched = PlayerPrefs.GetInt(key_ad_rewarded_count, 0);
                        totalWatched++;
                        PlayerPrefs.SetInt(key_ad_rewarded_count, totalWatched);
                        bool isTracking = totalWatched<=20;
                        if (!isTracking) return;

                        string eventName = ad_rewarded_show_count + totalWatched;
                        ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
                    }
                    break;
                case AdsType.INTERSTITIAL: {
                        int totalWatched = PlayerPrefs.GetInt(key_ad_inters_count, 0);
                        totalWatched++;
                        PlayerPrefs.SetInt(key_ad_inters_count, totalWatched);
                        bool isTracking = totalWatched <= 20;
                        if (!isTracking) return;

                        string eventName = ad_inters_show_count + totalWatched;
                        ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
#if UNITY_APPSFLYER
                        ABIAppsflyerManager.TrackInterstitial_ShowCount(totalWatched); 
#endif
                    }
                    break;
            }
        }
        #endregion

        #region Rewarded Ads
        public const string ads_reward_complete = "ads_reward_complete";
        public const string ads_reward_click = "ads_reward_click";
        public const string ads_reward_show = "ads_reward_show";
        public const string ads_reward_fail = "ads_reward_fail";
        public const string ads_reward_loadsuccess = "ads_reward_loadsuccess";

        public void TrackAdsReward_ClickOnButton() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_click);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_ClickShowButton();
#endif
        }
        public void TrackAdsReward_StartShow() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_show);

        }
        public void TrackAdsReward_ShowFail() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_fail);
        }
        public void TrackAdsReward_ShowCompleted(string placement) {
            Parameter[] parameters = new Parameter[] {
                new Parameter("placement", placement)
            };
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_complete, parameters);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_Displayed();
            EventManager.AddEventNextFrame(() =>
            {
                TrackLocalAdImpression(AdsType.REWARDED);
            });
#endif
        }
        public void TrackAdsReward_LoadSuccess() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_loadsuccess);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_LoadedSuccess();
#endif
        }
        #endregion

        #region Interstitial Ads

        private const string ad_inter_fail = "ad_inter_fail";
        private const string ad_inter_load = "ad_inter_load";
        private const string ad_inter_load_fail = "ad_inter_load_fail";
        private const string ad_inter_show = "ad_inter_show";
        private const string ad_inter_click = "ad_inter_click";
        private const string ad_inter_show_fail_by_load = "ad_inter_show_fail_by_load";
        private int interstitialAdsLoadSuccess = 0;
        private int interstitialAdsShowSuccess = 0;

        public void TrackAdsInterstitial_LoadedSuccess(float timeFromStartRequest) {
            interstitialAdsLoadSuccess++;
            //round time to 0.5 
            timeFromStartRequest = Mathf.Round(timeFromStartRequest * 2) / 2;
            Parameter[] parameters = new[] {
                new Parameter("count", interstitialAdsLoadSuccess.ToString()),
                new Parameter("request_time", timeFromStartRequest.ToString("F1")),
            };
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_load, parameters);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_LoadedSuccess();
#endif
        }
        public void TrackAdsInterstitial_LoadedFail(string error, float timeFromStartRequest)
        {
            //round time to 0.5 
            timeFromStartRequest = Mathf.Round(timeFromStartRequest * 2) / 2;
            Parameter[] parameters = new[] {
                new Parameter("error", error),
                new Parameter("request_time", timeFromStartRequest.ToString("F1")),
            };
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_load_fail, parameters);
        }
        public void TrackAdsInterstitial_ShowSuccess() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_show);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_Displayed();
            EventManager.AddEventNextFrame(() =>
            {
                TrackLocalAdImpression(AdsType.INTERSTITIAL);
            });
#endif
        }
        public void TrackAdsInterstitial_ShowFail() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_fail);
        }
        public void TrackAdsInterstitial_ShowFailByLoad()
        {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_show_fail_by_load);
        }
        public void TrackAdsInterstitial_ClickOnButton() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_click);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_ClickShowButton();
#endif
        }
        #endregion

        #endregion

        public void TrackSessionStart(int id) {
            string eventName = "session_start_" + id;
            ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
//#if UNITY_APPSFLYER
//            ABIAppsflyerManager.SendEvent(eventName, null);
//#endif
        }
        public void TrackEventMapComplete(int map) {

            string eventName = "map" + map + "_completed";
            ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.SendEvent(eventName, null);
#endif
        }
        
        public void TrackEventStartLevel(int lv)
        {
            string level = lv.ToString("000");
            string eventName = $"start_level_{level}";
            ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
        }
        public void TrackEventWinLevel(int lv)
        {
            string level = lv.ToString("000");
            string eventName = $"win_level_{level}";
            ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
        }
        
    }
    public class ImpressionData {
        public AdsMediationType ad_mediation;
        public string ad_type;
        public string ad_source;
        public string ad_unit_name;
        public string ad_format;
        public double ad_revenue;
        public string ad_currency;
        public string placement = "";
    }
}
