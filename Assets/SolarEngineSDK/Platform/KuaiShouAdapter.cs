#if SOLARENGINE_KUAISHOU&&(!UNITY_EDITOR||SE_DEV)

using System;
using System.Collections.Generic;
using KSWASM;
using SolarEngine.MiniGames.info;
using SolarEngine.Platform;
using UnityEngine;


public class KuaiShouAdapter : SEAdapterInterface
{
    public SEDeviceInfo setDeviceInfo()
    {
       
       
        
#if UNITY_EDITOR
        return new SEDeviceInfo();
#endif
        var info = KS.GetSystemInfoSync();
        
        string[] tempSys = (info.system?.Split(' ')) ?? new string[0]; // 最细粒度的系统版本号
        SEDeviceInfo seDeviceInfo = new SEDeviceInfo
        {
            _manufacturer = info.brand,
            _device_model = info.model,
            _device_type = AdapterTool.getDeviceType(info.platform),
            _platform = AdapterTool.getPlatform(info.platform),
            _os_version = tempSys.Length > 1 ? tempSys[1] : info.system,
            _screen_height =(int) info.screenHeight,
            _screen_width =(int) info.screenWidth,
            _language = info.language,
            //App 的版本，小程序下为宿主 App 版本号（宿主指微信、抖音、今日头条等）
            _app_version = info.version,
            //小程序客户端基础库版本
            _mp_version = "",
            _browser_version = info.version,
            _app_name = info.host.appId,
            _browser = ""
        };
        return seDeviceInfo;

    }

  
    public void saveData(string key, object value)
    {
 #if UNITY_EDITOR
        if (value.GetType() == typeof(int))
        {
      
            UnityEngine.PlayerPrefs.SetInt(key, (int)value);
        }
        else if (value.GetType() == typeof(float))
        {

            UnityEngine.PlayerPrefs.SetFloat(key, (float)value);
        }
        else if (value.GetType() == typeof(string))
        {

            UnityEngine.PlayerPrefs.SetString(key, (string)value);
        }

        UnityEngine.PlayerPrefs.Save();
return;
        #endif
        if (value.GetType() == typeof(int))
        {
            
            KS.StorageSetIntSync(key, (int)value);
      
        }
        else if (value.GetType() == typeof(float))
        {

            KS.StorageSetFloatSync(key, (float)value);
        }
        else if (value.GetType() == typeof(string))
        {

            KS.StorageSetStringSync(key, (string)value);
        }

    }

    public bool hasKey(string key)
    {
       
#if UNITY_EDITOR
        return  UnityEngine.PlayerPrefs.HasKey(key);
#endif
        return  KS.StorageHasKeySync(key);
    }

    public object getData(string key, Type type)
    {
  
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(key) && UnityEngine.PlayerPrefs.HasKey(key))
        {

            if (type == typeof(int))
            {
                return UnityEngine.PlayerPrefs.GetInt(key);
            }
            else if (type == typeof(float))
            {
                return UnityEngine.PlayerPrefs.GetFloat(key);
            }
            else if (type == typeof(string))
            {
                return UnityEngine.PlayerPrefs.GetString(key);
            }

            UnityEngine.PlayerPrefs.Save();
        }

        return null;
#endif
        if (!string.IsNullOrEmpty(key) && KS.StorageHasKeySync(key))
        {

          
                if (type == typeof(int))
                {
                   int value= KS.StorageGetIntSync(key,999);
             
                    return value;
                }
                else if (type == typeof(float))
                {
                    float value= KS.StorageGetFloatSync(key,999);
                 
                    return value;
                }
                else if (type == typeof(string))
                {
                    string value= KS.StorageGetStringSync(key,null);
               
                    return value;
                }
            
            }
        

        return null;
        
        }

    public void deleteData(string key)
    {
#if UNITY_EDITOR
        if (UnityEngine. PlayerPrefs.HasKey(key))
        {
            UnityEngine.PlayerPrefs.DeleteKey(key);
        }
        return;
#endif
        if (!string.IsNullOrEmpty(key))
        {

            if (KS.StorageHasKeySync(key))
            {
                KS.StorageDeleteKeySync(key);
            }

        }
    }

    public void deleteAll()
    {
#if UNITY_EDITOR
        UnityEngine. PlayerPrefs.DeleteAll();
        return;
#endif
       KS.StorageDeleteAllSync();
    }

    public EnterOptionsInfo getEnterOptionsInfo()
    {
#if UNITY_EDITOR
        return new EnterOptionsInfo();
#endif
        var info = KS.GetLaunchOptionsSync();
        Debug.Log("getEnterOptionsInfo:" + info);
        return new EnterOptionsInfo
        {
            _path = "",
            _query_info = info.query,
            _launch_scene = info.from
        };
    }

    public void login(SEAdapterInterface.OnLoginSuccessCallback successCallback, SEAdapterInterface.OnLoginFailedCallback failedCallback, bool forceLogin = true)
    {
      
      
#if UNITY_EDITOR
        return ;
#endif
        var option = new LoginOption
        {
            success = (LoginSuccessCallbackResult result) =>
            {
                Debug.Log("login success:code" + result.code );
                Debug.Log("login success:errMsg" + result.errMsg);
                successCallback?.Invoke(result.code, "", true);
            },

            fail = (KSGeneralCallbackResult err) =>
            {
                Debug.Log("login failed:" + err);
                failedCallback?.Invoke(err.msg);
            },

            complete = (GeneralCallbackResult completeResult) =>
            {
                Debug.Log("completeResult）");
            },

           
        };

      
        KS.Login( option);
      
    }

    public void triggerOnShow(SEAdapterInterface.OnShowEvent showEvent)
    {
      
     
  #if UNITY_EDITOR
        return;
#endif
        KS.OnShow(result =>
        {
            showEvent?.Invoke("", result.query, new ());
        });
     
    }

    public void triggerOnHide(SEAdapterInterface.OnHideEvent hideEvent)
    {
       
#if UNITY_EDITOR
        return;
#endif
        KS.OnHide(() =>
        {
            hideEvent?.Invoke();
        });
     
        
      
    }

    public string getmptype()
    {
        return "kwai";
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

#endif