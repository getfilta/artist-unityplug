#if UNITY_EDITOR
using UnityEditor;

public class HotKeys : Editor {
    [MenuItem("Filta Hotkeys/Pause or Resume Simulator &e")]
    static void ToggleSimulatorPlaying() {
        SimulatorBase simulator = FindObjectOfType<SimulatorBase>();
        if (simulator == null) {
            return;
        }

        if (simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
            Simulator sim = simulator.gameObject.GetComponent<Simulator>();
            if (sim.isPlaying) {
                sim.PauseSimulator();
            } else {
                sim.ResumeSimulator();
            }
        } else if (simulator._simulatorType == SimulatorBase.SimulatorType.Body) {
            BodySimulator sim = simulator.gameObject.GetComponent<BodySimulator>();
            if (sim.isPlaying) {
                sim.PauseSimulator();
            } else {
                sim.ResumeSimulator();
            }
        }
    }

    [MenuItem("Filta Hotkeys/Toggle Face Visualiser &f")]
    static void ToggleFaceVisualiser() {
        SimulatorBase simulator = FindObjectOfType<SimulatorBase>();
        if (simulator == null) {
            return;
        }
        if (simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
            Simulator sim = simulator.gameObject.GetComponent<Simulator>();
            sim.showFaceMeshVisualiser = !sim.showFaceMeshVisualiser;
        }
    }

    [MenuItem("Filta Hotkeys/Reset Simulator &r")]
    static void ResetSimulatorPlaying() {
        SimulatorBase simulator = FindObjectOfType<SimulatorBase>();
        if (simulator == null) {
            return;
        }
        if (simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
            Simulator sim = simulator.gameObject.GetComponent<Simulator>();
            sim.ResetSimulator();
        }
    }

    [MenuItem("Filta Hotkeys/Toggle Vertex Indices &v")]
    static void ToggleVertexIndices() {
        SimulatorBase simulator = FindObjectOfType<SimulatorBase>();
        if (simulator == null) {
            return;
        }
        if (simulator._simulatorType == SimulatorBase.SimulatorType.Face) {
            Simulator sim = simulator.gameObject.GetComponent<Simulator>();
            sim.showVertexNumbers = !sim.showVertexNumbers;
        }
    }
}
#endif
