using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace JisSDKAds.Common
{
     [ScriptOrder(-10)]
     public class InternetChecker : MonoBehaviour
     {
          private static InternetChecker instance;
          public static InternetChecker Instance => instance;
          [field: SerializeField] public bool IsInternetAvailable { get; set; }

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
          [field: SerializeField] public bool IsChecking { get; private set; }
          [field: SerializeField] public bool IsActive { get; set; }

          private void Awake()
          {
               if (instance == null)
               {
                    instance = this;
                    DontDestroyOnLoad(gameObject);
               }
               else
               {
                    DestroyImmediate(gameObject);
               }
          }

          public void StartCheckInternet()
          {
               IsActive = true;
               CheckInternet();
          }

          public void StopCheckInternet()
          {
               IsActive = false;
               IsChecking = false;
               if (internetCheckCoroutine != null)
               {
                    StopCoroutine(internetCheckCoroutine);
                    internetCheckCoroutine = null;
               }
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
               while (IsActive)
               {
                    yield return CheckInternetMultipleUrls();

                    if (IsInternetAvailable)
                    {
                         DebugAds.Log("🌐 Internet is available");
                    }
                    else
                    {
                         DebugAds.LogError("🚫 No internet connection.");
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
                    DebugAds.LogError("🚫 No network connectivity detected.");
                    yield break; // Exit early if no network is reachable
               }

               IsInternetAvailable = false;

               foreach (string url in testUrls)
               {
                    using var request = UnityWebRequest.Head(url);
                    request.timeout = 5;
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.ConnectionError &&
                        request.result != UnityWebRequest.Result.ProtocolError)
                    {
                         IsInternetAvailable = true;
                         DebugAds.Log($"✅ Connected successfully to: {url}");
                         break;
                    }
                    else
                    {
                         DebugAds.LogWarning($"❌ Cannot connect to: {url}");
                    }
               }
          }
     }
}