using Unity.VisualScripting;
using UnityEngine;

namespace Filta {
    [UnitCategory("Events/Filta")]
    public class OnMouthOpenValueChange : ManualEventUnit<Unit> {
        protected override string hookName => "mouthOpen";

        [DoNotSerialize]
        private ValueOutput _mouthOpenValue;

        GraphReference _graph;
        private Simulator _simulator;

        private float _val;
        protected override void Definition() {
            base.Definition();
            _mouthOpenValue = ValueOutput("Mouth Open Value", _ => _val);
        }

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<Simulator>();
            _simulator.onMouthOpenValueChange.AddListener(OnMouthOpenValueChanged);
        }

        private void OnMouthOpenValueChanged(float value) {
            _val = value;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onMouthOpenValueChange.RemoveListener(OnMouthOpenValueChanged);
        }
        
    }
}