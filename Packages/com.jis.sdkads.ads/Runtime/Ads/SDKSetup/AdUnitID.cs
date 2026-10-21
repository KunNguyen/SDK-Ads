namespace JisSDKAds.Ads
{
    /// <summary>Per-platform ad unit id (MAX setups). Both fields are always serialized so switching the
    /// build target never strips the other platform's id when Unity re-saves the asset.</summary>
    [System.Serializable]
    public class AdUnitID
    {
        public string AndroidID;
        public string IOSID;

        public string ID
        {
            get
            {
#if UNITY_ANDROID
                return AndroidID;
#elif UNITY_IOS
                return IOSID;
#else
                return "";
#endif
            }
            set
            {
#if UNITY_ANDROID
                AndroidID = value;
#elif UNITY_IOS
                IOSID = value;
#endif
            }
        }
    }
}
