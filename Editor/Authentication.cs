using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;

namespace Filta {

    public enum LoginResult {
        Success,
        Error,
        NoRefreshToken,
        ExpiredToken,
        Cancelled,
    }

    public enum AuthenticationState {
        LoggedOut,
        LoggedIn,
        PendingAsk,
        PendingAskApproval,
        LoggingIn,
        PendingRefresh,
        Cancelling,
    }

    public class Authentication {
        public static Authentication Instance { get; }
        static Authentication() {
            Instance = new Authentication();
        }
        private const string RefreshKey = "RefreshToken";
        private const string CustomTokenLoginURL = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key=";
        private const string RefreshURL = "https://securetoken.googleapis.com/v1/token?key=";
        private LoginResponse _loginData;
        public static bool IsAdmin;
        private static DateTime _expiryTime;

        public EventHandler AuthStateChanged = delegate { };
        private AuthenticationState _authState = AuthenticationState.LoggedOut;
        public AuthenticationState AuthState {
            get => _authState;
            private set {
                if (_authState != value) {
                    _authState = value;
                    AuthStateChanged(this, EventArgs.Empty);
                }
            }
        }

        public bool IsLoggedIn => AuthState == AuthenticationState.LoggedIn;

        public string RemoteLoginPin { get; private set; }
        public string RemoteLoginUrl { get; private set; }

        public string LoginToken => _loginData.idToken;
        public string Uid => _loginData.localId;

        public bool IsLoginExpired => _expiryTime < DateTime.Now;

        public void LogOut(bool hardLogout = true) {
            _loginData = null;
            // hardLogout means the user told us to log out, as 
            // opposed to we just noticed the token expired, so refreshing
            if (hardLogout) {
                AuthState = AuthenticationState.LoggedOut;
                PlayerPrefs.SetString(RefreshKey, null);
                PlayerPrefs.Save();
                Backend.Instance.LogToServer(LoggingLevel.LOG, "Logout", "complete");
            }
        }

