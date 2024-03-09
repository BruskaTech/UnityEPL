//Copyright (c) 2024 Jefferson University
//Copyright (c) 2024 Bruska Technologies LLC
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEPL {

    // This class is thread safe (except for setup)
    // TODO: JPB: (needed) Fix config setup thread safety
    public static partial class Config {

        // Private Internal Variables
        private const string SYSTEM_CONFIG_NAME = "config.json";
        private static ConcurrentDictionary<string, object> systemConfig = null;
        private static ConcurrentDictionary<string, object> experimentConfig = null;
        private static string configPath = "CONFIG_PATH_NOT_SET";

        public static string onlineSystemConfigText = null;
        public static string onlineExperimentConfigText = null;

        // Public Internal Variables
        public static string experimentConfigName = null;

        // System Settings
        public static bool logNetworkMessages { get { return Config.GetSetting<bool>("logNetworkMessages"); } }
        public static bool elememOn { get { return Config.GetSetting<bool>("elememOn"); } }
        public static string hostServerIP { get { return Config.GetSetting<string>("hostServerIP"); } }
        public static int hostServerPort { get { return Config.GetSetting<int>("hostServerPort"); } }
        public static int elememHeartbeatInterval { get { return Config.GetSetting<int>("elememHeartbeatInterval"); } }
        public static bool ramulatorOn { get { return Config.GetSetting<bool>("ramulatorOn"); } }
        public static string niclServerIP { get { return Config.GetSetting<string>("niclServerIP"); } }
        public static int niclServerPort { get { return Config.GetSetting<int>("niclServerPort"); } }

        // Hardware
        public static bool syncboxOn { get { return Config.GetSetting<bool>("syncboxOn"); } }
        public static bool ps4Controller { get { return Config.GetSetting<bool>("ps4Contoller"); } }

        // Programmer Conveniences
        public static bool lessTrials { get { return Config.GetSetting<bool>("lessTrials"); } }
        public static bool showFps { get { return Config.GetSetting<bool>("showFps"); } }

        // Game Section Skips
        //public static bool skipIntros { get { return Config.GetSetting<bool>("skipIntros"); } }
        //public static bool skipTownLearning { get { return Config.GetSetting<bool>("skipTownLearning"); } }
        //public static bool skipNewEfrKeypressCheck { get { return Config.GetSetting<bool>("skipNewEfrKeypressCheck"); } }
        //public static bool skipNewEfrKeypressPractice { get { return Config.GetSetting<bool>("skipNewEfrKeypressPractice"); } }

        // Local variables
        public static string subject {
            get { return Config.GetSetting<string>("subject"); }
            set { Config.SetSetting("subject", value); }
        }
        public static int sessionNum {
            get { return Config.GetSetting<int>("session"); }
            set { Config.SetSetting("session", value); }
        }
        public static string[] availableExperiments {
            get { return Config.GetSetting<string[]>("availableExperiments"); }
            set { Config.SetSetting("availableExperiments", value); }
        }

        // InterfaceManager.cs
        public static bool isTest { get { return Config.GetSetting<bool>("isTest"); } }
        public static int? eventsPerFrame { get { return Config.GetNullableSetting<int>("eventsPerFrame"); } }
        public static int vSync { get { return Config.GetSetting<int>("vSync"); } }
        public static int frameRate { get { return Config.GetSetting<int>("frameRate"); } }

        public static string experimentScene { get { return Config.GetSetting<string>("experimentScene"); } }
        public static string experimentClass { get { return Config.GetSetting<string>("experimentClass"); } }
        public static string launcherScene { get { return Config.GetSetting<string>("launcherScene"); } }
        public static string experimentName { get { return Config.GetSetting<string>("experimentName"); } }

        // FileManager.cs
        public static string dataPath { get { return Config.GetSetting<string>("dataPath"); } }
        public static string wordpool { get { return Config.GetSetting<string>("wordpool"); } }
        public static string practiceWordpool { get { return Config.GetSetting<string>("practiceWordpool"); } }
        public static string prefix { get { return Config.GetSetting<string>("prefix"); } }

        // ExperimentBase.cs
        
        public static bool quitAnytime { get { return Config.GetSetting<bool>("quitAnytime"); } }

        // ElememInterface.cs
        public static string stimMode { get { return Config.GetSetting<string>("stimMode"); } }


        // Functions
        public static void SaveConfigs(string path) {
            if (experimentConfig != null) {
                EventReporter.Instance.LogTS("experimentConfig", new(experimentConfig));
#if !UNITY_WEBGL // System.IO
                FlexibleConfig.WriteToText(experimentConfig, Path.Combine(path, experimentConfigName + ".json"));
#endif // !UNITY_WEBGL
            }

            if (systemConfig != null) {
                EventReporter.Instance.LogTS("systemConfig", new(systemConfig));
#if !UNITY_WEBGL // System.IO
                FlexibleConfig.WriteToText(systemConfig, Path.Combine(path, SYSTEM_CONFIG_NAME));
#endif // !UNITY_WEBGL
            }
        }

        public static bool IsExperimentConfigSetup() {
            return experimentConfigName != null;
        }

        // This has to be called before SetupExperimentConfig
        public static void SetupSystemConfig(string configPath) {
            systemConfig = null;
            Config.configPath = configPath;

#if !UNITY_WEBGL // System.IO
            GetSystemConfig();
#else // UNITY_WEBGL
            var ucr = UnityCoroutineRunner.Generate();
            ucr.RunCoroutine(SetupOnlineSystemConfig());
#endif // UNITY_WEBGL
        }

        public static void SetupExperimentConfig() {
            experimentConfig = null;

#if !UNITY_WEBGL // System.IO
            GetExperimentConfig();
#else // UNITY_WEBGL
            var ucr = UnityCoroutineRunner.Generate();
            ucr.RunCoroutine(SetupOnlineExperimentConfig());
#endif // UNITY_WEBGL
        }

#if UNITY_WEBGL // System.IO
        private static IEnumerator SetupOnlineSystemConfig() {
            string systemConfigPath = System.IO.Path.Combine(configPath, SYSTEM_CONFIG_NAME);
            UnityWebRequest systemWWW = UnityWebRequest.Get(systemConfigPath);
            yield return systemWWW.SendWebRequest();

            if (systemWWW.result != UnityWebRequest.Result.Success) {
                Debug.Log("Network error " + systemWWW.error);
            } else {
                var onlineSystemConfigText = systemWWW.downloadHandler.text;
                Debug.Log("Online System Config fetched!!");
                Debug.Log(onlineSystemConfigText);
                systemConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineSystemConfigText));
            }
        }

        private static IEnumerator SetupOnlineExperimentConfig() {
            string experimentConfigPath = System.IO.Path.Combine(configPath, experimentConfigName);
            UnityWebRequest experimentWWW = UnityWebRequest.Get(experimentConfigPath);
            yield return experimentWWW.SendWebRequest();

            if (experimentWWW.result != UnityWebRequest.Result.Success) {
                Debug.Log("Network error " + experimentWWW.error);
            } else {
                var onlineExperimentConfigText = experimentWWW.downloadHandler.text;
                Debug.Log("Online Experiment Config fetched!!");
                Debug.Log(onlineExperimentConfigText);
                experimentConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineExperimentConfigText));
            }
        }
