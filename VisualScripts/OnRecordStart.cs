using System;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace Filta.VisualScripting {
    [UnitCategory("Events/Filta")]
    public class OnRecordStart : ManualEventUnit<Unit> {
        protected override string hookName => "startRecord";

        GraphReference _graph;
        private SimulatorBase _simulator;

        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            _graph = stack.AsReference();
            _simulator = Object.FindObjectOfType<SimulatorBase>();
            _simulator.onRecordStart += OnRecordStarted;
        }

        private void OnRecordStarted(object sender, EventArgs e) {
            Trigger(_graph, this);
        }

        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);
            _simulator.onRecordStart -= OnRecordStarted;
        }
        
    }
}
