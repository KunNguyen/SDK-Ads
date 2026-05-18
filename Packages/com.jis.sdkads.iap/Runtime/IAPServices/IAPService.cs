#if UNITY_IAP_ACTIVE
using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

namespace JisSDKAds.IAP
{
    static class IAPService
    {
        const string k_ProductionEnvironment = "production";
        const string k_DevelopmentEnvironment = "development";

        public static async Task InitializeAsync(Action onSuccess, Action<string> onFailed, string environmentName = null)
        {
            var env =
                string.IsNullOrEmpty(environmentName)
                    ? (UnityEngine.Debug.isDebugBuild ? k_DevelopmentEnvironment : k_ProductionEnvironment)
                    : environmentName;

            var options = new InitializationOptions().SetEnvironmentName(env);
            try
            {
                await UnityServices.InitializeAsync(options);
                onSuccess?.Invoke();
            }
            catch (Exception e)
            {
                onFailed?.Invoke(e.Message);
            }
        }
    }
}
#endif
