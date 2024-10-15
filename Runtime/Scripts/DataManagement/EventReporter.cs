//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEPL.Extensions;

using UnityEPL.Threading;
using UnityEPL.Utilities;

namespace UnityEPL.DataManagement {
    [DefaultExecutionOrder(-998)]
    public class EventReporter : SingletonEventMonoBehaviour<EventReporter> {
        public enum FORMAT { JSON_LINES };

        private EventReporterLoop eventReporterLoop;

        public bool experimentConfigured = false;
        protected bool eventWrittenThisFrame = false;

        protected override void AwakeOverride() { }
        protected void Start() {
            eventReporterLoop = new();
            if (Config.logFrameDisplayTimes) {
                StartCoroutine(LogFrameDisplayTimes());
            }
        }

        private IEnumerator LogFrameDisplayTimes() {
            DateTime lastFrameTime = Clock.UtcNow;
            var waitForEndOfFrame = new WaitForEndOfFrame();
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 1;
            while (true) {
                yield return waitForEndOfFrame;
                DateTime now = Clock.UtcNow;
                if (experimentConfigured && eventWrittenThisFrame) {
                    // Debug.Log($"LogFrameDisplayTimes: {(now - lastFrameTime).TotalMilliseconds} {Time.frameCount} {now.ConvertToMillisecondsSinceEpoch()}");
                    LogTS("frameDisplayed", now, new() { 
                        { "frame", Time.frameCount },
                        { "timeSinceLastFrameMs", (now - lastFrameTime).TotalMilliseconds }
                    });
                }
                // Debug.Log($"LogFrameDisplayTimes 2: {(now - lastFrameTime).TotalMilliseconds} {Time.frameCount} {now.ConvertToMillisecondsSinceEpoch()}");
                eventWrittenThisFrame = false;
                lastFrameTime = now;
            }
        }
        // protected void FixedUpdate() {
        //     Debug.Log($"frameStarted: {Clock.UtcNow.ConvertToMillisecondsSinceEpoch()}");
        // }

        public void LogTS(string type, Dictionary<string, object> data = null) {
            LogTS(type, Clock.UtcNow, data);
        }
        public void LogTS(string type, DateTime time, Dictionary<string, object> data = null) {
            manager?.hostPC?.SendLogMsgIfConnectedTS(type, time, data ?? new());
            LogLocalTS(type, time, data);
        }

        // Do not use this unless you don't want the message logged to the HostPC or any other location.
        public void LogLocalTS(string type, DateTime time, Dictionary<string, object> data = null) {
            if (OnUnityThread()) { eventWrittenThisFrame = true; }
            eventReporterLoop.LogTS(type, time, data);
        }

        protected class EventReporterLoop : EventLoop {
            const FORMAT outputFormat = FORMAT.JSON_LINES;
            const string extensionlessFileName = "session";

            protected readonly string defaultFilePath = "";

            protected string filePath = "";
            protected int eventId = 0;

            public EventReporterLoop() {
                string directory = FileManager.DataPath();
                switch (outputFormat) {
                    case FORMAT.JSON_LINES:
                        filePath = Path.Combine(directory, extensionlessFileName + ".jsonl");
                        break;
                }
                defaultFilePath = filePath;
                File.Create(defaultFilePath);
            }

            public void LogTS(string type, DateTime time, Dictionary<string, object> data = null) {
                NativeDataPoint dataPoint = new(type, -1, time, data);
                DoTS(LogHelper, dataPoint);
            }

            protected void LogHelper(NativeDataPoint dataPoint) {
                dataPoint.id = eventId++;
                DoWrite(dataPoint);
                dataPoint.Dispose();
            }


            protected void DoWrite(NativeDataPoint dataPoint) {
                if (filePath == defaultFilePath) {
                    var sessionPath = FileManager.SessionPath();
                    if (sessionPath != null) {
                        switch (outputFormat) {
                            case FORMAT.JSON_LINES:
                                filePath = Path.Combine(sessionPath, extensionlessFileName + ".jsonl");
                                break;
                        }
                    }
                }

                // This was an idea for stopping the hanging loop that happens when there is no configs folder
                // TODO: JPB: (bug) If there is no configs folder with configs.json inside it, the program hangs
                // if (!File.Exists(filePath)) {
                //     return;
                // }

                string lineOutput = "Unrecognized DataReporter FORMAT";
                switch (outputFormat) {
                    case FORMAT.JSON_LINES:
                        lineOutput = dataPoint.ToJSON();
                        break;
                }

                File.AppendAllText(filePath, lineOutput + Environment.NewLine);
            }
        }
    }
}