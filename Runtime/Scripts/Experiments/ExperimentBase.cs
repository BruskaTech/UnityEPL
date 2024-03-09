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
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEPL {

    public struct KeyMsg {
        public string key;
        public bool down;

        public KeyMsg(string key, bool down) {
            this.key = key;
            this.down = down;
        }
    }

    public static class ExperimentActive {
        private static bool active = false;
        public static bool isActive() { return active; }
        public static void SetActive(bool val) {
            if (val && active) {
                throw new InvalidOperationException("Trying to make an experiment active when there is already an active experiment."
                    + "If you have more than one experiment, make sure to make them all inactive in the editor.");
            }
            active = val;
        }
    }

    public abstract class ExperimentBase<T> : SingletonEventMonoBehaviour<T>
            where T : ExperimentBase<T> {
        TaskCompletionSource<KeyMsg> tcs = new TaskCompletionSource<KeyMsg>();

        protected InputManager inputManager;
        protected TextDisplayer textDisplayer;
        protected ErrorNotifier errorNotifier;
        protected EventReporter eventReporter;

        protected new void Awake() {
            base.Awake();
            this.inputManager = InputManager.Instance;
            this.textDisplayer = TextDisplayer.Instance;
            this.errorNotifier = ErrorNotifier.Instance;
            this.eventReporter = EventReporter.Instance;
        }

        protected void OnEnable() {
            ExperimentActive.SetActive(true);
        }

        protected void OnDisable() {
            ExperimentActive.SetActive(false);
        }

        private bool endTrials = false;
        private bool endPracticeTrials = false;
        protected uint trialNum { get; private set; } = 0;
        protected uint practiceTrialNum { get; private set; } = 0;
        protected bool inPracticeTrials { get; private set; } = false;

        protected abstract Task PreTrialStates();
        protected abstract Task PracticeTrialStates();
        protected abstract Task TrialStates();
        protected abstract Task PostTrialStates();

        protected void EndTrials() {
            endTrials = true;
        }
        protected void EndPracticeTrials() {
            endPracticeTrials = true;
        }

        protected async void Run() {
            await DoWaitFor(RunHelper);
        }
        protected async Task RunHelper() {
            DoTS(ExperimentQuit);
            await PreTrialStates();
            inPracticeTrials = true;
            while (!endPracticeTrials) {
                practiceTrialNum++;
                await PracticeTrialStates();
            }
            inPracticeTrials = false;
            while (!endTrials) {
                trialNum++;
                await TrialStates();
            }
            await PostTrialStates();
            manager.QuitTS();
        }

        protected virtual void LogExperimentInfo() {
            //write versions to logfile
            Dictionary<string, object> versionsData = new() {
                { "application version", Application.version },
                { "build date", BuildInfo.ToString() }, // compiler magic, gives compile date
                { "experiment version", Config.experimentName },
                { "logfile version", "0" },
                { "participant", Config.subject },
                { "session", Config.sessionNum },
            };

            eventReporter.LogTS("session start", versionsData);
        }

        protected virtual async void ExperimentQuit() {
            var quitKeyCode = KeyCode.N;
            if (Config.quitAnytime) {
                while (quitKeyCode == KeyCode.N) {
                    await inputManager.GetKeyTS(new List<KeyCode>() { KeyCode.Q });
                    manager.PauseTS(true);
                    var titleOld = textDisplayer.titleElement.text;
                    var textOld = textDisplayer.textElement.text;
                    textDisplayer.Display("Experiment quit", "",
                        $"Do you want to quit" +
                        "\nPress Y to Quit, N to Resume.");
                    quitKeyCode = await inputManager.GetKeyTS(new List<KeyCode>() { KeyCode.Y, KeyCode.N }, unpausable: true);
                    textDisplayer.titleElement.text = titleOld;
                    textDisplayer.textElement.text = textOld;
                    manager.PauseTS(false);
                }
                UnityEngine.Debug.Log("QUITTING!");
                manager.QuitTS();
            }
        }

        // Wrapper/Replacement Functions
        protected bool IsNumericKeyCode(KeyCode keyCode) {
            bool isAlphaNum = keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9;
            bool isKeypadNum = keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9;
            return isAlphaNum || isKeypadNum;
        }
        protected virtual void SendRamulatorStateMsg(HostPcStateMsg state, bool stateToggle, Dictionary<string, object> extraData = null) {
            // Do nothing by default
        }
        protected async Task RepeatUntilYes(Func<Task> func, string description, string displayText) {
            var repeat = true;
            while (repeat) {
                await func();

                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), true);
                textDisplayer.Display(description, "", displayText);
                var keyCode = await inputManager.GetKeyTS(new List<KeyCode>() { KeyCode.Y, KeyCode.N });
                repeat = keyCode != KeyCode.Y;
                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), false);
            }
        }
        protected async Task RepeatUntilNo(Func<Task> func, string description, string displayText) {
            var repeat = true;
            while (repeat) {
                await func();

                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), true);
                textDisplayer.Display(description, "", displayText);
                var keyCode = await inputManager.GetKeyTS(new List<KeyCode>() { KeyCode.Y, KeyCode.N });
                repeat = keyCode != KeyCode.N;
                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), false);
            }
        }

        // Pre-Trial States
        protected virtual async Task Introduction() {
            await RepeatUntilYes(async () => {
                await textDisplayer.PressAnyKey("show instruction video", "Press any key to show instruction video");

                manager.videoControl.SetVideo(Config.introductionVideo, true);
                await manager.videoControl.PlayVideo();
            }, "repeat introduction video", "Press Y to continue, \n Press N to replay instructional video.");
        }
        protected virtual async Task MicrophoneTest() {
            await RepeatUntilYes(async () => {
                await textDisplayer.PressAnyKey("microphone test prompt", "Microphone Test", "Press any key to record a sound after the beep.");

                string wavPath = System.IO.Path.Combine(manager.fileManager.SessionPath(), "microphone_test_"
                        + Clock.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss") + ".wav");

                manager.lowBeep.Play();
                await DoWaitWhile(() => manager.lowBeep.isPlaying);
                await InterfaceManager.Delay(100); // This is needed so you don't hear the end of the beep

                manager.recorder.StartRecording(wavPath);
                textDisplayer.DisplayText("microphone test recording", "<color=red>Recording...</color>");
                await InterfaceManager.Delay(Config.micTestDuration);
                var clip = manager.recorder.StopRecording();

                textDisplayer.DisplayText("microphone test playing", "<color=green>Playing...</color>");
                manager.playback.Play(clip);
                await InterfaceManager.Delay(Config.micTestDuration);
            }, "repeat mic test", "Did you hear the recording ? \n(Y = Continue / N = Try Again).");
        }
        protected virtual async Task QuitPrompt() {
            SendRamulatorStateMsg(HostPcStateMsg.WAITING(), true);
            manager.hostPC?.SendStateMsgTS(HostPcStateMsg.WAITING());

            textDisplayer.Display("subject/session confirmation", "",
                $"Running {Config.subject} in session {Config.sessionNum} of {Config.experimentName}." +
                "\nPress Y to continue, N to quit.");
            var keyCode = await inputManager.GetKeyTS(new List<KeyCode>() { KeyCode.Y, KeyCode.N });

            SendRamulatorStateMsg(HostPcStateMsg.WAITING(), false);

            if (keyCode == KeyCode.N) {
                manager.QuitTS();
            }
        }
        protected virtual async Task ConfirmStart() {
            await textDisplayer.PressAnyKey("confirm start",
                "Please let the experimenter know if you have any questions about what you just did.\n\n" +
                "If you think you understand, please explain the task to the experimenter in your own words.\n\n" +
                "Press any key to continue to the first list.");
        }
    }
}