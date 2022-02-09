using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class BodySimulator : SimulatorBase
{
    public override SimulatorType _simulatorType => SimulatorType.Body;

    [SerializeField]
    private Transform _bodyVisualiser;

    [SerializeField]
    private Transform _bodyReference;

    private Avatar _referenceAvatar;
    private Avatar _visualiserAvatar;
    
    #region Trackers
    [SerializeField]
    private Transform _bodyTracker;

    [SerializeField]
    private Transform _bodyAvatars;

    [SerializeField]
    private Transform _lShoulderTracker;
    
    [SerializeField]
    private Transform _rShoulderTracker;
    
    [SerializeField]
    private Transform _lArmTracker;
    
    [SerializeField]
    private Transform _rArmTracker;

    [SerializeField]
    private Transform _lForearmTracker;
    
    [SerializeField]
    private Transform _rForearmTracker;
    
    [SerializeField]
    private Transform _lHandTracker;
    
    [SerializeField]
    private Transform _rHandTracker;
    
    [SerializeField]
    private Transform _lUpLegTracker;
    
    [SerializeField]
    private Transform _rUpLegTracker;
    
    [SerializeField]
    private Transform _lLegTracker;

    [SerializeField]
    private Transform _rLegTracker;

    [SerializeField]
    private Transform _lFootTracker;

    [SerializeField]
    private Transform _rFootTracker;

    [SerializeField]
    private Transform _spineTracker;

    [SerializeField]
    private Transform _neckTracker;

    [SerializeField]
    private Transform _headTracker;
    
    
    #endregion

    private bool _skipBodySimulator;
    private bool _skipBodyRecording;

    private long _prevTime;
    private int _previousFrame;
    
    private DateTime _startTime;

    [NonSerialized]
    public bool isPlaying;
    
    private ARBodyRecording _bodyRecording;
    private long _recordingLength;
    
    private void Awake(){
        isPlaying = true;
        _startTime = DateTime.Now;
        ToggleVisualiser(false);
        TryAutomaticSetup();
        InitializeBodyAvatars();
    }

    protected override void OnEnable(){
        base.OnEnable();
#if UNITY_EDITOR
        EditorApplication.hierarchyChanged += GetBodyAvatars;
#endif
        if (IsSetUpProperly()){
            InitializeBodyAvatars();
            GetRecordingData();
        }
    }

    private void OnDisable(){
#if UNITY_EDITOR
        EditorApplication.hierarchyChanged -= GetBodyAvatars;
#endif
    }
    private void GetRecordingData() {
        byte[] data = File.ReadAllBytes(Path.Combine(_filePath, "Simulator/BodyTracking/BodyRecording"));
        string bodyData = Encoding.ASCII.GetString(data);
        _bodyRecording = JsonConvert.DeserializeObject<ARBodyRecording>(bodyData);
        _recordingLength = _bodyRecording._bodyData[_bodyRecording._bodyData.Count - 1]._timestamp;
    }

    public override bool IsSetUpProperly(){
        return _filterObject != null && _bodyVisualiser != null && _visualiserAvatar != null && _bodyReference != null && _referenceAvatar != null && _bodyTracker != null &&
               _bodyAvatars != null && _lShoulderTracker != null && _rShoulderTracker != null && _lArmTracker != null &&
               _rArmTracker != null && _lForearmTracker != null && _rForearmTracker != null && _lHandTracker != null &&
               _rHandTracker != null && _lUpLegTracker != null && _rUpLegTracker != null && _lLegTracker != null &&
               _rLegTracker != null && _lFootTracker != null && _rFootTracker != null && _spineTracker != null &&
               _neckTracker != null && _headTracker != null;
    }

    protected override void EnforceObjectStructure(){
        _filterObject.name = "FilterBody";
        _bodyTracker.name = "BodyTracker";
        _bodyAvatars.name = "BodyAvatar";
        _lShoulderTracker.name = "LShoulderTracker";
        _rShoulderTracker.name = "RShoulderTracker";
        _lArmTracker.name = "LArmTracker";
        _rArmTracker.name = "RArmTracker";
        _lForearmTracker.name = "LForearmTracker";
        _rForearmTracker.name = "RForearmTracker";
        _lHandTracker.name = "LHandTracker";
        _rHandTracker.name = "RHandTracker";
        _lUpLegTracker.name = "LUpLegTracker";
        _rUpLegTracker.name = "RUpLegTracker";
        _lLegTracker.name = "LLegTracker";
        _rLegTracker.name = "RLegTracker";
        _lFootTracker.name = "LFootTracker";
        _rFootTracker.name = "RFootTracker";
        _spineTracker.name = "SpineTracker";
        _neckTracker.name = "NeckTracker";
        _headTracker.name = "HeadTracker";
    }

    private void OnRenderObject(){
#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying){
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }
    protected override void Update(){
        if (_skipBodySimulator)
            return;
        if ((_bodyVisualiser != null && _visualiserAvatar == null) || (_bodyReference != null && _referenceAvatar == null)){
            InitializeBodyAvatars();
        }
        if (!IsSetUpProperly()) {
            Debug.LogError(
                "The simulator object is not set up properly. Try clicking the Automatically Set Up button in the Dev Panel");
            _skipBodySimulator = true;
            return;
        }
        EnforceObjectStructure();
        if ((_bodyRecording._bodyData == null || _bodyRecording._bodyData.Count == 0) && !_skipBodyRecording){
            try{
                GetRecordingData();
            }
            catch (Exception e){
                Debug.LogError($"Could not get recorded body data. {e.Message}");
                _skipBodyRecording = true;
            }
        }
        
        if (!isPlaying) {
            _startTime = DateTime.Now;
            return;
        }

        long time = (long)(DateTime.Now - _startTime).TotalMilliseconds + _pauseTime;
        Playback(time);
    }
    
    private long _pauseTime;
    public void PauseSimulator(){
        _pauseTime = (long)(DateTime.Now - _startTime).TotalMilliseconds + _pauseTime;
        isPlaying = false;
    }

    public void ResumeSimulator(){
        isPlaying = true;
    }
    
    void Replay() {
        _startTime = DateTime.Now;
    }

    private void Playback(long currentTime){
        if (_recordingLength <= 0){
            return;
        }

        if (currentTime > _recordingLength){
            Replay();
            _pauseTime = 0;
            return;
        }

        if (_prevTime > currentTime){
            _previousFrame = 0;
        }

        _prevTime = currentTime;
        for (int i = _previousFrame; i < _bodyRecording._bodyData.Count; i++){
            ARBodyData bodyData = _bodyRecording._bodyData[i];
            long nextTimeStamp = bodyData._timestamp;
            if (nextTimeStamp < currentTime){
                if (i == _bodyRecording._bodyData.Count - 1){
                    i = 0;
                    break;
                }

                continue;
            }

            if (i == 0){
                break;
            }
            UpdateBodyVisualiser(bodyData);
            PositionTrackers(bodyData);
            UpdateBodyAvatars(bodyData);
            _previousFrame = i;
            break;
        }
    }

    public override void TryAutomaticSetup(){
        if (IsSetUpProperly()){
            return;
        }

        if (_bodyVisualiser == null){
            _bodyVisualiser = transform.GetChild(0);
        }

        if (_bodyReference == null){
            _bodyReference = transform.GetChild(1);
        }
        
        if (_filterObject == null){
            _filterObject = GameObject.Find("FilterBody").transform;
        }

        if (_filterObject != null){
            if (_bodyTracker == null){
                _bodyTracker = _filterObject.Find("BodyTracker");
            }
        }

        if (_bodyTracker != null){
            if (_bodyAvatars == null){
                _bodyAvatars = _bodyTracker.Find("BodyAvatar");
            }
            if (_lShoulderTracker == null){
                _lShoulderTracker = _bodyTracker.Find("LShoulderTracker");
            }
            if (_rShoulderTracker == null){
                _rShoulderTracker = _bodyTracker.Find("RShoulderTracker");
            }
            if (_lArmTracker == null){
                _lArmTracker = _bodyTracker.Find("LArmTracker");
            }
            if (_rArmTracker == null){
                _rArmTracker = _bodyTracker.Find("RArmTracker");
            }
            if (_lForearmTracker == null){
                _lForearmTracker = _bodyTracker.Find("LForearmTracker");
            }
            if (_rForearmTracker == null){
                _rForearmTracker = _bodyTracker.Find("RForearmTracker");
            }
            if (_lHandTracker == null){
                _lHandTracker = _bodyTracker.Find("LHandTracker");
            }
            if (_rHandTracker == null){
                _rHandTracker = _bodyTracker.Find("RHandTracker");
            }
            if (_lUpLegTracker == null){
                _lUpLegTracker = _bodyTracker.Find("LUpLegTracker");
            }
            if (_rUpLegTracker == null){
                _rUpLegTracker = _bodyTracker.Find("RUpLegTracker");
            }
            if (_lLegTracker == null){
                _lLegTracker = _bodyTracker.Find("LLegTracker");
            }
            if (_rLegTracker == null){
                _rLegTracker = _bodyTracker.Find("RLegTracker");
            }
            if (_lFootTracker == null){
                _lFootTracker = _bodyTracker.Find("LFootTracker");
            }
            if (_rFootTracker == null){
                _rFootTracker = _bodyTracker.Find("RFootTracker");
            }
            if (_spineTracker == null){
                _spineTracker = _bodyTracker.Find("SpineTracker");
            }
            if (_neckTracker == null){
                _neckTracker = _bodyTracker.Find("NeckTracker");
            }
            if (_headTracker == null){
                _headTracker = _bodyTracker.Find("HeadTracker");
            }
            
        }
        InitializeBodyAvatars();
        if (IsSetUpProperly()) {
            _skipBodySimulator = false;
            Debug.Log("Successfully Set up");
        } else {
            _skipBodySimulator = true;
            Debug.LogError("Failed to set up simulator");
        }
    }

    [NonSerialized]
    public bool isPose;
    public void ToggleVisualiser(bool setToPose){
        isPose = setToPose;
        _bodyVisualiser.gameObject.SetActive(!isPose);
        _bodyReference.gameObject.SetActive(isPose);
    }

    void PositionTrackers(ARBodyData bodyData){
        List<ARBodyData.Joint> joints = bodyData._joints;
        _bodyTracker.position = _visualiserAvatar._boneMapping[(int)Avatar.JointIndices.Root].position;
        _bodyTracker.eulerAngles = _visualiserAvatar._boneMapping[(int)Avatar.JointIndices.Root].eulerAngles;
        _lShoulderTracker.localPosition = joints[(int) Avatar.JointIndices.LeftShoulder1]._anchorPose;
        _lShoulderTracker.localEulerAngles = joints[(int) Avatar.JointIndices.LeftShoulder1]._anchorRotation;
        _rShoulderTracker.localPosition = joints[(int) Avatar.JointIndices.RightShoulder1]._anchorPose;
        _rShoulderTracker.localEulerAngles = joints[(int) Avatar.JointIndices.RightShoulder1]._anchorRotation;
        _lArmTracker.localPosition = joints[(int) Avatar.JointIndices.LeftArm]._anchorPose;
        _lArmTracker.localEulerAngles = joints[(int) Avatar.JointIndices.LeftArm]._anchorRotation;
        _rArmTracker.localPosition = joints[(int) Avatar.JointIndices.RightArm]._anchorPose;
        _rArmTracker.localEulerAngles = joints[(int) Avatar.JointIndices.RightArm]._anchorRotation;
        _lForearmTracker.localPosition = joints[(int) Avatar.JointIndices.LeftForearm]._anchorPose;
        _lForearmTracker.localEulerAngles = joints[(int) Avatar.JointIndices.LeftForearm]._anchorRotation;
        _rForearmTracker.localPosition = joints[(int) Avatar.JointIndices.RightForearm]._anchorPose;
        _rForearmTracker.localEulerAngles = joints[(int) Avatar.JointIndices.RightForearm]._anchorRotation;
        _lHandTracker.localPosition = joints[(int) Avatar.JointIndices.LeftHand]._anchorPose;
        _lHandTracker.localEulerAngles = joints[(int) Avatar.JointIndices.LeftHand]._anchorRotation;
        _rHandTracker.localPosition = joints[(int) Avatar.JointIndices.RightHand]._anchorPose;
        _rHandTracker.localEulerAngles = joints[(int) Avatar.JointIndices.RightHand]._anchorRotation;
        _lUpLegTracker.localPosition = joints[(int) Avatar.JointIndices.LeftUpLeg]._anchorPose;
        _lUpLegTracker.localEulerAngles = joints[(int) Avatar.JointIndices.LeftUpLeg]._anchorRotation;
        _rUpLegTracker.localPosition = joints[(int) Avatar.JointIndices.RightUpLeg]._anchorPose;
        _rUpLegTracker.localEulerAngles = joints[(int) Avatar.JointIndices.RightUpLeg]._anchorRotation;
        _lLegTracker.localPosition = joints[(int) Avatar.JointIndices.LeftLeg]._anchorPose;
        _lLegTracker.localEulerAngles = joints[(int) Avatar.JointIndices.LeftLeg]._anchorRotation;
        _rLegTracker.localPosition = joints[(int) Avatar.JointIndices.RightLeg]._anchorPose;
        _rLegTracker.localEulerAngles = joints[(int) Avatar.JointIndices.RightLeg]._anchorRotation;
        _lFootTracker.localPosition = joints[(int) Avatar.JointIndices.LeftFoot]._anchorPose;
        _lFootTracker.localEulerAngles = joints[(int) Avatar.JointIndices.LeftFoot]._anchorRotation;
        _rFootTracker.localPosition = joints[(int) Avatar.JointIndices.RightFoot]._anchorPose;
        _rFootTracker.localEulerAngles = joints[(int) Avatar.JointIndices.RightFoot]._anchorRotation;
        _spineTracker.localPosition = joints[(int) Avatar.JointIndices.Spine1]._anchorPose;
        _spineTracker.localEulerAngles = joints[(int) Avatar.JointIndices.Spine1]._anchorRotation;
        _neckTracker.localPosition = joints[(int) Avatar.JointIndices.Neck1]._anchorPose;
        _neckTracker.localEulerAngles = joints[(int) Avatar.JointIndices.Neck1]._anchorRotation;
        _headTracker.localPosition = joints[(int) Avatar.JointIndices.Head]._anchorPose;
        _headTracker.localEulerAngles = joints[(int) Avatar.JointIndices.Head]._anchorRotation;
    }
    
    #region Body Avatars

    private List<Avatar> _avatars;
    private int _avatarCount;
    void GetBodyAvatars(){
        if (_avatarCount == _bodyAvatars.childCount) {
            return;
        }
        InitializeBodyAvatars();
        _avatarCount = _avatars.Count;
    }

    void InitializeBodyAvatars(){
        _avatars = new List<Avatar>();
        _referenceAvatar = new Avatar(_bodyReference);
        _visualiserAvatar = new Avatar(_bodyVisualiser);
        _visualiserAvatar.Compensate(_visualiserAvatar._boneMapping);
        for (int i = 0; i < _bodyAvatars.childCount; i++){
            Avatar avatar = new Avatar(_bodyAvatars.GetChild(i));
            avatar.Compensate(_referenceAvatar._boneMapping);
            _avatars.Add(avatar);
        }
    }

    void UpdateBodyVisualiser(ARBodyData bodyData){
        _visualiserAvatar.ApplyBodyPose(bodyData);
    }
    
    void UpdateBodyAvatars(ARBodyData bodyData){
        if (_avatars == null){
            return;
        }
        for (int i = 0; i < _avatars.Count; i++){
            Avatar avatar = _avatars[i];
            for (int j = 0; j < avatar._boneMapping.Length; j++){
                avatar.ApplyBodyPose(bodyData);
            }
        }
    }

    class Avatar
    {
        public Transform root;
        public Transform[] _boneMapping = new Transform[NumSkeletonJoints];

        public Avatar(Transform model){
            root = model;
            InitializeSkeletonJoints();
        }

        public void Compensate(Transform[] visualiser){
            for (int i = 0; i < _boneMapping.Length; i++){
                if (_boneMapping[i] != null && visualiser[i] != null){
                    compensation[i] = Quaternion.Inverse(visualiser[i].rotation) * _boneMapping[i].rotation;
                }
                else{
                    compensation[i] = Quaternion.identity;
                }
            }
        }

        public Quaternion[] compensation = new Quaternion[NumSkeletonJoints];
        
        // 3D joint skeleton
        public enum JointIndices
        {
            Invalid = -1,
            Root = 0, // parent: <none> [-1]
            Hips = 1, // parent: Root [0]
            LeftUpLeg = 2, // parent: Hips [1]
            LeftLeg = 3, // parent: LeftUpLeg [2]
            LeftFoot = 4, // parent: LeftLeg [3]
            LeftToes = 5, // parent: LeftFoot [4]
            LeftToesEnd = 6, // parent: LeftToes [5]
            RightUpLeg = 7, // parent: Hips [1]
            RightLeg = 8, // parent: RightUpLeg [7]
            RightFoot = 9, // parent: RightLeg [8]
            RightToes = 10, // parent: RightFoot [9]
            RightToesEnd = 11, // parent: RightToes [10]
            Spine1 = 12, // parent: Hips [1]
            Spine2 = 13, // parent: Spine1 [12]
            Spine3 = 14, // parent: Spine2 [13]
            Spine4 = 15, // parent: Spine3 [14]
            Spine5 = 16, // parent: Spine4 [15]
            Spine6 = 17, // parent: Spine5 [16]
            Spine7 = 18, // parent: Spine6 [17]
            LeftShoulder1 = 19, // parent: Spine7 [18]
            LeftArm = 20, // parent: LeftShoulder1 [19]
            LeftForearm = 21, // parent: LeftArm [20]
            LeftHand = 22, // parent: LeftForearm [21]
            LeftHandIndexStart = 23, // parent: LeftHand [22]
            LeftHandIndex1 = 24, // parent: LeftHandIndexStart [23]
            LeftHandIndex2 = 25, // parent: LeftHandIndex1 [24]
            LeftHandIndex3 = 26, // parent: LeftHandIndex2 [25]
            LeftHandIndexEnd = 27, // parent: LeftHandIndex3 [26]
            LeftHandMidStart = 28, // parent: LeftHand [22]
            LeftHandMid1 = 29, // parent: LeftHandMidStart [28]
            LeftHandMid2 = 30, // parent: LeftHandMid1 [29]
            LeftHandMid3 = 31, // parent: LeftHandMid2 [30]
            LeftHandMidEnd = 32, // parent: LeftHandMid3 [31]
            LeftHandPinkyStart = 33, // parent: LeftHand [22]
            LeftHandPinky1 = 34, // parent: LeftHandPinkyStart [33]
            LeftHandPinky2 = 35, // parent: LeftHandPinky1 [34]
            LeftHandPinky3 = 36, // parent: LeftHandPinky2 [35]
            LeftHandPinkyEnd = 37, // parent: LeftHandPinky3 [36]
            LeftHandRingStart = 38, // parent: LeftHand [22]
            LeftHandRing1 = 39, // parent: LeftHandRingStart [38]
            LeftHandRing2 = 40, // parent: LeftHandRing1 [39]
            LeftHandRing3 = 41, // parent: LeftHandRing2 [40]
            LeftHandRingEnd = 42, // parent: LeftHandRing3 [41]
            LeftHandThumbStart = 43, // parent: LeftHand [22]
            LeftHandThumb1 = 44, // parent: LeftHandThumbStart [43]
            LeftHandThumb2 = 45, // parent: LeftHandThumb1 [44]
            LeftHandThumbEnd = 46, // parent: LeftHandThumb2 [45]
            Neck1 = 47, // parent: Spine7 [18]
            Neck2 = 48, // parent: Neck1 [47]
            Neck3 = 49, // parent: Neck2 [48]
            Neck4 = 50, // parent: Neck3 [49]
            Head = 51, // parent: Neck4 [50]
            Jaw = 52, // parent: Head [51]
            Chin = 53, // parent: Jaw [52]
            LeftEye = 54, // parent: Head [51]
            LeftEyeLowerLid = 55, // parent: LeftEye [54]
            LeftEyeUpperLid = 56, // parent: LeftEye [54]
            LeftEyeball = 57, // parent: LeftEye [54]
            Nose = 58, // parent: Head [51]
            RightEye = 59, // parent: Head [51]
            RightEyeLowerLid = 60, // parent: RightEye [59]
            RightEyeUpperLid = 61, // parent: RightEye [59]
            RightEyeball = 62, // parent: RightEye [59]
            RightShoulder1 = 63, // parent: Spine7 [18]
            RightArm = 64, // parent: RightShoulder1 [63]
            RightForearm = 65, // parent: RightArm [64]
            RightHand = 66, // parent: RightForearm [65]
            RightHandIndexStart = 67, // parent: RightHand [66]
            RightHandIndex1 = 68, // parent: RightHandIndexStart [67]
            RightHandIndex2 = 69, // parent: RightHandIndex1 [68]
            RightHandIndex3 = 70, // parent: RightHandIndex2 [69]
            RightHandIndexEnd = 71, // parent: RightHandIndex3 [70]
            RightHandMidStart = 72, // parent: RightHand [66]
            RightHandMid1 = 73, // parent: RightHandMidStart [72]
            RightHandMid2 = 74, // parent: RightHandMid1 [73]
            RightHandMid3 = 75, // parent: RightHandMid2 [74]
            RightHandMidEnd = 76, // parent: RightHandMid3 [75]
            RightHandPinkyStart = 77, // parent: RightHand [66]
            RightHandPinky1 = 78, // parent: RightHandPinkyStart [77]
            RightHandPinky2 = 79, // parent: RightHandPinky1 [78]
            RightHandPinky3 = 80, // parent: RightHandPinky2 [79]
            RightHandPinkyEnd = 81, // parent: RightHandPinky3 [80]
            RightHandRingStart = 82, // parent: RightHand [66]
            RightHandRing1 = 83, // parent: RightHandRingStart [82]
            RightHandRing2 = 84, // parent: RightHandRing1 [83]
            RightHandRing3 = 85, // parent: RightHandRing2 [84]
            RightHandRingEnd = 86, // parent: RightHandRing3 [85]
            RightHandThumbStart = 87, // parent: RightHand [66]
            RightHandThumb1 = 88, // parent: RightHandThumbStart [87]
            RightHandThumb2 = 89, // parent: RightHandThumb1 [88]
            RightHandThumbEnd = 90, // parent: RightHandThumb2 [89]
        }
        const int NumSkeletonJoints = 91;

        private void InitializeSkeletonJoints()
        {
            // Walk through all the child joints in the skeleton and
            // store the skeleton joints at the corresponding index in the m_BoneMapping array.
            // This assumes that the bones in the skeleton are named as per the
            // JointIndices enum above.
            Queue<Transform> nodes = new Queue<Transform>();
            nodes.Enqueue(root);
            while (nodes.Count > 0)
            {
                Transform next = nodes.Dequeue();
                for (int i = 0; i < next.childCount; ++i)
                {
                    nodes.Enqueue(next.GetChild(i));
                }
                ProcessJoint(next);
            }
        }

        public void ApplyBodyPose(ARBodyData body)
        {
            List<ARBodyData.Joint> joints = body._joints;

            for (int i = 0; i < NumSkeletonJoints; ++i)
            {
                ARBodyData.Joint joint = joints[i];
                var bone = _boneMapping[i];
                if (bone != null)
                {
                    bone.transform.rotation = Quaternion.Euler(joint._anchorRotation) * compensation[i];
                }
            }
        }

        void ProcessJoint(Transform joint)
        {
            int index = GetJointIndex(joint.name);
            if (index >= 0 && index < NumSkeletonJoints)
            {
                _boneMapping[index] = joint;
            }
        }

        // Returns the integer value corresponding to the JointIndices enum value
        // passed in as a string.
        int GetJointIndex(string jointName)
        {
            JointIndices val;
            if (Enum.TryParse(jointName, out val))
            {
                return (int)val;
            }
            return -1;
        }
    }
    
    #endregion
    
    #region Class/Struct Declarations

    [Serializable]
    public class ARBodyRecording
    {
        public List<ARBodyData> _bodyData;
    }
    
    [Serializable]
    public class ARBodyData
    {
        public List<Joint> _joints;
        public long _timestamp;

        [Serializable]
        public class Joint
        {
            public Vector3Json _localPose;
            public Vector3Json _localRotation;
            public Vector3Json _anchorPose;
            public Vector3Json _anchorRotation;
            
            [Serializable]
            public struct Vector3Json
            {
                public float x, y, z;

                public static implicit operator Vector3Json(Vector3 vector){
                    return new Vector3Json{x = vector.x, y = vector.y, z = vector.z};
                }

                public static implicit operator Vector3(Vector3Json vector){
                    return new Vector3{x = vector.x, y = vector.y, z = vector.z};
                }
            }
        }
    }
    #endregion
}
