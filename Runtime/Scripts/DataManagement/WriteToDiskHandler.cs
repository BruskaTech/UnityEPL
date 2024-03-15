//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEPL {

    [AddComponentMenu("UnityEPL/Handlers/Write to Disk Handler")]
    public class WriteToDiskHandler<T> : DataHandler<T>
            where T : DataReporter<T> {
        protected override void AwakeOverride() { }

        //more output formats may be added in the future
        public enum FORMAT { JSON_LINES };
        protected FORMAT outputFormat = FORMAT.JSON_LINES;

        [HideInInspector]
        [SerializeField]
        private bool writeAutomatically = true;
        [HideInInspector]
        [SerializeField]
        private uint framesPerWrite = 30;

        // TODO: JPB: (refactor) Why doesn't WriteToDiskHandler just use DataHandler::eventQueue here?
        private Queue<DataPoint> waitingPoints = new Queue<DataPoint>();

        public void SetFormat(FORMAT format) {
            Do(SetFormatHelper, format);
        }
        public void SetFormatTS(FORMAT format) {
            DoTS(SetFormatHelper, format);
        }
        protected void SetFormatHelper(FORMAT format) {
            outputFormat = format;
        }

        public void SetWriteAutomatically(Bool enable) {
            Do(SetWriteAutomaticallyHelper, enable);
        }
        public void SetWriteAutomaticallyTS(Bool enable) {
            DoTS(SetWriteAutomaticallyHelper, enable);
        }
        protected void SetWriteAutomaticallyHelper(Bool enable) {
            writeAutomatically = enable;
        }

        public bool GetWriteAutomatically() {
            return DoGet(GetWriteAutomaticallyHelper);
        }
        public async Task<bool> GetWriteAutomaticallyTS() {
            return await DoGetTS(GetWriteAutomaticallyHelper);
        }
        protected Bool GetWriteAutomaticallyHelper() {
            return writeAutomatically;
        }

        public void SetFramesPerWrite(uint framesPerWrite) {
            Do(SetFramesPerWriteHelper, framesPerWrite);
        }
        public void SetFramesPerWriteTS(uint framesPerWrite) {
            DoTS(SetFramesPerWriteHelper, framesPerWrite);
        }
        protected void SetFramesPerWriteHelper(uint framesPerWrite) {
            this.framesPerWrite = framesPerWrite;
        }

        public uint GetFramesPerWrite() {
            return DoGet(GetFramesPerWriteHelper);
        }
        public async Task<uint> GetFramesPerWriteTS() {
            return await DoGetTS(GetFramesPerWriteHelper);
        }
        protected uint GetFramesPerWriteHelper() {
            return framesPerWrite;
        }

        protected override void Update() {
            base.Update();

            if (Time.frameCount % framesPerWrite == 0)
                DoWrite();
        }

        protected override void HandleDataPoints(DataPoint[] dataPoints) {
            foreach (DataPoint dataPoint in dataPoints)
                waitingPoints.Enqueue(dataPoint);
        }

        /// <summary>
        /// Writes data from the waitingPoints queue to disk.  The waitingPoints queue will be automatically updated whenever reporters report data.
        /// 
        /// DoWrite() will also be automatically be called periodically according to the settings in the component inspector window, but you can invoke this manually if desired.
        /// </summary>
        protected void DoWrite() {
            while (waitingPoints.Count > 0) {
                string directory = manager.fileManager.SessionPath();
                if (directory == null) {
                    return;
                }
                Directory.CreateDirectory(directory);
                string filePath = Path.Combine(directory, "unnamed_file");

                DataPoint dataPoint = waitingPoints.Dequeue();
                string writeMe = "unrecognized type";
                string extensionlessFileName = "session";//DataReporter.GetStartTime ().ToString("yyyy-MM-dd HH mm ss");
                switch (outputFormat) {
                    case FORMAT.JSON_LINES:
                        writeMe = dataPoint.ToJSON();
                        filePath = Path.Combine(directory, extensionlessFileName + ".jsonl");
                        break;
                }
                File.AppendAllText(filePath, writeMe + System.Environment.NewLine);
            }
        }
    }

}