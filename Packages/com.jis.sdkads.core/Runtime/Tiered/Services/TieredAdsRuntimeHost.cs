using System.Collections;
using UnityEngine;

namespace JisSDKAds.Core.Tiered.Services
{
    /// <summary>MonoBehaviour host for tiered scheduler coroutines. Created by JisAds extension layer.</summary>
    public class TieredAdsRuntimeHost : MonoBehaviour
    {
        public Coroutine StartHostCoroutine(IEnumerator routine) => StartCoroutine(routine);

        public void StopHostCoroutine(Coroutine routine)
        {
            if (routine != null)
                StopCoroutine(routine);
        }
    }
}
