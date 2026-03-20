#if GOOGLE_REVIEW && UNITY_ANDROID
using Google.Play.Review;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppReviewManager : MonoBehaviour {
    private static AppReviewManager m_Instance;
    public static AppReviewManager Instance {
        get {
            return m_Instance;
        }
    }
#if GOOGLE_REVIEW && UNITY_ANDROID
    private ReviewManager m_ReviewManager;
    private PlayReviewInfo m_PlayerReviewInfo = null;

    private void Awake() {
        m_Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        m_ReviewManager = new ReviewManager();
    }
    public void StartRequestReviewInfo() {
        m_PlayerReviewInfo = null;
        StartCoroutine(coRequestReviewInfo());
    }
    IEnumerator coRequestReviewInfo() {
        var requestFlowOperation = m_ReviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        if (requestFlowOperation.Error != ReviewErrorCode.NoError) {
            // Log error. For example, using requestFlowOperation.Error.ToString().
            yield break;
        }
        m_PlayerReviewInfo = requestFlowOperation.GetResult();
    }
    public bool IsReviewReady() {
        return m_PlayerReviewInfo != null;
    }
    public void ShowReview() {
        StartCoroutine(coShowReview());
    }
    IEnumerator coShowReview() {
        var launchFlowOperation = m_ReviewManager.LaunchReviewFlow(m_PlayerReviewInfo);
        yield return launchFlowOperation;
        m_PlayerReviewInfo = null; // Reset the object
        if (launchFlowOperation.Error != ReviewErrorCode.NoError) {
            // Log error. For example, using requestFlowOperation.Error.ToString().
            yield break;
        }
        // The flow has finished. The API does not indicate whether the user
        // reviewed or not, or even whether the review dialog was shown. Thus, no
        // matter the result, we continue our app flow.
    }
#endif
    
#if UNITY_IOS
    
    public void RequestIOSReview() {
        
    }
#endif
}