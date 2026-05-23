#if SOLARENGINE_WECHAT&&(!UNITY_EDITOR||SE_DEV)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SolarEngine.MiniGames.info;
using SolarEngine.MiniGames.Utils;
using UnityEngine;
using WeChatWASM;

namespace SolarEngine.Platform
{
    public class WeChatAdapter : SEAdapterInterface
    {

        public SEDeviceInfo setDeviceInfo()
        {
            var sysInfo = WX.GetSystemInfoSync();

            if (sysInfo == null)
                return new SEDeviceInfo();
            LogTool.DebugLog("sysInfo" + JsonConvert.SerializeObject(sysInfo));
            string[] tempSys = (sysInfo.system?.Split(' ')) ?? new string[0]; // 最细粒度的系统版本号
            SEDeviceInfo seDeviceInfo = new SEDeviceInfo
            {
                _manufacturer = sysInfo.brand,
                _device_model = sysInfo.model,
                _device_type = AdapterTool.getDeviceType(sysInfo.platform),
                _platform = AdapterTool.getPlatform(sysInfo.platform),
                _os_version = tempSys.Length > 1 ? tempSys[1] : sysInfo.system,
                _screen_height = int.Parse(sysInfo.screenHeight.ToString()),
                _screen_width = int.Parse(sysInfo.screenWidth.ToString()),
                _language = sysInfo.language,
                //App 的版本，小程序下为宿主 App 版本号（宿主指微信、抖音、今日头条等）
                _app_version = sysInfo.version,
                //小程序客户端基础库版本
                _mp_version = sysInfo.SDKVersion,
                _browser_version = sysInfo.version,
                _app_name = "",
                _browser = ""
            };
            return seDeviceInfo;
        }

        public void saveData(string key, object value)
        {
            if (value.GetType() == typeof(int))
            {
                PlayerPrefs.SetInt(key, (int)value);
            }
            else if (value.GetType() == typeof(float))
            {
                PlayerPrefs.SetFloat(key, (float)value);
            }
            else if (value.GetType() == typeof(string))
            {
                PlayerPrefs.SetString(key, (string)value);
            }

            PlayerPrefs.Save();
        }

        public bool hasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public object getData(string key, Type type)
        {
            if (!string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key))
            {
                if (type == typeof(int))
                {
                    return PlayerPrefs.GetInt(key);
                }
                else if (type == typeof(float))
                {
                    return PlayerPrefs.GetFloat(key);
                }
                else if (type == typeof(string))
                {
                    return PlayerPrefs.GetString(key);
                }

                PlayerPrefs.Save();
            }

            return null;
        }

        public void deleteData(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        public void deleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        public EnterOptionsInfo getEnterOptionsInfo()
        {
         
            Dictionary<string, object> dic = new Dictionary<string, object>();

            LaunchOptionsGame launchOptionsSync = WX.GetLaunchOptionsSync();
            //  LogTool.DebugLog(JsonConvert.SerializeObject(launchOptionsSync));
#if UNITY_EDITOR
            launchOptionsSync = new LaunchOptionsGame();
            launchOptionsSync.scene = 123;
            launchOptionsSync.referrerInfo = new EnterOptionsGameReferrerInfo();
            launchOptionsSync.referrerInfo.extraData = new Dictionary<string, string>
            {
                { "extraData1", "value1" },
                { "extraData2", "value2" }
            };
            launchOptionsSync.referrerInfo.gameLiveInfo = new GameLiveInfo();
            launchOptionsSync.referrerInfo.gameLiveInfo.streamerOpenId = "teststreamerOpenId";
            launchOptionsSync.referrerInfo.gameLiveInfo.feedId = "testfeedId";

#endif

            string scene = launchOptionsSync.scene.ToString();
            dic.Add("appId", launchOptionsSync.referrerInfo.appId);
            dic.Add("extraData", launchOptionsSync.referrerInfo.extraData);
            dic.Add("gameLiveInfo", launchOptionsSync.referrerInfo.gameLiveInfo);

            // foreach (var VARIABLE in dic)
            // {
            //     Debug.LogWarning(VARIABLE);
            // }
            EnterOptionsInfo enterOptionsInfo = new EnterOptionsInfo
            {
                _launch_scene = (!string.IsNullOrEmpty(scene)) ? scene : "1000",
                // _path = !string.IsNullOrEmpty(launchOptionsSync.Path) ? launchOptionsSync.Path : "",

                _query_info = launchOptionsSync.query,
                _referrer_info = dic
            };

            LogTool.DebugLog(JsonConvert.SerializeObject(enterOptionsInfo));
            return enterOptionsInfo;
        }

        public void login(SEAdapterInterface.OnLoginSuccessCallback successCallback,
            SEAdapterInterface.OnLoginFailedCallback failedCallback, bool forceLogin = true)
        {
#if UNITY_EDITOR
            successCallback?.Invoke("", "", true);
            return;
#endif
            LoginOption login = new LoginOption();
            login.success = (e) => { successCallback?.Invoke(e.code, "", true); };
            login.fail = (e) => { failedCallback?.Invoke(e.errMsg); };
            WX.Login(login);
        }


        public void triggerOnShow(SEAdapterInterface.OnShowEvent showEvent)
        {
            WX.OnShow((e) =>
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("appId", e.referrerInfo.appId);

                dic.Add("extraData", e.referrerInfo.extraData);
                showEvent?.Invoke(e.scene.ToString(), e.query, dic);
            });
        }


        public void triggerOnHide(SEAdapterInterface.OnHideEvent hideEvent)
        {
            WX.OnHide((res) => { hideEvent?.Invoke(); });
        }

        public string getmptype()
        {
            return "wechat";
        }

        public string getsubmptype()
        {
            return "native";
        }

        public void init()
        {
        }

        public string createSign(Dictionary<string,object>data)
        {
            return AdapterTool.createSign(data);
        }

        public string createRequestSign(Dictionary<string,object>data)
        {
            return AdapterTool.createRequestSign(data);
        }

     
    
    }
}
#endif