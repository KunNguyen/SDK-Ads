using UnityEngine;

/// <summary>
/// Place on the JIS SDK scene root (e.g. JisSDK_Manager) so the whole hierarchy survives scene loads.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class Manager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
} 