#endif // UNITY_WEBGL

        private static Nullable<T> GetNullableSetting<T>(string setting) where T : struct {
            object value;

            if (IsExperimentConfigSetup()) {
                var experimentConfig = GetExperimentConfig();
                if (experimentConfig.TryGetValue(setting, out value))
                    return (T)value;
            }

            var systemConfig = GetSystemConfig();
            if (systemConfig.TryGetValue(setting, out value))
                return (T)value;

            return null;
        }

        private static T GetSetting<T>(string setting) {
            object value;

            if (IsExperimentConfigSetup()) {
                var experimentConfig = GetExperimentConfig();
                if (experimentConfig.TryGetValue(setting, out value))
                    return (T)value;
            }

            var systemConfig = GetSystemConfig();
            if (systemConfig.TryGetValue(setting, out value))
                return (T)value;

            string expConfigNotLoaded = IsExperimentConfigSetup() ? "" : "\nNote: Experiment config not loaded yet.";
            throw new MissingFieldException("Missing Config Setting " + setting + "." + expConfigNotLoaded);
        }

        private static void SetSetting<T>(string setting, T value) {
            object getValue;

            if (IsExperimentConfigSetup() && (GetExperimentConfig().TryGetValue(setting, out getValue)))
                // Setting is in Experiment Config
                GetExperimentConfig()[setting] = value;
            else if (GetSystemConfig().TryGetValue(setting, out getValue))
                // Setting is in System Config
                GetSystemConfig()[setting] = value;
            else if (IsExperimentConfigSetup())
                // Setting is not present, so put it in Experiment Config if it is setup
                GetExperimentConfig()[setting] = value;
            else
                // No other options, put it into System Config
                GetSystemConfig()[setting] = value;
        }

        private static IDictionary<string, object> GetSystemConfig() {
            if (systemConfig == null) {
                // Setup config file
#if !UNITY_WEBGL // System.IO
                string text = File.ReadAllText(Path.Combine(configPath, SYSTEM_CONFIG_NAME));
                systemConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(text));
#else
                if (onlineSystemConfigText == null)
                    Debug.Log("Missing config from web");
                else
                    systemConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineSystemConfigText));
