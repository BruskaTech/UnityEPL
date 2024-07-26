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
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

using UnityEPL;

public class TestExperiment : ExperimentBase<TestExperiment, TestSession, TestTrial> {
    protected override void AwakeOverride() {  }

    protected void Start() {
        Run();
    }

    protected override Task PostTrialStates() { return Task.CompletedTask; }
    protected override Task PracticeTrialStates() { return Task.CompletedTask; }
    protected override Task PreTrialStates() { return Task.CompletedTask; }

    protected async Task RepeatedGetKey() {
        var key = await inputManager.WaitForKey();
        UnityEngine.Debug.Log("Got key " + key);
        _ = DoWaitForTS(RepeatedGetKey);
    }

    protected override async Task TrialStates() {
        //await manager.textDisplayer.AwaitableUpdateText("AwaitableUpdateText");
        //await InterfaceManager2.Delay(1000);
        //manager.textDisplayer.UpdateText("DONE");
        //var a = await manager.textDisplayer.ReturnableUpdateText("ReturnableUpdateText");
        //UnityEngine.Debug.Log("DoGet: " + a);

        var cts = DoRepeatingTS(1000, 500, 10, () => { UnityEngine.Debug.Log("Repeat"); });
        await inputManager.WaitForKey();
        cts.Cancel();
        await inputManager.WaitForKey();

        //var key = await manager.inputManager.GetKey();
        //UnityEngine.Debug.Log("Got key " + key);

        //DelayedGet();
        //DelayedStop();
        //DelayedTriggerKeyPress();
        //KeyMsg keyMsg = await WaitOnKey(default);
        //UnityEngine.Debug.Log("MainStates - WaitOnKey: " + keyMsg.key);
        //manager.textDisplayer.UpdateText("UpdateText");
        //await InterfaceManager2.Delay(1000);
        //await DelayedGet();
    }
}