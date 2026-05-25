using System;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Notifications
{
    /// <summary>
    /// Central entry point for local notifications. Add once to your bootstrap scene
    /// or call <see cref="EnsureInstance"/> from game init.
    /// </summary>
    [ScriptOrder(-40)]
    public sealed class JisLocalNotificationManager : MonoBehaviour
    {
        public static JisLocalNotificationManager Instance { get; private set; }

        [SerializeField] private JisLocalNotificationSettings settings;

        public JisLocalNotificationSettings Settings => settings;
        public event Action<JisLocalNotificationPermissionStatus> PermissionChanged;

        private JisLocalNotificationPermissionStatus _lastPermission;

        public static JisLocalNotificationManager EnsureInstance(JisLocalNotificationSettings config = null)
        {
            if (Instance != null) return Instance;
            var existing = FindObjectOfType<JisLocalNotificationManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("JisLocalNotificationManager");
            var mgr = go.AddComponent<JisLocalNotificationManager>();
            if (config != null) mgr.settings = config;
            return mgr;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (settings == null)
                settings = Resources.Load<JisLocalNotificationSettings>("JisLocalNotificationSettings");

            JisLocalNotificationPlatform.InitializeChannel(settings);
        }

        private void Start()
        {
            RefreshPermissionStatus();

            if (settings == null) return;

            if (settings.requestPermissionOnStart)
                RequestPermission(_ => OnPermissionFlowFinished());

            if (settings.scheduleDailyRemindersOnStart)
                ScheduleDailyPlayReminders();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            // Refresh rolling window when app backgrounds.
            if (settings != null && settings.scheduleDailyRemindersOnStart)
                ScheduleDailyPlayReminders();
        }

        private void OnPermissionFlowFinished()
        {
            if (settings != null && settings.scheduleDailyRemindersOnStart)
                ScheduleDailyPlayReminders();
        }

        public JisLocalNotificationPermissionStatus GetPermissionStatus() =>
            JisLocalNotificationPlatform.GetPermissionStatus();

        public void RequestPermission(Action<bool> onFinished = null)
        {
            if (!JisLocalNotificationPlatform.IsSupported)
            {
                onFinished?.Invoke(false);
                return;
            }

            JisLocalNotificationPlatform.RequestPermission(granted =>
            {
                RefreshPermissionStatus();
                onFinished?.Invoke(granted);
            });
        }

        public void RefreshPermissionStatus()
        {
            var status = GetPermissionStatus();
            if (status == _lastPermission) return;
            _lastPermission = status;
            PermissionChanged?.Invoke(status);
        }

        /// <summary>Pre-schedules daily reminders for the next N days (default 10 from settings).</summary>
        public void ScheduleDailyPlayReminders(int? daysAhead = null, int? hour = null, int? minute = null)
        {
            if (settings == null) return;
            if (GetPermissionStatus() == JisLocalNotificationPermissionStatus.Denied) return;

            var count = daysAhead ?? settings.dailyReminderDaysAhead;
            var h = hour ?? settings.dailyReminderHour;
            var m = minute ?? settings.dailyReminderMinute;

            CancelDailyReminders();

            var first = GetNextDailySlot(h, m);
            for (var i = 0; i < count; i++)
            {
                var fire = first.AddDays(i);
                JisLocalNotificationPlatform.Schedule(
                    JisLocalNotificationIds.Daily(i),
                    settings.dailyTitle,
                    settings.dailyBody,
                    fire,
                    settings);
            }
        }

        public void CancelDailyReminders()
        {
            const int maxSlots = 31;
            for (var i = 0; i < maxSlots; i++)
                JisLocalNotificationPlatform.Cancel(JisLocalNotificationIds.Daily(i));
        }

        public void ScheduleAt(string identifier, string title, string body, DateTime fireLocalTime)
        {
            if (settings == null) return;
            if (GetPermissionStatus() == JisLocalNotificationPermissionStatus.Denied) return;
            JisLocalNotificationPlatform.Schedule(identifier, title, body, fireLocalTime, settings);
        }

        public void ScheduleAfter(string identifier, string title, string body, TimeSpan delay)
        {
            ScheduleAt(identifier, title, body, DateTime.Now.Add(delay));
        }

        public void Cancel(string identifier) => JisLocalNotificationPlatform.Cancel(identifier);

        public void CancelAll() => JisLocalNotificationPlatform.CancelAllScheduled();

        private static DateTime GetNextDailySlot(int hour, int minute)
        {
            var now = DateTime.Now;
            var target = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            if (target <= now)
                target = target.AddDays(1);
            return target;
        }
    }
}
