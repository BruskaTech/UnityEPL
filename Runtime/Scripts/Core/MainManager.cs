//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEPL.Utilities;
using UnityEPL.Networking;
using UnityEPL.Threading;
using UnityEPL.Extensions;
using UnityEPL.GUI;

namespace UnityEPL {

    [DefaultExecutionOrder(-10)]
    public class MainManager : SingletonEventMonoBehaviour<MainManager> {
        public static new MainManager Instance {
            get {
                var instance = SingletonEventMonoBehaviour<MainManager>.Instance;
                if (instance == null) {
                    throw new InvalidOperationException("InterfaceManager not initialized. The starting scene of the game MUST be the manager scene.");
                }
                return instance;
            }
            private set { }
        }

        const string SYSTEM_CONFIG = "config.json";

        //////////
        // ???
        //////////
        protected ConcurrentStack<float> pauseTimescales = new();

        //////////
        // Devices that can be accessed by managed
        // scripts
        //////////
        public HostPC hostPC;
        public RamulatorWrapper ramulator;
        public VideoControl videoControl;
        public SoundRecorder recorder;
        //public RamulatorInterface ramulator;
        public ISyncBox syncBox;

        //////////
        // Provided AudioSources
        //////////
        public AudioSource highBeep;
        public AudioSource lowBeep;
        public AudioSource lowerBeep;
        public AudioSource playback;

        //////////
        // Event Loop Handling
        //////////
        public ConcurrentBag<EventLoop> eventLoops = new();
        public ConcurrentQueue<IEnumerator> events = new();
        public ConcurrentQueue<IEnumerator> unpausableEvents = new();

        //////////
        // StartTime
        //////////
        public DateTime StartTimeTS { 
            get {
                return new();
            } 
            protected set { } 
        }
        public TimeSpan TimeSinceStartupTS {
            get { return Clock.UtcNow - StartTimeTS; }
            protected set { }
        }
        public DateTime TimeStampTS {
            get { return StartTimeTS.Add(TimeSinceStartupTS); }
            private set { }
        }

        //////////
        // Setup
        //////////

        protected async void OnDestroy() {
            await QuitTS();
        }

        void Update() {
            while (events.TryDequeue(out IEnumerator e)) {
                StartCoroutine(e);
            }
            while (unpausableEvents.TryDequeue(out IEnumerator e)) {
                StartCoroutine(e, true);
            }
        }

        protected override void AwakeOverride() {
            StartTimeTS = Clock.UtcNow;
        }

        protected void Start() {
            // Unity internal event handling
            SceneManager.sceneLoaded += onSceneLoaded;

            try {
                // Create objects not tied to unity
                // Nothing for now

                // Setup Configs
                var configs = SetupConfigs();
                GetExperiments(configs);
                FileManager.CreateDataFolder();

                // Setup Syncbox Interface
                if (!Config.isTest && Config.syncboxOn) {
                    syncBox.Init();
                }

                // Launch Startup Scene
                LaunchLauncher();
            } catch(Exception e) {
                ErrorNotifier.ErrorTS(e);
            }
        }

        protected string[] SetupConfigs() {
#if !UNITY_WEBGL // System.IO
            Config.SetupSystemConfig(FileManager.ConfigPath());
#else // !UNITY_WEBGL
            Config.SetupSystemConfig(Application.streamingAssetsPath);
#endif // !UNITY_WEBGL

            // Get all configuration files
            string configPath = FileManager.ConfigPath();
            string[] configs = Directory.GetFiles(configPath, "*.json");
            if (configs.Length < 2) {
                ErrorNotifier.ErrorTS(new Exception("Configuration File Error. Missing system or experiment configuration file in configs folder"));
            }
            return configs;
        }

        protected void GetExperiments(string[] configs) {
            List<string> exps = new List<string>();

            UnityEngine.Debug.Log("Experiment Options:\n" + string.Join("\n", configs));
            for (int i = 0, j = 0; i < configs.Length; i++) {
                if (!configs[i].Contains(SYSTEM_CONFIG))
                    exps.Add(Path.GetFileNameWithoutExtension(configs[i]));
                j++;
            }
            Config.availableExperiments = exps.ToArray();
        }

