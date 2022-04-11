using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta {
    [UnitCategory("Events/Filta")]
    public class OnRightEyeOpenValueChange : ManualEventUnit<Unit> {
        protected override string hookName => "RightEyeOpen";

        [DoNotSerialize]
        private ValueOutput _rightEyeOpenValue;

        GraphReference _graph;
        private Simulator _simulator;

        private float _val;
        protected override void Definition() {
            base.Definition();
            _rightEyeOpenValue = ValueOutput("Right Eye Open Value", _ => _val);
        }

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<Simulator>();
            _simulator.onRightEyeValueChange += OnRightEyeOpenValueChanged;
        }

        private void OnRightEyeOpenValueChanged(object sender, float val) {
            _val = val;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onRightEyeValueChange -= OnRightEyeOpenValueChanged;
        }
        
    }
}