using UnityEngine;

namespace JisSDKAds.Common
{
    /// <summary>
    /// When <see cref="JisSDKPersistentRoot"/> is on the scene root, children skip their own DontDestroyOnLoad
    /// so the full hierarchy stays intact across scene loads.
    /// </summary>
    public static class JisSDKPersistence
    {
        public static bool HasPersistentRoot(Transform transform)
        {
            if (transform == null) return false;
            var root = transform.root;
            return root.GetComponent<JisSDKPersistentRoot>() != null
                   || root.GetComponent<Manager>() != null;
        }

        public static void DontDestroyUnlessUnderPersistentRoot(GameObject gameObject)
        {
            if (gameObject == null) return;
            if (HasPersistentRoot(gameObject.transform)) return;
            Object.DontDestroyOnLoad(gameObject);
        }
    }
}
