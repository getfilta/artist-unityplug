using System;
using UnityEngine;

namespace Filta.VisualScripting {
    public class ArTriggerEvents : MonoBehaviour {
        private Simulator _simulator;

        public EventHandler onMouthOpen = delegate { };
        public EventHandler onMouthClose = delegate { };
        public EventHandler<float> onMouthOpenValueChange = delegate { };

        public EventHandler onRightEyeOpen = delegate { };
        public EventHandler onRightEyeClose = delegate { };
        public EventHandler<float> onRightEyeValueChange = delegate { };

        public EventHandler onLeftEyeOpen = delegate { };
        public EventHandler onLeftEyeClose = delegate { };
        public EventHandler<float> onLeftEyeValueChange = delegate { };

        private const float JawOpenFactor = 10f;
        private const float EyeBlinkFactor = 60f;

        private bool _isMouthOpen;
        private bool _isRightEyeOpen;
        private bool _isLeftEyeOpen;

        private void Awake() {
            _simulator = GetComponent<Simulator>();
        }

        public void Start() {
            _simulator.updateBlendShapeWeightEvent += OnUpdateBlendShapeWeightEvent;
        }

        public void OnDestroy() {
            _simulator.updateBlendShapeWeightEvent -= OnUpdateBlendShapeWeightEvent;
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
