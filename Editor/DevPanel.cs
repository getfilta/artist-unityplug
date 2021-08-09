using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;
using Newtonsoft.Json;
using Filta.Datatypes;
using UnityEditor.SceneManagement;

namespace Filta
{
    public class DevPanel : EditorWindow
    {
        private string email = "";
        private string password = "";
        private bool _stayLoggedIn;
        private const string TEST_FUNC_LOCATION = "http://localhost:5000/filta-machina/us-central1/";
        private const string FUNC_LOCATION = "https://us-central1-filta-machina.cloudfunctions.net/";
        private const string REFRESH_KEY = "RefreshToken";
        private string UPLOAD_URL { get { return runLocally ? TEST_FUNC_LOCATION + "uploadArtSource" : FUNC_LOCATION + "uploadArtSource"; } }
        private string DELETE_PRIV_ART_URL { get { return runLocally ? TEST_FUNC_LOCATION + "deletePrivArt" : FUNC_LOCATION + "deletePrivArt"; } }
        private const string loginURL = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=";
        private const string refreshURL = "https://securetoken.googleapis.com/v1/token?key=";
        private const string fbaseKey = "AIzaSyAiefSo-GLf2yjEwbXhr-1MxMx0A6vXHO0";
        private const string variantTempSave = "Assets/FilterVariant.prefab";
        private string _statusBar = "";
        private string statusBar { get { return _statusBar; } set { _statusBar = value; this.Repaint(); } }
        private string assetBundlePath = "";
        private bool runLocally = false;
        private string selectedArtTitle = "";
        private string selectedArtKey = "";
        private Dictionary<string, ArtMeta> privateCollection = new Dictionary<string, ArtMeta>();
        private static LoginResponse loginData;
        private int selGridInt = 0;

        private PluginInfo _pluginInfo;
        private static DateTime _expiryTime;

        [MenuItem("Filta/Artist Panel")]
        static void Init()
        {
            DevPanel window = (DevPanel)EditorWindow.GetWindow(typeof(DevPanel), true, "Filta: Artist Panel");
            window.Show();
        }
        
        #region Simulator
        private Simulator _simulator;

        private bool _activeSimulator;
        private bool _loggingIn;

        private void OnEnable(){
            GameObject simulatorObject = GameObject.Find("Simulator");
            if (simulatorObject != null){
                _simulator = simulatorObject.GetComponent<Simulator>();
                if (_simulator != null){
                    _activeSimulator = true;
                }
            }

            _pluginInfo = new PluginInfo{version = 1};
            if (loginData == null || String.IsNullOrEmpty(loginData.idToken)){
                LoginAutomatic();
            }
            else{
                if (DateTime.Now > _expiryTime){
                    loginData = null;
                    LoginAutomatic();
                }
                else{
                    GetPrivateCollection();
                }
            }
        }

        private void HandleSimulator(){
            if (!_activeSimulator) return;
            EditorGUILayout.LabelField("Simulator", EditorStyles.boldLabel);
            if (_simulator.isPlaying){
                if (GUILayout.Button("Stop")){
                    _simulator.isPlaying = false;
                }
            }
            else{
                if (GUILayout.Button("Play")){
                    _simulator.isPlaying = true;
                }
            }
        }

        #endregion

        void OnGUI()
        {
            CreateNewScene();
            Login();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            
            HandleSimulator();

            if (loginData != null && loginData.idToken != "")
            {
                if (selectedArtKey != "")
                {
                    SelectedArt();
                }
                else
                {
                    PrivateCollection();
                }
            }

            GUILayout.FlexibleSpace();
            runLocally = GUILayout.Toggle(runLocally, "(ADVANCED) Use local firebase host");
            if (GUILayout.Button("Get latest plugin version"))
            {
                UpdatePanel();
            };

            EditorGUILayout.LabelField(statusBar, EditorStyles.boldLabel);
        }

        private void UpdatePanel()
        {
            UnityEditor.PackageManager.Client.Add("https://github.com/getfilta/artist-unityplug.git");
        }

