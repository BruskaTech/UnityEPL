//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.IO;

namespace UnityEPL.DataManagement {
    public class DataReporter2<T> : SingletonEventMonoBehaviour<T> where T : DataReporter2<T> {
        public enum FORMAT { JSON_LINES };

        protected FORMAT outputFormat = FORMAT.JSON_LINES;
        protected string defaultFilePath = "";
        protected string filePath = "";
        string extensionlessFileName = "session";

        protected override void AwakeOverride() { }
        protected void Start() {
            string directory = FileManager.DataPath();
            switch (outputFormat) {
                case FORMAT.JSON_LINES:
                    filePath = Path.Combine(directory, extensionlessFileName + ".jsonl");
                    break;
            }
            defaultFilePath = filePath;
            File.Create(defaultFilePath);
        }

        protected void DoWrite(DataPoint dataPoint) {
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