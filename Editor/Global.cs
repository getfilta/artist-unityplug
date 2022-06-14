using UnityEditor;
using System;

namespace Filta {
    public static class Global {
        private const string TestEnvirSetting = "Filta_TestEnvir";
        public static bool UseTestEnvironment {
            get => EditorPrefs.GetBool(TestEnvirSetting, false);
            set => EditorPrefs.SetBool(TestEnvirSetting, value);
        }
        public static string FirebaseApikey => UseTestEnvironment ? "AIzaSyDaOuavnA9n0xpodrSrTO2QwoZLVhBkdVA" : "AIzaSyAiefSo-GLf2yjEwbXhr-1MxMx0A6vXHO0";

        public static EventHandler<StatusChangeEventArgs> StatusChange = delegate { };

        public static void FireStatusChange(object sender, string message, bool isError = false) {
            StatusChange(sender, new StatusChangeEventArgs(message, isError));
        }
        public static TimeSpan GetTimeSince(Int64 jsonTimestamp) {
            DateTime origin = new(1970, 1, 1, 0, 0, 0, 0);
            DateTime timestamp = origin.AddMilliseconds(jsonTimestamp); // convert from milliseconds to seconds
            var result = DateTime.UtcNow - timestamp;
            return result;
        }

    }

    public class StatusChangeEventArgs : EventArgs {
        public StatusChangeEventArgs(string message, bool isError) {
            Message = message;
            IsError = isError;
        }
        public string Message { get; }
        public bool IsError { get; }
    }


}