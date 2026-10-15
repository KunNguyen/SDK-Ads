namespace JisSDKAds.Ads
{
    /// <summary>Per-platform ad unit id (MAX setups). Serialized field name depends on the active build target.</summary>
    [System.Serializable]
    public class AdUnitID
    {
#if UNITY_ANDROID
        public string AndroidID;
#elif UNITY_IOS
        public string IOSID;
#endif
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
