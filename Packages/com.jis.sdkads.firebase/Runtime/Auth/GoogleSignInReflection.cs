#if FIREBASE_AUTH
using System;
using System.Reflection;
using System.Threading.Tasks;
using Firebase.Extensions;
using UnityEngine;

namespace JisSDKAds.Firebase
{
    /// <summary>
    /// Optional Google Sign-In Unity plugin invoked via reflection (no compile-time dependency on Google.dll).
    /// </summary>
    static class GoogleSignInReflection
    {
        public const string PluginHint =
            "Install Google Sign-In for Unity (https://github.com/googlesamples/google-signin-unity). " +
            "You do not need the GOOGLE_SIGNIN scripting define.";

        static Assembly _googleAssembly;
        static Type _configurationType;
        static Type _signInType;
        static Type _userType;
        static bool _resolved;
        static bool _available;

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
            if (_resolved)
                return;
            _resolved = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "Google.SignIn")
                    continue;
                _googleAssembly = assembly;
                break;
            }

            if (_googleAssembly == null)
                return;

            _configurationType = _googleAssembly.GetType("Google.GoogleSignInConfiguration");
            _signInType = _googleAssembly.GetType("Google.GoogleSignIn");
            _userType = _googleAssembly.GetType("Google.GoogleSignInUser");
            _available = _configurationType != null && _signInType != null && _userType != null;
        }

        public static async Task<string> SignInForIdTokenAsync(string webClientId)
        {
            if (!IsAvailable)
                throw new InvalidOperationException(PluginHint);

            if (string.IsNullOrEmpty(webClientId))
                throw new ArgumentException("webClientId is required.", nameof(webClientId));

            var config = Activator.CreateInstance(_configurationType);
            _configurationType.GetProperty("WebClientId")?.SetValue(config, webClientId);
            _configurationType.GetProperty("RequestIdToken")?.SetValue(config, true);
            _configurationType.GetProperty("RequestEmail")?.SetValue(config, true);
            _configurationType.GetProperty("UseGameSignIn")?.SetValue(config, false);

            var configurationProp = _signInType.GetProperty("Configuration", BindingFlags.Public | BindingFlags.Static);
            configurationProp?.SetValue(null, config);

            var defaultInstanceProp = _signInType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static);
            var instance = defaultInstanceProp?.GetValue(null);
            if (instance == null)
                throw new InvalidOperationException("GoogleSignIn.DefaultInstance is null.");

            var enableDebug = instance.GetType().GetMethod("EnableDebugLogging");
            enableDebug?.Invoke(instance, new object[] { true });

            var signInMethod = instance.GetType().GetMethod("SignIn", Type.EmptyTypes);
            if (signInMethod == null)
                throw new MissingMethodException("GoogleSignIn.SignIn() not found.");

            var signInTask = signInMethod.Invoke(instance, null);
            if (signInTask == null)
                throw new InvalidOperationException("Google SignIn did not return a Task.");

            var user = await AwaitTaskResultAsync(signInTask);
            if (user == null)
                throw new InvalidOperationException("Google Sign-In returned null user.");

            var idToken = _userType.GetProperty("IdToken")?.GetValue(user) as string;
            if (string.IsNullOrEmpty(idToken))
                throw new InvalidOperationException("Google Sign-In returned empty IdToken.");

            return idToken;
        }

        static async Task<object> AwaitTaskResultAsync(object taskObj)
        {
            var type = taskObj.GetType();
            while (!(bool)type.GetProperty("IsCompleted")!.GetValue(taskObj))
                await Task.Yield();

            if ((bool)type.GetProperty("IsCanceled")!.GetValue(taskObj))
                throw new TaskCanceledException();

            if ((bool)type.GetProperty("IsFaulted")!.GetValue(taskObj))
            {
                var ex = type.GetProperty("Exception")!.GetValue(taskObj) as AggregateException;
                throw ex?.GetBaseException() ?? new Exception("Google Sign-In task failed.");
            }

            return type.GetProperty("Result")!.GetValue(taskObj);
        }

        public static void SignOutIfAvailable()
        {
            if (!IsAvailable)
                return;

            try
            {
                var defaultInstanceProp = _signInType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static);
                var instance = defaultInstanceProp?.GetValue(null);
                instance?.GetType().GetMethod("SignOut")?.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAuth] Google SignOut failed: {ex.Message}");
            }
        }
    }
}
#endif
