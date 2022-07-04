using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class VideoSender : MonoBehaviour {
    public struct VideoMessage : NetworkMessage {
        public byte[] data;
    }

    private RemoteManager _remoteManager;

    private Camera _mainCam;

    private RenderTexture _rt;
    public RawImage rawImage;

    private Vector2Int _displaySize;

    private bool _isInitialized;
    private Texture2D _screenTex;
    private Texture2D _tex;
    private Texture2D _remoteTex;

    public static RenderTexture _cameraFeed;


    private void Awake() {
        _remoteManager = GetComponent<RemoteManager>();
    }

    public void Initialize(Vector3Int screenSize) {
        rawImage.gameObject.SetActive(true);
        _displaySize = new Vector2Int(screenSize.x/screenSize.z, screenSize.y/screenSize.z);
        _isInitialized = true;
        _rt = new RenderTexture(_displaySize.x, _displaySize.y, 24);
        _screenTex = new Texture2D(screenSize.x, screenSize.y, TextureFormat.ARGB32, false);
        _tex = new Texture2D(_displaySize.x, _displaySize.y, TextureFormat.ARGB32, false);
        _remoteTex = new Texture2D(_displaySize.x, _displaySize.y, TextureFormat.ARGB32, false);
    }

    public void UnInitialize() {
        rawImage.gameObject.SetActive(false);
    }

    private void Update() {
        if (NetworkClient.isConnected && _isInitialized)
            StartCoroutine(SendVideo());
    }

    WaitForEndOfFrame _frameEnd = new WaitForEndOfFrame();

    [Client]
    public IEnumerator SendVideo() {
        yield return _frameEnd;
        _screenTex.ReadPixels(new Rect(0, 0, _screenTex.width, _screenTex.height), 0, 0, false);
        _screenTex.Apply();
        Resize();
        byte[] bytes = _tex.EncodeToJPG();
        VideoMessage message = new VideoMessage {data = bytes};
        NetworkClient.Send(message, 0);
    }

    [Client]
    public void SetupClient() {
        NetworkClient.RegisterHandler<VideoMessage>(OnVideoMessage, false);
    }

    void Resize() {
        RenderTexture.active = _rt;
        Graphics.Blit(_screenTex, _rt);
        _tex.ReadPixels(new Rect(0, 0, _displaySize.x, _displaySize.y), 0, 0);
        _tex.Apply();
        RenderTexture.active = null;
    }

    void OnVideoMessage(VideoMessage videoMessage) {
        if (!_isInitialized) {
            return;
        }

        _remoteTex.LoadImage(videoMessage.data);
        rawImage.texture = _remoteTex;
        if (_cameraFeed != null) {
            RenderTexture.active = _cameraFeed;
            Graphics.Blit(_remoteTex, _cameraFeed);
        }
    }
}
