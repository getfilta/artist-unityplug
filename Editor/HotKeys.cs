#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class HotKeys : Editor {
    [MenuItem("Filta Hotkeys/Focus Scene Camera &c")]
    public static void FocusSceneViewCamera()
    {
        FusionSimulator fusionSimulator = FindObjectOfType<FusionSimulator>();
        SimulatorBase sim = fusionSimulator != null
            ? fusionSimulator.GetActiveSimulator()
            : FindObjectOfType<SimulatorBase>();
        Transform mainTracker = sim.mainTracker;
        SceneView.lastActiveSceneView.LookAt(mainTracker.position, Quaternion.Euler(Vector3.forward), 0.1f);
        SceneView.lastActiveSceneView.Repaint();
    }
    
    [MenuItem("Filta Hotkeys/Pause or Resume Simulator &e")]
    static void ToggleSimulatorPlaying() {
        FusionSimulator fusionSimulator = FindObjectOfType<FusionSimulator>();
        if (fusionSimulator != null) {
            TogglePlaying(fusionSimulator.GetActiveSimulator());
            return;
        }
        SimulatorBase simulator = FindObjectOfType<SimulatorBase>();
        if (simulator != null) {
            TogglePlaying(simulator);
        }
    }
    
    [MenuItem("Filta Hotkeys/Stop Simulator &s")]
    static void StopSimulator() {
        FusionSimulator fusionSimulator = FindObjectOfType<FusionSimulator>();
        if (fusionSimulator != null) {
            fusionSimulator.GetActiveSimulator().StopSimulator();
            return;
        }
        SimulatorBase simulator = FindObjectOfType<SimulatorBase>();
        if (simulator != null) {
            simulator.StopSimulator();
        }
    }

    static void TogglePlaying(SimulatorBase sim) {
        if (sim.isPlaying) {
            sim.PauseSimulator();
        } else {
            sim.ResumeSimulator();
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
        FusionSimulator fusionSimulator = FindObjectOfType<FusionSimulator>();
        if (fusionSimulator != null) {
            fusionSimulator.GetActiveSimulator().ResetSimulator();
            return;
        }
        SimulatorBase simulator = FindObjectOfType<SimulatorBase>();
        if (simulator != null) {
            simulator.ResetSimulator();
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
