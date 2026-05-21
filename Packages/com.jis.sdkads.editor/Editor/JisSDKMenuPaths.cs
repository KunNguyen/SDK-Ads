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
        public const string AdsCreateRewardPlacements = AdsRoot + "Create/Open Reward Placements Config";
        public const string AdsCreateTieredConfig = AdsRoot + "Create/Open Tiered Ads Config";

        public const string AdsSceneRoot = AdsRoot + "Scene/";
        public const string AdsSceneAddManager = AdsSceneRoot + "Add Manager Prefab";

        public const string AdsAutoApplyRoot = AdsRoot + "Auto Apply/";
        public const string AdsAutoApplyPlatformSwitch = AdsAutoApplyRoot + "Toggle On Platform Switch";
        public const string AdsAutoApplyOnPlay = AdsAutoApplyRoot + "Toggle On Play";
        public const string AdsAutoApplyOnBuild = AdsAutoApplyRoot + "Toggle On Build";
        public const string AdsAutoApplyNow = AdsAutoApplyRoot + "Apply Now (Active Build Target)";

        public const string AdsLegacyRoot = AdsRoot + "Legacy/";
        public const string AdsLegacyCreateContainer = AdsLegacyRoot + "Create/Open Setup Container";

        // IAP (separate from Ads)
        public const string IapRoot = Root + "IAP/";
        public const string IapEnable = IapRoot + "Enable IAP";
        public const string IapCreatePackagesConfig = IapRoot + "Create/Open Packages Config";
        public const string IapSceneAddPurchaser = IapRoot + "Scene/Add InApp Purchaser Prefab";

        // GameObject context
        public const string GameObjectAdsRoot = "GameObject/JIS SDK/Ads/";
        public const string GameObjectAddManager = GameObjectAdsRoot + "Add Manager";

        public const string GameObjectIapRoot = "GameObject/JIS SDK/IAP/";
        public const string GameObjectEnableIap = GameObjectIapRoot + "Enable IAP";
        public const string GameObjectAddInAppPurchaser = GameObjectIapRoot + "Add InApp Purchaser";
    }
}
#endif
