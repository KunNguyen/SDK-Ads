using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ABIMaxSDKAds.Scripts.Utils
{
    public class InternetChecker : MonoBehaviour
    {
        private static InternetChecker instance;
        public static InternetChecker Instance => instance;
        [field: SerializeField] public bool IsInternetAvailable
        {
            get;
            set;
        }

        [field: SerializeField]
        private List<string> testUrls { get; set; } = new List<string>
        {
            "https://clients3.google.com/generate_204", // Mỹ
            "https://www.apple.com/library/test/success.html", // Mỹ
            "https://www.cloudflare.com/cdn-cgi/trace", // Châu Âu / Trung Đông
            "https://www.google.com", // Đông Nam Á
            "https://www.microsoft.com", // Toàn cầu
            "https://yandex.com", // Đông Âu
            "https://bing.com"
        };

        private Coroutine internetCheckCoroutine;
        public bool IsChecking { get; private set; }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            CheckInternet();
        }

        public void CheckInternet()
        {
            if (internetCheckCoroutine != null)
            {
                StopCoroutine(internetCheckCoroutine);
            }

            IsChecking = true;
            internetCheckCoroutine = StartCoroutine(CheckInternetRepeatedly());
        }

        private IEnumerator CheckInternetRepeatedly()
        {
            while (true)
            {
                yield return CheckInternetMultipleUrls();

                if (IsInternetAvailable)
                {
                    Debug.Log("🌐 Internet is available");
                }
                else
                {
                    Debug.LogError("🚫 No internet connection.");
                }
                IsChecking = false;
                yield return new WaitForSeconds(30);
            }
        }

        private IEnumerator CheckInternetMultipleUrls()
        {
            // Preliminary check using Application.internetReachability
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                IsInternetAvailable = false;
                Debug.LogError("🚫 No network connectivity detected.");
                yield break; // Exit early if no network is reachable
            }

            IsInternetAvailable = false;

            foreach (string url in testUrls)
            {
                using (UnityWebRequest request = UnityWebRequest.Head(url))
                {
                    request.timeout = 5;
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.ConnectionError &&
                        request.result != UnityWebRequest.Result.ProtocolError)
                    {
                        IsInternetAvailable = true;
                        Debug.Log($"✅ Connected successfully to: {url}");
                        break;
                    }
                    else
                    {
                        Debug.LogWarning($"❌ Cannot connect to: {url}");
                    }
                }
            }
        }
    }
}