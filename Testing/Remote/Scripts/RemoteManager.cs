using System;
using Mirror;
using Mirror.Discovery;
using UnityEngine;

public class RemoteManager : MonoBehaviour {
    private NetworkDiscovery _networkDiscovery;

    public readonly int ResolutionFactor = 10;
    [NonSerialized]
    public DataSender sender;

    [NonSerialized]
    public VideoSender videoSender;
    
    public Transform captureCamera;

    private void Awake() {
        _networkDiscovery = GetComponent<NetworkDiscovery>();
        videoSender = GetComponent<VideoSender>();
    }

    private void OnEnable() {
        FindServers();
    }

    public void FindServers() {
        _networkDiscovery.StartDiscovery();
    }

    public void OnDiscoveredServer(ServerResponse response) {
        _networkDiscovery.StopDiscovery();
        NetworkManager.singleton.StartClient(response.uri);
    }
}
