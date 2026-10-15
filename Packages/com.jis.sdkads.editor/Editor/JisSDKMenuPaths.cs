#if UNITY_EDITOR
namespace JisSDKAds.Editor
{
    public static class JisSDKMenuPaths
    {
        public const string Root = "JIS SDK/";

        // Ads
        public const string AdsRoot = Root + "Ads/";
        public const string AdsCreateSettings = AdsRoot + "Create/Open Settings Asset";
        public const string AdsApplyToScene = AdsRoot + "Apply Settings to Scene";

        public const string AdsSceneRoot = AdsRoot + "Scene/";
        public const string AdsSceneAddManager = AdsSceneRoot + "Add JisSDK Manager Prefab";
        public const string AdsSceneRemoveLegacy = AdsSceneRoot + "Remove legacy Ads_Runtime";

        public const string AdsAutoApplyRoot = AdsRoot + "Auto Apply/";
        public const string AdsAutoApplyPlatformSwitch = AdsAutoApplyRoot + "Toggle On Platform Switch";
        public const string AdsAutoApplyOnPlay = AdsAutoApplyRoot + "Toggle On Play";
        public const string AdsAutoApplyOnBuild = AdsAutoApplyRoot + "Toggle On Build";
        public const string AdsAutoApplyNow = AdsAutoApplyRoot + "Apply Now (Active Build Target)";

        // IAP (separate from Ads)
        public const string IapRoot = Root + "IAP/";
        public const string IapEnable = IapRoot + "Enable IAP";
        public const string IapCreatePackagesConfig = IapRoot + "Create/Open Packages Config";
        public const string IapSceneAddPurchaser = IapRoot + "Scene/Add JisSDK InApp Purchaser Prefab";

        // GameObject context
        public const string GameObjectAdsRoot = "GameObject/JIS SDK/Ads/";
        public const string GameObjectAddManager = GameObjectAdsRoot + "Add JisSDK Manager";

        public const string GameObjectIapRoot = "GameObject/JIS SDK/IAP/";
        public const string GameObjectEnableIap = GameObjectIapRoot + "Enable IAP";
        public const string GameObjectAddInAppPurchaser = GameObjectIapRoot + "Add JisSDK InApp Purchaser";
    }
}
#endif
