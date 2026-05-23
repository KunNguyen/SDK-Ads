#if SOLARENGINE_WECHAT&&(!UNITY_EDITOR||SE_DEV)

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SolarEngine.MiniGames.Utils;
using UnityEngine;

namespace SolarEngine.Platform{

public class TencentAdvertisingGameSDK: TenCentAdInterface
{
#if !UNITY_IPHONE||!UNITY_ANDROID


    [DllImport("__Internal")]
    private static extern void _init(string tencentAdvertisingGameSDKInitParams);
    [DllImport("__Internal")]
    private static extern void _setOpenId(string openid);
    [DllImport("__Internal")]
    private static extern void _setUnionId(string openid);
    [DllImport("__Internal")]
    private static extern void _onPurchase(double purchaseValue);
    [DllImport("__Internal")]
    private static extern void _onRegister();
    [DllImport("__Internal")]
    private static extern void _onReActive(int backFlowDay,string customProperties);
    [DllImport("__Internal")]
    private static extern void _onAddToWishlist(string type,string customProperties );
    [DllImport("__Internal")]
    private static extern void _onShare(string target,string customProperties);
    [DllImport("__Internal")]
    private static extern void _onCreateRole(string roleName);
    [DllImport("__Internal")]
    private static extern void _onTutorialFinish();
    [DllImport("__Internal")]
    private static extern void _onUpdateLevel(int level, string customProperties);
    [DllImport("__Internal")]
    private static extern void _onViewContent(string item,string customProperties);
    
    [DllImport("__Internal")]
    private static extern void _onAppStart();
    [DllImport("__Internal")]
    private static extern void _setDebug();
        
#endif
    
    public  void init(string  initParams)
    {
      
     
 #if UNITY_EDITOR
       Debug.Log("init");
           return;
 #elif !UNITY_IPHONE||!UNITY_ANDROID
        _init(initParams);
#endif
        
      
    }
    public  void setOpenId(string openid)
    {
#if UNITY_EDITOR
       Debug.Log("setOpenId");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID
        _setOpenId(openid);
#endif
        
    }
    public  void setUnionId(string unionid)
    {
#if UNITY_EDITOR
       Debug.Log("setUnionId");
           return;
#elif !UNITY_IPHONE||!UNITY_ANDROID     
        _setUnionId(unionid);
#endif    
      
    }



    public void onViewContentActivity(string item, Dictionary<string, object> customProperties)
    {
        throw new System.NotImplementedException();
    }

    public  void onAppStart()
    {
#if UNITY_EDITOR
       Debug.Log("_onAppStart");
           return;
#elif !UNITY_IPHONE||!UNITY_ANDROID    
        _onAppStart();
#endif  
      
    }
    public  void onPurchase(double purchaseValue)
    {
#if UNITY_EDITOR
       Debug.Log("onPurchase");
           return;
#elif !UNITY_IPHONE||!UNITY_ANDROID    
        _onPurchase(purchaseValue);
#endif  
        
    }
    public  void onRegister()
    {
#if UNITY_EDITOR
       Debug.Log("onRegister");
           return;
#elif !UNITY_IPHONE||!UNITY_ANDROID      
       _onRegister();
#endif  
      
    }
    public  void onReActive(int backFlowDay,Dictionary<string,object> customProperties)
    {
       
#if UNITY_EDITOR
          Debug.Log("onReActive");
           return;
#elif !UNITY_IPHONE||!UNITY_ANDROID      
        string customPropertiesJson = JsonConvert.SerializeObject(customProperties);
        _onReActive(backFlowDay,customPropertiesJson);
#endif
      
    }
    public  void onAddToWishlist(string type,Dictionary<string,object> customProperties)
    {
       
#if UNITY_EDITOR
        Debug.Log("onAddToWishlist");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID     
        string customPropertiesJson = JsonConvert.SerializeObject(customProperties);
        _onAddToWishlist(type,customPropertiesJson);
#endif
       
    }
    public  void onShare(string target,Dictionary<string,object> customProperties)
    {
#if UNITY_EDITOR
        Debug.Log("onShare");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID      
        string customPropertiesJson = JsonConvert.SerializeObject(customProperties);
        
        _onShare(target,customPropertiesJson);
#endif
      
       
      
    }
    public  void onCreateRole(string roleName)
    {
#if UNITY_EDITOR
        Debug.Log("onCreateRole");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID      
       _onCreateRole(roleName);
#endif
       
    }
    public  void onTutorialFinish()
    {
#if UNITY_EDITOR
        Debug.Log("onTutorialFinish");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID      
        _onTutorialFinish();
#endif
       
    }

  

    public  void onUpdateLevel(int level,Dictionary<string ,object> customProperties)
    {
#if UNITY_EDITOR
        Debug.Log("onUpdateLevel");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID     
        string customPropertiesJson = JsonConvert.SerializeObject(customProperties);
        _onUpdateLevel(level,customPropertiesJson);
#endif
      
    }
    public  void onViewContentMall(Dictionary<string,object> customProperties)
    {
#if UNITY_EDITOR
        Debug.Log("onViewContentMall");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID   
        string customPropertiesJson = JsonConvert.SerializeObject(customProperties);
        _onViewContent("Mall",customPropertiesJson);
#endif
     
    }
    public  void onViewContentActivity(Dictionary<string,object> customProperties)
    {
#if UNITY_EDITOR
        Debug.Log("onViewContentActivity");
        return;
#elif !UNITY_IPHONE||!UNITY_ANDROID       
        string customPropertiesJson = JsonConvert.SerializeObject(customProperties);
        _onViewContent("Activity",customPropertiesJson);
#endif
        
      
        
        
      
    }

    public  void setDebug()
    {
#if UNITY_EDITOR
        Debug.Log("setDebug");
        
#elif !UNITY_IPHONE||!UNITY_ANDROID 
        _setDebug();
#endif
    }

}
}
#endif