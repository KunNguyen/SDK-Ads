using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if FIREBASE_AUTH
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

#if GOOGLE_PLAY_GAMES
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

namespace JisSDKAds.Firebase
{
    public enum LoginMethod
    {
        None,
        Google,
        PlayGames,
        GameCenter,
        Anonymous
    }

    [System.Serializable]
    public class FirebaseAuthManager
    {
        private const string LAST_LOGIN_METHOD_KEY = "FirebaseAuth_LastLoginMethod";
        
        [field: SerializeField] private string GoogleClientId { get; set; } = "101377628372-lol1cepipibdajc1t4cr72dukqog6cfe.apps.googleusercontent.com";
        public event Action<FirebaseUser> SignedIn;

        /// <summary>User id only — safe for <c>JisSDKAds.Firebase</c> without referencing Firebase.Auth.dll.</summary>
        public event Action<string> SignedInUserId;

        public event Action SignedOut;
        public event Action<string> SignedInFailed;

        [Obsolete("Use SignedInFailed.")]
        public event Action<string> SignInFailed
        {
            add => SignedInFailed += value;
            remove => SignedInFailed -= value;
        }

        void NotifySignedIn(FirebaseUser user)
        {
            if (user == null) return;
                    NotifySignedIn(user);
            SignedInUserId?.Invoke(user.UserId);
        }

        void NotifySignedInFailed(string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "Sign-in failed.";
            Debug.LogWarning($"[FirebaseAuth] {message}");
            SignedInFailed?.Invoke(message);
        }

        public FirebaseAuth Auth { get; private set; }
        public FirebaseUser CurrentUser => Auth?.CurrentUser;

        public bool IsInitialized { get; private set; }
        public bool IsSignedIn => CurrentUser != null;

        private bool stateChangeSubscribed;
        private LoginMethod _currentLoginMethod = LoginMethod.None;
        public LoginMethod CurrentLoginMethod => _currentLoginMethod;

        public void Init()
        {
            if (IsInitialized) return;
            

            Auth = FirebaseAuth.DefaultInstance;

            SubscribeAuthStateChanged();
            IsInitialized = true;

            // Restore session if user already signed in from previous app run.
            if (Auth.CurrentUser != null)
            {
                _currentLoginMethod = (LoginMethod)PlayerPrefs.GetInt(LAST_LOGIN_METHOD_KEY, (int)LoginMethod.None);
                NotifySignedIn(Auth.CurrentUser);
            }
            else
            {
                _currentLoginMethod = LoginMethod.None;
                SignedOut?.Invoke();
            }
            
            // TrySilentSignInPlayGamesAsync().Forget();
        }

        private void SubscribeAuthStateChanged()
        {
            if (stateChangeSubscribed) return;
            stateChangeSubscribed = true;

            Auth.StateChanged += OnAuthStateChanged;
            OnAuthStateChanged(this, EventArgs.Empty);
        }

        private void OnAuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (Auth == null) return;

            if (Auth.CurrentUser != null)
            {
                NotifySignedIn(Auth.CurrentUser);
            }
            else
            {
                SignedOut?.Invoke();
            }
        }

        public Task SignOut()
        {
            if (Auth == null)
            {
                Debug.LogWarning("FirebaseAuthManager.SignOut called before Init().");
                return Task.CompletedTask;
            }

            Auth.SignOut();

            if (_currentLoginMethod == LoginMethod.Google)
                GoogleSignInReflection.SignOutIfAvailable();

            _currentLoginMethod = LoginMethod.None;
            PlayerPrefs.SetInt(LAST_LOGIN_METHOD_KEY, (int)LoginMethod.None);
            PlayerPrefs.Save();

            SignedOut?.Invoke();
            return Task.CompletedTask;
        }

#if GOOGLE_PLAY_GAMES
        public async Task<FirebaseUser> SignInWithPlayGamesAsync(CancellationToken ct = default)
        {
            if (!IsInitialized) Init();

            string authCode;
            try
            {
                var tcs = new TaskCompletionSource<SignInStatus>();
                PlayGamesPlatform.Instance.Authenticate(status => tcs.TrySetResult(status));
                var status = await tcs.Task;

                if (status != SignInStatus.Success)
                {
                    var msg = $"Play Games authentication failed with status: {status}";
                    Debug.LogWarning(msg);
                    NotifySignedInFailed(msg);
                    throw new InvalidOperationException(msg);
                }

                var scopes = new List<AuthScope> { AuthScope.OPEN_ID };
                var authCodeTcs = new TaskCompletionSource<AuthResponse>();
                PlayGamesPlatform.Instance.RequestServerSideAccess(false, scopes, response => authCodeTcs.TrySetResult(response));
                var authResponse = await authCodeTcs.Task;
                authCode = authResponse.GetAuthCode();
                if (string.IsNullOrEmpty(authCode))
                {
                    var msg = "Play Games returned null/empty auth code.";
                    Debug.LogWarning(msg);
                    NotifySignedInFailed(msg);
                    throw new InvalidOperationException(msg);
                }
            }
            catch (Exception e)
            {
                var msg = $"Play Games Sign-In failed: {e.Message}";
                Debug.LogWarning(msg);
                NotifySignedInFailed(msg);
                throw;
            }

            var credential = PlayGamesAuthProvider.GetCredential(authCode);

            try
            {
                var user = await Auth.SignInWithCredentialAsync(credential)
                    .ContinueWithOnMainThread(task => task.Result)
                    ;

                _currentLoginMethod = LoginMethod.PlayGames;
                PlayerPrefs.SetInt(LAST_LOGIN_METHOD_KEY, (int)_currentLoginMethod);
                PlayerPrefs.Save();

                NotifySignedIn(user);
                return user;
            }
            catch (Exception e)
            {
                var msg = $"Firebase SignInWithCredential (Play Games) failed: {e.Message}";
                Debug.LogWarning(msg);
                NotifySignedInFailed(msg);
                throw;
            }
        }

