using UnityEngine;

namespace SolarEngineSDK.Editor
{
    public class ConstString
    {
        public const string solorEngineLog = "[SolorEngineSDK]\n";


        public const string storageWarning = "You can only choose either China or Overseas！";

        //
        public const string applySuccess = "";
        //public const string applyFail = "";
        //

        public const string confirmMessage = "You cancelled the operation";


        public const string storage = "";


        public const string OK = "OK";
        public const string cancel = "Cancel";


        //confirm
        public const string confirm = "Confirm";

        //  public const string apply = "Apply";
        public const string success = "Success";
        public const string fail = "Fail";

        public static string currentData = solorEngineLog + "The current data storage region is set to";

        public class MenuItem
        {
            public const string solarEngineSDK = "SolarEngineSDK";

            // SDK Edit Settings
            public const string sdkEditSettings = solarEngineSDK + "/SDK Edit Settings";

            // SolarEngineSDK/Documentation/UnityDocumentation
            public const string documentation = solarEngineSDK + "/Documentation";

            public const string unityDocumentation = documentation + "/UnityDocumentation";

            //iOS ChangleLog
            public const string iOSChangelog = documentation + "/iOS Changelog";

            public const string androidChangelog = documentation + "/Android Changelog";

            //Create Analytics Object
            public const string createAnalyticsObject = solarEngineSDK + "/Create Analytics Object";
        }


        #region EditorPanel

        public const string chinaMainland = "China Mainland";

        //Non-China-Mainland
        public const string nonChinaMainland = "Non-China-Mainland";

        public const string oaid = "OAID";
        public const string ODMInfo = "ODMInfo";


        //Please confirm whether you want to enable Oaid in Oversea
        public const string oaidEnable =
            "When the selected data storage region is outside Mainland China, please confirm whether OAID (Open Advertising ID) is needed. This could impact the application's compliance and eligibility for listing on the Google Play Store.";

        public const string storageEnableOaidCN =
            "When the data storage region is set to China Mainland, OAID cannot be disabled.";

        //he specified version can be used. If not filled in, the latest version will be used by default
        public const string confirmVersion =
            "Specify the Android/iOS SDK version. If not provided, the latest version will be used by default.";

        public const string odmInfoEnable = "Enable Integrated Conversion Measurement (ICM) feature of Google Ads ";


        public const string remoteConfigMsg = "Please confirm the platform for the online parameter plugin.";


        public const string removeSDKMsg = "删除.";

        #endregion


        #region pop

        public const string nostorageWarning = solorEngineLog +
                                               "It is mandatory to select the data storage region for products created in the SolarEngine backend.";


        public const string confirmMessage2 = "Are you sure you want to perform this operation?";

        //Please set up your data storage area.
        public const string storageAreaMessage =
            "Please select the data storage region for the products created in the SolarEngine backend.";

        //Confirm Operation
        public const string pleaseConfirm = "Please Confirm";

        public const string applyFail = solorEngineLog +
                                        "Setup failed.Please check the console error log, make the necessary modifications, and try applying the changes again.";

        #endregion
    }

    public class EditorUtils
    {
        public enum EditorColor
        {
            SoftBlue,
            Cyan,
            GraphiteGray,
            OliveGreen,
            Golden,
            Red,
            Green,
            Blue,
            Yellow,
            Purple,
            SoftPurple,
            DarkPurple,

            SoftGreen,
            AppleGreen,
            GrayGreen
        }

        /// <summary>
        ///     通过枚举获取预设颜色
        /// </summary>
        public static Color32 GetColor(EditorColor col)
        {
            switch (col)
            {
                case EditorColor.SoftBlue: return new Color32(120, 170, 255, 255); // 柔和蓝
                case EditorColor.Cyan: return new Color32(0, 200, 200, 255); // 浅青
                case EditorColor.GraphiteGray: return new Color32(160, 160, 160, 255); // 石墨灰
                case EditorColor.OliveGreen: return new Color32(130, 210, 130, 255); // 橄榄绿
                case EditorColor.Golden: return new Color32(210, 180, 80, 255); // 金色
                case EditorColor.Red: return new Color32(255, 80, 80, 255);
                case EditorColor.Green: return new Color32(80, 255, 120, 255);
                case EditorColor.Blue: return new Color32(80, 160, 255, 255);
                case EditorColor.Yellow: return new Color32(255, 220, 100, 255);
                case EditorColor.Purple: return new Color32(155, 110, 245, 255); // 中紫
                case EditorColor.SoftPurple: return new Color32(180, 140, 255, 255); // 柔和紫（推荐）
                case EditorColor.DarkPurple: return new Color32(120, 80, 200, 255); // 暗紫
                case EditorColor.SoftGreen: return new Color32(120, 220, 160, 255); // 柔和绿（推荐）
                case EditorColor.AppleGreen: return new Color32(100, 200, 100, 255); // 苹果绿
                case EditorColor.GrayGreen: return new Color32(100, 130, 100, 255); // 灰绿
                default: return new Color32(255, 255, 255, 255); // 默认白色
            }
        }
    }
}