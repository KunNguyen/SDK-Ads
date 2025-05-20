using UnityEngine;

namespace ABIMaxSDKAds.Scripts
{
     public static class DebugAds
     {
          public static void Log(string message)
          {
               if (!IsDebugMode())
                    return;
               Debug.Log(message);
          }
          public static void LogError(string message)
          {
               if (!IsDebugMode())
                    return;
               Debug.LogError(message);
          }
          public static void LogWarning(string message)
          {
               if (!IsDebugMode())
                    return;
               Debug.LogWarning(message);
          }
          public static void LogException(string message)
          {
               if (!IsDebugMode())
                    return;
               Debug.LogException(new System.Exception(message));
          }
          public static void LogException(System.Exception exception)
          {    
               if (!IsDebugMode())
                    return;
               Debug.LogException(exception);
          }
          public static void LogException(string message, System.Exception exception)
          {
               if (!IsDebugMode())
                    return;
               Debug.LogException(new System.Exception(message, exception));
          }
          public static void LogFormat(string format, params object[] args)
          {
               if (!IsDebugMode())
                    return;
               Debug.LogFormat(format, args);
          }
          private static bool IsDebugMode()
          {
#if DEBUG_ADS
                    return true;
#else
               return false;
#endif
          }
     }
}