        /// <summary>
        /// Attempts a silent sign-in with Google Play Games.
        /// Useful during game startup to log in the user without showing a UI if they have already signed in previously.
        /// </summary>
        public async Task<FirebaseUser> TrySilentSignInPlayGamesAsync(CancellationToken ct = default)
        {
            if (!IsInitialized) Init();

            try
            {
                var tcs = new TaskCompletionSource<SignInStatus>();
                // Requesting silent sign-in. This will fail if the user has not previously authorized the app.
                PlayGamesPlatform.Instance.Authenticate(status => tcs.TrySetResult(status));
                var status = await tcs.Task;

                if (status == SignInStatus.Success)
                {
                    // If success, proceed to Firebase sign-in.
                    return await SignInWithPlayGamesAsync(ct);
                }
                else
                {
                    Debug.Log($"Play Games silent sign-in failed or required UI: {status}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Play Games Silent Sign-In attempt failed: {e.Message}");
                return null;
            }
        }
#endif

#if UNITY_IOS
        public async Task<FirebaseUser> SignInWithGameCenterAsync(CancellationToken ct = default)
        {
            if (!IsInitialized) Init();

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                Social.localUser.Authenticate(success => tcs.TrySetResult(success));
                var authenticated = await tcs.Task;

                if (!authenticated)
                {
                    var msg = "Game Center authentication failed.";
                    Debug.LogWarning(msg);
                    NotifySignedInFailed(msg);
                    throw new InvalidOperationException(msg);
                }
            }
            catch (Exception e)
            {
                var msg = $"Game Center Sign-In failed: {e.Message}";
                Debug.LogWarning(msg);
                NotifySignedInFailed(msg);
                throw;
            }

            var credential = CreateGameCenterCredential();

            try
            {
                var user = await Auth.SignInWithCredentialAsync(credential)
                    .ContinueWithOnMainThread(task => task.Result)
                    ;

                _currentLoginMethod = LoginMethod.GameCenter;
                PlayerPrefs.SetInt(LAST_LOGIN_METHOD_KEY, (int)_currentLoginMethod);
                PlayerPrefs.Save();

                NotifySignedIn(user);
                return user;
            }
            catch (Exception e)
            {
                var msg = $"Firebase SignInWithCredential (Game Center) failed: {e.Message}";
                Debug.LogWarning(msg);
                NotifySignedInFailed(msg);
                throw;
            }
        }
#endif

#if UNITY_IOS
        private Credential CreateGameCenterCredential()
        {
            var providerType = typeof(GameCenterAuthProvider);
            var noArgGetCredential = providerType.GetMethod(
                "GetCredential",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            if (noArgGetCredential != null)
            {
                return (Credential)noArgGetCredential.Invoke(null, null);
            }

            throw new NotSupportedException(
                "Firebase GameCenterAuthProvider.GetCredential() API is not available in this Firebase Auth version. " +
                "Please update SignInWithGameCenterAsync to match the current SDK method signature.");
        }
#endif

        public async Task<FirebaseUser> SignInWithPlatformAsync(CancellationToken ct = default)
        {
#if UNITY_ANDROID && GOOGLE_PLAY_GAMES
            return await SignInWithPlayGamesAsync(ct);
#elif UNITY_IOS
            return await SignInWithGameCenterAsync(ct);
#else
            return await SignInWithGoogleAsync(ct);
#endif
        }

        public async Task<FirebaseUser> SignInWithGoogleAsync(CancellationToken ct = default)
        {
            return await SignInWithGoogleAsync(GoogleClientId, ct);
        }

        /// <summary>
        /// Google Sign-In -> Firebase Auth SignInWithCredential.
        /// Requires Google Sign-In for Unity plugin.
        /// </summary>
#pragma warning disable CS1998
        public async Task<FirebaseUser> SignInWithGoogleAsync(string webClientId, CancellationToken ct = default)
        {
            Debug.Log("[FirebaseAuth] Start Google Sign In");
            if (!IsInitialized)
                Init();

            if (string.IsNullOrEmpty(webClientId))
            {
                var msg =
                    "webClientId is null/empty. Provide your OAuth Web Client ID (from Google Cloud / Firebase settings).";
                Debug.LogError(msg);
                NotifySignedInFailed(msg);
                throw new ArgumentException(msg, nameof(webClientId));
            }

            if (!GoogleSignInReflection.IsAvailable)
            {
                var msg = GoogleSignInReflection.PluginHint;
                Debug.LogError(msg);
                NotifySignedInFailed(msg);
                throw new InvalidOperationException(msg);
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                var idToken = await GoogleSignInReflection.SignInForIdTokenAsync(webClientId);
                var credential = GoogleAuthProvider.GetCredential(idToken, null);

                var user = await Auth.SignInWithCredentialAsync(credential)
                    .ContinueWithOnMainThread(task => task.Result);

                Debug.Log("[FirebaseAuth] Firebase sign-in with Google credential: " + user.DisplayName);

                _currentLoginMethod = LoginMethod.Google;
                PlayerPrefs.SetInt(LAST_LOGIN_METHOD_KEY, (int)_currentLoginMethod);
                PlayerPrefs.Save();

                NotifySignedIn(user);
                return user;
            }
            catch (Exception e)
            {
                var msg = e.GetBaseException().Message;
                Debug.LogError($"[FirebaseAuth] Google Sign-In failed: {msg}");
                NotifySignedInFailed(msg);
                throw;
            }
        }
#pragma warning restore CS1998
        

        public async Task<FirebaseUser> SignInAnonymouslyAsync(CancellationToken ct = default)
        {
            if (!IsInitialized) Init();

#if UNITY_EDITOR
            try
            {
                var user = await SignInWithEditorDeviceIDAsync(ct);
                if (user != null)
                {
                    _currentLoginMethod = LoginMethod.Anonymous;
                    PlayerPrefs.SetInt(LAST_LOGIN_METHOD_KEY, (int)_currentLoginMethod);
                    PlayerPrefs.Save();

                    NotifySignedIn(user);
                    return user;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FirebaseAuth] Editor device-based sign-in failed: {e.Message}. Falling back to normal anonymous sign-in.");
            }
#endif

            try
            {
                var result = await Auth.SignInAnonymouslyAsync()
                    .ContinueWithOnMainThread(task => task.Result)
                    ;

                _currentLoginMethod = LoginMethod.Anonymous;
                PlayerPrefs.SetInt(LAST_LOGIN_METHOD_KEY, (int)_currentLoginMethod);
                PlayerPrefs.Save();

                NotifySignedIn(result.User);
                return result.User;
            }
            catch (Exception e)
            {
                var msg = $"Anonymous sign-in failed: {e.Message}";
                Debug.LogWarning(msg);
                NotifySignedInFailed(msg);
                throw;
            }
        }

#if UNITY_EDITOR
        private async Task<FirebaseUser> SignInWithEditorDeviceIDAsync(CancellationToken ct)
        {
            // Create a stable email based on device ID for Editor testing
            string deviceId = SystemInfo.deviceUniqueIdentifier.Replace("-", "").Replace("{", "").Replace("}", "").ToLower();
            string email = $"{deviceId}@editor-test.com";
            string password = "EditorPassword123!"; // Static password for editor testing accounts

            try
            {
                var result = await Auth.SignInWithEmailAndPasswordAsync(email, password)
                    .ContinueWithOnMainThread(task => task.Result)
                    ;
                
                Debug.Log($"[FirebaseAuth] Editor signed in with stable ID: {result.User.UserId}");
                return result.User;
            }
            catch (Exception)
            {
                // If user doesn't exist, create it
                try
                {
                    var result = await Auth.CreateUserWithEmailAndPasswordAsync(email, password)
                        .ContinueWithOnMainThread(task => task.Result);
                    
                    Debug.Log($"[FirebaseAuth] Editor created and signed in with stable ID: {result.User.UserId}");
                    return result.User;
                }
                catch (Exception createEx)
                {
                    // If even creation fails (e.g. Email/Password provider not enabled in Firebase Console),
                    // rethrow to let the caller handle fallback.
                    throw new Exception($"Failed to create editor test account. Ensure 'Email/Password' provider is enabled in Firebase Console. Error: {createEx.Message}");
                }
            }
        }
#endif
    }
}
#endif