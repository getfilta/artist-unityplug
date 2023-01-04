#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Filta {
    public class FeedbackWindow : EditorWindow
    {
        [MenuItem("Filta/Send Feedback", false, 5)]
        static void InitFloating() {
            FeedbackWindow window = (FeedbackWindow)GetWindow(typeof(FeedbackWindow), false, "Feedback");
            window.Show();
        }

        private string _feedbackText;
        private const string DiscordLink = "http://discord.gg/CWed5Krf7X";

        private void OnGUI() {
            EditorGUILayout.LabelField("Send feedback to team", EditorStyles.boldLabel);
            _feedbackText = EditorGUILayout.TextArea(_feedbackText);
            if (GUILayout.Button("Send")) {
                SendFeedback();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Join Discord")) {
                Application.OpenURL(DiscordLink);
            }
        }

        private void SendFeedback() {
            _feedbackText = "";
        }
    }
}
#endif
