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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEPL {

    [DefaultExecutionOrder(-10)]
    public class InterfaceManager : SingletonEventMonoBehaviour<InterfaceManager> {
        public static new InterfaceManager Instance {
            get {
                var instance = SingletonEventMonoBehaviour<InterfaceManager>.Instance;
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
        public FileManager fileManager;
        protected float pausedTimescale;

        //////////
        // Devices that can be accessed by managed
        // scripts
        //////////
        public HostPC hostPC;
        public RamulatorWrapper ramulator;
        public VideoControl videoControl;
        public TextDisplayer textDisplayer;
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

        protected void OnDestroy() {
            QuitTS();
        }

        void Update() {
            while (events.TryDequeue(out IEnumerator e)) {
                StartCoroutine(e);
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
                fileManager = new FileManager(this);

                // Setup Pausing
                pausedTimescale = Time.timeScale;

                // Setup Configs
                var configs = SetupConfigs();
                GetExperiments(configs);
                fileManager.CreateDataFolder();

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
            Config.SetupSystemConfig(fileManager.ConfigPath());
#else // !UNITY_WEBGL
            Config.SetupSystemConfig(Application.streamingAssetsPath);
#endif // !UNITY_WEBGL

            // Get all configuration files
            string configPath = fileManager.ConfigPath();
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
        public void PauseTS(bool pause) {
            DoTS<Bool>(PauseHelper, pause);
        }
        protected void PauseHelper(Bool pause) {
            // TODO: JPB: (needed) Implement pause functionality correctly
            if (pause) {
                pausedTimescale = Time.timeScale;
                Time.timeScale = 0;
            } else {
                Time.timeScale = pausedTimescale;
            }
            if (videoControl != null) { videoControl.PauseVideo(pause); }
        }

        public void QuitTS() {
            ramulator?.SendExitMsg();
            hostPC?.QuitTS();
            DoWaitForTS(QuitHelper);
        }
        protected async Task QuitHelper() {
            foreach (var eventLoop in eventLoops) {
                //eventLoop.Stop();
                eventLoop.Abort();
            }

            EventReporter.Instance.LogTS("experiment quitted");
            await Timing.Delay(500);
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
    }

}
