using System;
using System.Collections;
using System.Collections.Generic;
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

    private Vector2Int _displaySize;

    private bool _isInitialized;

    private void Awake() {
        _remoteManager = GetComponent<RemoteManager>();
        _mainCam = _remoteManager.captureCamera.GetComponent<Camera>();
    }

    public void Initialize(Vector2Int screenSize) {
        _displaySize = screenSize;
        _rt = new RenderTexture(_displaySize.x, _displaySize.y, 32, RenderTextureFormat.ARGB32);
        _mainCam.targetTexture = _rt;
        _isInitialized = true;
        //FillWithEmptiness();
    }
    
    private void Update() {
        if (NetworkClient.isConnected && _isInitialized)
            StartCoroutine(SendVideo());
    }
    WaitForEndOfFrame _frameEnd = new WaitForEndOfFrame();

    [Client]
    public IEnumerator SendVideo() {
        yield return _frameEnd;
        RenderTexture.active = _rt;
        Texture2D tex = new Texture2D(_displaySize.x, _displaySize.y, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
        byte[] bytes = tex.EncodeToPNG();
        VideoMessage message = new VideoMessage {data = bytes};
        NetworkClient.Send(message, 0);
    }
}
