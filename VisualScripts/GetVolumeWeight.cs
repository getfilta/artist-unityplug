using Unity.VisualScripting;
using UnityEngine.Rendering;

namespace Filta.VisualScripting {
    [UnitCategory("Filta/Internal")]
    public class GetVolumeWeight : Unit {
        [DoNotSerialize]
        public ValueInput volume;

        [DoNotSerialize]
        public ValueOutput weight;

        private float _weightValue;

        protected override void Definition() {
            volume = ValueInput<Volume>("Volume", null);
            weight = ValueOutput<float>("Weight", (flow) => flow.GetValue<Volume>(volume).weight);
        }
    }
}
