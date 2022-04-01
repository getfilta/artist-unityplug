using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using EvtSource;
using Newtonsoft.Json;
using Filta.Datatypes;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Filta {
    public static class Global {
        private const string TestEnvirSetting = "Filta_TestEnvir";
        public static bool UseTestEnvironment {
            get { return EditorPrefs.GetBool(TestEnvirSetting, false); }
            set { EditorPrefs.SetBool(TestEnvirSetting, value); }
        }
        public static string FIREBASE_APIKEY { get { return UseTestEnvironment ? "AIzaSyDaOuavnA9n0xpodrSrTO2QwoZLVhBkdVA" : "AIzaSyAiefSo-GLf2yjEwbXhr-1MxMx0A6vXHO0"; } }

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
            this.Message = message;
            this.IsError = isError;
        }
        public string Message { get; private set; }
        public bool IsError { get; private set; }
    }


}