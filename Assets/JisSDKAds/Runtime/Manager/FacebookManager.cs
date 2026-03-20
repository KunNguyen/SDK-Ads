using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Facebook.Unity;

namespace SDK
{
    [ScriptOrder(-100)]
    public class FacebookManager : MonoBehaviour
    {
        // TODO: +++++ Change AppName In FacebookSetting
        private string m_FacebookID;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            //if (!FB.IsInitialized)
            //{
            //    // Initialize the Facebook SDK
            //    FB.Init(InitCallback, OnHideUnity);
            //}
            //else
            //{
            //    // Already initialized, signal an app activation App Event
            //    FB.ActivateApp();
            //}
        }
        private void InitCallback()
        {
            //if (FB.IsInitialized)
            //{
            //    // Signal an app activation App Event
            //    FB.ActivateApp();
            //    Debug.Log("INIT success");
            //    if (FB.IsLoggedIn)
            //    {
            //        Debug.Log("FB is LoggedIn");
            //        Authenticate(false);
            //    }
            //}
            //else
            //{
            //    Debug.Log("Failed to Initialize the Facebook SDK");
            //}
        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            }
            else
            {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        }
        void Authenticate(bool showLoading = true)
        {
            if (showLoading)
            {
                //ShowFBLoading();
            }
            //m_FacebookID = Facebook.Unity.AccessToken.CurrentAccessToken.UserId;
            //GetMyInfo();
        }
    }
}

