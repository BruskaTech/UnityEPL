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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using UnityEPL.Utilities;

namespace UnityEPL {

    // This class is thread safe (except for setup)
    // TODO: JPB: (needed) Fix config setup thread safety
    public static partial class Config {

        // Private Internal Variables
        private const string SYSTEM_CONFIG_NAME = "config.json";
        private static ConcurrentDictionary<string, object> systemConfig = null;
        private static ConcurrentDictionary<string, object> experimentConfig = null;
        private static string configPath = "CONFIG_PATH_NOT_SET";

        private static string onlineSystemConfigText = null;
        private static string onlineExperimentConfigText = null;

        // Public Internal Variables

        /// <summary>
        /// The name of the experiment config file.
        /// </summary>
        public static string experimentConfigName = null;

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
        internal static void SetupSystemConfig(string configPath) {
            systemConfig = null;
            Config.configPath = configPath;

#if !UNITY_WEBGL // System.IO
            if (!Directory.Exists(configPath)) {
                throw new IOException($"Config directory path does not exist: {configPath}");
            }
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

        private static T? GetOptionalSetting<T>(string setting) where T : struct {
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

#nullable enable
        private static T? GetOptionalClassSetting<T>(string setting) where T : class {
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
#nullable disable

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
                if (!Directory.Exists(configPath)) {
                    throw new IOException($"Config directory path does not exist: {configPath}");
                }
                string text = File.ReadAllText(Path.Combine(configPath, SYSTEM_CONFIG_NAME));
                systemConfig = new ConcurrentDictionary<string, object>(FlexibleConfig.LoadFromText(text));
#else
                if (onlineSystemConfigText == null)
                    Debug.Log("Missing config from web");
                else
                    systemConfig = new ConcurrentDictionary<string, object>(FlexibleConfig.LoadFromText(onlineSystemConfigText));
#endif
            }
            return systemConfig;
        }

        private static IDictionary<string, object> GetExperimentConfig() {
            if (experimentConfig == null) {
                // Setup config file
#if !UNITY_WEBGL // System.IO
                string text = File.ReadAllText(Path.Combine(configPath, experimentConfigName + ".json"));
                experimentConfig = new ConcurrentDictionary<string, object>(FlexibleConfig.LoadFromText(text));
#else
                if (onlineExperimentConfigText == null)
                    Debug.Log("Missing config from web");
                else
                    experimentConfig = new ConcurrentDictionary<string, object>(FlexibleConfig.LoadFromText(onlineExperimentConfigText));
#endif
            }
            return experimentConfig;
        }

        // TODO: JPB: Refactor this to be of the singleton form (likely needs to use the new threading system)
        public static IEnumerator GetOnlineConfig() {
            Debug.Log("setting web request");
            string systemConfigPath = Path.Combine(Application.streamingAssetsPath, "config.json");

#if !UNITY_WEBGL
            yield return MainManager.Instance.DelayE(1);
            onlineSystemConfigText = File.ReadAllText(systemConfigPath);
#else
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
#endif

            string experimentConfigPath = Path.Combine(Application.streamingAssetsPath, "CourierOnline.json");

#if !UNITY_WEBGL
            yield return MainManager.Instance.DelayE(1);
            onlineExperimentConfigText = File.ReadAllText(experimentConfigPath);
#else
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
#endif

        }

    }
}