#if (SOLARENGINE_BYTEDANCE||SOLARENGINE_WECHAT||SOLARENGINE_KUAISHOU||SOLARENGINE_BYTEDANCE_CLOUD||SOLARENGINE_BYTEDANCE_STARK)&&(!UNITY_EDITOR||SE_DEV||SOLORENGINE_DEVELOPEREDITOR)&&!SE_MINI_DIS_RC
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using AOT;
using SolarEngine.MiniGameRemoteConfig;
using SolarEngine.MiniGameRemoteConfig.Info;

namespace SolarEngine
{
    public partial class SESDKRemoteConfig : MonoBehaviour
    {
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initializes()
        {

#if SOLARENGINE_WECHAT
            SEAdapterInterface _adapter = new SolarEngine.Platform. WeChatAdapter();
                MiniRemoteConfigInfo.setAdapterInterface(_adapter);
#elif SOLARENGINE_BYTEDANCE
                SEAdapterInterface _adapter = new SolarEngine.Platform.ByteDanceAdapter();
                
                MiniRemoteConfigInfo .setAdapterInterface(_adapter);
#elif SOLARENGINE_KUAISHOU
                SEAdapterInterface _adapter = new  KuaiShouAdapter();
                MiniRemoteConfigInfo.setAdapterInterface(_adapter);
#elif SOLARENGINE_BYTEDANCE_CLOUD
                SEAdapterInterface _adapter = new SolarEngine.Platform.ByteDanceStarkSDKAdapter();MiniRemoteConfigInfo.setAdapterInterface(_adapter);
                MiniRemoteConfigInfo.setAdapterInterface(_adapter);
#elif SOLARENGINE_BYTEDANCE_STARK
            SEAdapterInterface _adapter = new SolarEngine.Platform.ByteDanceStarkSDKAdapter();
            MiniRemoteConfigInfo.setAdapterInterface(_adapter);
                
#endif

        }

        private void init(SERemoteConfigInterface se)
        {
            MiniRemoteConfigWrapper.Instance.init(se);
               
        }
   
        private void SESDKSetRemoteDefaultConfig(SESDKRemoteConfig.Item[] defaultConfig)
        {

            if (defaultConfig == null)
            {
                return;
            }
            foreach (var item in defaultConfig)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                
                dic.Add("name", item.name);
             
                if(item.type==4)
                    dic.Add("value",JsonConvert.SerializeObject(item.value));
                else
                    dic.Add("value", item.value);
                
                dic.Add("type", item.type);
                
                MiniRemoteConfigWrapper.Instance.setRemoteDefaultConfig(dic);
            }
          
        }


        private void SESDKSetRemoteConfigEventProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }
            MiniRemoteConfigWrapper.Instance.setRemoteConfigEventProperties(properties);
            

        }

        private void SESDKSetRemoteConfigUserProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }
            MiniRemoteConfigWrapper.Instance.setRemoteConfigUserProperties(properties);
    

        }

       
        private string SESDKFastFetchRemoteConfig(string key)
        {
            if (key == null)
            {
                return null;
            }
       
            return MiniRemoteConfigWrapper.Instance.fastFetchRemoteConfig(key);

        }

        private Dictionary<string, object> SESDKFastFetchAllRemoteConfig()
        {
       
            return MiniRemoteConfigWrapper.Instance.fastFetchRemoteConfig();
        }

        private void SESDKAsyncFetchAllRemoteConfig()
        {

            MiniRemoteConfigWrapper.MiniFetchAllRemoteConfigCallback _miniFetchAll=  (MiniRemoteConfigWrapper.MiniFetchAllRemoteConfigCallback)Delegate.CreateDelegate(typeof(MiniRemoteConfigWrapper.MiniFetchAllRemoteConfigCallback), SESDKRemoteConfig.Instance.fetchAllRemoteConfigCallback_private.Target, SESDKRemoteConfig.Instance.fetchAllRemoteConfigCallback_private.Method);
            MiniRemoteConfigWrapper.Instance.asyncFetchRemoteConfig(_miniFetchAll);
        }

        private void SESDKAsyncFetchRemoteConfig(string key)
        {
            if (key == null)
            {
                return;
            }
            MiniRemoteConfigWrapper.MiniFetchRemoteConfigCallback _miniFetch=  (MiniRemoteConfigWrapper.MiniFetchRemoteConfigCallback)Delegate.CreateDelegate(typeof(MiniRemoteConfigWrapper.MiniFetchRemoteConfigCallback), SESDKRemoteConfig.Instance.fetchRemoteConfigCallback_private.Target, SESDKRemoteConfig.Instance.fetchRemoteConfigCallback_private.Method);
          
            MiniRemoteConfigWrapper.Instance.asyncFetchRemoteConfig(key,_miniFetch);

        }


    }
}
#endif