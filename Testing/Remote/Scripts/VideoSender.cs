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

    public Camera mainCam;
    public RawImage rawImage;

    private RenderTexture _rt;

    [NonSerialized]
    public bool connected;

    private Vector2Int displaySize;
    
    private void OnEnable() {
        displaySize = new Vector2Int(Display.main.systemWidth / 10, Display.main.systemHeight / 10);
        _rt = new RenderTexture(displaySize.x, displaySize.y, 32, RenderTextureFormat.ARGB32);
        mainCam.targetTexture = _rt;
        //FillWithEmptiness();
    }
    
    private void Update() {
        if (NetworkClient.isConnected)
            StartCoroutine(SendVideo());
    }
    WaitForEndOfFrame _frameEnd = new WaitForEndOfFrame();
    
    void FillWithEmptiness() {
        Texture2D tex = new Texture2D (displaySize.x, displaySize.y, TextureFormat.ARGB32, false);
 
        Color fillColor = Color.clear;
        Color[] fillPixels = new Color[tex.width * tex.height];
 
        for (int i = 0; i < fillPixels.Length; i++)
        {
            fillPixels[i] = fillColor;
        }
 
        tex.SetPixels(fillPixels);
        tex.Apply();
        rawImage.texture = tex;
    }

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
