#if UNITY_AD_ADMOB
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Ads
{
    /// <summary>
    /// Optional Google UMP (User Messaging Platform) via reflection — no compile-time dependency on GoogleMobileAds.Ump.
    /// </summary>
    internal static class AdmobUmpConsent
    {
        public const string PluginHint =
            "Install/update com.google.ads.mobile (Google Mobile Ads UPM) with User Messaging Platform support.";

        static bool _resolved;
        static bool _available;
        static Type _consentInformationType;
        static Type _consentFormType;
        static Type _formErrorType;
        static Type _consentRequestParametersType;
        static Type _consentDebugSettingsType;
        static Type _debugGeographyType;

        public static bool IsAvailable
        {
            get
            {
                EnsureResolved();
                return _available;
            }
        }

        static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;

            _consentInformationType = FindType("GoogleMobileAds.Ump.Api.ConsentInformation");
            _consentFormType = FindType("GoogleMobileAds.Ump.Api.ConsentForm");
            _formErrorType = FindType("GoogleMobileAds.Ump.Api.FormError");
            _consentRequestParametersType = FindType("GoogleMobileAds.Ump.Api.ConsentRequestParameters");
            _consentDebugSettingsType = FindType("GoogleMobileAds.Ump.Api.ConsentDebugSettings");
            _debugGeographyType = FindType("GoogleMobileAds.Ump.Api.DebugGeography");

            _available = _consentInformationType != null &&
                         _consentFormType != null &&
                         _formErrorType != null &&
                         _consentRequestParametersType != null;
        }

        static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }

            return null;
        }

        public static void RequestConsentAndInitialize(Action onReadyToInitAds, Action<string> onError)
        {
            if (!IsAvailable)
            {
                onError?.Invoke(PluginHint);
                return;
            }

            try
            {
                var request = BuildConsentRequestParameters();
                var update = _consentInformationType.GetMethod(
                    "Update",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { _consentRequestParametersType, typeof(Action<>).MakeGenericType(_formErrorType) },
                    null);

                if (update == null)
                {
                    onError?.Invoke("ConsentInformation.Update not found.");
                    return;
                }

                Action<object> onUpdated = error =>
                {
                    if (error != null)
                    {
                        onError?.Invoke(GetFormErrorMessage(error));
                        return;
                    }

                    LoadAndShowConsentFormIfRequired(onReadyToInitAds, onError);
                };

                update.Invoke(null, new[] { request, CreateFormErrorCallback(onUpdated) });
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.GetBaseException().Message);
            }
        }

        public static void ShowPrivacyOptionsForm(Action<string> onError, Action onDismiss)
        {
            if (!IsAvailable)
            {
                onError?.Invoke(PluginHint);
                return;
            }

            try
            {
                var load = _consentFormType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                if (load == null)
                {
                    onError?.Invoke("ConsentForm.Load not found.");
                    return;
                }

                Action<object, object> onLoaded = (form, loadError) =>
                {
                    if (loadError != null)
                    {
                        onError?.Invoke(GetFormErrorMessage(loadError));
                        return;
                    }

                    if (form == null)
                    {
                        onError?.Invoke("ConsentForm.Load returned null form.");
                        return;
                    }

                    var show = form.GetType().GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
                    if (show == null)
                    {
                        onError?.Invoke("ConsentForm.Show not found.");
                        return;
                    }

                    Action<object> onShown = showError =>
                    {
                        if (showError != null)
                            onError?.Invoke(GetFormErrorMessage(showError));
                        else
                            onDismiss?.Invoke();
                    };

                    show.Invoke(form, new object[] { CreateFormErrorCallback(onShown) });
                };

                load.Invoke(null, new object[] { CreateConsentFormLoadCallback(onLoaded) });
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.GetBaseException().Message);
            }
        }

        static void LoadAndShowConsentFormIfRequired(Action onReadyToInitAds, Action<string> onError)
        {
            var method = _consentFormType.GetMethod(
                "LoadAndShowConsentFormIfRequired",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Action<>).MakeGenericType(_formErrorType) },
                null);

            if (method == null)
            {
                onError?.Invoke("ConsentForm.LoadAndShowConsentFormIfRequired not found.");
                return;
            }

            Action<object> onComplete = formError =>
            {
                if (formError != null)
                {
                    onError?.Invoke(GetFormErrorMessage(formError));
                    return;
                }

                if (CanRequestAds())
                    onReadyToInitAds?.Invoke();
                else
                    onError?.Invoke("Consent gathered but CanRequestAds() is false.");
            };

            method.Invoke(null, new object[] { CreateFormErrorCallback(onComplete) });
        }

        static bool CanRequestAds()
        {
            var prop = _consentInformationType.GetProperty("CanRequestAds", BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
                return prop.GetValue(null) is bool b && b;

            var method = _consentInformationType.GetMethod("CanRequestAds", BindingFlags.Public | BindingFlags.Static);
            return method != null && method.Invoke(null, null) is bool can && can;
        }

        static object BuildConsentRequestParameters()
        {
            var request = Activator.CreateInstance(_consentRequestParametersType);

#if UNITY_EDITOR
            // Debug-only: simulate EEA geography so the consent form appears in Editor testing.
            if (_consentDebugSettingsType != null && _debugGeographyType != null)
            {
                var debug = Activator.CreateInstance(_consentDebugSettingsType);
                var eea = Enum.Parse(_debugGeographyType, "EEA");
                _consentDebugSettingsType.GetProperty("DebugGeography")?.SetValue(debug, eea);
                _consentDebugSettingsType.GetProperty("TestDeviceHashedIds")?.SetValue(debug,
                    new List<string>
                    {
                        "8EC8C174AE81E71DF002C15B0B8458D9",
                        "4849D928DCE8B9A471CE3BAB5E57E08A",
                        "CA578C5A7014A1C2606A4810118F95C2"
                    });
                _consentRequestParametersType.GetProperty("ConsentDebugSettings")?.SetValue(request, debug);
            }
#endif

            return request;
        }

        static Delegate CreateFormErrorCallback(Action<object> handler)
        {
            var forwarder = new FormErrorForwarder(handler);
            var actionType = typeof(Action<>).MakeGenericType(_formErrorType);
            return Delegate.CreateDelegate(actionType, forwarder, typeof(FormErrorForwarder).GetMethod(nameof(FormErrorForwarder.Invoke)));
        }

        static Delegate CreateConsentFormLoadCallback(Action<object, object> handler)
        {
            var forwarder = new ConsentFormLoadForwarder(handler);
            var actionType = typeof(Action<,>).MakeGenericType(_consentFormType, _formErrorType);
            return Delegate.CreateDelegate(actionType, forwarder, typeof(ConsentFormLoadForwarder).GetMethod(nameof(ConsentFormLoadForwarder.Invoke)));
        }

        static string GetFormErrorMessage(object formError) =>
            formError == null
                ? string.Empty
                : formError.GetType().GetProperty("Message")?.GetValue(formError) as string ?? "UMP error";

        sealed class FormErrorForwarder
        {
            readonly Action<object> _handler;
            public FormErrorForwarder(Action<object> handler) => _handler = handler;
            public void Invoke(object formError) => _handler(formError);
        }

        sealed class ConsentFormLoadForwarder
        {
            readonly Action<object, object> _handler;
            public ConsentFormLoadForwarder(Action<object, object> handler) => _handler = handler;
            public void Invoke(object form, object loadError) => _handler(form, loadError);
        }
    }
}
#endif
