using System;

namespace JisSDKAds.Notifications
{
    /// <summary>Convenience APIs for common idle / card-shop gameplay timers.</summary>
    public static class JisGameplayNotifications
    {
        public static void ScheduleEnergyFull(JisLocalNotificationManager manager, TimeSpan untilFull,
            string title = null, string body = null)
        {
            if (manager == null || manager.Settings == null) return;
            var s = manager.Settings;
            manager.Cancel(JisLocalNotificationIds.EnergyFull);
            manager.ScheduleAfter(
                JisLocalNotificationIds.EnergyFull,
                title ?? s.energyFullTitle,
                body ?? s.energyFullBody,
                untilFull);
        }

        public static void ScheduleShopReset(JisLocalNotificationManager manager, DateTime resetLocalTime,
            string title = null, string body = null)
        {
            if (manager == null || manager.Settings == null) return;
            var s = manager.Settings;
            manager.Cancel(JisLocalNotificationIds.ShopReset);
            manager.ScheduleAt(
                JisLocalNotificationIds.ShopReset,
                title ?? s.shopResetTitle,
                body ?? s.shopResetBody,
                resetLocalTime);
        }

        public static void ScheduleDailyReward(JisLocalNotificationManager manager, DateTime claimDeadlineLocal,
            string title = null, string body = null)
        {
            if (manager == null || manager.Settings == null) return;
            var s = manager.Settings;
            manager.Cancel(JisLocalNotificationIds.DailyReward);
            manager.ScheduleAt(
                JisLocalNotificationIds.DailyReward,
                title ?? s.dailyRewardTitle,
                body ?? s.dailyRewardBody,
                claimDeadlineLocal);
        }

        public static void ScheduleEventEnding(JisLocalNotificationManager manager, DateTime eventEndLocal,
            string title, string body, TimeSpan remindBefore = default)
        {
            if (manager == null) return;
            if (remindBefore == default) remindBefore = TimeSpan.FromHours(1);
            var fire = eventEndLocal - remindBefore;
            manager.Cancel(JisLocalNotificationIds.EventEnding);
            manager.ScheduleAt(JisLocalNotificationIds.EventEnding, title, body, fire);
        }

        public static void ScheduleInactivityReminder(JisLocalNotificationManager manager, TimeSpan inactiveFor,
            string title, string body)
        {
            if (manager == null) return;
            manager.Cancel(JisLocalNotificationIds.Inactivity);
            manager.ScheduleAfter(JisLocalNotificationIds.Inactivity, title, body, inactiveFor);
        }

        public static void CancelEnergyFull(JisLocalNotificationManager manager) =>
            manager?.Cancel(JisLocalNotificationIds.EnergyFull);

        public static void CancelShopReset(JisLocalNotificationManager manager) =>
            manager?.Cancel(JisLocalNotificationIds.ShopReset);

        public static void CancelDailyReward(JisLocalNotificationManager manager) =>
            manager?.Cancel(JisLocalNotificationIds.DailyReward);
    }
}
