using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
#if UNITY_CRASHLYTICS
using Firebase.Crashlytics;
#endif
using Firebase.Extensions;
using Firebase.RemoteConfig;
#if FIREBASE_AUTH
using Firebase.Auth;
#endif
using JisSDKAds.Common;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Firebase
{
    [ScriptOrder(-10)]
    public class FirebaseManager : MonoBehaviour
    {
        public enum FirebaseInitializationMode
        {
            AutoOnAwake = 0,
            Manual = 1
        }

        private static FirebaseManager instance;
        public static FirebaseManager Instance => instance;

        public bool IsFirebaseReady { get; private set; }
        public bool IsRemoteConfigReady { get; private set; }

        public FirebaseApp FirebaseApp { get; private set; }

        private bool isInitializing;

        public UnityAction OnInitedSuccessCallback { get; set; }
        [field: SerializeField] public FirebaseInitializationMode InitializationMode { get; set; } = FirebaseInitializationMode.AutoOnAwake;

#if FIREBASE_AUTH
        /// <summary>Sign-in API: <see cref="FirebaseAuthManager.SignInWithGoogleAsync"/>.</summary>
        public FirebaseAuthManager FirebaseAuth { get; private set; }

        [Obsolete("Use FirebaseAuth property.")]
        public FirebaseAuthManager FirebaseAuthManager => FirebaseAuth;

        public bool IsSignedIn => FirebaseAuth != null && FirebaseAuth.IsSignedIn;

        /// <summary>Fired when Firebase Auth sign-in succeeds (includes restored session on <see cref="InitAsync"/>).</summary>
        public event Action<FirebaseUser> SignedInWithUser;

        /// <summary>Fired when the user signs out or no session exists.</summary>
        public event Action SignedOut;

        /// <summary>Fired when an explicit sign-in attempt fails (Google / Play Games / Game Center / Anonymous).</summary>
        public event Action<string> SignedInFailed;
#endif

        private FirebaseAnalyticsManager analytics;
        private FirebaseRemoteConfigManager remoteConfig;

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (InitializationMode == FirebaseInitializationMode.AutoOnAwake)
            {
                _ = InitAsync();
            }
        }

        #endregion

        // ======================= PUBLIC ASYNC API =======================

        /// <summary>
        /// Init Firebase core (Dependency + App + Auth + Crashlytics)
        /// Safe to call multiple times
        /// </summary>
        public async Task InitAsync()
        {
            if (IsFirebaseReady || isInitializing)
                return;

            isInitializing = true;

            analytics = new FirebaseAnalyticsManager();
            remoteConfig = new FirebaseRemoteConfigManager();

#if FIREBASE_AUTH
            FirebaseAuth = new FirebaseAuthManager();
#endif

            var status = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"Firebase dependency error: {status}");
                isInitializing = false;
                return;
            }

            FirebaseApp = FirebaseApp.DefaultInstance;
#if UNITY_CRASHLYTICS
            Crashlytics.IsCrashlyticsCollectionEnabled = true;
#endif

#if FIREBASE_AUTH
            InitAuth();
#endif

            IsFirebaseReady = true;
            isInitializing = false;

            OnInitedSuccessCallback?.Invoke();
        }

        /// <summary>
        /// Fetch & activate RemoteConfig
        /// </summary>
        public async Task FetchRemoteConfigAsync()
        {
            if (!IsFirebaseReady)
                await InitAsync();

            var tcs = new TaskCompletionSource<bool>();

            remoteConfig.InitRemoteConfig(() =>
            {
                IsRemoteConfigReady = true;
                tcs.TrySetResult(true);
            });

            await tcs.Task;
        }

        #region Auth

#if FIREBASE_AUTH
        void InitAuth()
        {
            FirebaseAuth ??= new FirebaseAuthManager();
            FirebaseAuth.SignedIn += user => SignedInWithUser?.Invoke(user);
            FirebaseAuth.SignedOut += () => SignedOut?.Invoke();
            FirebaseAuth.SignedInFailed += message => SignedInFailed?.Invoke(message);
            FirebaseAuth.Init();
        }
#endif

        #endregion

        #region Analytics / Remote Config (giữ API cũ)

        public void LogEvent(string eventName)
        {
            if (IsFirebaseReady)
                analytics.LogEvent(eventName);
        }

        public void LogEvent(string eventName, Parameter[] parameters)
        {
            if (IsFirebaseReady)
                analytics.LogEvent(eventName, parameters);
        }
        public void SetUserProperty(string propertyName, string property)
        {
            if (IsFirebaseReady)
                analytics.SetUserProperty(propertyName, property);
        }

        public ConfigValue GetConfigValue(string key)
            => remoteConfig.GetValues(key);

        public string GetConfigString(string key)
            => GetConfigValue(key).StringValue;

        public double GetConfigDouble(string key)
            => GetConfigValue(key).DoubleValue;

        public bool GetConfigBool(string key)
            => GetConfigValue(key).BooleanValue;

        #endregion
    }
}
