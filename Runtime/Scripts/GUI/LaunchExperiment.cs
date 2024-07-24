//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
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
                await Timing.Delay(Config.syncBoxTestDurationMs);
                manager.syncBox.StopPulse();

                syncButton.GetComponent<Button>().interactable = true;
            }
        }

        // activated by UI launch button
        public void LaunchExp() {
            DoTS(LaunchExpHelper);
        }
        protected IEnumerator LaunchExpHelper() {
            if (manager.syncBox?.IsRunning() ?? false) {
                cantGoPrompt.GetComponent<Text>().text = "Can't start while Syncbox Test is running";
                cantGoPrompt.SetActive(true);
                yield break;
            } else if (participantNameInput.text.Equals("")) {
                cantGoPrompt.GetComponent<Text>().text = "Please enter a participant";
                cantGoPrompt.SetActive(true);
                yield break;
            } else if (!isValidParticipant(participantNameInput.text)) {
                cantGoPrompt.GetComponent<Text>().text = "Please enter a valid participant name (ex. R1123E or LTP123)";
                cantGoPrompt.SetActive(true);
                yield break;
            } else if (!Config.IsExperimentConfigSetup()) {
                throw new Exception("No experiment configuration loaded");
            }

            int sessionNumber = ParticipantSelection.nextSessionNumber;

            Config.subject = participantNameInput.text;
            Config.sessionNum = sessionNumber;

            launchButton.SetActive(false);
            loadingButton.SetActive(true);

            Random.SetStableRndSeed(Config.subject.GetHashCode());

            Cursor.visible = false;
            Application.runInBackground = true;

            // Make the game run as fast as possible
            QualitySettings.vSyncCount = Config.vSync;
            Application.targetFrameRate = Config.frameRate;

            // Create path for current participant/session
            manager.fileManager.CreateSession();

            // Save Configs
            Config.SaveConfigs(manager.fileManager.SessionPath());

            // Connect to HostPC
            if (Config.elememOn) {
                TextDisplayer.Instance.Display("Elemem connection display", LangStrings.Blank(), LangStrings.ElememConnection());
                manager.hostPC = new ElememInterface();
            } else if (Config.ramulatorOn) {
                TextDisplayer.Instance.Display("Ramulator connection display", LangStrings.Blank(), LangStrings.ElememConnection());
                manager.ramulator = new RamulatorWrapper(manager);
                yield return manager.ramulator.BeginNewSession();
            }
            yield return manager.hostPC?.ConnectTS().ToEnumerator();
            yield return manager.hostPC?.ConfigureTS().ToEnumerator();

            SceneManager.sceneLoaded += onExperimentSceneLoaded;
            SceneManager.LoadScene(Config.experimentScene);
        }

        private static void onExperimentSceneLoaded(Scene scene, LoadSceneMode mode) {
            // Experiment Manager
            // TODO: JPB: (bug) Fix issue where unity crashes if I check for multiple experiments
            try {
                // Use gameObject.scene to get values in DontDestroyOnLoad
                var activeExperiments = MainManager.Instance.gameObject.scene.GetRootGameObjects()
                    .Where(go => go.name == Config.experimentClass && go.activeSelf);

                if (activeExperiments.Count() == 0) {
                    var expManager = scene.GetRootGameObjects().Where(go => go.name == Config.experimentClass).First();
                    expManager.SetActive(true);
                }
            } catch (InvalidOperationException exception) {
                ErrorNotifier.ErrorTS(new Exception(
                    $"Missing experiment GameObject that is the same name as the experiment class ({Config.experimentClass})",
                    exception));
            }

            SceneManager.sceneLoaded -= onExperimentSceneLoaded;
        }

        private bool isValidParticipant(string name) {
            return manager.fileManager.isValidParticipant(name);
        }
    }
}