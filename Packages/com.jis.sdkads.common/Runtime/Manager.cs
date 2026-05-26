using System;
using UnityEngine;

namespace JisSDKAds.Common
{
    /// <summary>
    /// Obsolete name kept so existing scenes/prefabs keep their script reference. Use <see cref="JisSDKPersistentRoot"/>.
    /// </summary>
    [Obsolete("Renamed to JisSDKPersistentRoot. Add JisSDKPersistentRoot on new JisSDK_Manager prefabs.")]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class Manager : JisSDKPersistentRoot
    {
    }
}
