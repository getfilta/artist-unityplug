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

    private Vector2Int displaySize;

    private void Awake() {
        _remoteManager = GetComponent<RemoteManager>();
        _mainCam = _remoteManager.captureCamera.GetComponent<Camera>();
    }

    private void OnEnable() {
        displaySize = new Vector2Int(Display.main.systemWidth / 10, Display.main.systemHeight / 10);
        _rt = new RenderTexture(displaySize.x, displaySize.y, 32, RenderTextureFormat.ARGB32);
        _mainCam.targetTexture = _rt;
        //FillWithEmptiness();
    }
    
    private void Update() {
        if (NetworkClient.isConnected)
            StartCoroutine(SendVideo());
    }
    WaitForEndOfFrame _frameEnd = new WaitForEndOfFrame();

    [Client]
    public IEnumerator SendVideo() {
        yield return _frameEnd;
        RenderTexture.active = _rt;
        Texture2D tex = new Texture2D(displaySize.x, displaySize.y, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
        byte[] bytes = tex.EncodeToPNG();
        VideoMessage message = new VideoMessage {data = bytes};
        NetworkClient.Send(message, 0);
    }
}
