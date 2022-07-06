using System;
using System.Reflection;
using Mirror;
using UnityEngine;

public class RemoteInput
{
    public static RemoteInput Instance { get; }

    static RemoteInput() {
        Instance = new RemoteInput();
        Input.simulateMouseWithTouches = false;
    }
    public struct SimulatedTouch : NetworkMessage {
        public int fingerId;
        public Vector2 position;
        public TouchPhase phase;
        
        public static implicit operator SimulatedTouch(Touch touch) {
            return new SimulatedTouch {fingerId = touch.fingerId, phase = touch.phase, position = touch.position};
        }

        public static implicit operator Touch(SimulatedTouch simTouch) {
            return new Touch {fingerId = simTouch.fingerId, phase = simTouch.phase, position = simTouch.position};
        }
    }

    [Client]
    public void SetupClient() {
        NetworkClient.RegisterHandler<SimulatedTouch>(OnSimulatedTouch, false);
    }

    void OnSimulatedTouch(SimulatedTouch simulatedTouch) {
        Simulate(simulatedTouch);
    }

    public static void Simulate(Touch touch) {
        var simulateTouchMethod = typeof(Input).GetMethod("SimulateTouch", BindingFlags.NonPublic | BindingFlags.Static);
        if (simulateTouchMethod == null) {
            Debug.LogError("Could not get touch method");
            return;
        }
        simulateTouchMethod.Invoke(null, GetParameters());
        object[] GetParameters() {
            var numOfParameters = simulateTouchMethod.GetParameters().Length;
            switch (numOfParameters) {
                case 1:
                    return new object[] {touch};
                case 3:
                    return new object[] {touch.fingerId, touch.position, touch.phase};
                default:
                    throw new Exception(
                        $"UnityEngine.Input.SimulateTouch() has an unexpected number of parameters: {numOfParameters}. Please contact plugin support.");
            }
        }
    }
}