        //////////
        // Collect references to managed objects
        // and release references to non-active objects
        //////////
        private void onSceneLoaded(Scene scene, LoadSceneMode mode) {
            // TODO: JPB: (needed) Check
            //onKey = new ConcurrentQueue<Action<string, bool>>(); // clear keyhandler queue on scene change

            // Voice Activity Detector
            //GameObject voice = GameObject.Find("VAD");
            //if (voice != null) {
            //    voiceActity = voice.GetComponent<VoiceActivityDetection>();
            //    Debug.Log("Found VoiceActivityDetector");
            //}

            // Video Control
            GameObject video = GameObject.Find("VideoPlayer");
            if (video != null) {
                videoControl = video.GetComponent<VideoControl>();
                video.SetActive(false);
                Debug.Log("Initalized VideoPlayer");
            }

            // Beep Sounds
            GameObject sound = GameObject.Find("Sounds");
            if (sound != null) {
                lowBeep = sound.transform.Find("LowBeep").gameObject.GetComponent<AudioSource>();
                lowerBeep = sound.transform.Find("LowerBeep").gameObject.GetComponent<AudioSource>();
                highBeep = sound.transform.Find("HighBeep").gameObject.GetComponent<AudioSource>();
                playback = sound.transform.Find("Playback").gameObject.GetComponent<AudioSource>();
                Debug.Log("Initialized Sounds");
            }

            // Sound Recorder
            GameObject soundRecorder = GameObject.Find("SoundRecorder");
            if (soundRecorder != null) {
                recorder = soundRecorder.GetComponent<SoundRecorder>();
                Debug.Log("Initialized Sound Recorder");
            }

            // Ramulator Interface
            //GameObject ramulatorObject = GameObject.Find("RamulatorInterface");
            //if (ramulatorObject != null) {
            //    ramulator = ramulatorObject.GetComponent<RamulatorInterface>();
            //    Debug.Log("Found Ramulator");
            //}
        }

        protected void LaunchLauncher() {
            // Reset external hardware state if exiting task
            //syncBox.StopPulse();
            hostPC?.SendExitMsgTS();

            //mainEvents.Pause(true);
            for (int i = 0; i < SceneManager.sceneCount; ++i) {
                UnityEngine.Debug.Log(SceneManager.GetSceneAt(i).name);
            }
            SceneManager.LoadScene(Config.launcherScene);
        }

        // These can be called by anything
        
        // public void PauseTS(bool pause) {
        //     // This is ONLY done for pause because it is a special case
        //     unpausableEvents.Enqueue(PauseHelperEnumerator(pause));
        // }
        // protected IEnumerator PauseHelperEnumerator(Bool pause) {
        //     PauseHelper(pause);
        //     yield return null;
        // }
        public void Pause(bool pause) {
            Do<Bool>(PauseHelper, pause);
        }
        protected void PauseHelper(Bool pause) {
            // TODO: JPB: (needed) Implement pause functionality correctly
            float oldTimeScale = 0;
            if (pause) {
                pauseTimescales.Push(Time.timeScale);
                Time.timeScale = 0;
            } else {
                if (pauseTimescales.TryPop(out oldTimeScale) ) {
                    Time.timeScale = oldTimeScale;
                }
            }
            if (videoControl != null) { videoControl.PauseVideo(oldTimeScale != 0); }
        }
        public bool IsPausedTS() {
            return IsPausedHelper();
        }
        public bool IsPaused() {
            return DoGet(IsPausedHelper);
        }
        protected bool IsPausedHelper() {
            return pauseTimescales.Count > 0;
        }

        public async Task QuitTS() {
            ramulator?.SendExitMsg();
            hostPC?.QuitTS();
            await DoWaitForTS(QuitHelper);
        }
        protected async Task QuitHelper() {
            // TODO: JPB: (feature) Make EventLoops stop gracefully by awaiting the stop with a timeout that gets logged if triggered
            foreach (var eventLoop in eventLoops) {
                _ = eventLoop.Abort();
            }

            EventReporter.Instance.LogTS("experiment quitted");
            await Delay(500);
            this.Quit();
        }


        // Helpful functions
        public void LockCursor(CursorLockMode isLocked) {
            Do(LockCursorHelper, isLocked);
        }
        public void LockCursorTS(CursorLockMode isLocked) {
            DoTS(LockCursorHelper, isLocked);
        }
        public void LockCursorHelper(CursorLockMode isLocked) {
            UnityEngine.Cursor.lockState = isLocked;
            UnityEngine.Cursor.visible = isLocked == CursorLockMode.None;
        }

        // Timing Functions
        public async Task DelayTS(int millisecondsDelay) {
            await DoWaitForTS(DelayEHelper, millisecondsDelay);
        }
        public async Task Delay(int millisecondsDelay) {
            await ToCoroutineTask(DelayE(millisecondsDelay));
        }
        public IEnumerator DelayE(int millisecondsDelay) {
            return DoWaitFor(DelayEHelper, millisecondsDelay);
        }
        public IEnumerator DelayEHelper(int millisecondsDelay) {
            if (millisecondsDelay < 0) {
                throw new ArgumentOutOfRangeException($"millisecondsDelay <= 0 ({millisecondsDelay})");
            } else if (millisecondsDelay == 0) {
                yield break;
            }

            yield return new Delay(millisecondsDelay);
        }
    }

    class Delay : CustomYieldInstruction {
        private double seconds;
        private DateTime lastTime;

        public Delay(double seconds) {
            this.seconds = seconds;
            lastTime = Clock.UtcNow;
        }

        public Delay(int millisecondsDelay) {
            seconds = millisecondsDelay / 1000f;
            lastTime = Clock.UtcNow;
        }

        public override bool keepWaiting {
            get {
                if (MainManager.Instance.IsPaused()) { return true; }
                var time = Clock.UtcNow;
                var diff = (time - lastTime).TotalSeconds;
                seconds -= diff;
                lastTime = time;
                return seconds > 0;
            }
        }
    }
}
