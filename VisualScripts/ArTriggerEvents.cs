using System;
using UnityEngine;

namespace Filta.VisualScripting {
    public class ArTriggerEvents : MonoBehaviour {
        private Simulator _simulator;
        public static EventHandler<bool> onArModeSwitch = delegate { };

        //in seconds
        public const float MaximumDoubleTapTime = 0.25f;

        public EventHandler onMouthOpen = delegate { };
        public EventHandler onMouthClose = delegate { };
        public EventHandler<float> onMouthOpenValueChange = delegate { };

        public EventHandler onRightEyeOpen = delegate { };
        public EventHandler onRightEyeClose = delegate { };
        public EventHandler<float> onRightEyeValueChange = delegate { };

        public EventHandler onLeftEyeOpen = delegate { };
        public EventHandler onLeftEyeClose = delegate { };
        public EventHandler<float> onLeftEyeValueChange = delegate { };

        public EventHandler onScreenTap = delegate { };
        public EventHandler onScreenDoubleTap = delegate { };

        public EventHandler<string> onZooPalEvent = delegate { };
        public EventHandler<Vector3> onPetEvent = delegate { };
        public EventHandler<Vector3> onPetDragEvent = delegate { };

        private const float JawOpenFactor = 10f;
        private const float EyeBlinkFactor = 60f;

        private bool _isMouthOpen;
        private bool _isRightEyeOpen;
        private bool _isLeftEyeOpen;

        private float _tapTime;
        private bool _isDragging;
        private Vector2 _prevDragPos;
        private Vector3 _prevWorldPos;
        private float _dragDistance;
        private const float MinDragDistance = 2f; //in screenspace pixels
        private const float PetDragThreshold = 2000f;
        protected static readonly int Petted = Animator.StringToHash("Petted");
        protected static readonly int FullPetted = Animator.StringToHash("FullPetted");//in screenspace pixels

        private void Awake() {
            _simulator = GetComponent<Simulator>();
        }

        public void Start() {
            _simulator.updateBlendShapeWeightEvent += OnUpdateBlendShapeWeightEvent;
            onZooPalEvent += HandleZooPalEvent;
        }

        public void OnDestroy() {
            _simulator.updateBlendShapeWeightEvent -= OnUpdateBlendShapeWeightEvent;
            onZooPalEvent -= HandleZooPalEvent;
        }

        void Update() {
            _tapTime += Time.deltaTime;
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) {
                onScreenTap(this, null);
                if (_tapTime < MaximumDoubleTapTime) {
                    onScreenDoubleTap(this, null);
                }
                _tapTime = 0;
            }
            
            if (Input.GetMouseButtonUp(0)) {
                Ray ray = _simulator.mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100)) {
                    if (_simulator.zooPalAnimator != null) {
                        _simulator.zooPalAnimator.SetTrigger(Petted);
                    }
                    onPetEvent.Invoke(this, hit.point);
                }
            }

            if (Input.GetMouseButton(0)) {
                HandleDragPetting();
            } else {
                ResetDrag();
            }
        }
        
        private void HandleZooPalEvent(object sender, string e) {
            if (e == "pet") {
                if (_simulator.zooPalAnimator != null) {
                    _simulator.zooPalAnimator.SetTrigger(FullPetted);
                }
            }
        }

        private void HandleDragPetting() {
            Vector2 currentDragPos = Input.mousePosition;
            Ray ray = _simulator.mainCamera.ScreenPointToRay(currentDragPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100)) {
                if (_isDragging && currentDragPos != _prevDragPos) {
                    float drag = Vector3.Distance(currentDragPos, _prevDragPos);
                    if (drag > MinDragDistance) {
                        onPetDragEvent.Invoke(this, _prevWorldPos);
                        _dragDistance += drag;
                        if (_dragDistance > PetDragThreshold) {
                            ResetDrag();
                            _dragDistance = 0;
                            onZooPalEvent.Invoke(this, "pet");
                        }
                    }
                    
                }
                _isDragging = true;
                _prevWorldPos = hit.point;
                _prevDragPos = Input.mousePosition;
            } else {
                ResetDrag();
            }
        }

        private void ResetDrag() {
            _isDragging = false;
            _prevDragPos = Vector2.zero;
            _prevWorldPos = Vector3.zero;
        }

        private void OnUpdateBlendShapeWeightEvent(object sender, Simulator.UpdateBlendShapeWeightEventArgs e) {
            switch (e.Location) {
                case Simulator.ARKitBlendShapeLocation.JawOpen:
                    HandleMouthOpening(e.Weight);
                    break;
                case Simulator.ARKitBlendShapeLocation.EyeBlinkLeft:
                    HandleLeftEyeOpening(e.Weight);
                    break;
                case Simulator.ARKitBlendShapeLocation.EyeBlinkRight:
                    HandleRightEyeOpening(e.Weight);
                    break;
            }
        }

        void HandleMouthOpening(float coefficient) {
            onMouthOpenValueChange(this, coefficient);
            if (coefficient > JawOpenFactor) {
                if (!_isMouthOpen) {
                    onMouthOpen(this, null);
                }

                _isMouthOpen = true;
            } else {
                if (_isMouthOpen) {
                    onMouthClose(this, null);
                }

                _isMouthOpen = false;
            }
        }

        void HandleLeftEyeOpening(float coefficient) {
            onLeftEyeValueChange(this, coefficient);
            if (coefficient < EyeBlinkFactor) {
                if (!_isLeftEyeOpen) {
                    onLeftEyeOpen(this, null);
                }

                _isLeftEyeOpen = true;
            } else {
                if (_isLeftEyeOpen) {
                    onLeftEyeClose(this, null);
                }

                _isLeftEyeOpen = false;
            }
        }

        void HandleRightEyeOpening(float coefficient) {
            onRightEyeValueChange(this, coefficient);
            if (coefficient < EyeBlinkFactor) {
                if (!_isRightEyeOpen) {
                    onRightEyeOpen(this, null);
                }

                _isRightEyeOpen = true;
            } else {
                if (_isRightEyeOpen) {
                    onRightEyeClose(this, null);
                }

                _isRightEyeOpen = false;
            }
        }
    }
}