        public async Task<LoginResult> LoginAutomatic() {
            string token = PlayerPrefs.GetString(RefreshKey, null);
            if (String.IsNullOrEmpty(token)) {
                return LoginResult.NoRefreshToken;
            }

            AuthState = AuthenticationState.PendingRefresh;
            WWWForm postData = new();
            postData.AddField("grant_type", "refresh_token");
            postData.AddField("refresh_token", token);
            using UnityWebRequest www = UnityWebRequest.Post(RefreshURL + Global.FirebaseApikey, postData);
            Global.FireStatusChange(this, "Logging you in...");
            await www.SendWebRequest();
            string response = www.downloadHandler.text;
            if (response.Contains("TOKEN_EXPIRED")) {
                AuthState = AuthenticationState.LoggedOut;
                Global.FireStatusChange(this, "Error: Token has expired", true);
                return LoginResult.ExpiredToken;
            }

            if (response.Contains("USER_NOT_FOUND")) {
                AuthState = AuthenticationState.LoggedOut;
                Global.FireStatusChange(this, "Error: User was not found", true);
            } else if (response.Contains("INVALID_REFRESH_TOKEN")) {
                AuthState = AuthenticationState.LoggedOut;
                Global.FireStatusChange(this, "Error: Invalid token provided", true);
            } else if (response.Contains("id_token")) {
                RefreshResponse refreshData = JsonUtility.FromJson<RefreshResponse>(response);
                _loginData = new LoginResponse {
                    refreshToken = refreshData.refresh_token,
                    idToken = refreshData.id_token,
                    expiresIn = refreshData.expires_in,
                    localId = refreshData.user_id
                };
                Global.FireStatusChange(this, "Login successful!");
                _expiryTime = DateTime.Now.AddSeconds(_loginData.expiresIn);
                PlayerPrefs.SetString(RefreshKey, _loginData.refreshToken);
                PlayerPrefs.Save();
                AuthState = AuthenticationState.LoggedIn;
                return LoginResult.Success;
            } else {
                Debug.LogError(response);
                Global.FireStatusChange(this, "Unknown Error. Check console for more information.", true);
                return LoginResult.Error;
            }
            return LoginResult.Error;
        }
        public async Task<LoginResult> Login(bool stayLoggedIn) {
            AuthState = AuthenticationState.PendingAsk;

            try {
                Backend.Instance.LogToServer(LoggingLevel.LOG, "Login", "start");

                var response = await Backend.Instance.AskForRemoteLogin();
                if (AuthState == AuthenticationState.Cancelling) {
                    AuthState = AuthenticationState.LoggedOut;
                    Global.FireStatusChange(this, "Cancelled log in");
                    return LoginResult.Cancelled;
                }
                RemoteLoginPin = response.pin;
                RemoteLoginUrl = response.url;
                AuthState = AuthenticationState.PendingAskApproval;

                // every two seconds, check if the user has approved the login
                DateTime startPolling = DateTime.Now;
                GetRemoteLoginAskStatusResponse statusResponse = null;
                while (AuthState == AuthenticationState.PendingAskApproval && DateTime.Now - startPolling < TimeSpan.FromMinutes(6)) {
                    await Task.Delay(2000);
                    statusResponse = await Backend.Instance.GetRemoteLoginAskStatus(response.statusKey);
                    if (statusResponse.status != "pending") {
                        break;
                    }
                }

                if (AuthState == AuthenticationState.Cancelling) {
                    AuthState = AuthenticationState.LoggedOut;
                    Global.FireStatusChange(this, "Cancelled log in");
                    return LoginResult.Cancelled;
                }
                if (statusResponse != null && statusResponse.status == "granted") {
                    AuthState = AuthenticationState.LoggingIn;
                    Global.FireStatusChange(this, "Logging you in...");
                    WWWForm postData = new();
                    postData.AddField("token", statusResponse.token);
                    postData.AddField("returnSecureToken", "true");
                    using UnityWebRequest www = UnityWebRequest.Post(CustomTokenLoginURL + Global.FirebaseApikey, postData);
                    await www.SendWebRequest();
                    string signinResponse = www.downloadHandler.text;

                    if (signinResponse.Contains("idToken")) {
                        var signinData = JsonUtility.FromJson<LoginResponse>(signinResponse);
                        // the problem at this point is that the response only includes a token, refresh token, 
                        // and expiry. We need also the localId (user_id). So the first thing we do is a refresh. 
                        // we don't expect this to fail.
                        WWWForm refreshPostData = new();
                        refreshPostData.AddField("grant_type", "refresh_token");
                        refreshPostData.AddField("refresh_token", signinData.refreshToken);
                        using UnityWebRequest refreshRequest = UnityWebRequest.Post(RefreshURL + Global.FirebaseApikey, refreshPostData);
                        Global.FireStatusChange(this, "Verifying login...");
                        await refreshRequest.SendWebRequest();
                        string refreshResponse = refreshRequest.downloadHandler.text;
                        if (refreshResponse.Contains("id_token")) {
                            RefreshResponse refreshData = JsonUtility.FromJson<RefreshResponse>(refreshResponse);
                            _loginData = new LoginResponse {
                                refreshToken = refreshData.refresh_token,
                                idToken = refreshData.id_token,
                                expiresIn = refreshData.expires_in,
                                localId = refreshData.user_id
                            };
                            _expiryTime = DateTime.Now.AddSeconds(_loginData.expiresIn);
                            if (stayLoggedIn) {
                                PlayerPrefs.SetString(RefreshKey, _loginData.refreshToken);
                                PlayerPrefs.Save();
                            }
                            AuthState = AuthenticationState.LoggedIn;
                            Backend.Instance.LogToServer(LoggingLevel.LOG, "Login", "success");
                            Backend.Instance.LogAnalyticsEvent("login", new AnalyticsEventParam() { name = "method", value = "remote" });
                            return LoginResult.Success;
                        }

                        Debug.LogError(refreshResponse);
                        Backend.Instance.LogToServer(LoggingLevel.ERROR, "Login", "Failed to acquire refreshed security token");
                        Global.FireStatusChange(this, "Failed to acquire refreshed security token", true);
                    } else {
                        // important as status message tells user to check console
                        Debug.LogError(signinResponse);
                        Backend.Instance.LogToServer(LoggingLevel.ERROR, "Login", "Server provided invalid security token");
                        Global.FireStatusChange(this, "Server provided invalid security token", true);
                    }
                } else {
                    // we could distinguish between denied and expired, but we don't
                    Debug.LogError("Request expired.");
                    Backend.Instance.LogToServer(LoggingLevel.WARN, "Login", "This remote login request has expired");
                    Global.FireStatusChange(this, "This remote login request has expired", true);
                }
            } catch (Exception e) {
                Debug.LogError(e);
                Backend.Instance.LogToServer(LoggingLevel.ERROR, "Login", "Unknown Error:" + e.ToString());
                Global.FireStatusChange(this, "Unknown Error. Check console for more information.", true);
            }
            // if we got this far, we failed.
            AuthState = AuthenticationState.LoggedOut;
            Backend.Instance.LogToServer(LoggingLevel.LOG, "Login", "error");
            return LoginResult.Error;

        }

        public void CancelLogin() {
            AuthState = AuthenticationState.Cancelling;
            Global.FireStatusChange(this, "Cancelling log in");
        }
    }
    [Serializable]
    public class LoginResponse {
        public string localId;
        public string displayName;
        public string idToken;
        public string refreshToken;
        public int expiresIn;
    }

    [Serializable]
    public class RefreshResponse {
        public string id_token;
        public string refresh_token;
        public int expires_in;
        public string user_id;
    }

}