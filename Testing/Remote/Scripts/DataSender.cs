using System;
using Mirror;
using UnityEngine;

public class DataSender : NetworkBehaviour {
    private RemoteManager _remoteManager;

    [SyncVar]
    private FaceData _data;

    private bool _cliented;
    private int _count;
    
    [Server]
    public override void OnStartServer() {
        _remoteManager = FindObjectOfType<RemoteManager>();
    }

    [Client]
    public override void OnStartLocalPlayer() {
        _remoteManager = FindObjectOfType<RemoteManager>();
        _cliented = true;
    }

    [Server]
    public void SetData(FaceData faceData) {
        _data = faceData;
    }

    [Client]
    private void Update() {
        if (_cliented) {
            transform.position = _data.facePosition;
            transform.eulerAngles = _data.faceRotation;
        }
    }

    public readonly struct FaceData : IEquatable<FaceData> {
        public readonly Vector3 facePosition;
        public readonly Vector3 faceRotation;

        public FaceData(Vector3 pos, Vector3 rot) {
            facePosition = pos;
            faceRotation = rot;
        }
        public bool Equals(FaceData other) {
            return false;
        }

        public override bool Equals(object obj) {
            return obj is FaceData other && Equals(other);
        }

        public override int GetHashCode() {
            return 0;
        }
    }
}
