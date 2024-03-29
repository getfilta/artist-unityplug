using Unity.VisualScripting;
using UnityEngine;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta/Internal")]
    public class OnPetEvent : ManualEventUnit<Unit> {
        protected override string hookName => "petEvent";
        
        [DoNotSerialize]
        private ValueOutput _eventName;
        
        private GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        private Vector3 _val;

        protected override void Definition() {
            base.Definition();
            _eventName = ValueOutput("World Position", _ => _val);
        }

        public override void StartListening(GraphStack stack) {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onPetEvent += OnNewPetEvent;
        }

        private void OnNewPetEvent(object sender, Vector3 pos) {
            _val = pos;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack) {
            base.StopListening(stack);
            _arTriggerEvents.onPetEvent -= OnNewPetEvent;
        }
    }
}

