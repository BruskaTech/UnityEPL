//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEPL {

    /// <summary>
    /// This handles the button which launches the experiment.
    /// 
    /// DoLaunchExperiment is responsible for calling EditableExperiment.ConfigureExperiment with the proper parameters.
    /// </summary>
    [AddComponentMenu("UnityEPL/Internal/LaunchExperiment")]
    public class LaunchExperiment : EventMonoBehaviour {
        protected override void AwakeOverride() { }

        public GameObject cantGoPrompt;
        public InputField participantNameInput;
        public GameObject launchButton;

        public GameObject syncButton;
        public GameObject greyedLaunchButton;
        public GameObject loadingButton;

        void Update() {
            launchButton.SetActive(isValidParticipant(participantNameInput.text));
            greyedLaunchButton.SetActive(!launchButton.activeSelf);

            if (isValidParticipant(participantNameInput.text)) {
                int sessionNumber = ParticipantSelection.nextSessionNumber;
                launchButton.GetComponentInChildren<Text>().text = "Start session " + sessionNumber.ToString();
            }
        }

        public async void DoSyncBoxTest() {
            await DoWaitFor(DoSyncBoxTestHelper);
        }
        protected async Task DoSyncBoxTestHelper() {
            if (!manager.syncBox?.IsRunning() ?? false) {
                syncButton.GetComponent<Button>().interactable = false;

                // TODO: JPB: (need) Fix Syncbox test
                manager.syncBox.StartPulse();
                await InterfaceManager.Delay(5000);
                //await InterfaceManager.Delay(Config.syncboxTestLength);
                manager.syncBox.StopPulse();

                syncButton.GetComponent<Button>().interactable = true;
            }
        }

        // activated by UI launch button
        public void DoLaunchExperiment() {
            Do(DoLaunchExperimentHelper);
        }
        protected void DoLaunchExperimentHelper() {
            if (manager.syncBox?.IsRunning() ?? false) {
                cantGoPrompt.GetComponent<Text>().text = "Can't start while Syncbox Test is running";
                cantGoPrompt.SetActive(true);
                return;
            }

            if (participantNameInput.text.Equals("")) {
                cantGoPrompt.GetComponent<Text>().text = "Please enter a participant";
                cantGoPrompt.SetActive(true);
                return;
            }
            if (!isValidParticipant(participantNameInput.text)) {
                cantGoPrompt.GetComponent<Text>().text = "Please enter a valid participant name (ex. R1123E or LTP123)";
                cantGoPrompt.SetActive(true);
                return;
            }

            int sessionNumber = ParticipantSelection.nextSessionNumber;

            Config.subject = participantNameInput.text;
            Config.sessionNum = sessionNumber;

            launchButton.SetActive(false);
            loadingButton.SetActive(true);

            manager.LaunchExperimentTS();
        }

        private bool isValidParticipant(string name) {
            return manager.fileManager.isValidParticipant(name);
        }
    }
}