using System.Collections;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads.UnitAdManagers
{
    public class AppOpenAdManager : UnitAdManager
    {
        [field: SerializeField] public bool IsActiveByRemoteConfig { get; set; } = true;
        [field: SerializeField] public bool IsFirstOpen { get; set; } = true;
        [field: SerializeField] public bool IsActiveShowAdsFirstTime { get; set; } = true;

        [Header("AppOpen Safety Settings")]
        [SerializeField] private bool showOnColdStart = false;               
        [SerializeField] private float firstShowDelayMs = 600f;              
        [SerializeField] private float firstShowWaitLoadTimeoutSec = 2.5f;   
        [SerializeField] private float minIntervalBetweenShowsSec = 20f;     
        private float lastShowRealtime = -9999f;

        public override void Init()
        {
            if (IsRemoveAds() || IsCheatAds()) return;

            MediationController.InitAppOpenAds(
                OnAdLoadSuccess,
                OnAdLoadFail,
                OnAdClose,
                OnAdShowSuccess,
                OnAdShowFailed);

            Status = AdStatus.Inited;
        }

        private IEnumerator CoTryShowFirstTimeSafely()
        {
            // Qua frame đầu để tránh xung đột add window
            yield return null;

            // Chờ realtime để hệ thống ổn định
            if (firstShowDelayMs > 0) yield return new WaitForSecondsRealtime(firstShowDelayMs / 1000f);

            // Remote config và điều kiện chung
            if (!IsActiveByRemoteConfig || IsCheatAds() || IsRemoveAds()) yield break;

            // Không show nếu app chưa focus hoặc đang có ad khác
            if (!Application.isFocused) yield break;
            if (IsShowingAdChecking != null && IsShowingAdChecking()) yield break;

            // Đợi load tối đa N giây (không block)
            float waited = 0f;
            while (waited < firstShowWaitLoadTimeoutSec && !IsLoaded())
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            // Chỉ show khi đã sẵn sàng
            if (IsLoaded())
            {
                Show();
            }
        }

        protected override void UpdateRemoteConfigValue()
        {
            {
                IsActiveByRemoteConfig = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_aoa_active);
                DebugAds.Log("App Open Ads Active = " + IsActiveByRemoteConfig);
            }

            {
                IsActiveShowAdsFirstTime = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_aoa_show_first_time_active);
                DebugAds.Log("AOA active show first time = " + IsActiveShowAdsFirstTime);
            }

            IsReady = true;
            BeginLoadingAfterRemoteConfig();
        }

        void BeginLoadingAfterRemoteConfig()
        {
            if (IsRemoveAds() || IsCheatAds()) return;

            RequestAd();

            if (showOnColdStart && IsFirstOpen && IsActiveShowAdsFirstTime)
                StartCoroutine(CoTryShowFirstTimeSafely());
        }

        // Giữ API cũ nhưng hợp nhất flow (đảm bảo MarkShowingAds được gọi qua Show())
        public void ShowAdsFirstTime()
        {
            if (IsCheatAds() || IsRemoveAds()) return;
            Show();
        }

        public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
        {
            base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
            if (IsCheatAds() || IsRemoveAds()) return;
            Show();
        }

        public override void RequestAd()
        {
            if (IsRemoveAds() || IsCheatAds()) return;
            MediationController.RequestAppOpenAds();
        }

        public override void Show()
        {
            // Debounce khoảng cách giữa 2 lần show
            if (Time.realtimeSinceStartup - lastShowRealtime < minIntervalBetweenShowsSec) return;
            if (!IsAdReady()) return;

            IsShowingAd = true;
            Debug.Log("[AOA] Start Show App Open Ads");
            MediationController.ShowAppOpenAds();
            lastShowRealtime = Time.realtimeSinceStartup;
        }

        public override void OnAdClose()
        {
            base.OnAdClose();
            RequestAd();
        }

        public override void OnAdShowSuccess()
        {
            base.OnAdShowSuccess();
        }

        public override bool IsLoaded()
        {
            return MediationController != null && MediationController.IsAppOpenAdsLoaded();
        }

        public override bool IsAdReady()
        {
            #if UNITY_EDITOR
            return false;
            #endif
            return IsActive && IsActiveByRemoteConfig && !IsRemoveAds() && !IsCheatAds() && IsLoaded() && Application.isFocused
                   && (IsShowingAdChecking == null || !IsShowingAdChecking());
        }
    }
}