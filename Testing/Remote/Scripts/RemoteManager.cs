using System;
using Mirror;
using Mirror.Discovery;
using TMPro;
using UnityEngine;

public class RemoteManager : MonoBehaviour {
    private NetworkDiscovery _networkDiscovery;

    private void Awake() {
        _networkDiscovery = GetComponent<NetworkDiscovery>();
        FindServers();
    }

    private void FindServers() {
        _networkDiscovery.StartDiscovery();
    }

    private void OnDiscoveredServer(ServerResponse response) {
        _networkDiscovery.StopDiscovery();
        NetworkManager.singleton.StartClient(response.uri);
    }
}
