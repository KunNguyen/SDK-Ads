using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using SolarEngineSDK.Editor;
using UnityEditor;
#endif


namespace SolarEngine
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class SolarEngineSettings : ScriptableObject
    {
        private const string ASSET_NAME = "SolarEngineSettings";
        private const string ASSET_PATH = "Resources";
        private const string ASSET_EXT = ".asset";

        private static SolarEngineSettings instance;

        //可选功能
        [HideInInspector] public bool _OptionalFeatures;


        public static SolarEngineSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load(ASSET_NAME) as SolarEngineSettings;
                    if (instance == null)
                    {
                        // If not found, autocreate the asset object.
                        instance = CreateInstance<SolarEngineSettings>();
#if UNITY_EDITOR
                        var properPath = Path.Combine(Application.dataPath, ASSET_PATH);
                        if (!Directory.Exists(properPath)) Directory.CreateDirectory(properPath);

                        var fullPath = Path.Combine(Path.Combine("Assets", ASSET_PATH), ASSET_NAME + ASSET_EXT);
                        AssetDatabase.CreateAsset(instance, fullPath);
#endif
                    }
                }

                return instance;
            }
        }

        public static string iOSUrlIdentifier
        {
            get => Instance._iOSUrlIdentifier;
            set => Instance._iOSUrlIdentifier = value;
        }

        public static string[] iOSUrlSchemes
        {
            get => Instance._iOSUrlSchemes;
            set => Instance._iOSUrlSchemes = value;
        }

        public static string[] iOSUniversalLinksDomains
        {
            get => Instance._iOSUniversalLinksDomains;
            set => Instance._iOSUniversalLinksDomains = value;
        }

        public static string[] AndroidUrlSchemes
        {
            get => Instance._AndroidUrlSchemes;
            set => Instance._AndroidUrlSchemes = value;
        }

        public static bool isCN => Instance._China;


        public static bool isOversea => Instance._Oversea;


        public static bool removeAndroidSDK
        {
            get => Instance._RemoveAndroidSDK;
            set => Instance._RemoveAndroidSDK = value;
        }


        public static bool isUseMiniGame
        {
            get => Instance._MiniGame;
        }

        public static bool isUseOpenHarmony
        {
            get => Instance._OpenHarmony;
            set => Instance._OpenHarmony = value;
        }

        public static bool isUseMacOS
        {
            get => Instance._iOS;
        }

        public static bool isUseAndroid
        {
            get => Instance._Android;
            set => Instance._Android = value;
        }

        public static bool isUseiOS
        {
            get => Instance._iOS;
            set => Instance._iOS = value;
        }

        public static bool isUseAll => Instance._All;

        public static bool isUseOaid
        {
            get => Instance._Oaid;
            set => Instance._Oaid = value;
        }

        public static bool isUseODMInfo
        {
            get => Instance._ODMInfo;
            set => Instance._ODMInfo = value;
        }

        public static bool isSpecifyVersio
        {
            get => Instance._SpecifyVersion;
            set => Instance._SpecifyVersion = value;
        }

        public static string iOSVersion
        {
            get => Instance._iOSVersion;
            set => Instance._iOSVersion = value;
        }

        public static string AndroidVersion
        {
            get => Instance._AndroidVersion;
            set => Instance._AndroidVersion = value;
        }

        public static string OpenHarmonyVersion
        {
            get => Instance._OpenHarmonyVersion;
            set => Instance._OpenHarmonyVersion = value;
        }

        public static string MacOSVersion => Instance._iOSVersion;
        //  set { Instance._iOSVersion = value; }
#if UNITY_EDITOR
        [MenuItem(ConstString.MenuItem.sdkEditSettings, false, 0)]
        public static void EditSettings()
        {
            Selection.activeObject = Instance;
        }
#endif

        #region 数据存储区域

        [SerializeField] private bool _China;
        [SerializeField] private bool _Oversea;

        #endregion

        #region RC

        //默认开启
        [SerializeField] private bool _All = true;
        [SerializeField] private bool _iOS = true;
        [SerializeField] private bool _Android = true;
        [SerializeField] private bool _MiniGame = true;
        [SerializeField] private bool _OpenHarmony = true;

        #endregion

        #region _Oaid、_ODMInfo、_RemoveAndroidSDK

        [SerializeField] private bool _Oaid = true;
        [SerializeField] private bool _ODMInfo;


        [SerializeField] private bool _RemoveAndroidSDK;

        #endregion


        #region deeplink

        [SerializeField] private bool _DeepLink;
        [SerializeField] private string _iOSUrlIdentifier;
        [SerializeField] private string[] _iOSUrlSchemes = new string[0];
        [SerializeField] private string[] _iOSUniversalLinksDomains = new string[0];


        [SerializeField] private string[] _AndroidUrlSchemes = new string[0];

        #endregion


        #region Version;

        [SerializeField] private bool _SpecifyVersion;


        [SerializeField] private string _iOSVersion;
        [SerializeField] private string _AndroidVersion;

        [SerializeField] private string _OpenHarmonyVersion;
        // [SerializeField] private string _MacOSVersion;

        #endregion
    }
}