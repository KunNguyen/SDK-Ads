namespace ABIMaxSDKAds.Scripts.Utils
{
     [System.Serializable]
     public class UUID
     {
          public string uuid;

          public static string Generate()
          {
               UUID newUuid = new UUID { uuid = System.Guid.NewGuid().ToString() };
               return newUuid.uuid;
          }
     }
}