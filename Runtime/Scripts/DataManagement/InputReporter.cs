//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEPL {

    [AddComponentMenu("UnityEPL/Singleton Reporters/Input Reporter")]
    public class InputReporter : SingletonEventMonoBehaviour<InputReporter> {
        protected override void AwakeOverride() { }

        public bool reportKeyStrokes = true;
        public bool reportMouseClicks = false;
        public bool reportMousePosition = false;
        public int framesPerMousePositionReport = 60;
        private Dictionary<int, bool> keyDownStates = new();
        private Dictionary<int, bool> mouseDownStates = new();

        private int lastMousePositionReportFrame;

        void Update() {
            if (reportKeyStrokes)
                CollectKeyEvents();
            if (reportMousePosition && Time.frameCount - lastMousePositionReportFrame > framesPerMousePositionReport)
                CollectMousePosition();
        }

        /// <summary>
        /// Collects the key events.  This includes mouse events, which are part of Unity's KeyCode enum.
        /// </summary>

        private void CollectKeyEvents() {
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode))) {
                if (Input.GetKeyDown(keyCode)) {
                    ReportKey((int)keyCode, true);
                }
                if (Input.GetKeyUp(keyCode)) {
                    ReportKey((int)keyCode, false);
                }
            }
        }

        private void ReportKey(int keyCode, bool pressed) {
            var key = (Enum.GetName(typeof(KeyCode), keyCode) ?? "none").ToLower();
            Dictionary<string, object> dataDict = new() {
                { "key code", key },
                { "is pressed", pressed },
            };
            var label = "key/mouse press/release";
            EventReporter.Instance.LogTS(label, dataDict);
        }

        private void CollectMousePosition() {
            Dictionary<string, object> dataDict = new() {
                { "position", Input.mousePosition },
            };
            EventReporter.Instance.LogTS("mouse position", dataDict);
            lastMousePositionReportFrame = Time.frameCount;
        }
    }

}