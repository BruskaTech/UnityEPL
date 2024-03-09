//Copyright (c) 2024 Jefferson University
//Copyright (c) 2024 Bruska Technologies LLC
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
namespace UnityEPL {

    [AddComponentMenu("UnityEPL/Singleton Reporters/Event Reporter")]
    public class EventReporter : DataReporter2<EventReporter> {
        public void LogTS(string type, Dictionary<string, object> data = null) {
            manager.hostPC?.SendUncheckedLogMsgTS(type, data ?? new());
            var time = Clock.UtcNow;
            LogLocalTS(type, time, data);
        }
        public void LogTS(string type, DateTime time, Dictionary<string, object> data = null) {
            manager?.hostPC.SendUncheckedLogMsgTS(type, data ?? new());
            LogLocalTS(type, time, data);
        }

        // TODO: JPB: (needed) (bug) Make LogLocalTS use a blittable type instead of Dictionary
        //            Or at least have it use Mutex.
        //            Even better, just make DataPoint a Native type and then use that

        // Do not use this unless you don't want the message logged to the HostPC or any other location.
        public void LogLocalTS(string type, DateTime time, Dictionary<string, object> data = null) {
            DoTS(() => {
                LogHelper(type.ToNativeText(), time, data);
            });
            //DoTS((type, time) => {
            //    ReportHelper(type, time, data);
            //}, type.ToNativeText(), time);
            //DoTS<NativeText, BlitDateTime, Dictionary<string, object>>(ReportScriptedEventHelper, type.ToNativeText(), time, data);
        }

        protected void LogHelper(NativeText type, BlitDateTime time, Dictionary<string, object> data = null) {
            DoWrite(new DataPoint(type.ToString(), time, data));
            type.Dispose();
        }
    }

    public class DataReporter2<T> : SingletonEventMonoBehaviour<T> where T : DataReporter2<T> {
        public enum FORMAT { JSON_LINES };

        protected FORMAT outputFormat = FORMAT.JSON_LINES;
        protected string filePath = "";
        string extensionlessFileName = "session";
        protected bool experimentPathSetup = false;

        protected override void AwakeOverride() { }
        protected void Start() {
            string directory = manager.fileManager.DataPath();
            switch (outputFormat) {
                case FORMAT.JSON_LINES:
                    filePath = Path.Combine(directory, extensionlessFileName + ".jsonl");
                    break;
            }
        }

        protected void DoWrite(DataPoint dataPoint) {
            if (!experimentPathSetup) {
                try {
                    string directory = manager.fileManager.SessionPath();
                    switch (outputFormat) {
                        case FORMAT.JSON_LINES:
                            filePath = Path.Combine(directory, extensionlessFileName + ".jsonl");
                            break;
                    }
                    experimentPathSetup = true;
                } catch { }
            }

            string lineOutput = "unrecognized type";
            switch (outputFormat) {
                case FORMAT.JSON_LINES:
                    lineOutput = dataPoint.ToJSON();
                    break;
            }

            File.AppendAllText(filePath, lineOutput + Environment.NewLine);
        }
    }
}