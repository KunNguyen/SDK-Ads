using JisSDKAds.Ads;
using UnityEngine;

namespace JisSDKAds.Ads.Settings
{
    /// <summary>
    /// Legacy bootstrap — adds/configures <see cref="JisAds"/> on the same GameObject.
    /// Prefer attaching <see cref="JisAds"/> directly in new projects.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SdkAdsBootstrap : MonoBehaviour
    {
        [SerializeField] private JisSDKAdsSettings settings;
        [SerializeField] private bool useCoreForStandardFormats = true;
        [SerializeField] private bool autoInitializeOnStart = true;

        private void Awake()
        {
            var jisAds = GetComponent<JisAds>();
            if (jisAds == null)
                jisAds = gameObject.AddComponent<JisAds>();

            jisAds.Configure(settings, useCoreForStandardFormats, autoInitializeOnStart);
        }
    }
}
