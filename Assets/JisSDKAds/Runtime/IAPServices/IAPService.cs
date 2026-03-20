using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

namespace ABIMaxSDKAds.Scripts.IAPServices
{
    static class IAPService
    {
        const string k_ProductionEnvironment = "production";
        const string k_DevelopmentEnvironment = "development";

        public static async Task InitializeAsync(string environmentName = null)
        {
            var env =
                string.IsNullOrEmpty(environmentName)
                    ? (UnityEngine.Debug.isDebugBuild ? k_DevelopmentEnvironment : k_ProductionEnvironment)
                    : environmentName;

            var options = new InitializationOptions().SetEnvironmentName(env);

            await UnityServices.InitializeAsync(options);
        }
    }
}