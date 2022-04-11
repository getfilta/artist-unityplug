using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

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
            _simulator.onMouthOpen += OnMouthOpened;
        }

        private void OnMouthOpened(object sender, EventArgs e) {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onMouthOpen -= OnMouthOpened;
        }
        
    }
}
