using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Firebase;
using Firebase.Analytics;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.RemoteConfig;
using Google;
using UnityEngine;

namespace PixelSurvival
{
    public class FirebaseManager : SingletonBehaviour<FirebaseManager>
    {
        private FirebaseApp _app;
        private FirebaseRemoteConfig _remoteConfig;
        private bool _isRemoteConfigInit = false;
        private Dictionary<string, object> _remoteConfigDic = new Dictionary<string, object>();
        
        // Auth
        private FirebaseAuth _auth;
        private bool _isAuthInit = false;
        private const string GOOGLE_WEB_CLIENT_ID = "";
        private GoogleSignInConfiguration _googleSignInConfig;
        private FirebaseUser _firebaseUser;
        private IAppleAuthManager _appleAuthManager;
        private string _rawNonce;
        private string _nonce;
        public bool HasSignedInWithGoogle { get; private set; } = false;
        public bool HasSignedInWithApple { get; private set; } = false;
        
        // Firestore Database
        private const string REAL_DATABASE_ID = "pixel-orc-slayer-real";
        private string _unityEditorUserId = "";
        private FirebaseFirestore _database;
        private bool _isFirestoreInit = false;
        
        // Analytics
        private bool _isAnalyticsInit = false;
        
        protected override void Init()
        {
            base.Init();

            LoadData();
#if UNITY_IOS            
            InitAppleAuth();
#endif            
            StartCoroutine(InitFirebaseServiceCo());
        }

        private void InitAppleAuth()
        {
            if (AppleAuthManager.IsCurrentPlatformSupported)
            {
                var deserializer = new PayloadDeserializer();
                _appleAuthManager = new AppleAuthManager(deserializer);
            }
        }
        
#if UNITY_IOS
        private void Update()
        {
            _appleAuthManager?.Update();
        }
#endif

        private string GeneratSHA256Nonce(string rawNonce)
        {
            var sha = new SHA256Managed();
            var utf8RawNonce = Encoding.UTF8.GetBytes(rawNonce);
            var hash = sha.ComputeHash(utf8RawNonce);

            var result = string.Empty;
            for (int i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("x2");
            }
            
            return result;
        }
        
        public bool IsInit()
        {
            return _isRemoteConfigInit && _isAuthInit && _isFirestoreInit && _isAnalyticsInit;
        }

        private void LoadData()
        {
            HasSignedInWithGoogle = PlayerPrefs.GetInt("HasSignedInWithGoogle") == 1 ? true : false;;
            HasSignedInWithApple = PlayerPrefs.GetInt("HasSignedInWithApple") == 1 ? true : false;
        }

        private void SaveData()
        {
            PlayerPrefs.SetInt("HasSignedInWithGoogle", HasSignedInWithGoogle ? 1 : 0);
            PlayerPrefs.SetInt("HasSignedInWithApple", HasSignedInWithApple ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        private IEnumerator InitFirebaseServiceCo()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Logger.Log($"FirebaseApp initialized success.");
                    _app = FirebaseApp.DefaultInstance;
                    InitRemoteConfig();
                    InitAuth();
                    InitFirestore();
                    InitAnalytics();
                }
                else
                {
                    Logger.LogError($"FirebaseApp initialization failed : {dependencyStatus}");
                }
            });
            
            var elapsedTime = 0f;
            while (elapsedTime < GlobalDefine.THIRD_PARTY_SERVICE_INIT_TIME)
            {
                if (IsInit())
                {
                    Logger.Log($"{GetType()}:: initialization sucess.");
                    yield break;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            Logger.LogError($"FirebaseApp initialization failed.");
        }

        #region REMOTE_CONFIG

        private void InitRemoteConfig()
        {
            _remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            if (_remoteConfig == null)
            {
                Logger.LogError($"FirebaseApp initialization failed. FirebaseRemoteConfig is null.");
                return;
            }
            
            _remoteConfigDic.Add("dev_app_version", string.Empty);
            _remoteConfigDic.Add("real_app_version", string.Empty);
            
            _remoteConfig.SetDefaultsAsync(_remoteConfigDic).ContinueWithOnMainThread(task =>
            {
                _remoteConfig.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetchTask =>
                {
                    if (fetchTask.IsCompleted)
                    {
                        _remoteConfig.ActivateAsync().ContinueWithOnMainThread(activeTask =>
                        {
                            if (activeTask.IsCompleted)
                            {
                                _remoteConfigDic["dev_app_version"] = _remoteConfig.GetValue("dev_app_version").StringValue;
                                _remoteConfigDic["real_app_version"] = _remoteConfig.GetValue("real_app_version").StringValue;
                                _isRemoteConfigInit = true;
                            }
                        });
                    }
                });
            });
        }

