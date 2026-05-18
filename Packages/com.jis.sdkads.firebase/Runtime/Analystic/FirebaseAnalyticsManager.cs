using System;
using UnityEngine;
using Firebase.Analytics;
namespace JisSDKAds.Firebase
{
    public class FirebaseAnalyticsManager
    {
        public void LogEvent(string eventName, string parameterName, int value)
        {
            try
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, value);
            }
            catch (System.Exception)
            {
            }
        }
        public void LogEvent(string eventName, string parameterName, double value)
        {
            try
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, value);
            }
            catch (System.Exception)
            {
            }
        }
        public void LogEvent(string eventName, Parameter[] param)
        {
            try
            {
                FirebaseAnalytics.LogEvent(eventName, param);
            }
            catch (System.Exception ex)
            {
                Debug.Log("EX " + ex.Message);
            }
        }
        public void LogEvent(string eventName)
        {
            try
            {
                FirebaseAnalytics.LogEvent(eventName);
            }
            catch (System.Exception ex)
            {
                Debug.Log("EX " + ex.Message);
            }
        }
        public void SetUserProperty(string propertyName, string property)
        {
            try
            {
                FirebaseAnalytics.SetUserProperty(propertyName, property);
            }
            catch (System.Exception)
            {
            }
        }
        public void LogPurchase(double amount, string currencyCode)
        {
            try
            {
                Parameter[] bundle = new Parameter[] {
                new Parameter(FirebaseAnalytics.ParameterCurrency, currencyCode),
                new Parameter(FirebaseAnalytics.ParameterValue, amount),
            };
                //FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventEcommercePurchase, bundle);
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase, bundle);
            }
            catch
            {
            }
        }
    }
}

