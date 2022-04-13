using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnLeftEyeClose : ManualEventUnit<Unit> {
        protected override string hookName => "leftEyeClose";

        GraphReference _graph;
        private ArTriggerEvents _arTriggerEvents;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _arTriggerEvents = Object.FindObjectOfType<ArTriggerEvents>();
            _arTriggerEvents.onLeftEyeClose += OnLeftEyeClosed;
        }

        private void OnLeftEyeClosed(object sender, EventArgs e) {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _arTriggerEvents.onLeftEyeClose -= OnLeftEyeClosed;
        }
        
    }
}