using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnMouthOpenValueChange : ManualEventUnit<Unit> {
        protected override string hookName => "mouthOpen";

        [DoNotSerialize]
        private ValueOutput _mouthOpenValue;

        GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        private float _val;
        protected override void Definition() {
            base.Definition();
            _mouthOpenValue = ValueOutput("Mouth Open Value", _ => _val);
        }

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onMouthOpenValueChange += OnMouthOpenValueChanged;
        }

        private void OnMouthOpenValueChanged(object sender, float val) {
            _val = val;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _arTriggerEvents.onMouthOpenValueChange -= OnMouthOpenValueChanged;
        }
        
    }
}