//Copyright (c) 2024 Columbia University (James Bruska)
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEPL {

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

    public readonly struct TextSlide {
        public readonly string description;
        public readonly LangString title;
        public readonly LangString text;

        public TextSlide(string description, LangString title, LangString text) {
            this.description = description;
            this.title = title;
            this.text = text;
        }
    }

    public abstract class ExperimentBase<T> : SingletonEventMonoBehaviour<T>
            where T : ExperimentBase<T> {

        protected InputManager inputManager;
        protected TextDisplayer textDisplayer;
        protected ErrorNotifier errorNotifier;
        protected EventReporter eventReporter;

        protected new void Awake() {
            base.Awake();
            inputManager = InputManager.Instance;
            textDisplayer = TextDisplayer.Instance;
            errorNotifier = ErrorNotifier.Instance;
            eventReporter = EventReporter.Instance;
            LogExperimentInfo();
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

        protected void Run() {
            DoTS(RunHelper().ToEnumerator);
        }
        protected async Task RunHelper() {
            DoTS(ExperimentQuit);
            await PreTrialStates();
            inPracticeTrials = true;
            while (!endPracticeTrials) {
                trialNum++;
                await PracticeTrialStates();
            }
            trialNum = 0;
            inPracticeTrials = false;
            while (!endTrials) {
                trialNum++;
                await TrialStates();
            }
            await PostTrialStates();
            manager.QuitTS();
        }

        protected virtual void LogExperimentInfo() {
            // Log versions and experiment info
            eventReporter.LogTS("session start", new() {
                { "application version", Application.version },
                { "build date", BuildInfo.Date() },
                { "experiment version", Config.experimentName },
                { "logfile version", "1.0.0" },
                { "participant", Config.subject },
                { "session", Config.sessionNum },
            });
        }

        protected virtual async void ExperimentQuit() {
            if (Config.quitAnytime) {
                await RepeatUntilYes(async (CancellationToken ct) => {
                    // Resume since they don't want to quit (or haven't tried yet)
                    manager.PauseTS(false);
                    // Wait for the quit key
                    // TODO: JPB: Figure out how to add a Cancellation Token to GetKeyTS
                    await inputManager.GetKeyTS(new List<KeyCode>() { KeyCode.Q });
                    // Pause everything and ask if they want to quit
                    manager.PauseTS(true);
                }, "experiment quit", LangStrings.ExperimentQuit(), new(), unpausable: true);
                
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
        protected async Task RepeatUntilYes(Func<CancellationToken, Task> preFunc, string description, LangString displayText, CancellationToken ct, Func<bool, CancellationToken, Task> postFunc = null, bool unpausable = false) {
            var repeat = true;
            while (repeat && !ct.IsCancellationRequested) {
                await preFunc(ct);
                ct.ThrowIfCancellationRequested();

                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), true);
                await textDisplayer.DisplayForTask(description, LangStrings.Blank(), displayText, ct, async (CancellationToken ct) => {
                    var keyCode = await inputManager.WaitForKey(new List<KeyCode>() { KeyCode.Y, KeyCode.N }, unpausable: unpausable, ct: ct);
                    repeat = keyCode != KeyCode.Y;
                });
                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), false);
                ct.ThrowIfCancellationRequested();

                if (postFunc != null) { await postFunc(repeat, ct); }
            }
        }
        protected async Task RepeatUntilNo(Func<CancellationToken, Task> preFunc, string description, LangString displayText, CancellationToken ct, Func<bool, CancellationToken, Task> postFunc = null, bool unpausable = false) {
            var repeat = true;
            while (repeat && !ct.IsCancellationRequested) {
                await preFunc(ct);
                ct.ThrowIfCancellationRequested();

                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), true);
                await textDisplayer.DisplayForTask(description, LangStrings.Blank(), displayText, ct, async (CancellationToken ct) => {
                    var keyCode = await inputManager.WaitForKey(new List<KeyCode>() { KeyCode.Y, KeyCode.N }, unpausable: unpausable, ct: ct);
                    repeat = keyCode != KeyCode.N;
                });
                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), false);
                ct.ThrowIfCancellationRequested();

                if (postFunc != null) { await postFunc(repeat, ct); }
            }
        }

        // Pre-Trial States
        protected virtual async Task Introduction() {
            await RepeatUntilYes(async (CancellationToken ct) => {
                await textDisplayer.PressAnyKey("show instruction video", "Press any key to show instruction video");

                manager.videoControl.SetVideo(Config.introductionVideo, true);
                await manager.videoControl.PlayVideo();
            }, "repeat introduction video", LangStrings.RepeatIntroductionVideo(), new());
        }
        protected virtual async Task MicrophoneTest() {
            await RepeatUntilYes(async (CancellationToken ct) => {
                await textDisplayer.PressAnyKey("microphone test prompt", "Microphone Test", "Press any key to record a sound after the beep.");

                string wavPath = System.IO.Path.Combine(manager.fileManager.SessionPath(), "microphone_test_"
                        + Clock.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss") + ".wav");

                manager.lowBeep.Play();
                await DoWaitWhile(() => manager.lowBeep.isPlaying);
                await Timing.Delay(100); // This is needed so you don't hear the end of the beep in the recording

                manager.recorder.StartRecording(wavPath);
                var coloredTestRec = LangStrings.GenForAllLangs("<color=red>") + LangStrings.MicrophoneTestRecording() + LangStrings.GenForAllLangs("</color>");
                textDisplayer.DisplayText("microphone test recording", coloredTestRec);
                await Timing.Delay(Config.micTestDurationMs);
                var clip = manager.recorder.StopRecording();

                var coloredTestPlay = LangStrings.GenForAllLangs("<color=green>") + LangStrings.MicrophoneTestPlaying() + LangStrings.GenForAllLangs("</color>");
                textDisplayer.DisplayText("microphone test playing", coloredTestPlay);
                manager.playback.Play(clip);
                await Timing.Delay(Config.micTestDurationMs);
            }, "repeat mic test", LangStrings.RepeatMicTest(), new());
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
                "Please let the experimenter know if you have any questions about the task.\n\n" +
                "If you think you understand, please explain the task to the experimenter in your own words.\n\n" +
                "Press any key to continue to start.");
        }
        protected async Task DisplayTextSlides(List<TextSlide> textSlides) {
            // Resize based on all text item sizes
            var strList = textSlides.Select(item => item.text + LangStrings.SlideControlLine()).ToList();
            var fontSize = (int)textDisplayer.FindMaxFittingFontSize(strList);
            // UnityEngine.Debug.Log("Font size: " + fontSize);

            // Display all instruction texts
            var keys = new List<KeyCode>() { KeyCode.LeftArrow, KeyCode.RightArrow };
            for (int i = 0; i < textSlides.Count; ++i) {
                var slide = textSlides[i];
                var text = slide.text + LangStrings.SlideControlLine();
                await textDisplayer.DisplayForTask(slide.description, slide.title, text, new(), async (CancellationToken ct) => {
                    var keycode = await inputManager.WaitForKey(keys, ct: ct);
                    if (keycode == KeyCode.LeftArrow && i > 0) { i -= 2; }
                });
            }
        }
    }
}