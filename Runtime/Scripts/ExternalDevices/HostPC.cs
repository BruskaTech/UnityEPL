//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using PsyForge.Utilities;

namespace PsyForge.ExternalDevices {
    public abstract class HostPC : NetworkInterface {
        protected abstract Task DoLatencyCheckTS();
        protected abstract CancellationTokenSource DoHeartbeatsForeverTS();

        public abstract Task ConnectTS();
        public abstract Task ConfigureTS();
        public abstract Task QuitTS();
        public abstract Task<TimeSpan> GetLastAvgHeartbeatDelayTS();
        public abstract Task<TimeSpan> GetLastHeartbeatDelayTS();
        public abstract Task<TimeSpan> GetMsgQueueDelayTS();

        // public abstract Task SendMathMsgTS(string problem, string response, int responseTimeMs, bool correct);
        public abstract Task SendStimSelectMsgTS(string tag);
        public abstract Task SendStimMsgTS();
        public abstract Task SendCLMsgTS(HostPcClMsg type);
        public abstract Task SendCCLMsgTS(HostPcCclMsg type);
        // public abstract Task SendSessionMsgTS(int session);
        // public abstract Task SendTrialMsgTS(int trial, bool stim);
        // public abstract Task SendWordMsgTS(string word, int serialPos, bool stim, Dictionary<string, object> extraData = null);
        public abstract Task SendStateMsgTS(HostPcStatusMsg state, Dictionary<string, object> extraData = null);
        public abstract Task SendExpMsgTS(HostPcExpMsg exp, Dictionary<string, object> extraData = null);
        public abstract Task SendExitMsgTS();
        public abstract Task SendLogMsgTS(string type, DateTime time, Dictionary<string, object> data = null);
        /// <summary>
        /// This is just like SendLogMsgTS except that it does not throw an exception if the connection is not established.
        /// Only use this if you truly don't care if the message is sent or not.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="time"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract Task SendLogMsgIfConnectedTS(string type, DateTime time, Dictionary<string, object> data = null);
    }

}