        private string sceneName;
        void CreateNewScene(){
            sceneName = (string)EditorGUILayout.TextField("filter name", sceneName);
            if (GUILayout.Button("Create new filter")){
                bool success;
                success = AssetDatabase.CopyAsset("Packages/com.getfilta.artist-unityplug/Core/templateScene.unity", $"Assets/Filters/{sceneName}.unity");
                //success = AssetDatabase.CopyAsset("Assets/Core/templateScene.unity", $"Assets/Filters/{sceneName}.unity");
                if (!success)
                    statusBar = "Failed to create new filter scene";
                else{
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()){
                        EditorSceneManager.SaveOpenScenes();
                    }

                    EditorSceneManager.OpenScene($"Assets/Filters/{sceneName}.unity", OpenSceneMode.Single);
                }
            }
            
        }

        private async void GenerateAndUploadAssetBundle(string name)
        {
            if (String.IsNullOrEmpty(selectedArtKey)){
                selectedArtKey = SceneManager.GetActiveScene().name;
                Debug.LogError("Error uploading! selectedArtKey is empty. Please report this bug");
                return;
            }
            bool assetBundleButton = GUILayout.Button($"Generate & upload asset bundle: {name}");
            if (!assetBundleButton) { return; }

            statusBar = "Generating asset bundles";

            var filterObject = GameObject.Find("Filter");
            if (filterObject == null)
            {
                EditorUtility.DisplayDialog("Error", "The object 'Filter' wasn't found in the hierarchy. Did you rename/remove it?", "Ok");
                return;
            }
            
            try
            {
                //PrefabUtility.ApplyPrefabInstance(filterObject, InteractionMode.AutomatedAction);
                PrefabUtility.SaveAsPrefabAsset(filterObject, variantTempSave, out bool success);
                if (success){
                    AssetImporter.GetAtPath(variantTempSave).assetBundleName =
                        "filter";
                }
                
            } catch
            {
                EditorUtility.DisplayDialog("Error", "The object 'Filter' isn't a prefab. Did you delete it from your assets?", "Ok");
                return;
            }

            try{
                File.WriteAllText(
                    Path.Combine(Application.dataPath, "pluginInfo.json"),JsonConvert.SerializeObject(_pluginInfo));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch{
                Debug.Log("Could not attach plugin info");
            }
            string assetBundleDirectory = "AssetBundles";
            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            var manifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                    BuildAssetBundleOptions.None,
                                    BuildTarget.iOS);
            assetBundlePath = $"{assetBundleDirectory}/{name}";
            statusBar = "Asset bundle generated";


            statusBar = "Connecting...";
            Hash128 hash;
            if (!BuildPipeline.GetHashForAssetBundle(assetBundlePath, out hash))
            {
                statusBar = "Asset bundle not found";
                return;
            }
            WWWForm postData = new WWWForm();
            if (selectedArtKey != "temp")
            {
                Debug.LogWarning("Updating Art with artid: " + selectedArtKey);
                postData.AddField("artid", selectedArtKey);
            }
            postData.AddField("uid", loginData.idToken);
            postData.AddField("hash", hash.ToString());
            postData.AddField("title", selectedArtTitle);
            var www = UnityWebRequest.Post(UPLOAD_URL, postData);
            await www.SendWebRequest();
            statusBar = "Connected! Uploading...";
            var response = www.downloadHandler.text;
            UploadBundleResponse parsed;
            try
            {
                parsed = JsonUtility.FromJson<UploadBundleResponse>(response);
            }
            catch
            {
                statusBar = "Error! Check console for more information";
                Debug.LogError(response);
                return;
            }
            var bytes = File.ReadAllBytes(assetBundlePath);
            var upload = UnityWebRequest.Put(parsed.url, bytes);
            await upload.SendWebRequest();
            await GetPrivateCollection();
            selectedArtKey = parsed.artid;
            statusBar = "Upload successful";
            AssetDatabase.DeleteAsset(variantTempSave);
        }

