#if SOLARENGINE_CustomEditor&&(!UNITY_EDITOR||SE_DEV)
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SolarEngine.MiniGames.info;
using SolarEngine.MiniGames.Utils;

using UnityEngine;

namespace SolarEngine.Platform
{
    public class CustomEditorAdapter : SEAdapterInterface
    {
       
        public SEDeviceInfo setDeviceInfo()
        {
         
            SEDeviceInfo seDeviceInfo = new SEDeviceInfo();
          
            return seDeviceInfo;

        }





        public void saveData(string key, object value)
        {
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
        }
        public bool  hasKey(string key)
        {
           return  UnityEngine.PlayerPrefs.HasKey(key);
        }

   
    
        public object getData(string key, Type type)
        {
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
        }

        public void deleteData(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {

                if (UnityEngine.PlayerPrefs.HasKey(key))
                {
                   UnityEngine.PlayerPrefs.DeleteKey(key);
                }

            }
        }

        public void deleteAll()
        {
           UnityEngine.PlayerPrefs.DeleteAll();
        }


        public EnterOptionsInfo getEnterOptionsInfo()
        {
         
            EnterOptionsInfo enterOptionsInfo = new EnterOptionsInfo();
         
            LogTool.DebugLog(JsonConvert.SerializeObject(enterOptionsInfo));
            return enterOptionsInfo;
        }

        public void login(SEAdapterInterface.OnLoginSuccessCallback successCallback,
            SEAdapterInterface.OnLoginFailedCallback failedCallback, bool forceLogin = true)
        {


        }

        public void triggerOnShow(SEAdapterInterface.OnShowEvent showEvent)
        {
          

        }

        public void triggerOnHide(SEAdapterInterface.OnHideEvent hideEvent)
        {
        ;
        }

        public string getmptype()
        {
            return "douyin";
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