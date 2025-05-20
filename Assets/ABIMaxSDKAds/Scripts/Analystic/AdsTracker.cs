using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Firebase.Analytics;

namespace SDK {
    [ScriptOrder(-98)]
    public class AdsTracker : MonoBehaviour {
        private static AdsTracker instance;
        public static AdsTracker Instance => instance;

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

        private const string key_ad_rewarded_count = "ad_rewarded_count";
        private const string key_ad_inters_count = "ad_inters_count";
        private const string ad_inters_show_count = "ad_inters_show_count_";
        private const string ad_rewarded_show_count = "ad_rewarded_show_count_";

        public static void TrackAdImpression(ImpressionData impressionData, bool isTrackingAdImpression = true, bool isTrackingCustomAdImpressionEvent = false, string customAdImpressionEvent = "") {
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
            if (isTrackingAdImpression)
            {
                FirebaseManager.Instance.LogFirebaseEvent("ad_impression", impressionParameters);
            }
            if (isTrackingCustomAdImpressionEvent)
            {
                FirebaseManager.Instance.LogFirebaseEvent(customAdImpressionEvent, impressionParameters);
            }
            
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
                        FirebaseManager.Instance.LogFirebaseEvent(eventName);
                    }
                    break;
                case AdsType.INTERSTITIAL: {
                        int totalWatched = PlayerPrefs.GetInt(key_ad_inters_count, 0);
                        totalWatched++;
                        PlayerPrefs.SetInt(key_ad_inters_count, totalWatched);
                        bool isTracking = totalWatched <= 20;
                        if (!isTracking) return;

                        string eventName = ad_inters_show_count + totalWatched;
                        FirebaseManager.Instance.LogFirebaseEvent(eventName);
#if UNITY_APPSFLYER
                        ABIAppsflyerManager.TrackInterstitial_ShowCount(totalWatched); 
#endif
                    }
                    break;
            }
        }
        #endregion

        #region Rewarded Ads

        private const string ads_reward_complete = "ads_reward_complete";
        private const string ads_reward_click = "ads_reward_click";
        private const string ads_reward_show = "ads_reward_show";
        private const string ads_reward_fail = "ads_reward_fail";
        private const string ads_reward_loadsuccess = "ads_reward_loadsuccess";

        public void TrackAdsReward_ClickOnButton() {
            FirebaseManager.Instance.LogFirebaseEvent(ads_reward_click);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_ClickShowButton();
#endif
        }
        public void TrackAdsReward_StartShow() {
            FirebaseManager.Instance.LogFirebaseEvent(ads_reward_show);

        }
        public void TrackAdsReward_ShowFail() {
            FirebaseManager.Instance.LogFirebaseEvent(ads_reward_fail);
        }
        public void TrackAdsReward_ShowCompleted(string placement) {
            Parameter[] parameters = new Parameter[] {
                new Parameter("placement", placement)
            };
            FirebaseManager.Instance.LogFirebaseEvent(ads_reward_complete, parameters);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_Displayed();
            EventManager.AddEventNextFrame(() =>
            {
                TrackLocalAdImpression(AdsType.REWARDED);
            });
#endif
        }
        public void TrackAdsReward_LoadSuccess() {
            FirebaseManager.Instance.LogFirebaseEvent(ads_reward_loadsuccess);
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
            FirebaseManager.Instance.LogFirebaseEvent(ad_inter_load, parameters);
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
            FirebaseManager.Instance.LogFirebaseEvent(ad_inter_load_fail, parameters);
        }
        public void TrackAdsInterstitial_ShowSuccess() {
            FirebaseManager.Instance.LogFirebaseEvent(ad_inter_show);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_Displayed();
            EventManager.AddEventNextFrame(() =>
            {
                TrackLocalAdImpression(AdsType.INTERSTITIAL);
            });
#endif
        }
        public void TrackAdsInterstitial_ShowFail() {
            FirebaseManager.Instance.LogFirebaseEvent(ad_inter_fail);
        }
        public void TrackAdsInterstitial_ShowFailByLoad()
        {
            FirebaseManager.Instance.LogFirebaseEvent(ad_inter_show_fail_by_load);
        }
        public void TrackAdsInterstitial_ClickOnButton() {
            FirebaseManager.Instance.LogFirebaseEvent(ad_inter_click);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_ClickShowButton();
#endif
        }
        #endregion

        #endregion
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