        private async void LoginAutomatic(){
            string token = PlayerPrefs.GetString(REFRESH_KEY, null);
            if (String.IsNullOrEmpty(token)){
                return;
            }

            _loggingIn = true;
            WWWForm postData = new WWWForm();
            postData.AddField("grant_type", "refresh_token");
            postData.AddField("refresh_token", token);
            UnityWebRequest www = UnityWebRequest.Post(refreshURL + fbaseKey, postData);
            statusBar = "Logging you in...";
            await www.SendWebRequest();
            _loggingIn = false;
            string response = www.downloadHandler.text;
            if (response.Contains("TOKEN_EXPIRED"))
            {
                statusBar = "Error: Token has expired";
            }
            else if (response.Contains("USER_NOT_FOUND"))
            {
                statusBar = "Error: User was not found";
            }
            else if (response.Contains("INVALID_REFRESH_TOKEN"))
            {
                statusBar = "Error: Invalid token provided";
            }
            else if (response.Contains("id_token")){
                RefreshResponse refreshData = JsonUtility.FromJson<RefreshResponse>(response);
                loginData = new LoginResponse{refreshToken = refreshData.refresh_token, idToken = refreshData.id_token, expiresIn = refreshData.expires_in, localId = refreshData.user_id};
                statusBar = $"Login successful!";
                _expiryTime = DateTime.Now.AddSeconds(loginData.expiresIn);
                PlayerPrefs.SetString(REFRESH_KEY, loginData.refreshToken);
                PlayerPrefs.Save();
                try
                {
                    await GetPrivateCollection();
                }
                catch (Exception e)
                {
                    statusBar = "Error downloading collection. Try again. Check console for more information.";
                    Debug.LogError("Error downloading: " + e.Message);
                }
            }
            else
            {
                statusBar = "Unknown Error. Check console for more information.";
                Debug.LogError(response);
            }
        }

        private async void Login()
        {
            if (loginData != null && !String.IsNullOrEmpty(loginData.idToken))
            {
                bool logout = GUILayout.Button("Logout");
                if (logout)
                {
                    password = "";
                    loginData = null;
                    PlayerPrefs.SetString(REFRESH_KEY, null);
                    PlayerPrefs.Save();
                    GUI.FocusControl(null);
                }
                return;
            }
            if (!_loggingIn){
                EditorGUILayout.LabelField("Login to your user account", EditorStyles.boldLabel);
                email = (string)EditorGUILayout.TextField("email", email);
                password = (string)EditorGUILayout.PasswordField("password", password);
                _stayLoggedIn = EditorGUILayout.Toggle("Stay logged in", _stayLoggedIn);
            }
            bool submitButton = !_loggingIn && GUILayout.Button("Login");
            if (!submitButton) { return; }
            
            selectedArtKey = "";
            _loggingIn = true;
            WWWForm postData = new WWWForm();
            postData.AddField("email", email);
            postData.AddField("password", password);
            postData.AddField("returnSecureToken", "true");
            var www = UnityWebRequest.Post(loginURL + fbaseKey, postData);
            statusBar = "Connecting...";

            await www.SendWebRequest();
            _loggingIn = false;
            var response = www.downloadHandler.text;
            if (response.Contains("EMAIL_NOT_FOUND"))
            {
                statusBar = "Error: Email not found";
            }
            else if (response.Contains("MISSING_PASSWORD"))
            {
                statusBar = "Error: Missing Password";
            }
            else if (response.Contains("INVALID_PASSWORD"))
            {
                statusBar = "Error: Invalid Password";
            }
            else if (response.Contains("idToken"))
            {
                loginData = JsonUtility.FromJson<LoginResponse>(response);
                statusBar = $"Login successful!";
                _expiryTime = DateTime.Now.AddSeconds(loginData.expiresIn);
                if (_stayLoggedIn){
                    PlayerPrefs.SetString(REFRESH_KEY, loginData.refreshToken);
                    PlayerPrefs.Save();
                }
                
                try
                {
                    await GetPrivateCollection();
                }
                catch (Exception e)
                {
                    statusBar = "Error downloading collection. Try again. Check console for more information.";
                    Debug.LogError("Error downloading: " + e.Message);
                }
            }
            else
            {
                statusBar = "Unknown Error. Check console for more information.";
                Debug.LogError(response);
            }
        }

