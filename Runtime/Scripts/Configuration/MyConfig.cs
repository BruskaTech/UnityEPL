//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

namespace UnityEPL {
    public static partial class Config {
        // System Settings
        public static bool logNetworkMessages { get { return GetSetting<bool>("logNetworkMessages"); } }
        public static bool debugEventLoopExtendedStackTrace { get { return GetSetting<bool>("debugEventLoopExtendedStackTrace"); } }
        public static bool elememOn { get { return GetSetting<bool>("elememOn"); } }
        public static string hostServerIP { get { return GetSetting<string>("hostServerIP"); } }
        public static int hostServerPort { get { return GetSetting<int>("hostServerPort"); } }
        public static int elememHeartbeatInterval { get { return GetSetting<int>("elememHeartbeatInterval"); } }
        public static bool ramulatorOn { get { return GetSetting<bool>("ramulatorOn"); } }
        public static string niclServerIP { get { return GetSetting<string>("niclServerIP"); } }
        public static int niclServerPort { get { return GetSetting<int>("niclServerPort"); } }

        // Hardware
        public static bool syncboxOn { get { return GetSetting<bool>("syncboxOn"); } }
        public static int syncBoxTestDurationMs { get { return GetSetting<int>("syncBoxTestDurationMs"); } }
        public static bool ps4Controller { get { return GetSetting<bool>("ps4Contoller"); } }

        // Programmer Conveniences
        public static bool lessTrials { get { return GetSetting<bool>("lessTrials"); } }
        public static bool showFps { get { return GetSetting<bool>("showFps"); } }

        // ExperimentBase.cs
        public static int micTestDurationMs { get { return GetSetting<int>("micTestDurationMs"); } }
        public static string introductionVideo { get { return GetSetting<string>("introductionVideo"); } }

        // Local variables
#nullable enable
        public static string? subject {
            get { return GetOptionalClassSetting<string>("subject"); }
            set { SetSetting("subject", value); }
        }
#nullable disable
        public static int? sessionNum {
            get { return GetOptionalSetting<int>("session"); }
            set { SetSetting("session", value); }
        }
        public static string[] availableExperiments {
            get { return GetSetting<string[]>("availableExperiments"); }
            set { SetSetting("availableExperiments", value); }
        }

        // InterfaceManager.cs
        public static bool isTest { get { return GetSetting<bool>("isTest"); } }
        public static int vSync { get { return GetSetting<int>("vSync"); } }
        public static int frameRate { get { return GetSetting<int>("frameRate"); } }

        public static string experimentScene { get { return GetSetting<string>("experimentScene"); } }
        public static string experimentClass { get { return GetSetting<string>("experimentClass"); } }
        public static string launcherScene { get { return GetSetting<string>("launcherScene"); } }
        public static string experimentName { get { return GetSetting<string>("experimentName"); } }

        // FileManager.cs
#nullable enable
        /// <summary>
        /// The path to the data folder.
        /// If not set, defaults to the desktop.
        /// </summary>
        public static string? dataPath { get { return GetOptionalClassSetting<string>("dataPath"); } }
#nullable disable
        /// <summary>
        /// The path to the wordpool file.
        /// </summary>
        public static string wordpool { get { return GetSetting<string>("wordpool"); } }
        public static string practiceWordpool { get { return GetSetting<string>("practiceWordpool"); } }
        /// <summary>
        /// The prefix for participant IDs.
        /// If set to "any", any participant ID is valid.
        /// </summary>
        public static string participantIdPrefix { get { return GetSetting<string>("participantIdPrefix"); } }

        // ExperimentBase.cs
        
        public static bool quitAnytime { get { return GetSetting<bool>("quitAnytime"); } }
        public static bool pauseAnytime { get { return GetSetting<bool>("pauseAnytime"); } }

        // ElememInterface.cs
        public static string stimMode { get { return GetSetting<string>("stimMode"); } }
    }
}