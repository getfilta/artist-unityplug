using System;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnFaceUpdate : ManualEventUnit<Unit> {
        protected override string hookName => "faceUpdate";

        [DoNotSerialize]
        private ValueOutput _positionValue;
        
        [DoNotSerialize]
        private ValueOutput _rotationValue;

        GraphReference _graph;
        private Simulator _simulator;

        private Vector3 _pos;
        private Vector3 _rot;
        
        protected override void Definition() {
            base.Definition();
            _positionValue = ValueOutput("Position", _ => _pos);
            _rotationValue = ValueOutput("Rotation", _ => _rot);
        }

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<Simulator>();
            _simulator.onFaceUpdate += OnFaceUpdated;
        }

        private void OnFaceUpdated(object sender, Simulator.FaceData.Trans val) {
            _pos = val.localPosition;
            _rot = val.localRotation;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onFaceUpdate -= OnFaceUpdated;
        }
        
    }
}