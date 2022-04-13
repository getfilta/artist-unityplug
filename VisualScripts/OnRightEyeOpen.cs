using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnRightEyeOpen : ManualEventUnit<Unit> {
        protected override string hookName => "rightEyeOpen";

        GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onRightEyeOpen += OnRightEyeOpened;
        }

        private void OnRightEyeOpened(object sender, EventArgs e) {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _arTriggerEvents.onRightEyeOpen -= OnRightEyeOpened;
        }
        
    }
}
