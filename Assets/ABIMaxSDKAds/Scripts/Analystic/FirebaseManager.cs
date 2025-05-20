using System;
using System.Collections;
#if UNITY_APPSFLYER
using AppsFlyerSDK; 
#endif
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.Events;

namespace SDK {
    [ScriptOrder(-10)]
    public class FirebaseManager : MonoBehaviour {
        private FirebaseAnalyticsManager m_FirebaseAnalyticsManager;
        private FirebaseRemoteConfigManager m_FirebaseRemoteConfigManager;

        public UnityAction m_FirebaseInitedCallback;
        public UnityAction m_FirebaseInitedSuccessCallback;

        private static FirebaseManager m_Instance;
        public static FirebaseManager Instance => m_Instance;
        
        public bool IsFirebaseReady { get; private set; } = false;
        public bool IsFirebaseRemoteFetchingSuccess { get; private set; } = false;

        public FirebaseApp FirebaseApp { get; set; }
        private void Awake() {
            m_Instance = this;
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
            AppsFlyer.updateServerUninstallToken(token.Token);
#endif
        } 
#endif

        private void Init() {
            m_FirebaseAnalyticsManager = new FirebaseAnalyticsManager();
            m_FirebaseRemoteConfigManager = new FirebaseRemoteConfigManager();
            Debug.Log("Start Config");
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                DependencyStatus dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available) {
                    InitializedFirebase();
                } else {
                    Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result);
                }
            });
        }
        private void InitializedFirebase()
        {
            Debug.Log("Initialize Firebase");
            FirebaseApp = FirebaseApp.DefaultInstance;
            m_FirebaseInitedSuccessCallback?.Invoke();
            SetupRemoteConfig();
            IsFirebaseReady = true;
        }
        private void SetupRemoteConfig()
        {
            m_FirebaseRemoteConfigManager.InitRemoteConfig(OnFetchSuccess);
        }
        private void OnFetchSuccess() {
            Debug.Log("---------------------Update All RemoteConfigs----------------------");
            EventManager.AddEventNextFrame(() => EventManager.TriggerEvent("UpdateRemoteConfigs"));
            IsFirebaseRemoteFetchingSuccess = true;
        }
        

        public void LogFirebaseEvent(string eventName, string eventParamete, double eventValue) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.LogEvent(eventName, eventParamete, eventValue);
            }
        }
        public void LogFirebaseEvent(string eventName, Parameter[] paramss) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.LogEvent(eventName, paramss);
            }
        }
        public void LogFirebaseEvent(string eventName) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.LogEvent(eventName);
            }
        }
        public void SetUserProperty(string propertyName, string property) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.SetUserProperty(propertyName, property);
            }
        }
        public void FetchData(System.Action successCallback) {
            m_FirebaseRemoteConfigManager.FetchRemoteConfig(successCallback);
        }
        public ConfigValue GetConfigValue(string key) {
            return m_FirebaseRemoteConfigManager.GetValues(key);
        }
        public string GetConfigString(string key)
        {
            return m_FirebaseRemoteConfigManager.GetValues(key).StringValue;
        }
        public double GetConfigDouble(string key)
        {
            return m_FirebaseRemoteConfigManager.GetValues(key).DoubleValue;
        }
        public bool GetConfigBool(string key)
        {
            return m_FirebaseRemoteConfigManager.GetValues(key).BooleanValue;
        }
    }
}

