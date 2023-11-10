using Unity.VisualScripting;
using UnityEngine;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta/Internal")]
    public class OnZooPalStateChange : ManualEventUnit<Unit> {
        protected override string hookName => "zooPalStateChange";

        [DoNotSerialize]
        private ValueOutput _affection;
        [DoNotSerialize]
        private ValueOutput _wellBeing;
        [DoNotSerialize]
        private ValueOutput _fullness;
        [DoNotSerialize]
        private ValueOutput _cleanliness;
        
        private GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        private float _affectionValue;
        private float _wellBeingValue;
        private int _fullnessValue;
        private int _cleanlinessValue;

        protected override void Definition() {
            base.Definition();
            _affection = ValueOutput("Affection Value", _ => _affectionValue);
            _wellBeing = ValueOutput("WellBeing Value", _ => _wellBeingValue);
            _fullness = ValueOutput("Fullness Value", _ => _fullnessValue);
            _cleanliness = ValueOutput("Cleanliness Value", _ => _cleanlinessValue);
        }

        public override void StartListening(GraphStack stack) {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onZooPalStateChange += OnNewZooPalStateChange;
        }

        private void OnNewZooPalStateChange(object sender, ChatheadPetState newState) {
            _affectionValue = newState.affection;
            _wellBeingValue = newState.wellBeing;
            _fullnessValue = newState.fullness;
            _cleanlinessValue = newState.cleanliness;
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack) {
            base.StopListening(stack);
            _arTriggerEvents.onZooPalStateChange -= OnNewZooPalStateChange;
        }
    }
}