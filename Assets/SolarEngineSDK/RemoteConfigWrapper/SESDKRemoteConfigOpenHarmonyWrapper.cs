#if UNITY_OPENHARMONY&&!UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
// using SolarEngine.Sample;
using AOT;

namespace SolarEngine
{
    public partial class SESDKRemoteConfig : MonoBehaviour
    {

        private static readonly OpenHarmonyJSClass seopenHarmonyRemoteConfigJSClass =
            new OpenHarmonyJSClass("SERCOpenHarmonyProxy");
        
        private static Action<string> newfetchRemoteConfigCallback_private;
        private static Action<Dictionary<string, object>> newfetchAllRemoteConfigCallback_private;
        

        private void SESDKSetRemoteDefaultConfig(Item[] defaultConfig)
        {

            if (defaultConfig == null)
            {
                return;
            }

            string defaultConfigJSONString = JsonConvert.SerializeObject(defaultConfig);

            seopenHarmonyRemoteConfigJSClass.CallStatic("setRemoteDefaultConfig", defaultConfigJSONString);


        }


        private void SESDKSetRemoteConfigEventProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }

            string propertiesJSONString = JsonConvert.SerializeObject(properties);

            seopenHarmonyRemoteConfigJSClass.CallStatic("setRemoteConfigEventProperties", propertiesJSONString);


        }

        private void SESDKSetRemoteConfigUserProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }

            string propertiesJSONString = JsonConvert.SerializeObject(properties);

            seopenHarmonyRemoteConfigJSClass.CallStatic("setRemoteConfigUserProperties", propertiesJSONString);


        }


        private void SESDKFastFetchRemoteConfig(string key,Action<string> callback)
        {
           
            newfetchRemoteConfigCallback_private=callback;
            OpenHarmonyJSCallback  calllback = new OpenHarmonyJSCallback(fastFetchRemoteConfigCallback);
            seopenHarmonyRemoteConfigJSClass.CallStatic("fastFetchRemoteConfig",key, calllback);        

        }

        private void SESDKFastFetchAllRemoteConfig(Action<Dictionary<string, object>>callback)
        {

            newfetchAllRemoteConfigCallback_private=callback;
            OpenHarmonyJSCallback  calllback = new OpenHarmonyJSCallback( fastFetchAllRemoteConfigCallback);
            seopenHarmonyRemoteConfigJSClass.CallStatic("fastAllFetchRemoteConfig", calllback);        


        }

        private void SESDKAsyncFetchAllRemoteConfig()
        {

            OpenHarmonyJSCallback  calllback = new OpenHarmonyJSCallback(asyncFetchAllRemoteConfigCallback);
            seopenHarmonyRemoteConfigJSClass.CallStatic("asyncAllFetchRemoteConfig", calllback);        

        }

        private void SESDKAsyncFetchRemoteConfig(string key)
        {

            OpenHarmonyJSCallback  calllback = new OpenHarmonyJSCallback(asyncFetchRemoteConfigCallback);
            seopenHarmonyRemoteConfigJSClass.CallStatic("asyncFetchRemoteConfig",key, calllback);        

        }

        private static object asyncFetchRemoteConfigCallback(params OpenHarmonyJSObject[] args)
        {
          
            if (args.Length > 0)
            {
              
                string value = args[0].As<string>();
                Debug.Log("asyncFetchRemoteConfigCallback  value: " + value);
              
                OnFetchRemoteConfigCallback(value);
            }

            return true;
        }
        private static object fastFetchRemoteConfigCallback(params OpenHarmonyJSObject[] args)
        {
          
            if (args.Length > 0)
            {
              
                string value = args[0].As<string>();
                Debug.Log("fastFetchRemoteConfigCallback  value: " + value);
              
                newfetchRemoteConfigCallback_private?.Invoke(value);
            }

            return true;
        }

        
        
        private static object fastFetchAllRemoteConfigCallback(params OpenHarmonyJSObject[] args)
        {
          
            if (args.Length > 0)
            {
              
                string value = args[0].As<string>();
                Debug.Log("fastFetchAllRemoteConfigCallback  value: " + value);
              
                Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);

                if(dict != null){
                
                    newfetchAllRemoteConfigCallback_private?.Invoke(dict);
                }
            }

            return true;
        }
        private static object asyncFetchAllRemoteConfigCallback(params OpenHarmonyJSObject[] args)
        {
          
            if (args.Length > 0)
            {
              
                string value = args[0].As<string>();
                Debug.Log("asyncFetchRemoteConfigCallback  value: " + value);
              
                Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
                if(dict != null){
                    OnFetchAllRemoteConfigCallback(dict);
                }
            }

            return true;
        }



    }


}
#endif