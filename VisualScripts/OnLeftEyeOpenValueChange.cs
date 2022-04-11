using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta {
    [UnitCategory("Events/Filta")]
    public class OnLeftEyeOpenValueChange : ManualEventUnit<Unit> {
        protected override string hookName => "LeftEyeOpen";

        [DoNotSerialize]
        private ValueOutput _leftEyeOpenValue;

        GraphReference _graph;
        private Simulator _simulator;

        private float _val;
        protected override void Definition() {
            base.Definition();
            _leftEyeOpenValue = ValueOutput("Left Eye Open Value", _ => _val);
        }

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<Simulator>();
            _simulator.onLeftEyeValueChange += OnLeftEyeOpenValueChanged;
        }

        private void OnLeftEyeOpenValueChanged(object sender, float val) {
            _val = val;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onLeftEyeValueChange -= OnLeftEyeOpenValueChanged;
        }
        
    }
}