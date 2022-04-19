using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class VideoSender : MonoBehaviour
{
    public struct VideoMessage : NetworkMessage {
        public byte[] data;
    }

    private RemoteManager _remoteManager;

    private Camera _mainCam;

    private RenderTexture _rt;
    public RawImage rawImage;

    private Vector2Int _displaySize;

    private bool _isInitialized;
    private Texture2D _tex;
    private Texture2D _remoteTex;
    

    private void Awake() {
        _remoteManager = GetComponent<RemoteManager>();
    }

    public void Initialize(Vector2Int screenSize) {
        rawImage.gameObject.SetActive(true);
        _displaySize = screenSize;
        _isInitialized = true;
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
        _tex.ReadPixels(new Rect(0, 0, _tex.width, _tex.height), 0, 0, false);
        byte[] bytes = _tex.EncodeToJPG();
        VideoMessage message = new VideoMessage {data = bytes};
        NetworkClient.Send(message, 0);
    }
    
    [Client]
    public void SetupClient() {
        NetworkClient.RegisterHandler<VideoMessage>(OnVideoMessage, false);
    }
    
    void OnVideoMessage(VideoMessage videoMessage) {
        if (!_isInitialized) {
            return;
        }

        _remoteTex.LoadImage(videoMessage.data);
        rawImage.texture = _remoteTex;
    }
}
