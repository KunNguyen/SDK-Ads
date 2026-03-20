using System;
using System.Collections;
using Firebase;
using Firebase.Analytics;
#if UNITY_CRASHLYTICS
using Firebase.Crashlytics;
#endif
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.Events;

namespace SDK {
    [ScriptOrder(-10)]
    public class FirebaseManager : MonoBehaviour {
        private FirebaseAnalyticsManager FirebaseAnalyticsManager { get; set; }
        private FirebaseRemoteConfigManager FirebaseRemoteConfigManager { get; set; }

        public UnityAction OnInitedSuccessCallback { get; set; }

        private static FirebaseManager instance;
        public static FirebaseManager Instance => instance;
        
        public bool IsFirebaseReady { get; private set; } = false;
        public bool IsFirebaseRemoteFetchingSuccess { get; private set; } = false;

        public FirebaseApp FirebaseApp { get; set; }
        
        private void Awake() {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        IEnumerator Start() {
            yield return new WaitUntil(() => IsFirebaseReady);
#if FIREBASE_MESSAGING
            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived; 
#endif
        }
#if FIREBASE_MESSAGING
        public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
#if UNITY_ANDROID && UNITY_APPSFLYER
            AppsFlyerSDK.AppsFlyer.updateServerUninstallToken(token.Token);
#endif
        } 
#endif

        private void Init() {
            FirebaseAnalyticsManager = new FirebaseAnalyticsManager();
            FirebaseRemoteConfigManager = new FirebaseRemoteConfigManager();
            Debug.Log("Start Config");
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted || task.IsCanceled) {
                    Debug.LogError("Firebase dependency check failed: " + task.Exception);
                    return;
                }
                DependencyStatus dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available) {
                    InitializedFirebase();
                } else {
                    Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
                }
            });
        }
        private void InitializedFirebase()
        {
            Debug.Log("Initialize Firebase");
            FirebaseApp = FirebaseApp.DefaultInstance;
            OnInitedSuccessCallback?.Invoke();
            SetupRemoteConfig();
            IsFirebaseReady = true;
#if UNITY_CRASHLYTICS
            Crashlytics.IsCrashlyticsCollectionEnabled = true;
#endif
        }
       
        private void SetupRemoteConfig()
        {
            FirebaseRemoteConfigManager.InitRemoteConfig(OnFetchSuccess);
        }
        private void OnFetchSuccess() {
            Debug.Log("---------------------Update All RemoteConfigs----------------------");
            EventManager.AddEventNextFrame(() => EventManager.TriggerEvent("UpdateRemoteConfigs"));
            IsFirebaseRemoteFetchingSuccess = true;
        }
        

        public void LogFirebaseEvent(string eventName, string eventParamete, double eventValue) {
            if (IsFirebaseReady) {
                FirebaseAnalyticsManager.LogEvent(eventName, eventParamete, eventValue);
            }
        }
        public void LogFirebaseEvent(string eventName, Parameter[] paramss) {
            if (IsFirebaseReady) {
                FirebaseAnalyticsManager.LogEvent(eventName, paramss);
            }
        }
        public void LogFirebaseEvent(string eventName) {
            if (IsFirebaseReady) {
                FirebaseAnalyticsManager.LogEvent(eventName);
            }
        }
        public void SetUserProperty(string propertyName, string property) {
            if (IsFirebaseReady) {
                FirebaseAnalyticsManager.SetUserProperty(propertyName, property);
            }
        }
        public void FetchData(System.Action successCallback) {
            FirebaseRemoteConfigManager.FetchRemoteConfig(successCallback);
        }
        public ConfigValue GetConfigValue(string key) {
            return FirebaseRemoteConfigManager.GetValues(key);
        }
        public string GetConfigString(string key)
        {
            return FirebaseRemoteConfigManager.GetValues(key).StringValue;
        }
        public double GetConfigDouble(string key)
        {
            return FirebaseRemoteConfigManager.GetValues(key).DoubleValue;
        }
        public bool GetConfigBool(string key)
        {
            return FirebaseRemoteConfigManager.GetValues(key).BooleanValue;
        }
    }
}

