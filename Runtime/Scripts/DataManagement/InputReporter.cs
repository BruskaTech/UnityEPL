//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEPL.Utilities;

namespace UnityEPL.DataManagement {

    public class InputReporter : SingletonEventMonoBehaviour<InputReporter> {
        public bool reportKeyStrokes = true;
        public bool reportMouseClicks = false;
        public bool reportMousePosition = false;
        public bool reportMousePositionOnFrame = false;

        public int framesPerMousePositionReport = 1;

        private List<float> mousePosition = new() {0f, 0f};
        private int framesSinceLastMousePositionReport = 0;

        protected override void AwakeOverride() {
            InputSystem.pollingFrequency = 500;
            InputSystem.onEvent += Event;
        }

        void OnDestroy() {
            InputSystem.onEvent -= Event;
        }

        private void Event(InputEventPtr eventPtr, InputDevice device) {
            // Ignore anything that isn't a state event.
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) { return; }

            // This is just for implementing new input recording parts
            // UnityEngine.Debug.Log(string.Join("\n",
            //     eventPtr
            //         // .EnumerateChangedControls(device)
            //         .EnumerateControls(InputControlExtensions.Enumerate.IncludeNonLeafControls)
            //         .Where(control => control.device == device)
            //         .Select(control => $"{control.GetType().Name} {control.name} {device.name}")
            // ));

            var eventTime = MainManager.Instance.StartTimeTS + TimeSpan.FromSeconds(eventPtr.time);
            var timeOffsetFromLoggingMs = (Clock.UtcNow - eventTime).TotalMilliseconds;

            var changedControls = eventPtr.EnumerateChangedControls(device);
            foreach (var control in changedControls) {
                // Handle keyboard input
                if (reportKeyStrokes && device is Keyboard && control is KeyControl keyControl) {
                    eventReporter.LogTS("input event", eventTime, new Dictionary<string, object> {
                        { "device", control.device.name },
                        { "input type", control.layout },
                        { "key name", control.name },
                        { "key display name", control.displayName },
                        { "value", keyControl.ReadValueFromEvent(eventPtr) },
                        { "control", control.ToString() },
                        { "time offset from logging ms", timeOffsetFromLoggingMs},
                    });
                }

                // Handle mouse button input
                if (reportMouseClicks && device is Mouse && control is ButtonControl buttonControl) {
                    eventReporter.LogTS("input event", eventTime, new Dictionary<string, object> {
                        { "device", control.device.name },
                        { "input type", control.layout },
                        { "button name", control.name },
                        { "value", buttonControl.ReadValueFromEvent(eventPtr) },
                        { "button display name", control.displayName },
                        { "position", mousePosition },
                        { "control", control.ToString() },
                        { "time offset from logging ms", timeOffsetFromLoggingMs},
                    });
                }

                // Handle mouse position input
                // This may need to be updated to handle Vector2Control in the future
                if (reportMousePosition && !reportMousePositionOnFrame && device is Mouse && control is AxisControl axisControl && axisControl.parent.name == "position") { //
                    if (axisControl.name == "x") {
                        mousePosition[0] = axisControl.ReadValueFromEvent(eventPtr);
                    } else if (axisControl.name == "y") {
                        mousePosition[1] = axisControl.ReadValueFromEvent(eventPtr);
                    }
                    eventReporter.LogTS("input event", eventTime, new Dictionary<string, object> {
                        { "device", control.device.name },
                        { "input type", control.layout },
                        { "position name", control.name },
                        { "value", mousePosition },
                        { "position display name", control.displayName },
                        { "control", control.ToString() },
                        { "time offset from logging ms", timeOffsetFromLoggingMs},
                    });
                }
            }
        }

        void Update() {
            if (reportMousePosition && reportMousePositionOnFrame && ++framesSinceLastMousePositionReport >= framesPerMousePositionReport) {
                framesSinceLastMousePositionReport = 0;
                var mouse = Mouse.current;
                var control = mouse.position;
                var position = control.ReadValue();
                eventReporter.LogTS("input event", new Dictionary<string, object> {
                    { "device", mouse.name },
                    { "input type", control.layout },
                    { "position name", control.name },
                    { "value", new float[2] {position.x, position.y} },
                    { "value2", mousePosition },
                    { "position display name", control.displayName },
                    { "control", control.ToString() },
                    { "time offset from logging ms", 0.0},
                });
            }
        }
    }

}