//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using UnityEngine;
using UnityEPL;

public class FpsDisplayer : MonoBehaviour {
    DateTime updateStartTime = DateTime.Now;
    int updateFrames = 0;
    DateTime fixedStartTime = DateTime.Now;    
    int fixedFrames = 0;
    DateTime lateStartTime = DateTime.Now;
    int lateFrames = 0;
    DateTime guiStartTime = DateTime.Now;
    int guiFrames = 0;

    bool showFPS = false;

    void Update() {
        if (!showFPS) { return; }
        updateFrames++;
        var timeDiffSecs = (DateTime.Now - updateStartTime).TotalSeconds;
        if (timeDiffSecs >= 1) {
            var updateFPS = updateFrames / timeDiffSecs;
            UnityEngine.Debug.Log($"Update FPS: {updateFPS:0.00} | Frames: {updateFrames}");
            updateFrames = 0;
            updateStartTime = DateTime.Now;
        }
    }

    void FixedUpdate() {
        if (!showFPS) { return; }
        fixedFrames++;
        var timeDiffSecs = (DateTime.Now - fixedStartTime).TotalSeconds;
        if (timeDiffSecs >= 1) {
            var fixedFPS = fixedFrames / timeDiffSecs;
            UnityEngine.Debug.Log($"Fixed FPS: {fixedFPS:0.00} | Frames: {fixedFrames}");
            fixedFrames = 0;
            fixedStartTime = DateTime.Now;
        }
    }

    void LateUpdate() {
        if (!showFPS) { return; }
        lateFrames++;
        var timeDiffSecs = (DateTime.Now - lateStartTime).TotalSeconds;
        if (timeDiffSecs >= 1) {
            var lateFPS = lateFrames / timeDiffSecs;
            UnityEngine.Debug.Log($"Late FPS: {lateFPS:0.00} | Frames: {lateFrames}");
            lateFrames = 0;
            lateStartTime = DateTime.Now;
        }
    }

    void OnGUI() {
        if (!showFPS) { return; }
        guiFrames++;
        var timeDiffSecs = (DateTime.Now - guiStartTime).TotalSeconds;
        if (timeDiffSecs >= 1) {
            var guiFPS = guiFrames / timeDiffSecs;
            UnityEngine.Debug.Log($"GUI FPS: {guiFPS:0.00} | Frames: {guiFrames}");
            guiFrames = 0;
            guiStartTime = DateTime.Now;
        }
    }
}