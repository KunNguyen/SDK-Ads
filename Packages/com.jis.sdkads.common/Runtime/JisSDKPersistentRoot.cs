using UnityEngine;

namespace JisSDKAds.Common
{
    /// <summary>
    /// Attach to the JIS SDK ads scene root (<c>JisSDK_Manager</c>) so the full hierarchy survives scene loads.
    /// Not used on <c>JisSDK_InAppPurchaser</c> (IAP has its own singleton + DontDestroyOnLoad).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("JIS SDK/Jis SDK Persistent Root")]
    [DefaultExecutionOrder(-1000)]
    public class JisSDKPersistentRoot : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
