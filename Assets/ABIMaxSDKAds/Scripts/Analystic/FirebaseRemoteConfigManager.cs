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
                        { ABI.Keys.key_remote_aoa_active, true },
                        { ABI.Keys.key_remote_ads_resume_capping_time, 10 },
                        { ABI.Keys.key_remote_aoa_show_first_time_active, false },
                        { ABI.Keys.key_remote_ads_resume_pause_time, 5 },
                        { ABI.Keys.key_remote_interstitial_level, 3 },
                        { ABI.Keys.key_remote_interstitial_capping_time, 25 },
                        { ABI.Keys.key_remote_inter_reward_interspersed, true },
                        { ABI.Keys.key_remote_inter_reward_interspersed_time, 10 },
                        { ABI.Keys.key_remote_mrec_active, false },
                        { ABI.Keys.key_remote_free_ads, 1 },
                        { ABI.Keys.key_remote_resume_ads_type, false },
                        { ABI.Keys.key_remote_ads_resume_ads_active, false }
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
            if (ABIFirebaseManager.Instance.FirebaseApp == null)
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

