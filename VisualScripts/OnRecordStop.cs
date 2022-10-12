using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnRecordStop : ManualEventUnit<Unit> {
        protected override string hookName => "stopRecord";

        GraphReference _graph;
        private SimulatorBase _simulator;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<SimulatorBase>();
            _simulator.onRecordStop += OnRecordStopped;
        }

        private void OnRecordStopped(object sender, EventArgs e) {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onRecordStop -= OnRecordStopped;
        }
        
    }
}
