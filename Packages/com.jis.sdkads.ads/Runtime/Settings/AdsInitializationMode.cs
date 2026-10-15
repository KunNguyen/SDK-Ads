namespace JisSDKAds.Ads.Settings
{
    /// <summary>
    /// How ads start. Manual: the game calls JisAds.InitializeAsync() from its loading flow.
    /// AutoOnStart: JisAds initializes itself in Start (prototypes only).
    /// Values are serialized as ints; keep the order.
    /// </summary>
    public enum AdsInitializationMode
    {
        AutoOnStart = 0,
        Manual = 1
    }
}
