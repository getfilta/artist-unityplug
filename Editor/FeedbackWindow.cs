#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
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

        private bool _sending;

        private string _feedbackText;
        private const string DiscordLink = "http://discord.gg/CWed5Krf7X";
        private string _process;

        private void OnGUI() {
            EditorGUILayout.LabelField("Send feedback to team", EditorStyles.boldLabel);
            if (_sending) {
                EditorGUILayout.LabelField(_process);
                _feedbackText = "";
                return;
            }
            _feedbackText = EditorGUILayout.TextArea(_feedbackText);
            if (GUILayout.Button("Send")) {
                SendFeedback();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Join Discord")) {
                Application.OpenURL(DiscordLink);
            }
        }

        private async void SendFeedback() {
            try {
                _sending = true;
                _process = "Sending...";
                Repaint();
                bool result = await Backend.Instance.SendFeedback(_feedbackText);
                _process = result ? "Feedback sent successfully" : "Failed to send feedback";
                Repaint();
                await Task.Delay(2000);
                _sending = false;
                Repaint();
            }
            catch (Exception e) {
                _process = "Failed to send feedback. Check console for error";
                Repaint();
                Debug.LogError(e);
                await Task.Delay(2000);
                _sending = false;
                Repaint();
            }
        }
    }
}
#endif
