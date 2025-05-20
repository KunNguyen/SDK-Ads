using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Firebase.RemoteConfig;
using System.Threading.Tasks;
using Firebase.Extensions;

namespace SDK
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
                    };

            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            remoteConfig.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task =>
            {
                FetchRemoteConfig(onFetchAndActivateSuccessful);
            });
        }
        
        public ConfigValue GetValues(string key)
        {
            return FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        }

        public void FetchRemoteConfig(System.Action onFetchAndActivateSuccessful)
        {
            if (FirebaseManager.Instance.FirebaseApp == null)
            {
                return;
            }
            Debug.Log("Fetching data...");
            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            remoteConfig.FetchAsync(System.TimeSpan.Zero).ContinueWithOnMainThread(
                previousTask=>
                {
                    if (!previousTask.IsCompleted)
                    {
                        Debug.LogError($"{nameof(remoteConfig.FetchAsync)} incomplete: Status '{previousTask.Status}'");
                        return;
                    }
                    ActivateRetrievedRemoteConfigValues(onFetchAndActivateSuccessful);
                });
            
        }

        private void ActivateRetrievedRemoteConfigValues(System.Action onFetchAndActivateSuccessful)
        {
            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            ConfigInfo info = remoteConfig.Info;
            if(info.LastFetchStatus == LastFetchStatus.Success)
            {
                remoteConfig.ActivateAsync().ContinueWithOnMainThread(
                    previousTask =>
                    {
                        Debug.Log($"Remote data loaded and ready (last fetch time {info.FetchTime}).");
                        onFetchAndActivateSuccessful();
                    });
            }
        }
        
    }
}