#endif
            }
            return systemConfig;
        }

        private static IDictionary<string, object> GetExperimentConfig() {
            if (experimentConfig == null) {
                // Setup config file
#if !UNITY_WEBGL // System.IO
                string text = File.ReadAllText(Path.Combine(configPath, experimentConfigName + ".json"));
                experimentConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(text));
#else
                if (onlineExperimentConfigText == null)
                    Debug.Log("Missing config from web");
                else
                    experimentConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineExperimentConfigText));
#endif
            }
            return experimentConfig;
        }

        // TODO: JPB: Refactor this to be of the singleton form (likely needs to use the new threading system)
        public static IEnumerator GetOnlineConfig() {
            Debug.Log("setting web request");
            string systemConfigPath = Path.Combine(Application.streamingAssetsPath, "config.json");

#if !UNITY_EDITOR
            UnityWebRequest systemWWW = UnityWebRequest.Get(systemConfigPath);
            yield return systemWWW.SendWebRequest();

            // TODO: LC: 
            if (systemWWW.result != UnityWebRequest.Result.Success)
            // if (systemWWW.isNetworkError || systemWWW.isHttpError)
            {
                Debug.Log("Network error " + systemWWW.error);
            } else {
                onlineSystemConfigText = systemWWW.downloadHandler.text;
                Debug.Log("Online System Config fetched!!");
                Debug.Log(onlineSystemConfigText);
            }
#else
            yield return new WaitForSeconds(1f);
            onlineSystemConfigText = File.ReadAllText(systemConfigPath);
#endif

            string experimentConfigPath = Path.Combine(Application.streamingAssetsPath, "CourierOnline.json");

#if !UNITY_EDITOR
            UnityWebRequest experimentWWW = UnityWebRequest.Get(experimentConfigPath);
            yield return experimentWWW.SendWebRequest();

            // TODO: LC: 
            if (experimentWWW.result != UnityWebRequest.Result.Success)
            // if (experimentWWW.isNetworkError || experimentWWW.isHttpError)
            {
                Debug.Log("Network error " + experimentWWW.error);
            } else {
                onlineExperimentConfigText = experimentWWW.downloadHandler.text;
                Debug.Log("Online Experiment Config fetched!!");
                Debug.Log(Config.onlineExperimentConfigText);
            }
#else
            yield return new WaitForSeconds(1f);
            onlineExperimentConfigText = File.ReadAllText(experimentConfigPath);
#endif

        }

    }
}