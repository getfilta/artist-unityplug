using Unity.VisualScripting;
using UnityEngine;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta/Internal")]
    public class OnArModeSwitch : ManualEventUnit<Unit> {
        protected override string hookName => "modeSwitch";

        [DoNotSerialize]
        private ValueOutput _eventName;

        private GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        private bool _val;

        protected override void Definition() {
            base.Definition();
            _eventName = ValueOutput("Is New Mode AR", _ => _val);
        }

        public override void StartListening(GraphStack stack) {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onArModeSwitch += OnModeSwitch;
        }

        private void OnModeSwitch(object sender, bool isAr) {
            _val = isAr;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack) {
            base.StopListening(stack);
            _arTriggerEvents.onArModeSwitch -= OnModeSwitch;
        }
    }
}
