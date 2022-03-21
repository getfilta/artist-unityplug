using System;
using Mirror;
using UnityEditor;
using UnityEngine;

public class DataSender : NetworkBehaviour {
    private RemoteManager _remoteManager;

    [SyncVar]
    private FaceData _data;

    [SyncVar(hook = nameof(OnGottenResolution))]
    private Vector2Int _screenResolution;

    private bool _cliented;
    private int _count;

    [Server]
    public override void OnStartServer() {
        _remoteManager = FindObjectOfType<RemoteManager>();
        _remoteManager.sender = this;
    }

    [Client]
    public override void OnStartLocalPlayer() {
        _cliented = true;
        RequestScreenResolution();
    }

    [Client]
    void OnGottenResolution(Vector2Int oldValue, Vector2Int newValue) {
#if UNITY_EDITOR
        GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, GameViewSizeGroupType.iOS, newValue.x, newValue.y, "FiltaSimulatorRemote");
#endif
    }


    [Server]
    public void SetData(FaceData faceData) {
        _data = faceData;
    }

    [Client]
    private void Update() {
        if (_cliented) {
            _remoteManager.captureCamera.position = _data.cameraPosition;
            _remoteManager.captureCamera.eulerAngles = _data.cameraRotation;
        }
    }

    [Command]
    private void RequestScreenResolution() {
        _screenResolution = new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
    }

    public readonly struct FaceData : IEquatable<FaceData> {
        public readonly float[] blendshapeData;
        public readonly Vector3 facePosition;
        public readonly Vector3 faceRotation;
        public readonly Vector3 leftEyePosition;
        public readonly Vector3 leftEyeRotation;
        public readonly Vector3 rightEyePosition;
        public readonly Vector3 rightEyeRotation;
        public readonly Vector3 cameraPosition;
        public readonly Vector3 cameraRotation;
        public readonly Vector3[] vertices;
        public readonly Vector3[] normals;
        public readonly int[] indices;
        public readonly Vector2[] uvs;

        public FaceData(float[] blendshape, Vector3 pos, Vector3 rot, Vector3 cameraPos, Vector3 cameraRot,
            Vector3 leftEyePos, Vector3 leftEyeRot, Vector3 rightEyePos, Vector3 rightEyeRot, Vector3[] vert,
            Vector3[] norm, int[] ind, Vector2[] uv) {
            blendshapeData = blendshape;
            facePosition = pos;
            faceRotation = rot;
            cameraPosition = cameraPos;
            cameraRotation = cameraRot;
            leftEyePosition = leftEyePos;
            leftEyeRotation = leftEyeRot;
            rightEyePosition = rightEyePos;
            rightEyeRotation = rightEyeRot;
            vertices = vert;
            normals = norm;
            indices = ind;
            uvs = uv;
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
