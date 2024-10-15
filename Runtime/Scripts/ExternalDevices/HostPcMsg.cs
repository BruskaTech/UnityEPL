//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEPL.Utilities;

namespace UnityEPL.ExternalDevices {
    public abstract class HostPcMsg {
        public readonly string name;
        public readonly Dictionary<string, object> dict;

        protected HostPcMsg(string name, Dictionary<string, object> dict = null) {
            this.name = name;
            this.dict = dict ?? new();
        }
    }

    public class HostPcExpMsg : HostPcMsg {
        protected HostPcExpMsg(string name, Dictionary<string, object> dict = null) : base(name, dict) {}

        public static HostPcExpMsg TRIAL(int trialNum, bool stim, bool isPractice) { return new HostPcExpMsg("TRIAL", new() {
            {"trial", trialNum},
            {"stim", stim},
            {"practice", isPractice},
        }); }
        public static HostPcExpMsg SESSION(int sessionNum) { return new HostPcExpMsg("SESSION", new() {
            {"session", sessionNum},
        }); }
        public static HostPcExpMsg WORD(string[] words, int serialPos, bool stim) { return new HostPcExpMsg("WORD", new() {
            {"words", words},
            {"serialpos", serialPos},
            {"stimWord", stim},
        }); }
        
    }

    // Host PC State Message
    public partial class HostPcStatusMsg : HostPcMsg {
        protected HostPcStatusMsg(string name, Dictionary<string, object> dict = null) 
            : base("TASK_STATUS", new() { {"status", name}, {"data", dict} }) {}

        public static HostPcStatusMsg PAUSE(bool pauseStart) { return new HostPcStatusMsg("PAUSE", new() { {"pause start", pauseStart} }); }
        public static HostPcStatusMsg REST() { return new HostPcStatusMsg("REST"); }
        public static HostPcStatusMsg ORIENT() { return new HostPcStatusMsg("ORIENT"); }
        public static HostPcStatusMsg COUNTDOWN() { return new HostPcStatusMsg("COUNTDOWN"); }
        public static HostPcStatusMsg DISTRACT() { return new HostPcStatusMsg("DISTRACT"); }
        public static HostPcStatusMsg INSTRUCT() { return new HostPcStatusMsg("INSTRUCT"); }
        public static HostPcStatusMsg WAITING() { return new HostPcStatusMsg("WAITING"); }
        public static HostPcStatusMsg SYNC() { return new HostPcStatusMsg("SYNC"); }
        public static HostPcStatusMsg VOCALIZATION() { return new HostPcStatusMsg("VOCALIZATION"); }
        public static HostPcStatusMsg FIXATION() { return new HostPcStatusMsg("FIXATION"); }
        public static HostPcStatusMsg ENCODING() { return new HostPcStatusMsg("ENCODING"); }
        public static HostPcStatusMsg RETRIEVAL() { return new HostPcStatusMsg("RETRIEVAL"); }
        public static HostPcStatusMsg MATH() { return new HostPcStatusMsg("MATH"); }
        public static HostPcStatusMsg ISI(float duration) { return new HostPcStatusMsg("ISI", new() {{"duration", duration}}); }
        public static HostPcStatusMsg FREE_RECALL(float duration) { return new HostPcStatusMsg("FREE_RECALL", new() {{"duration", duration}}); }
        public static HostPcStatusMsg CUED_RECALL(float duration) { return new HostPcStatusMsg("CUED_RECALL", new() {{"duration", duration}}); }
        public static HostPcStatusMsg FINAL_RECALL(float duration) { return new HostPcStatusMsg("FINAL_RECALL", new() {{"duration", duration}}); }
        public static HostPcStatusMsg RECOGNITION(float duration) { return new HostPcStatusMsg("RECOGNITION", new() {{"duration", duration}}); }
    }

    // Host PC Closed Loop Message
    public class HostPcClMsg : HostPcMsg {
        private HostPcClMsg(string name, Dictionary<string, object> dict) : base(name, dict) {}

        public static HostPcClMsg STIM(uint durationMs) { return new HostPcClMsg("STIM", new() {{ "classifyms", durationMs }}); }
        public static HostPcClMsg SHAM(uint durationMs) { return new HostPcClMsg("SHAM", new() {{ "classifyms", durationMs }}); }
        public static HostPcClMsg NORMALIZE(uint durationMs) { return new HostPcClMsg("NORMALIZE", new() {{ "classifyms", durationMs }}); }
    }

    // Host PC Continuous Closed Loop Message
    public class HostPcCclMsg {
        public readonly string name;
        public readonly Dictionary<string, object> dict;

        private HostPcCclMsg(string name) {
            this.name = name;
            this.dict = new();
        }
        private HostPcCclMsg(string name, Dictionary<string, object> dict) {
            this.name = name;
            this.dict = dict;
        }

        public static HostPcCclMsg START_STIM(int durationS) { return new HostPcCclMsg("START_STIM", new() {{"duration", durationS}}); }
        public static HostPcCclMsg PAUSE_STIM() { return new HostPcCclMsg("PAUSE_STIM"); }
        public static HostPcCclMsg RESUME_STIM() { return new HostPcCclMsg("RESUME_STIM"); }
        public static HostPcCclMsg STOP_STIM() { return new HostPcCclMsg("STOP_STIM"); }
    }
}