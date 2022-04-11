using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

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
            _simulator.onMouthClose += OnMouthClosed;
        }

        private void OnMouthClosed(object sender, EventArgs e) {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onMouthClose -= OnMouthClosed;
        }
        
    }
}