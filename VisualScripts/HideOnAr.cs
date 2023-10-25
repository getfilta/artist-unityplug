using Filta.VisualScripting;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class HideOnAr : MonoBehaviour {
    public enum Mode{Ar, NonAr}

    public Mode hideMode;
    private ArTriggerEvents _arTriggerEvents;

    private void Awake() {
        _arTriggerEvents = FindObjectOfType<ArTriggerEvents>();
        
    }

    private void Start() {
        if (_arTriggerEvents != null) {
            _arTriggerEvents.onArModeSwitch += OnModeSwitch;
        }
    }

    private void OnDestroy() {
        if (_arTriggerEvents != null) {
            _arTriggerEvents.onArModeSwitch -= OnModeSwitch;
        }
    }
    

    private void OnModeSwitch(object sender, bool isAr) {
        if (hideMode == Mode.Ar) {
            gameObject.SetActive(!isAr);
        } else {
            gameObject.SetActive(isAr);
        }
        
    }
}
