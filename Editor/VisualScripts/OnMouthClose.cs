using Unity.VisualScripting;
using UnityEngine;

namespace Filta {
    [UnitCategory("Events/Filta")]
    public class OnMouthClose : ManualEventUnit<Unit> {
        protected override string hookName => "mouthClose";

        GraphReference _graph;
        private Simulator _simulator;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<Simulator>();
            _simulator.onMouthClose.AddListener(OnMouthClosed);
        }

        private void OnMouthClosed() {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onMouthClose.RemoveListener(OnMouthClosed);
        }
        
    }
}