        private async Task GetPrivateCollection()
        {
            string url = $"https://filta-machina.firebaseio.com/priv_collection/{loginData.localId}/.json?auth={loginData.idToken}";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                await req.SendWebRequest();
                if (req.isNetworkError || req.isHttpError)
                {
                    throw new Exception(req.error.ToString());
                }
                var result = JsonConvert.DeserializeObject<Dictionary<string, ArtMeta>>(req.downloadHandler.text);
                privateCollection = result;
                this.Repaint();
            }
        }

        private void PrivateCollection()
        {
            EditorGUILayout.LabelField("Choose the filter to update:", EditorStyles.boldLabel);
            bool newClicked = GUILayout.Button("CREATE NEW PIECE");
            EditorGUILayout.Space();
            if (newClicked)
            {
                selectedArtTitle = SceneManager.GetActiveScene().name;
                selectedArtKey = "temp";
            }
            
            if (privateCollection == null || privateCollection.Count < 1) { return; }
            foreach (var item in privateCollection)
            {
                bool clicked = GUILayout.Button(item.Value.title);
                if (clicked)
                {
                    selectedArtTitle = item.Value.title;
                    selectedArtKey = item.Key;
                }
            }
            

        }

        private async void DeletePrivArt(string artId)
        {
            if (GUILayout.Button("Delete upload from Filta"))
            {
                if (!EditorUtility.DisplayDialog("Delete", "Are you sure you want to delete this from Filta?", "yes", "cancel"))
                {
                    return;
                }
            }
            else
            {
                return;
            }

            statusBar = "Deleting...";
            try
            {
                WWWForm postData = new WWWForm();
                postData.AddField("uid", loginData.idToken);
                postData.AddField("artid", artId);
                var www = UnityWebRequest.Post(DELETE_PRIV_ART_URL, postData);
                await www.SendWebRequest();
                var response = www.downloadHandler.text;
                if (www.isHttpError || www.isNetworkError)
                {
                    statusBar = $"Error Deleting. Check console for details.";
                    Debug.LogError(www.error + " " + www.downloadHandler.text);
                    return;
                }
                statusBar = $"Delete: {response}";
            }
            finally
            {
                privateCollection.Remove(selectedArtKey);
                selectedArtKey = "";
            }
        }

        private void SelectedArt()
        {
            if (GUILayout.Button("Back"))
            {
                selectedArtKey = "";
            }
            EditorGUILayout.Space();
            selectedArtTitle = (string)EditorGUILayout.TextField("Title", selectedArtTitle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Select an asset bundle to upload:");

            var names = AssetDatabase.GetAllAssetBundleNames();


            if (names.Length == 0)
            {
                EditorGUILayout.LabelField("No asset bundles found. Create one!");
            }
            else
            {
                selGridInt = GUILayout.SelectionGrid(selGridInt, names, names.Length);
                EditorGUILayout.Space();
                GenerateAndUploadAssetBundle(names[selGridInt]);
            }
                EditorGUILayout.Space();
                EditorGUILayout.Space();

            DeletePrivArt(selectedArtKey);
        }
    }

    [Serializable]
    public class LoginResponse
    {
        public string localId;
        public string displayName;
        public string idToken;
        public string refreshToken;
        public int expiresIn;
    }

    [Serializable]
    public class RefreshResponse
    {
        public string id_token;
        public string refresh_token;
        public int expires_in;
        public string user_id;
    }

    [Serializable]
    public class UploadBundleResponse
    {
        public string url;
        public string artid;
    }

    public class UnityWebRequestAwaiter : INotifyCompletion
    {
        private UnityWebRequestAsyncOperation asyncOp;
        private Action continuation;

        public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
        {
            this.asyncOp = asyncOp;
            asyncOp.completed += OnRequestCompleted;
        }

        public bool IsCompleted { get { return asyncOp.isDone; } }

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
        }

        private void OnRequestCompleted(AsyncOperation obj)
        {
            continuation();
        }
    }

    public static class ExtensionMethods
    {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            return new UnityWebRequestAwaiter(asyncOp);
        }
    }

    public struct PluginInfo
    {
        public int version;
    }
}