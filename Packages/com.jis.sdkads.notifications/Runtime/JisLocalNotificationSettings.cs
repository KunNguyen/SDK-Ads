using UnityEngine;

namespace JisSDKAds.Notifications
{
    [CreateAssetMenu(fileName = "JisLocalNotificationSettings", menuName = "JIS SDK/Local Notification Settings")]
    public class JisLocalNotificationSettings : ScriptableObject
    {
        [Header("Startup")]
        public bool requestPermissionOnStart = true;
        public bool scheduleDailyRemindersOnStart = true;

        [Header("Daily play reminder")]
        [Tooltip("How many upcoming days to pre-schedule (e.g. 10).")]
        [Min(1)] public int dailyReminderDaysAhead = 10;
        [Range(0, 23)] public int dailyReminderHour = 19;
        [Range(0, 59)] public int dailyReminderMinute = 0;
        public string dailyTitle = "Time to play!";
        [TextArea(2, 4)] public string dailyBody = "Come back and continue your adventure.";

        [Header("Android channel")]
        public string androidChannelId = "jis_default";
        public string androidChannelName = "Game reminders";
        public string androidChannelDescription = "Energy, shop reset, and daily reminders";

        [Header("Default gameplay copy")]
        public string energyFullTitle = "Energy full!";
        [TextArea(2, 3)] public string energyFullBody = "Your energy has recovered. Jump back in!";
        public string shopResetTitle = "Shop refreshed!";
        [TextArea(2, 3)] public string shopResetBody = "New items are waiting in the shop.";
        public string dailyRewardTitle = "Daily reward!";
        [TextArea(2, 3)] public string dailyRewardBody = "Claim your free reward before it expires.";
    }
}
