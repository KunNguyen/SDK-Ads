#if (SOLARENGINE_BYTEDANCE||SOLARENGINE_WECHAT||SOLARENGINE_KUAISHOU||SOLARENGINE_BYTEDANCE_CLOUD||SOLARENGINE_BYTEDANCE_STARK)&&(!UNITY_EDITOR||SE_DEV||SOLORENGINE_DEVELOPEREDITOR)

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
#if !TUANJIE_2022_3_OR_NEWER
using System.Security.Cryptography;
#endif
using System.Text;
using Newtonsoft.Json;
using SolarEngine.MiniGames.info;
using SolarEngineHelper;
using UnityEngine;

namespace SolarEngine.Platform
{
    public class AdapterTool
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initializes()
        {
#if SOLARENGINE_WECHAT
             SEAdapterInterface _adapter = new SolarEngine.Platform. WeChatAdapter();
             SESDKInfo.setAdapterWapperInterface(_adapter);

             TenCentAdInterface ad = new SolarEngine.Platform.TencentAdvertisingGameSDK();
             SESDKInfo.setTengCentInterface(ad);

#elif SOLARENGINE_BYTEDANCE
                SEAdapterInterface _adapter = new  SolarEngine.Platform.ByteDanceAdapter();
                SESDKInfo.setAdapterWapperInterface(_adapter);
#elif SOLARENGINE_KUAISHOU
                SEAdapterInterface _adapter = new  KuaiShouAdapter();
                SESDKInfo.setAdapterWapperInterface(_adapter);
 #elif SOLARENGINE_BYTEDANCE_CLOUD
                SEAdapterInterface _adapter = new SolarEngine.Platform.ByteDanceStarkSDKAdapter();
                SESDKInfo.setAdapterWapperInterface(_adapter);
#elif  SOLARENGINE_BYTEDANCE_STARK
                SEAdapterInterface _adapter = new SolarEngine.Platform.ByteDanceStarkSDKAdapter();
                SESDKInfo.setAdapterWapperInterface(_adapter);
#endif

            

        }
        public static int getDeviceType(string platform)
        {
            if (string.IsNullOrEmpty(platform))
                return 0;
            Dictionary<string, int> platformMap = new Dictionary<string, int>
            {
                { "other", 0 },
                { "android phone", 1 },
                { "android pad", 2 },
                { "ios", 3 },
                { "ipad", 4 },
                { "mac", 5 },
                { "windows", 6 },
                { "devtools", 7 }
            };

            string matchingKey = platformMap.Keys.FirstOrDefault(key => key.IndexOf(platform) > -1) ?? "other";
            return platformMap.TryGetValue(matchingKey, out int deviceType) ? deviceType : 0;
        }

        public static int getPlatform(string platform)
        {
            if (string.IsNullOrEmpty(platform))
                return 0;
            Dictionary<string, int> platformMap = new Dictionary<string, int>
            {
                { "other", 0 },
                { "android", 1 },
                { "ios", 2 },
                { "windows", 3 },
                { "mac", 4 },
                { "devtools", 8 }
            };


            string matchingKey = platformMap.Keys.FirstOrDefault(key => key.IndexOf(platform) > -1) ?? "other";
            return platformMap.TryGetValue(matchingKey, out int deviceType) ? deviceType : 0;
        }
        

#if  TUANJIE_2022_3_OR_NEWER&&(TUANJIE_WEIXINMINIGAME||UNITY_WEBGL)

        [DllImport("__Internal")]
        public static extern string _createSign(string  reportData);
        
        [DllImport("__Internal")]
        public static extern string _createRequestSign(string  reportData);
#endif
        public static string createSign(Dictionary<string, object> data)
        {
           
#if  TUANJIE_2022_3_OR_NEWER&&(TUANJIE_WEIXINMINIGAME||UNITY_WEBGL)
         return   _createSign(JsonConvert.SerializeObject(data));
#else
          return  SolarEngineUnityTool.createSign(data);
#endif
        }

        public static string createRequestSign(Dictionary<string, object> data)
        {
#if  TUANJIE_2022_3_OR_NEWER&&(TUANJIE_WEIXINMINIGAME||UNITY_WEBGL)

            return _createRequestSign(JsonConvert.SerializeObject(data));
 #else 
            return SolarEngineUnityTool.createRequestSign(data);
#endif


        }

    }
}
#endif