        public string GetAppVersion()
        {
            #if DEV_VER
            if (_remoteConfigDic.ContainsKey("dev_app_version"))
            {
                return _remoteConfigDic["dev_app_version"].ToString();
            }
            #else
            if (_remoteConfigDic.ContainsKey("real_app_version"))
            {
                return _remoteConfigDic["real_app_version"].ToString();
            }
            #endif
            
            return string.Empty;
        }
        
        #endregion

        #region AUTH

        private void InitAuth()
        {
            _auth = FirebaseAuth.DefaultInstance;
            if (_auth == null)
            {
                Logger.Log("FirebaseAuth initialization failed. FirebaseAuth is null.");
                return;
            }

            _auth.StateChanged += OnAuthStateChanged;

            _googleSignInConfig = new GoogleSignInConfiguration
            {
                WebClientId = GOOGLE_WEB_CLIENT_ID,
                RequestIdToken = true
            };
            
            _isAuthInit = true;

            if (_auth.CurrentUser == null)
            {
                if (HasSignedInWithGoogle)
                {
                    SignInWithGoogle();
                }
                else if (HasSignedInWithApple)
                {
                    SignInWithApple();
                }
            }
            else
            {
                _firebaseUser = _auth.CurrentUser;
            }
        }

        private void OnAuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (SceneLoader.Instance.GetCurrentScene() == SceneType.Title)
            {
                return;
            }

