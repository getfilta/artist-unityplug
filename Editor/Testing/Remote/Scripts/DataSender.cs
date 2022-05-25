using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DataSender : NetworkBehaviour {
    private RemoteManager _remoteManager;

    [SyncVar]
    public FaceData _data;

    [SyncVar]
    public BodyData _bodyData;

    [SyncVar(hook = nameof(OnGottenResolution))]
    private Vector3Int _screenResolution;
    
    private int _count;

    private int _simType;
    
    public struct SimulatorTypeMessage : NetworkMessage {
        public int type; //Face = 0, Body = 1
    }

    public void SendSimulatorType(int simType) {
        if (_simType != simType) {
            _simType = simType;
            SimulatorTypeMessage message = new SimulatorTypeMessage {type = simType};
            NetworkClient.Send(message, 0);
        }
    }

    [Server]
    public override void OnStartServer() {
        _remoteManager = FindObjectOfType<RemoteManager>();
        _remoteManager.sender = this;
    }

    [Client]
    public override void OnStartLocalPlayer() {
        _remoteManager = FindObjectOfType<RemoteManager>();
        _remoteManager.videoSender.SetupClient();
        RequestScreenResolution();
    }

    [Client]
    void OnGottenResolution(Vector3Int oldValue, Vector3Int newValue) {
#if UNITY_EDITOR
        GameViewUtils.SetGameView(GameViewUtils.GameViewSizeType.FixedResolution, newValue.x, newValue.y, "FiltaSimulatorRemote");
#endif
        _remoteManager.videoSender.Initialize(newValue);
    }


    [Server]
    public void SetData(FaceData faceData) {
        _data = faceData;
    }

    [Command]
    private void RequestScreenResolution() {
        _screenResolution = new Vector3Int(Display.main.systemWidth, Display.main.systemHeight);
    }

    public readonly struct BodyData : IEquatable<BodyData> {
        public readonly Joint[] joints;
        
        public BodyData(Joint[] joints) {
            this.joints = joints;
        }
        public readonly struct Joint : IEquatable<Joint> {
            public readonly Vector3 _localPose;
            public readonly Vector3 _localRotation;
            public readonly Vector3 _anchorPose;
            public readonly Vector3 _anchorRotation;

            public Joint(Vector3 lPose, Vector3 lRot, Vector3 aPose, Vector3 aRot) {
                _localPose = lPose;
                _localRotation = lRot;
                _anchorPose = aPose;
                _anchorRotation = aRot;
            }
            
            public bool Equals(Joint other) {
                return _localPose == other._localPose && _localRotation == other._localRotation;
            }

            public override bool Equals(object obj) => (obj is Joint other) && Equals(other);
            
            public override int GetHashCode() {
                return 0;
            }
        }
        
        public bool Equals(BodyData other) {
            return joints.Equals(other.joints);
        }

        public override bool Equals(object obj) {
            return obj is BodyData other && Equals(other);
        }

        public override int GetHashCode() {
            return 0;
        }
    }

    public readonly struct FaceData : IEquatable<FaceData> {
        public readonly List<ARKitBlendShapeCoefficient> blendshapeData;
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

        public FaceData(List<ARKitBlendShapeCoefficient> blendshape, Vector3 pos, Vector3 rot, Vector3 cameraPos, Vector3 cameraRot,
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
        
        public readonly struct ARKitBlendShapeCoefficient : IEquatable<ARKitBlendShapeCoefficient>{
            public readonly ARKitBlendShapeLocation blendShapeLocation;
            public readonly float coefficient;

            public ARKitBlendShapeCoefficient(ARKitBlendShapeLocation loc, float coeff) {
                blendShapeLocation = loc;
                coefficient = coeff;
            }
            
            public bool Equals(ARKitBlendShapeCoefficient other)
            {
                return
                    (blendShapeLocation == other.blendShapeLocation) &&
                    coefficient.Equals(other.coefficient);
            }

            public override bool Equals(object obj) => (obj is ARKitBlendShapeCoefficient other) && Equals(other);
            
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = ((int)blendShapeLocation).GetHashCode();
                    hash = hash * 486187739 + coefficient.GetHashCode();
                    return hash;
                }
            }
        }
    
        public enum ARKitBlendShapeLocation
        {
            BrowDownLeft,
            BrowDownRight,
            BrowInnerUp,
            BrowOuterUpLeft,
            BrowOuterUpRight,
            CheekPuff,
            CheekSquintLeft,
            CheekSquintRight,
            EyeBlinkLeft,
            EyeBlinkRight,
            EyeLookDownLeft,
            EyeLookDownRight,
            EyeLookInLeft,
            EyeLookInRight,
            EyeLookOutLeft,
            EyeLookOutRight,
            EyeLookUpLeft,
            EyeLookUpRight,
            EyeSquintLeft,
            EyeSquintRight,
            EyeWideLeft,
            EyeWideRight,
            JawForward,
            JawLeft,
            JawOpen,
            JawRight,
            MouthClose,
            MouthDimpleLeft,
            MouthDimpleRight,
            MouthFrownLeft,
            MouthFrownRight,
            MouthFunnel,
            MouthLeft,
            MouthLowerDownLeft,
            MouthLowerDownRight,
            MouthPressLeft,
            MouthPressRight,
            MouthPucker,
            MouthRight,
            MouthRollLower,
            MouthRollUpper,
            MouthShrugLower,
            MouthShrugUpper,
            MouthSmileLeft,
            MouthSmileRight,
            MouthStretchLeft,
            MouthStretchRight,
            MouthUpperUpLeft,
            MouthUpperUpRight,
            NoseSneerLeft,
            NoseSneerRight,
            TongueOut,
        }

        public bool Equals(FaceData other) {
            return facePosition == other.facePosition && faceRotation == other.faceRotation;
        }

        public override bool Equals(object obj) {
            return obj is FaceData other && Equals(other);
        }

        public override int GetHashCode() {
            return 0;
        }
    }
}
