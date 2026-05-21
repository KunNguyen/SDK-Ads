namespace JisSDKAds.Ads
{
    /// <summary>
    /// UMP consent (implemented by AdMob mediation controller when <c>UNITY_AD_ADMOB</c> package is present).
    /// </summary>
    public interface IAdMobConsentHost
    {
        void ShowConsentFormAgain();
    }
}
