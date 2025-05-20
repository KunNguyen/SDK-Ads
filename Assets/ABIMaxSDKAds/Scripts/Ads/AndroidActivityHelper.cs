using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class AndroidHelper
{
     public static AndroidJavaObject GetCurrentActivity()
     {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
#else
          Debug.LogError("CurrentActivity is only available on Android platform.");
          return null;
#endif
     }
}