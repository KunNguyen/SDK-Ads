using System;
using System.Collections.Generic;
using SDK;
#if UNITY_SOLAR_ENGINE
using SolarEngine;
#endif
using UnityEngine;

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
        Banner = 5,
        MRec = 10
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
        
        SolarEngine.Analytics.preInitSeSdk(appKey);
    }

    void Start()
    {
        SEConfig seConfig = new SEConfig
        {
            logEnabled = true,
            initCompletedCallback = OnInitCallback
        };
        SolarEngine.Analytics.initSeSdk(appKey, seConfig);
    }
    private void OnInitCallback(int code) {
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

        SolarEngine.Analytics.track(eventName, parameters);
    }

    public void TrackAdImpression(ImpressionData impressionData)
    {
        ImpressionAttributes attributes = new ImpressionAttributes
        {
            ad_platform = impressionData.ad_source,
            ad_id = impressionData.ad_unit_name,
            ad_type = (int)ConvertAdType(impressionData.ad_type),
            ad_ecpm = impressionData.ad_revenue*1000,
            currency_type = "USD",
            mediation_platform = ConvertMediationPlatform(impressionData.ad_mediation),
            is_rendered = true
        };
        SolarEngine.Analytics.trackAdImpression(attributes);
    }

    public void TrackPurchase(string productId, double payAmount, string currency, string status)
    {
        if (!IsReady)
        {
            Debug.LogWarning("SolarEngine SDK is not ready. Cannot track purchase.");
            return;
        }
        PayStatus payStatus = status switch
        {
            "success" => PayStatus.Success,
            "failed" => PayStatus.Fail,
            "restored" => PayStatus.Restored,
            _ => PayStatus.Success
        };

        ProductsAttributes attributes = new ProductsAttributes
        {
            product_id = productId,
            pay_amount = payAmount,
            currency_type = currency,
            paystatus = payStatus
        };

        SolarEngine.Analytics.trackPurchase(attributes);
    }
    
    private string ConvertMediationPlatform(AdsMediationType adsMediationType)
    {
        switch (adsMediationType)
        {
            case AdsMediationType.ADMOB:
                return "AdMob";
            case AdsMediationType.MAX:
                return "Max";
            case AdsMediationType.IRONSOURCE:
                return "IronSource";
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
                return AdType.MRec;
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
            case AdsType.MREC:
                return AdType.MRec;
            case AdsType.APP_OPEN:
            case AdsType.COLLAPSIBLE_BANNER:
                return AdType.Other;
            default:
                return AdType.Other;
        }
    }
#endif
}