            if (_auth != null && _auth.CurrentUser == null)
            {
                Logger.Log("User signed out or disconnected.");
                _firebaseUser = null;
                HasSignedInWithGoogle = false;
                HasSignedInWithApple = false;
                SaveData();
                
                AudioManager.Instance.StopBGM();
                UIManager.Instance.CloseAllOpenUI();
                SceneLoader.Instance.LoadScene(SceneType.Title);
            }
        }

        public bool IsSignedIn()
        {
#if UNITY_EDITOR
            return true;
#else
            return _firebaseUser != null;
#endif
        }

        public void SignInWithGoogle()
        {
            GoogleSignIn.Configuration = _googleSignInConfig;
            GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    if (task.IsCanceled)
                    {
                        Logger.LogError("SignInWithGoogle was canceled.");
                    }
                    else if (task.IsFaulted)
                    {
                        Logger.LogError($"SignInWithGoogle was failed. error : {task.Exception}");
                    }

                    ShowLoginFailUI();
                    return;
                }
                
                GoogleSignInUser googleUser = task.Result;
                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(authTask =>
                {
                    if (authTask.IsCanceled || authTask.IsFaulted)
                    {
                        if (authTask.IsCanceled)
                        {
                            Logger.LogError("SignInWithGoogle was canceled.");
                        }
                        else if (authTask.IsFaulted)
                        {
                            Logger.LogError($"SignInWithGoogle was faulted. error : {authTask.Exception}");
                        }

                        ShowLoginFailUI();
                        return;
                    }
                    
                    _firebaseUser = authTask.Result;
                    Logger.Log($"User signed in. successfully: {_firebaseUser.DisplayName} ({_firebaseUser.UserId})");
                    
                    HasSignedInWithGoogle = true;
                    HasSignedInWithApple = false;
                    SaveData();
                });
            });
        }

        public void SignInWithApple()
        {
#if UNITY_ANDROID
            SignInWithAppleOnAndroid();
#elif UNITY_IOS
            SignInWithAppleOniOS();
#endif
        }

        private void SignInWithAppleOnAndroid()
        {
            FederatedOAuthProviderData providerData = new FederatedOAuthProviderData();
            providerData.ProviderId = "apple.com";
            FederatedOAuthProvider provider = new FederatedOAuthProvider();
            provider.SetProviderData(providerData);

            _auth.SignInWithProviderAsync(provider).ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled || authTask.IsFaulted)
                {
                    if (authTask.IsCanceled)
                    {
                        Logger.LogError("SignInWithProviderAsync was canceled.");
                    }
                    else if (authTask.IsFaulted)
                    {
                        Logger.LogError($"SignInWithProviderAsync was faulted. error : {authTask.Exception}");
                    }
                    
                    ShowLoginFailUI();
                    return;
                }
                
                _firebaseUser = authTask.Result.User;
                Logger.Log($"User signed in successfully: {_firebaseUser.DisplayName} ({_firebaseUser.UserId})");

                HasSignedInWithGoogle = false;
                HasSignedInWithGoogle = true;
                
                SaveData();
            });
        }

        private void SignInWithAppleOniOS()
        {
            if (PlayerPrefs.HasKey("AppleUserId"))
            {
                QuickSignInWithAppleOniOS();
            }
            else
            {
                NormalSignInWithAppleOniOS();
            }
        }

        private void QuickSignInWithAppleOniOS()
        {
            Logger.Log($"Trying QuickSignIn With AppleUserId : {PlayerPrefs.GetString("AppleUserId")}...");
            
            _rawNonce = Guid.NewGuid().ToString();
            _nonce = GeneratSHA256Nonce(_rawNonce);
            
            var quickLoginArgs = new AppleAuthQuickLoginArgs(_nonce);
            _appleAuthManager.QuickLogin(quickLoginArgs, credential =>
            {
                var appleIdCredential = credential as IAppleIDCredential;
                if (appleIdCredential != null)
                {
                    Logger.Log($"QuickLogin success. AppleUserId : {appleIdCredential.User}");
                    
                    AuthentialcateFirebaseWithAppleId(appleIdCredential, _rawNonce);
                }
                else
                {
                    Logger.LogError("AppleIdCredential is null");
                    PlayerPrefs.DeleteKey("AppleUserId");
                    ShowLoginFailUI();
                }
            },
            error =>
            {
                Logger.Log($"QuickLogin failed. error : {AppleErrorExtensions.GetAuthorizationErrorCode(error)}");
                PlayerPrefs.DeleteKey("AppleUserId");
                ShowLoginFailUI();
            });
        }
        
        private void NormalSignInWithAppleOniOS()
        {
            _rawNonce = Guid.NewGuid().ToString();
            _nonce = GeneratSHA256Nonce(_rawNonce);

            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName, _nonce);
            _appleAuthManager.LoginWithAppleId(loginArgs, credential =>
            {
                var appleIdCredential = credential as IAppleIDCredential;
                if (appleIdCredential != null)
                {
                    Logger.Log($"Login success. AppleUserId : {appleIdCredential.User}");
                    PlayerPrefs.SetString("AppleUserId", appleIdCredential.User);
                    PlayerPrefs.Save();
                    
                    AuthentialcateFirebaseWithAppleId(appleIdCredential, _rawNonce);
                }
                else
                {
                    Logger.LogError("AppleIdCredential is null");
                    ShowLoginFailUI();
                }
            },
            error =>
            {
                Logger.Log($"QuickLogin failed. error : {AppleErrorExtensions.GetAuthorizationErrorCode(error)}");
                ShowLoginFailUI();
            });
        }

        private void AuthentialcateFirebaseWithAppleId(IAppleIDCredential appleIdCredential, string rawNonce)
        {
            var identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken);
            var authorizationCode = Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode);
            var firebaseCredential = OAuthProvider.GetCredential("apple.com",  identityToken, rawNonce, authorizationCode);

            _auth.SignInWithCredentialAsync(firebaseCredential).ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled || authTask.IsFaulted)
                {
                    if (authTask.IsCanceled)
                    {
                        Logger.LogError("SignInWithCredentialAsync was canceled.");
                    }
                    else if (authTask.IsFaulted)
                    {
                        Logger.LogError($"SignInWithCredentialAsync failed. error : {authTask.Exception}");
                    }

                    ShowLoginFailUI();
                    return;
                }

                _firebaseUser = authTask.Result;
                Logger.Log($"User signed in successfully : {_firebaseUser.DisplayName}  ({_firebaseUser.UserId})");

                HasSignedInWithGoogle = false;
                HasSignedInWithApple = true;
                SaveData();
            });
        }
        
        public void SignOut()
        {
            if (_firebaseUser != null)
            {
                _auth.SignOut();
                Logger.Log($"User Signed out successfully");
            }
            
#if UNITY_EDITOR
    Logger.Log("User signed out or disconnected.");
    _firebaseUser = null;
    HasSignedInWithGoogle = false;
    HasSignedInWithApple = false;
    SaveData();
                
    AudioManager.Instance.StopBGM();
    UIManager.Instance.CloseAllOpenUI();
    SceneLoader.Instance.LoadScene(SceneType.Title);
#endif
        }

        private void ShowLoginFailUI()
        {
            var uiData = new ConfirmUIData();
            uiData.ConfirmType = ConfirmType.OK;
            uiData.TitleTxt = "Error";
            uiData.DescTxt = "Failed to sign in";
            uiData.OKBtnTxt = "OK";
            uiData.OnClickOKBtn = () =>
            {
                var uiData = new BaseUIData();
                UIManager.Instance.OpenUIFromAA<LoginUI>(uiData);
            };
            
            UIManager.Instance.OpenUIFromAA<ConfirmUI>(uiData);
        }

        private string GetUserId()
        {
#if UNITY_EDITOR
            return _unityEditorUserId;
#else
            return _firebaseUser != null ? _firebaseUser.UserId : string.Empty;
#endif
        }
        
        #endregion

        #region FIRESTORE

        private void InitFirestore()
        {
#if DEV_VER
            _database = FirebaseFirestore.DefaultInstance;
#else
            _database = FirebaseFirestore.GetInstance(FirebaseApp.DefaultInstance, REAL_DATABASE_ID);
#endif
            
            if (_database == null)
            {
                Logger.LogError($"FirebaseFirestore initialized faild. FirebaseFirestore is null.");
                return;
            }

            _isFirestoreInit = true;
        }

        public void LoadUserData<T>(Action onFinishLoad = null) where T : class, IUserData
        {
            Type type = typeof(T);
            _database.Collection($"{type.Name}").Document(GetUserId()).GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        IUserData userData = UserDataManager.Instance.GetUserData<T>();
                        DocumentSnapshot snapshot = task.Result;
                        if (snapshot.Exists)
                        {
                            Logger.Log($"{type.Name} successfully loaded");
                            
                            Dictionary<string, object> userDataDic = snapshot.ToDictionary();
                            userData.SetData(userDataDic);
                        }
                        else
                        {
                            Logger.Log($"{type.Name} failed to load");
                            userData.SetDefaultData();
                            userData.SaveData();
                        }
                        
                        onFinishLoad?.Invoke();
                    }
                    else
                    {
                        Logger.LogError($"Failed to load {type.Name}: {task.Exception}");
                    }
                });
        }

        public void SaveUserData<T>(Dictionary<string, object> userDataDic) where T : class, IUserData
        {
            Type type = typeof(T);
            DocumentReference docRef = _database.Collection($"{type.Name}").Document(GetUserId());
            docRef.SetAsync(userDataDic).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Logger.Log($"{type.Name} successfully saved");
                }
                else
                {
                    Logger.LogError($"{type.Name} failed to save: {task.Exception}");
                }
            });
        }

        public async Task<DateTime> GetCurrentDateTime()
        {
            DocumentReference serverTimeDoc = _database.Collection("time").Document("server_time");

            await serverTimeDoc.SetAsync(new { timestamp = FieldValue.ServerTimestamp });
            for (int i = 0; i < 5; i++)
            {
                DocumentSnapshot snap = await serverTimeDoc.GetSnapshotAsync();

                if (snap.TryGetValue("timestamp", out Timestamp timestamp))
                {
                    DateTime dateTime = timestamp.ToDateTime().ToLocalTime();
                    Logger.Log($"CurrentDateTime: {dateTime}");
                    return dateTime;
                }

                await Task.Delay(100);
            }

            Logger.LogError("Server timestamp not available (null) after retries.");
            return DateTime.UtcNow.ToLocalTime();
        }

        
        #endregion

        #region ANALYTICS

        private void InitAnalytics()
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            _isAnalyticsInit = true;
        }

        public void LogCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            List<Parameter> firebaseParameters = new List<Parameter>();
            foreach (var param in parameters)
            {
                firebaseParameters.Add(new Parameter(param.Key, param.Value.ToString()));
            }
            
            FirebaseAnalytics.LogEvent(eventName, firebaseParameters.ToArray());
        }

        #endregion
        
        protected override void Dispose()
        {
            base.Dispose();

            if (_auth != null)
            {
                _auth.StateChanged -= OnAuthStateChanged;
            }
        }
    }
}

