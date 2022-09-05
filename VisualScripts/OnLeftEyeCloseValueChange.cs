using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnLeftEyeCloseValueChange : ManualEventUnit<Unit> {
        protected override string hookName => "LeftEyeClose";

        [DoNotSerialize]
        private ValueOutput _leftEyeCloseValue;

        GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        private float _val;
        protected override void Definition() {
            base.Definition();
            _leftEyeCloseValue = ValueOutput("Left Eye Close Value", _ => _val);
        }

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onLeftEyeValueChange += OnLeftEyeCloseValueChanged;
        }

        private void OnLeftEyeCloseValueChanged(object sender, float val) {
            _val = val;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _arTriggerEvents.onLeftEyeValueChange -= OnLeftEyeCloseValueChanged;
        }
        
    }
}