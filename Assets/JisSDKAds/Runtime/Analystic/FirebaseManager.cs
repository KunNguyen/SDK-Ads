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
using UnityEngine;
using UnityEngine.Events;

namespace SDK
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
        [field: SerializeField] public FirebaseAuthManager FirebaseAuthManager { get; private set; }
        public bool IsSignedIn => FirebaseAuthManager.IsSignedIn;

        public event Action<Firebase.Auth.FirebaseUser> SignedInWithUser;
        public event Action SignedInWithoutUser;
        public event Action<string> SignedInFailed;
        public event Action SignedOut;
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
            FirebaseAuthManager = new FirebaseAuthManager();
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
        private void InitAuth()
        {
            FirebaseAuthManager.Init();
            FirebaseAuthManager.SignedIn += user =>
            {
                SignedInWithUser?.Invoke(user);
                SignedInWithoutUser?.Invoke();
            };
            FirebaseAuthManager.SignInFailed += msg =>
            {
                SignedInFailed?.Invoke(msg);
            };
            FirebaseAuthManager.SignedOut += () =>
            {
                SignedOut?.Invoke();
            };
        }

        public Task<Firebase.Auth.FirebaseUser> SignInWithGoogle(CancellationToken ct = default)
            => FirebaseAuthManager.SignInWithGoogleAsync(ct);

        public Task<Firebase.Auth.FirebaseUser> SignInWithPlatform(CancellationToken ct = default)
            => FirebaseAuthManager.SignInWithPlatformAsync(ct);

#if GOOGLE_PLAY_GAMES
        public Task<Firebase.Auth.FirebaseUser> SignInWithPlayGames(CancellationToken ct = default)
            => FirebaseAuthManager.SignInWithPlayGamesAsync(ct);

        public Task<Firebase.Auth.FirebaseUser> TrySilentSignInPlayGames(CancellationToken ct = default)
            => FirebaseAuthManager.TrySilentSignInPlayGamesAsync(ct);
#endif

#if UNITY_IOS
        public Task<Firebase.Auth.FirebaseUser> SignInWithGameCenter(CancellationToken ct = default)
            => FirebaseAuthManager.SignInWithGameCenterAsync(ct);
#endif

        public Task<Firebase.Auth.FirebaseUser> SignInAnonymously(CancellationToken ct = default)
            => FirebaseAuthManager.SignInAnonymouslyAsync(ct);
        
        public Task SignOut()
            => FirebaseAuthManager.SignOut();
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
