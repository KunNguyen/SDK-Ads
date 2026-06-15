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

        /// <summary>Fired when sign-in succeeds (includes restored session). For full <c>FirebaseUser</c>, use <see cref="FirebaseAuth.SignedIn"/>.</summary>
        public event Action<string> SignedInWithUserId;

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
            JisSDKPersistence.DontDestroyUnlessUnderPersistentRoot(gameObject);

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
        /// Fetch and activate Remote Config. Always completes (never hangs on network/SDK errors).
        /// </summary>
        /// <returns><c>true</c> when fetch + activate succeeded; <c>false</c> on failure (defaults may still apply).</returns>
        public async Task<bool> FetchRemoteConfigAsync()
        {
            if (!IsFirebaseReady)
                await InitAsync();

            if (!IsFirebaseReady || remoteConfig == null)
                return false;

            var tcs = new TaskCompletionSource<bool>();

            remoteConfig.InitRemoteConfig(fetchAndActivateSucceeded =>
            {
                if (remoteConfig.DefaultsApplied)
                    IsRemoteConfigReady = true;
                tcs.TrySetResult(fetchAndActivateSucceeded);
            });

            return await tcs.Task;
        }

        #region Auth

#if FIREBASE_AUTH
        void InitAuth()
        {
            FirebaseAuth ??= new FirebaseAuthManager();
            FirebaseAuth.SignedInUserId += userId => SignedInWithUserId?.Invoke(userId);
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
