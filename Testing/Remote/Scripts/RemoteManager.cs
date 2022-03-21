using System;
using Mirror;
using Mirror.Discovery;
using UnityEngine;

public class RemoteManager : MonoBehaviour {
    private NetworkDiscovery _networkDiscovery;

    [NonSerialized]
    public DataSender sender;
    
    public Transform captureCamera;

    private void Awake() {
        _networkDiscovery = GetComponent<NetworkDiscovery>();
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
