using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnScreenTap : ManualEventUnit<Unit> {
        protected override string hookName => "screenTap";

        GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onScreenTap += OnScreenTapped;
        }

        private void OnScreenTapped(object sender, EventArgs e) {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _arTriggerEvents.onScreenTap -= OnScreenTapped;
        }
        
    }
}