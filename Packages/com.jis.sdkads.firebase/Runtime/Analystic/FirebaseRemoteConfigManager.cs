using System;
using System.Collections.Generic;
using JisSDKAds.Common;
using UnityEngine;
using UnityEngine.Events;
using Firebase.RemoteConfig;
using System.Threading.Tasks;
using Firebase.Extensions;

namespace JisSDKAds.Firebase
{
    public class FirebaseRemoteConfigManager
    {
        public void InitRemoteConfig(System.Action onFetchAndActivateSuccessful)
        {
            Dictionary<string, object> defaults =
                    new Dictionary<string, object>
                    {
                        { Keys.key_remote_aoa_active, true },
                        { Keys.key_remote_aoa_show_first_time_active, true },
                        { Keys.key_remote_ads_resume_ads_active, true },
                        { Keys.key_remote_ads_resume_ads_type, "APP_OPEN"},
                        { Keys.key_remote_ads_resume_pause_time, 5 },
                        { Keys.key_remote_ads_resume_capping_time, 10 },
                        { Keys.key_remote_interstitial_level, 3 },
                        { Keys.key_remote_interstitial_capping_time, 30 },
                        { Keys.key_remote_inter_reward_interspersed, false },
                        { Keys.key_remote_inter_reward_interspersed_time, 10 },
                        { Keys.key_remote_mrec_active, false },
                        { Keys.key_remote_free_ads, 1 },
                        { Keys.key_remote_banner_auto_refresh, false},
                        { Keys.key_remote_banner_auto_refresh_time, 15},
                        { Keys.key_remote_inter_premium_id, "" },
                        { Keys.key_remote_inter_high_id, "" },
                        { Keys.key_remote_inter_mid_id, "" },
                        { Keys.key_remote_inter_low_id, "" },
                        { Keys.key_remote_inter_fill_id, "" },
                        { Keys.key_remote_reward_premium_id, "" },
                        { Keys.key_remote_reward_high_id, "" },
                        { Keys.key_remote_reward_mid_id, "" },
                        { Keys.key_remote_reward_low_id, "" },
                        { Keys.key_remote_reward_fill_id, "" },
                    };

            var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            remoteConfig.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("SetDefaultsAsync failed: " + task.Exception);
                    return;
                }
                FetchRemoteConfig(onFetchAndActivateSuccessful);
            });
        }
        
        public ConfigValue GetValues(string key)
        {
            return FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        }

        public void FetchRemoteConfig(Action onFetchAndActivateSuccessful)
        {
            if (FirebaseManager.Instance.FirebaseApp == null)
            {
                Debug.LogError("FirebaseApp is null, cannot fetch remote config.");
                return;
            }
            Debug.Log("Fetching data...");
            var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            remoteConfig.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("FetchAsync failed: " + task.Exception);
                    return;
                }
                ActivateRetrievedRemoteConfigValues(onFetchAndActivateSuccessful);
            });
        }

        private void ActivateRetrievedRemoteConfigValues(Action onFetchAndActivateSuccessful)
        {
            var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            var info = remoteConfig.Info;
            if (info.LastFetchStatus == LastFetchStatus.Success)
            {
                remoteConfig.ActivateAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Debug.LogError("ActivateAsync failed: " + task.Exception);
                        return;
                    }
                    Debug.Log($"Remote data loaded and ready (last fetch time {info.FetchTime}).");
                    onFetchAndActivateSuccessful?.Invoke();
                });
            }
            else
            {
                Debug.LogError($"LastFetchStatus not success: {info.LastFetchStatus}");
            }
        }
        
    }
}

