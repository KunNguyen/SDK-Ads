namespace JisSDKAds.Notifications
{
    /// <summary>Stable string keys for scheduled notifications (mapped to platform ids).</summary>
    public static class JisLocalNotificationIds
    {
        public const string DailyPrefix = "jis.daily.";
        public const string GameplayPrefix = "jis.gameplay.";

        public static string Daily(int dayOffset) => $"{DailyPrefix}{dayOffset:00}";

        public static string EnergyFull => $"{GameplayPrefix}energy_full";
        public static string ShopReset => $"{GameplayPrefix}shop_reset";
        public static string DailyReward => $"{GameplayPrefix}daily_reward";
        public static string EventEnding => $"{GameplayPrefix}event_ending";
        public static string Inactivity => $"{GameplayPrefix}inactivity";

        public static string Custom(string suffix) => $"{GameplayPrefix}custom.{suffix}";
    }
}
