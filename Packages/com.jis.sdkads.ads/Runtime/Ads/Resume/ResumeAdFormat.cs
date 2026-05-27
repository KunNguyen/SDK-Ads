namespace JisSDKAds.Ads.Resume
{
    /// <summary>
    /// Ad format shown when the app returns to foreground (Resume policy).
    /// Not an <see cref="AdsType"/> — configured via Firebase <c>ads_resume_type</c>.
    /// </summary>
    public enum ResumeAdFormat
    {
        AppOpen = 0,
        Interstitial = 1
    }
}
