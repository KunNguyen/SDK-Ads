using System;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace JisSDKAds.Notifications
{
    internal static class JisLocalNotificationPlatform
    {
        public static bool IsSupported =>
#if UNITY_ANDROID || UNITY_IOS
            !Application.isEditor;
#else
            false;
#endif

        public static void InitializeChannel(JisLocalNotificationSettings settings)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var channel = new AndroidNotificationChannel
            {
                Id = settings.androidChannelId,
                Name = settings.androidChannelName,
                Description = settings.androidChannelDescription,
                Importance = Importance.Default,
                CanShowBadge = true,
                EnableLights = true,
                EnableVibration = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
        }

        public static JisLocalNotificationPermissionStatus GetPermissionStatus()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidNotificationCenter.UserPermissionToPost switch
            {
                PermissionStatus.Allowed => JisLocalNotificationPermissionStatus.Authorized,
                PermissionStatus.Denied => JisLocalNotificationPermissionStatus.Denied,
                _ => JisLocalNotificationPermissionStatus.NotRequested
            };
#elif UNITY_IOS && !UNITY_EDITOR
            var status = iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus;
            return status switch
            {
                AuthorizationStatus.Authorized => JisLocalNotificationPermissionStatus.Authorized,
                AuthorizationStatus.Denied => JisLocalNotificationPermissionStatus.Denied,
                AuthorizationStatus.Provisional => JisLocalNotificationPermissionStatus.Provisional,
                AuthorizationStatus.NotDetermined => JisLocalNotificationPermissionStatus.NotRequested,
                _ => JisLocalNotificationPermissionStatus.NotRequested
            };
#else
            return JisLocalNotificationPermissionStatus.NotSupported;
#endif
        }

        public static void RequestPermission(Action<bool> onFinished)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var request = new PermissionRequest();
            AndroidNotificationCenter.RequestPermission(request);
            JisLocalNotificationPermissionPoller.Run(() =>
            {
                if (request.Status == PermissionStatus.RequestPending) return;
                JisLocalNotificationPermissionPoller.Stop();
                onFinished?.Invoke(request.Status == PermissionStatus.Allowed);
            });
#elif UNITY_IOS && !UNITY_EDITOR
            var options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
            iOSNotificationCenter.RequestAuthorization(options, granted =>
            {
                onFinished?.Invoke(granted);
            });
#else
            onFinished?.Invoke(false);
#endif
        }

        public static void Schedule(string identifier, string title, string body, DateTime fireLocalTime,
            JisLocalNotificationSettings settings)
        {
            if (!IsSupported) return;
            if (fireLocalTime <= DateTime.Now) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            var notification = new AndroidNotification
            {
                Title = title,
                Text = body,
                FireTime = fireLocalTime
            };
            AndroidNotificationCenter.SendNotificationWithExplicitID(
                notification,
                settings.androidChannelId,
                ToAndroidId(identifier));
#elif UNITY_IOS && !UNITY_EDITOR
            var trigger = new iOSNotificationCalendarTrigger
            {
                Year = fireLocalTime.Year,
                Month = fireLocalTime.Month,
                Day = fireLocalTime.Day,
                Hour = fireLocalTime.Hour,
                Minute = fireLocalTime.Minute,
                Second = fireLocalTime.Second,
                Repeats = false
            };
            var notification = new iOSNotification
            {
                Identifier = identifier,
                Title = title,
                Body = body,
                ShowInForeground = false,
                ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
                Trigger = trigger
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#endif
        }

        public static void Cancel(string identifier)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelNotification(ToAndroidId(identifier));
#elif UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveScheduledNotification(identifier);
            iOSNotificationCenter.RemoveDeliveredNotification(identifier);
#endif
        }

        public static void CancelAllScheduled()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#elif UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }

        public static int ToAndroidId(string identifier) =>
            Mathf.Abs(Animator.StringToHash(identifier));
    }

    internal static class JisLocalNotificationPermissionPoller
    {
        private static Action _poll;

        public static void Run(Action poll)
        {
            _poll = poll;
            if (JisLocalNotificationPermissionPollerBehaviour.Instance == null)
            {
                var go = new GameObject("[JIS] NotificationPermissionPoller");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<JisLocalNotificationPermissionPollerBehaviour>();
            }
        }

        public static void Stop() => _poll = null;

        internal static void Tick() => _poll?.Invoke();
    }

    internal sealed class JisLocalNotificationPermissionPollerBehaviour : MonoBehaviour
    {
        internal static JisLocalNotificationPermissionPollerBehaviour Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update() => JisLocalNotificationPermissionPoller.Tick();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
