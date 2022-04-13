using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnRightEyeOpenValueChange : ManualEventUnit<Unit> {
        protected override string hookName => "RightEyeOpen";

        [DoNotSerialize]
        private ValueOutput _rightEyeOpenValue;

        GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        private float _val;
        protected override void Definition() {
            base.Definition();
            _rightEyeOpenValue = ValueOutput("Right Eye Open Value", _ => _val);
        }

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onRightEyeValueChange += OnRightEyeOpenValueChanged;
        }

        private void OnRightEyeOpenValueChanged(object sender, float val) {
            _val = val;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _arTriggerEvents.onRightEyeValueChange -= OnRightEyeOpenValueChanged;
        }
        
    }
}