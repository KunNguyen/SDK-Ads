using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Core.Interfaces
{
    /// <summary>
    /// ScriptableObject config that creates an IAdService.
    /// Implement in each provider assembly (Max, AdMob, ).
    /// </summary>
    public interface IAdProviderConfig
    {
        AdProviderId ProviderId { get; }
        IAdService CreateProvider();
    }
}
