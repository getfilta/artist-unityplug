using System;
using System.Collections.Generic;
using Mirror;
using Mirror.Discovery;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class RemoteManager : MonoBehaviour {
    [SerializeField]
    private TextMeshProUGUI status;

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
        status.text = "Started discovery";
    }

    public void OnDiscoveredServer(ServerResponse response) {
        _networkDiscovery.StopDiscovery();
        NetworkManager.singleton.StartClient(response.uri);
        status.text = "Connected to client";
    }
}
