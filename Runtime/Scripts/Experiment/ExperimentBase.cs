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

using UnityEPL.Utilities;
using UnityEPL.ExternalDevices;
using UnityEPL.GUI;
using UnityEPL.Extensions;

namespace UnityEPL.Experiment {

    static class ExperimentActive {
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

        public TextSlide(string description, LangString text) {
            this.description = description;
            this.title = LangStrings.Blank();
            this.text = text;
        }

        public TextSlide(string description, LangString title, LangString text) {
            this.description = description;
            this.title = title;
            this.text = text;
        }
    }

    public abstract class ExperimentBase<T, SessionType, TrialType> : SingletonEventMonoBehaviour<T>
        where T : ExperimentBase<T, SessionType, TrialType>
        where SessionType : ExperimentSession<TrialType>
    {

        protected InputManager inputManager;
        protected TextDisplayer textDisplayer;

        protected SessionType session;
        protected SessionType practiceSession;
        protected SessionType normalSession;

        protected new void Awake() {
            base.Awake();
            inputManager = InputManager.Instance;
            textDisplayer = TextDisplayer.Instance;
            LogExperimentInfo();
        }

        protected void OnEnable() {
            ExperimentActive.SetActive(true);
        }

        protected void OnDisable() {
            ExperimentActive.SetActive(false);
        }

        protected abstract Task PreTrialStates();
        protected abstract Task PracticeTrialStates();
        protected abstract Task TrialStates();
        protected abstract Task PostTrialStates();

        protected void EndCurrentSession() {
            throw new EndSessionException();
        }

        protected void Run() {
            DoTS(RunHelper().ToEnumerator);
        }
        protected async Task RunHelper() {
            DoTS(ExperimentQuit);
            DoTS(ExperimentPause);
            await PreTrialStates();

            if (practiceSession == null) {
                throw new Exception("The Experiment did not set any practice sessions.");
            } else if (normalSession == null) {
                throw new Exception("The Experiment did not set any normal session.");
            }

            session = practiceSession;
            try {
                while (true) {
                    session.TrialNum++;
                    await PracticeTrialStates();
                }
            } catch (EndSessionException) {} // do nothing

            session = normalSession;
            try {
                while (true) {
                    session.TrialNum++;
                    await TrialStates();
                }
            } catch (EndSessionException) {} // do nothing

            await PostTrialStates();
            await manager.QuitTS();
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
                { "rndSeed", Utilities.Random.RndSeed },
                { "stableRndSeed", Utilities.Random.StableRndSeed },
            });
        }

        protected virtual async void ExperimentQuit() {
            if (Config.quitAnytime) {
                await RepeatUntilYes(async (CancellationToken ct) => {
                    // Resume since they don't want to quit (or haven't tried yet)
                    manager.Pause(false);
                    // Wait for the quit key
                    await inputManager.WaitForKey(new List<KeyCode>() { KeyCode.Q }, unpausable: true, ct: ct);
                    // Pause everything and ask if they want to quit
                    manager.Pause(true);
                }, "experiment quit", LangStrings.ExperimentQuit(), new(), unpausable: true);
                
                manager.Pause(false);
                await manager.QuitTS();
            }
        }

        protected virtual async void ExperimentPause() {
            if (Config.pauseAnytime) {
                var pauseKeyCodes = new List<KeyCode>() { KeyCode.P };
                await RepeatForever(async (CancellationToken ct) => {
                    // Resume since they don't want to quit (or haven't tried yet)
                    manager.Pause(false);
                    // Wait for the pause key
                    await inputManager.WaitForKey(pauseKeyCodes, ct: ct);
                    // Pause everything and ask if they want to quit
                    manager.Pause(true);
                }, "experiment pause", LangStrings.ExperimentPaused(), pauseKeyCodes, new(), unpausable: true);
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
        protected async Task RepeatForever(Func<CancellationToken, Task> preFunc, string description, LangString displayText, List<KeyCode> keyCodes, CancellationToken ct, Func<CancellationToken, Task> postFunc = null, bool unpausable = false) {
            while (!ct.IsCancellationRequested) {
                await preFunc(ct);
                ct.ThrowIfCancellationRequested();

                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), true);
                await textDisplayer.DisplayForTask(description, LangStrings.Blank(), displayText, ct, async (CancellationToken ct) => {
                    var keyCode = await inputManager.WaitForKey(keyCodes, unpausable: unpausable, ct: ct);
                });
                SendRamulatorStateMsg(HostPcStateMsg.WAITING(), false);
                ct.ThrowIfCancellationRequested();

                if (postFunc != null) { await postFunc(ct); }
            }
        }

        // Pre-Trial States
        protected virtual async Task Introduction() {
            await RepeatUntilYes(async (CancellationToken ct) => {
                await textDisplayer.PressAnyKey("show instruction video", LangStrings.ShowInstructionVideo());

                manager.videoControl.SetVideo(Config.introductionVideo, true);
                await manager.videoControl.PlayVideo();
            }, "repeat introduction video", LangStrings.RepeatIntroductionVideo(), new());
        }
        protected virtual async Task MicrophoneTest() {
            await RepeatUntilYes(async (CancellationToken ct) => {
                await textDisplayer.PressAnyKey("microphone test prompt", LangStrings.MicrophoneTestTitle(), LangStrings.MicrophoneTest());

                string wavPath = System.IO.Path.Combine(FileManager.SessionPath(), "microphone_test_"
                        + Clock.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss") + ".wav");

                manager.lowBeep.Play();
                await DoWaitWhile(() => manager.lowBeep.isPlaying);
                await manager.Delay(100); // This is needed so you don't hear the end of the beep in the recording

                manager.recorder.StartRecording(wavPath);
                var coloredTestRec = LangStrings.MicrophoneTestRecording().Color("red");
                textDisplayer.DisplayText("microphone test recording", coloredTestRec);
                await manager.Delay(Config.micTestDurationMs);
                var clip = manager.recorder.StopRecording();

                var coloredTestPlay = LangStrings.MicrophoneTestPlaying().Color("green");
                textDisplayer.DisplayText("microphone test playing", coloredTestPlay);
                manager.playback.Play(clip);
                await manager.Delay(Config.micTestDurationMs);
            }, "repeat mic test", LangStrings.RepeatMicTest(), new());
        }
        protected virtual async Task QuitPrompt() {
            SendRamulatorStateMsg(HostPcStateMsg.WAITING(), true);
            manager.hostPC?.SendStateMsgTS(HostPcStateMsg.WAITING());

            textDisplayer.Display("subject/session confirmation", LangStrings.Blank(),
                LangStrings.SubjectSessionConfirmation(Config.subject, Config.sessionNum.Value, Config.experimentName));
            var keyCode = await inputManager.WaitForKey(new List<KeyCode>() { KeyCode.Y, KeyCode.N });

            SendRamulatorStateMsg(HostPcStateMsg.WAITING(), false);

            if (keyCode == KeyCode.N) {
                await manager.QuitTS();
            }
        }
        protected virtual async Task ConfirmStart() {
            await textDisplayer.PressAnyKey("confirm start", LangStrings.ConfirmStart());
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