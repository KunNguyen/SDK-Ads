using System;
using Firebase.Analytics;
using JisSDKAds.Ads.Tracking;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using Parameter = Firebase.Analytics.Parameter;
using UnityEngine;

namespace JisSDKAds.Ads
{
    [ScriptOrder(-98)]
    public class AdsTracker : MonoBehaviour
    {
        private static AdsTracker instance;
        public static AdsTracker Instance => instance;

        void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }

            JisSDKPersistence.DontDestroyUnlessUnderPersistentRoot(gameObject);
            instance = this;
        }

        #region Ads Tracking

        #region Revenue

        private const string key_ad_rewarded_count = "ad_rewarded_count";
        private const string key_ad_inters_count = "ad_inters_count";
        private const string ad_inters_show_count = "ad_inters_show_count_";
        private const string ad_rewarded_show_count = "ad_rewarded_show_count_";
        private const string session_ad_index_parameter = "session_ad_index";

        public static void TrackAdImpression(ImpressionData impressionData, bool isTrackingAdImpression = true,
            bool isTrackingCustomAdImpressionEvent = false, string customAdImpressionEvent = "")
        {
            // Requirement: measure ad revenue to Firebase only for MAX (AppLovin).
            // AdMob paid events should not be sent to Firebase.
            if (impressionData.ad_mediation == AdsMediationType.ADMOB)
                return;

            var revenue = impressionData.ad_revenue;
            var adPlatform = impressionData.ad_mediation.ToString();
            switch (impressionData.ad_mediation)
            {
                case AdsMediationType.MAX:
                    adPlatform = "Applovin";
                    break;
                case AdsMediationType.ADMOB:
                    adPlatform = "Admob";
                    break;
            }

            var impressionParameters = new[]
            {
                new Parameter("ad_platform", adPlatform),
                new Parameter("ad_source", impressionData.ad_source),
                new Parameter("ad_unit_name", impressionData.ad_unit_name),
                new Parameter("ad_format", impressionData.ad_format),
                new Parameter("value", revenue),
                new Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            if (isTrackingAdImpression)
            {
                FirebaseManager.Instance.LogEvent("ad_impression", impressionParameters);
            }

            if (isTrackingCustomAdImpressionEvent)
            {
                FirebaseManager.Instance.LogEvent(customAdImpressionEvent, impressionParameters);
            }
        }

        public static void TrackLocalAdImpression(AdsType adsType)
        {
            DebugAds.Log("TrackLocalAdImpression " + adsType);
            switch (adsType)
            {
                case AdsType.REWARDED:
                {
                    lock (key_ad_rewarded_count)
                    {
                        var totalWatched = PlayerPrefs.GetInt(key_ad_rewarded_count, 0);
                        if (totalWatched == 0) FirebaseManager.Instance.LogEvent(ads_reward_first_show);
                        totalWatched++;
                        PlayerPrefs.SetInt(key_ad_rewarded_count, totalWatched);
                        
                        if (totalWatched == 5 || totalWatched == 10 || totalWatched == 20 || totalWatched == 50 || totalWatched == 100)
                        {
                            var eventName = ad_rewarded_show_count + totalWatched;
                            FirebaseManager.Instance.LogEvent(eventName);
                        }
                    }
                }
                    break;
                case AdsType.INTERSTITIAL:
                {
                    lock (key_ad_inters_count)
                    {
                        var totalWatched = PlayerPrefs.GetInt(key_ad_inters_count, 0);
                        if( totalWatched == 0) FirebaseManager.Instance.LogEvent(ad_inter_first_show);
                        totalWatched++;
                        PlayerPrefs.SetInt(key_ad_inters_count, totalWatched);
                        
                        if (totalWatched == 5 || totalWatched == 10 || totalWatched == 20 || totalWatched == 50 || totalWatched == 100)
                        {
                            var eventName = ad_inters_show_count + totalWatched;
                            FirebaseManager.Instance.LogEvent(eventName);
#if UNITY_APPSFLYER
                            AppsflyerManager.TrackInterstitial_ShowCount(totalWatched);
#endif
                        }
                    }
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
        private const string ads_reward_show_fail_by_load = "ads_reward_show_fail_by_load";
        private const string ads_reward_load_success = "ads_reward_load_success";
        private const string ads_reward_first_show = "ads_reward_first_show";
        private int rewardedAdsLoadSuccess = 0;
        private int rewardedAdsShowAttempt = 0;
        private int currentRewardedAdsShowAttempt = 0;

        public void TrackAdsReward_ClickOnButton()
        {
            FirebaseManager.Instance.LogEvent(ads_reward_click, CreateSessionAdIndexParameters(BeginRewardedShowAttempt()));
#if UNITY_APPSFLYER
            AppsflyerManager.TrackRewarded_ClickShowButton();
#endif
        }

        public void TrackAdsReward_StartShow()
        {
            FirebaseManager.Instance.LogEvent(ads_reward_show, CreateSessionAdIndexParameters(EnsureRewardedShowAttempt()));
        }

        public void TrackAdsReward_ShowFail()
        {
            FirebaseManager.Instance.LogEvent(ads_reward_fail, CreateSessionAdIndexParameters(EnsureRewardedShowAttempt()));
            ClearRewardedShowAttempt();
        }

        public void TrackAdsReward_ShowFailByLoad()
        {
            FirebaseManager.Instance.LogEvent(ads_reward_show_fail_by_load, CreateSessionAdIndexParameters(EnsureRewardedShowAttempt()));
            ClearRewardedShowAttempt();
        }

        public void TrackAdsReward_ShowCompleted(string placement)
        {
            var parameters = CreatePlacementSessionAdIndexParameters(placement, EnsureRewardedShowAttempt());
            FirebaseManager.Instance.LogEvent(ads_reward_complete, parameters);
            ClearRewardedShowAttempt();
            EventManager.InvokeNextFrame(() => { TrackLocalAdImpression(AdsType.REWARDED); });
#if UNITY_APPSFLYER
            AppsflyerManager.TrackRewarded_Displayed();
#endif
        }

        public void TrackAdsReward_LoadSuccess()
        {
            rewardedAdsLoadSuccess++;
            FirebaseManager.Instance.LogEvent(ads_reward_load_success, CreateSessionAdIndexParameters(rewardedAdsLoadSuccess));
#if UNITY_APPSFLYER
            AppsflyerManager.TrackRewarded_LoadedSuccess();
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
        private const string ad_inter_first_show = "ad_inter_first_show";
        private int interstitialAdsLoadSuccess = 0;
        private int interstitialAdsLoadFail = 0;
        private int interstitialAdsShowSuccess = 0;
        private int interstitialAdsShowAttempt = 0;
        private int currentInterstitialAdsShowAttempt = 0;

        public void TrackAdsInterstitial_LoadedSuccess(float timeFromStartRequest)
        {
            interstitialAdsLoadSuccess++;
            timeFromStartRequest = Mathf.Round(timeFromStartRequest * 2) / 2;
            var parameters = new[]
            {
                new Parameter("count", interstitialAdsLoadSuccess.ToString()),
                new Parameter("request_time", timeFromStartRequest.ToString("F1")),
                new Parameter(session_ad_index_parameter, interstitialAdsLoadSuccess),
            };
            FirebaseManager.Instance.LogEvent(ad_inter_load, parameters);
#if UNITY_APPSFLYER
            AppsflyerManager.TrackInterstitial_LoadedSuccess();
#endif
        }

        public void TrackAdsInterstitial_LoadedSuccess(float timeFromStartRequest, string placement = "")
        {
            interstitialAdsLoadSuccess++;
            timeFromStartRequest = Mathf.Round(timeFromStartRequest * 2) / 2;
            if (string.IsNullOrEmpty(placement))
            {
                var parameters = new[]
                {
                    new Parameter("count", interstitialAdsLoadSuccess.ToString()),
                    new Parameter("request_time", timeFromStartRequest.ToString("F1")),
                    new Parameter(session_ad_index_parameter, interstitialAdsLoadSuccess),
                };
                FirebaseManager.Instance.LogEvent(ad_inter_load, parameters);
            }
            else
            {
                var parameters = new[]
                {
                    new Parameter("count", interstitialAdsLoadSuccess.ToString()),
                    new Parameter("request_time", timeFromStartRequest.ToString("F1")),
                    new Parameter("placement", placement),
                    new Parameter(session_ad_index_parameter, interstitialAdsLoadSuccess),
                };
                FirebaseManager.Instance.LogEvent(ad_inter_load, parameters);
            }

#if UNITY_APPSFLYER
            AppsflyerManager.TrackInterstitial_LoadedSuccess();
#endif
        }

        public void TrackAdsInterstitial_LoadedFail(string error, float timeFromStartRequest, string placement = "")
        {
            //round time to 0.5 
            interstitialAdsLoadFail++;
            timeFromStartRequest = Mathf.Round(timeFromStartRequest * 2) / 2;
            if (string.IsNullOrEmpty(placement))
            {
                var parameters = new[]
                {
                    new Parameter("error", error),
                    new Parameter("request_time", timeFromStartRequest.ToString("F1")),
                    new Parameter(session_ad_index_parameter, interstitialAdsLoadFail),
                };
                FirebaseManager.Instance.LogEvent(ad_inter_load_fail, parameters);
            }
            else
            {
                var parameters = new[]
                {
                    new Parameter("error", error),
                    new Parameter("request_time", timeFromStartRequest.ToString("F1")),
                    new Parameter("placement", placement),
                    new Parameter(session_ad_index_parameter, interstitialAdsLoadFail),
                };
                FirebaseManager.Instance.LogEvent(ad_inter_load_fail, parameters);
            }
        }

        public void TrackAdsInterstitial_ShowSuccess(string placement = "")
        {
            interstitialAdsShowSuccess++;
            LogInterstitialEvent(ad_inter_show, placement, EnsureInterstitialShowAttempt());
            ClearInterstitialShowAttempt();
            EventManager.InvokeNextFrame(() => { TrackLocalAdImpression(AdsType.INTERSTITIAL); });
#if UNITY_APPSFLYER
            AppsflyerManager.TrackInterstitial_Displayed();
#endif
        }

        public void TrackAdsInterstitial_ShowFail(string placement = "")
        {
            LogInterstitialEvent(ad_inter_fail, placement, EnsureInterstitialShowAttempt());
            ClearInterstitialShowAttempt();
        }

        public void TrackAdsInterstitial_ShowFailByLoad(string placement = "")
        {
            LogInterstitialEvent(ad_inter_show_fail_by_load, placement, EnsureInterstitialShowAttempt());
            ClearInterstitialShowAttempt();
        }

        public void TrackAdsInterstitial_ClickOnButton(string placement = "")
        {
            LogInterstitialEvent(ad_inter_click, placement, BeginInterstitialShowAttempt());
#if UNITY_APPSFLYER
            AppsflyerManager.TrackInterstitial_ClickShowButton();
#endif
        }

        public void TrackAdsInterstitial_FirstShow()
        {
            FirebaseManager.Instance.LogEvent(ad_inter_first_show);
        }

        static void LogInterstitialEvent(string eventName, string placement, int sessionAdIndex)
        {
            FirebaseManager.Instance.LogEvent(eventName, CreatePlacementSessionAdIndexParameters(placement, sessionAdIndex));
        }

        int BeginRewardedShowAttempt()
        {
            rewardedAdsShowAttempt++;
            currentRewardedAdsShowAttempt = rewardedAdsShowAttempt;
            return currentRewardedAdsShowAttempt;
        }

        int EnsureRewardedShowAttempt()
        {
            return currentRewardedAdsShowAttempt > 0
                ? currentRewardedAdsShowAttempt
                : BeginRewardedShowAttempt();
        }

        void ClearRewardedShowAttempt()
        {
            currentRewardedAdsShowAttempt = 0;
        }

        int BeginInterstitialShowAttempt()
        {
            interstitialAdsShowAttempt++;
            currentInterstitialAdsShowAttempt = interstitialAdsShowAttempt;
            return currentInterstitialAdsShowAttempt;
        }

        int EnsureInterstitialShowAttempt()
        {
            return currentInterstitialAdsShowAttempt > 0
                ? currentInterstitialAdsShowAttempt
                : BeginInterstitialShowAttempt();
        }

        void ClearInterstitialShowAttempt()
        {
            currentInterstitialAdsShowAttempt = 0;
        }

        static Parameter[] CreateSessionAdIndexParameters(int sessionAdIndex)
        {
            return new[] { new Parameter(session_ad_index_parameter, sessionAdIndex) };
        }

        static Parameter[] CreatePlacementSessionAdIndexParameters(string placement, int sessionAdIndex)
        {
            if (string.IsNullOrEmpty(placement))
                return CreateSessionAdIndexParameters(sessionAdIndex);

            return new[]
            {
                new Parameter("placement", placement),
                new Parameter(session_ad_index_parameter, sessionAdIndex),
            };
        }

        #endregion

        #endregion
    }

    public class ImpressionData
    {
        public AdsMediationType ad_mediation;
        public string ad_type;
        public string ad_sourceID;
        public string ad_source;
        public string ad_unit_name;
        public string ad_format;
        public double ad_revenue;
        public string ad_currency;
        public string placement = "";
    }
}
