#if UNITY_AD_MAX
using System;

namespace JisSDKAds.Providers.Max
{
    /// <summary>Re-opens the MAX CMP (Google UMP via MAX) for a user who already answered it.</summary>
    [UnityEngine.Scripting.Preserve] // loaded by reflection from ConsentFormBridge
    public static class MaxConsentForm
    {
        public static void ShowForExistingUser(Action<string> onError, Action onDismiss)
        {
            if (!MaxSdk.IsInitialized())
            {
                onError?.Invoke("MAX SDK not initialized.");
                return;
            }

            MaxSdk.CmpService.ShowCmpForExistingUser(error =>
            {
                if (error != null)
                    onError?.Invoke(error.Message);
                else
                    onDismiss?.Invoke();
            });
        }
    }
}
#endif
