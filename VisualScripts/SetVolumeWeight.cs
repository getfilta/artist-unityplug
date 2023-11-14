using Unity.VisualScripting;
using UnityEngine.Rendering;

namespace Filta.VisualScripting {
    [UnitCategory("Filta/Internal")]
    public class SetVolumeWeight : Unit {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ValueInput volume;
        
        [DoNotSerialize]
        public ValueInput newWeight;

        [DoNotSerialize]
        public ValueOutput weightResult;
        
        private float _newWeightValue;

        protected override void Definition() {
            inputTrigger = ControlInput("inputTrigger", (flow) => {
                _newWeightValue = flow.GetValue<float>(newWeight);
                flow.GetValue<Volume>(volume).weight = _newWeightValue;
                return outputTrigger;
            });
            outputTrigger = ControlOutput("outputTrigger");
        
            volume = ValueInput<Volume>("Volume", null);
            newWeight = ValueInput<float>("New Weight", 0);
            weightResult = ValueOutput<float>("Weight", (flow) => _newWeightValue);
        }
    }
}
