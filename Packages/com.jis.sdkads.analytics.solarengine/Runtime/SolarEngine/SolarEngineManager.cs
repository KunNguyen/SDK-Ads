using System;
using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Common;
#if UNITY_SOLAR_ENGINE
using SE = global::SolarEngine;
#endif
using UnityEngine;

namespace JisSDKAds.Analytics.SolarEngineIntegration
{
    [ScriptOrder(-1000)]
    public class SolarEngineManager : MonoBehaviour
    {
#if UNITY_SOLAR_ENGINE
        public static SolarEngineManager Instance { get; private set; }
        [field: SerializeField] private string appKey = "";
        [field: SerializeField] private bool IsReady = false;

        public enum SolarInitCode
        {
            Success = 0,
            FailedByPreIntBeNotCall = 101,
            FailedByIllegalAppKey = 102,
            FailedByNullContextAndroid = 103,
            FailedByMissingDistinctID = 104
        }

        private enum AdType
        {
            Other = 0,
            RewardedVideo = 1,
            Interstitial = 3,
            Banner = 5
        }

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SE.Analytics.preInitSeSdk(appKey);
        }

        void Start()
        {
            var seConfig = new SE.SEConfig
            {
                logEnabled = true,
                initCompletedCallback = OnInitCallback
            };
            SE.Analytics.initSeSdk(appKey, seConfig);
        }

        private void OnInitCallback(int code)
        {
            SolarInitCode initCode = (SolarInitCode)code;
            if (initCode == SolarInitCode.Success)
            {
                IsReady = true;
                Debug.Log("SolarEngine SDK initialized successfully.");
            }
            else
            {
                IsReady = false;
                Debug.LogError($"SolarEngine SDK initialization failed with code: {code}");
            }
        }

        public void TrackEvent(string eventName, string parameterName, object parameterValue)
        {
            if (!IsReady)
            {
                Debug.LogWarning("SolarEngine SDK is not ready. Cannot track event.");
                return;
            }

            var parameters = new Dictionary<string, object> { { parameterName, parameterValue } };
            SE.Analytics.track(eventName, parameters);
        }

        public void TrackAdImpression(ImpressionData impressionData)
        {
            var attributes = new SE.ImpressionAttributes
            {
                ad_platform = impressionData.ad_source,
                ad_id = impressionData.ad_unit_name,
                ad_type = (int)ConvertAdType(impressionData.ad_type),
                ad_ecpm = impressionData.ad_revenue * 1000,
                currency_type = "USD",
                mediation_platform = ConvertMediationPlatform(impressionData.ad_mediation),
                is_rendered = true
            };
            SE.Analytics.trackAdImpression(attributes);
        }

        public void TrackPurchase(string productId, double payAmount, string currency, string status)
        {
            if (!IsReady)
            {
                Debug.LogWarning("SolarEngine SDK is not ready. Cannot track purchase.");
                return;
            }

            var payStatus = status switch
            {
                "success" => SE.PayStatus.Success,
                "failed" => SE.PayStatus.Fail,
                "restored" => SE.PayStatus.Restored,
                _ => SE.PayStatus.Success
            };

            var attributes = new SE.ProductsAttributes
            {
                product_id = productId,
                pay_amount = payAmount,
                currency_type = currency,
                paystatus = payStatus
            };

            SE.Analytics.trackPurchase(attributes);
        }

        private string ConvertMediationPlatform(AdsMediationType adsMediationType)
        {
            switch (adsMediationType)
            {
                case AdsMediationType.ADMOB:
                    return "AdMob";
                case AdsMediationType.MAX:
                    return "Max";
                default:
                    return "Unknown";
            }
        }

        private AdType ConvertAdType(string adTypeString)
        {
            switch (adTypeString)
            {
                case "banner":
                    return AdType.Banner;
                case "interstitial":
                    return AdType.Interstitial;
                case "rewarded":
                    return AdType.RewardedVideo;
                case "mrec":
                case "collapsible":
                case "app_open_ad":
                default:
                    return AdType.Other;
            }
        }

        private AdType ConvertAdType(AdsType adsType)
        {
            switch (adsType)
            {
                case AdsType.BANNER:
                    return AdType.Banner;
                case AdsType.INTERSTITIAL:
                    return AdType.Interstitial;
                case AdsType.REWARDED:
                    return AdType.RewardedVideo;
                case AdsType.APP_OPEN:
                default:
                    return AdType.Other;
            }
        }
#endif
    }
}
