using Unity.VisualScripting;
using UnityEngine;

namespace Filta {
    [UnitCategory("Events/Filta")]
    public class OnMouthOpen : ManualEventUnit<Unit> {
        protected override string hookName => "mouthOpen";

        GraphReference _graph;
        private Simulator _simulator;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<Simulator>();
            _simulator.onMouthOpen.AddListener(OnMouthOpened);
        }

        private void OnMouthOpened() {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onMouthOpen.RemoveListener(OnMouthOpened);
        }
        
    }
}
