//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PsyForge.DataManagement;
using PsyForge.Extensions;
using PsyForge.Utilities;

namespace PsyForge.ExternalDevices {

    public class ElememInterface : HostPC {
        private readonly int sessionNum;

        public ElememInterface(int sessionNum) {
            this.sessionNum = sessionNum;
        }

        public override async Task ConnectTS() {
            await ConnectTS(Config.hostServerIP, Config.hostServerPort);
        }

        public override async Task ConfigureTS() {
            await DoWaitForTS(ConfigureHelper);
        }
        protected async Task ConfigureHelper() {
            // Configure Elemem
            await SendAndReceiveJsonTS("CONNECTED", "CONNECTED_OK");

            string stimMode = Config.stimMode switch {
                "ReadOnly" => "none",
                "OpenLoop" => "open",
                "ClosedLoop" => "closed",
                _ => "ERROR"
            };

            Dictionary<string, object> configDict = new() {
                { "stim_mode", stimMode },
                { "experiment", Config.experimentName },
                { "subject", Config.subject },
                { "session", Config.sessionNum },
            };
            await SendAndReceiveJsonTS("CONFIGURE", configDict, "CONFIGURE_OK");

            // Latency Check
            await DoLatencyCheckTS();

            // Start Heartbeats
            DoHeartbeatsForeverTS();

            await SendTS("SESSION", new() {
                { "session", sessionNum },
            });

            // Start Elemem
            await SendTS("READY");
        }

        public override async Task QuitTS() {
            await SendExitMsgTS();
            DisconnectTS();
        }

        private uint heartbeatCount = 0;
        private TimeSpan LastHeartbeatDelay = TimeSpan.Zero;
        private Queue<TimeSpan> LastTenHeartbeatDelays = new(Enumerable.Repeat(TimeSpan.Zero, 10));
        protected override CancellationTokenSource DoHeartbeatsForeverTS() {
            return DoRepeatingTS(0, Config.elememHeartbeatInterval, null, DoHeartbeatHelper);
        }
        protected async Task DoHeartbeatHelper() {
            Dictionary<string, object> data = new() {
                { "count", heartbeatCount }
            };
            heartbeatCount++;

            var startTime = Clock.UtcNow;
            await SendAndReceiveJsonTS("HEARTBEAT", data, "HEARTBEAT_OK");
            LastHeartbeatDelay = Clock.UtcNow - startTime;
            LastTenHeartbeatDelays.Dequeue();
            LastTenHeartbeatDelays.Enqueue(LastHeartbeatDelay);
        }
        public override async Task<TimeSpan> GetLastHeartbeatDelayTS() {
            return await DoGetTS(GetLastHeartbeatDelayHelper);
        }
        protected TimeSpan GetLastHeartbeatDelayHelper() {
            return LastHeartbeatDelay;
        }
        public override async Task<TimeSpan> GetLastAvgHeartbeatDelayTS() {
            return await DoGetTS(GetLastAvgHeartbeatDelayHelper);
        }
        protected TimeSpan GetLastAvgHeartbeatDelayHelper() {
            return new TimeSpan((long)LastTenHeartbeatDelays.Average(ts => ts.Ticks));
        }

        public override async Task<TimeSpan> GetMsgQueueDelayTS() {
            // This use of a lambda to pass captured values across the thread boundary is not thread safe.
            // However, it is safe in this case because the stopwatch is only used in this one place
            // and it uses an await to make sure things don't happen at the same time.
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await DoWaitForTS(() => stopwatch.Stop());
            return stopwatch.Elapsed;
        }

        protected readonly static double maxSingleTimeMs = 20;
        protected readonly static double meanSingleTimeMs = 5;
        protected override async Task DoLatencyCheckTS() {
            await DoWaitForTS(DoLatencyCheckHelper);
        }
        protected async Task DoLatencyCheckHelper() {
            DateTime startTime;
            double[] delay = new double[20];

            // Send 20 heartbeats, every 50ms, except if max latency is out of tolerance
            for (int i = 0; i < 20; i++) {
                UnityEngine.Debug.Log($"Latency Check {i}");
                startTime = Clock.UtcNow;
                await DoHeartbeatHelper();
                delay[i] = (Clock.UtcNow - startTime).TotalMilliseconds;

                if (delay[i] >= maxSingleTimeMs) {
                    throw new TimeoutException($"Single heartbeat time ({delay[i]}ms) greater than {maxSingleTimeMs}ms");
                }

                await Task.Delay(50 - (int)delay[i]);
            }

            // Check average latency
            double max = delay.Max();
            double mean = delay.Average();
            if (mean >= meanSingleTimeMs) {
                throw new TimeoutException($"Mean heartbeat time ({mean}ms) greater than {meanSingleTimeMs}ms");
            }

            // the maximum resolution of the timer in nanoseconds
            long acc = (1000L * 1000L * 1000L) / Stopwatch.Frequency;

            Dictionary<string, object> dict = new() {
                { "max_latency_ms", max },
                { "mean_latency_ms", mean },
                { "resolution_ns", acc },
            };
            EventReporter.Instance.LogTS("latency check", dict);
            UnityEngine.Debug.Log(string.Join(Environment.NewLine, dict));
        }

        protected async Task SendTS(string type, Dictionary<string, object> data = null) {
            await SendJsonTS(type, data);
        }

        protected async Task<JObject> ReceiveTS(string type) {
            var json = await ReceiveJsonTS(type);
            var msgType = json.GetValue("type").Value<string>();

            if (msgType == "EXIT") {
                DisconnectTS();
                throw new InvalidOperationException("Elemem exited and ended it's connection");
            } else if (msgType.Contains("ERROR")) {
                throw new InvalidOperationException($"Error received from {this.GetType().Name} is {msgType}: {json.GetValue("data")}");
            }
            return json;
        }

        // public override async Task SendMathMsgTS(string problem, string response, int responseTimeMs, bool correct) {
        //     Dictionary<string, object> data = new() {
        //         { "problem", problem },
        //         { "response", response },
        //         { "response_time_ms", responseTimeMs },
        //         { "correct", correct },
        //     };
        //     await SendTS("MATH", data);
        // }

        public override async Task SendStimSelectMsgTS(string tag) {
            Dictionary<string, object> data = new() {
                { "stimtag", tag },
            };
            await SendTS("STIMSELECT", data);
        }

        public override async Task SendStimMsgTS() {
            UnityEngine.Debug.Log("Sending STIM");
            await SendTS("STIM");
        }

        public override async Task SendCLMsgTS(HostPcClMsg type) {
            await SendTS(type.name, type.dict);
        }

        public override async Task SendCCLMsgTS(HostPcCclMsg type) {
            await SendTS(type.name, type.dict);
        }

        // public override async Task SendSessionMsgTS(int session) {
        //     Dictionary<string, object> data = new() {
        //         { "session", session },
        //     };
        //     await SendTS("SESSION", data);
        // }
        public override Task SendExpMsgTS(HostPcExpMsg exp, Dictionary<string, object> extraData = null) {
            var dict = (extraData ?? new()).Concat(exp.dict).ToDictionary(x=>x.Key,x=>x.Value);
            EventReporter.Instance.LogTS(exp.name, extraData);
            return Task.CompletedTask;
        }

        public override Task SendStateMsgTS(HostPcStatusMsg state, Dictionary<string, object> extraData = null) {
            var dict = (extraData ?? new()).Concat(state.dict).ToDictionary(x=>x.Key,x=>x.Value);
            EventReporter.Instance.LogTS(state.name, extraData);
            return Task.CompletedTask;
        }

        // public override async Task SendTrialMsgTS(int trial, bool stim) {
        //     Dictionary<string, object> data = new() {
        //         { "trial", trial },
        //         { "stim", stim },
        //     };
        //     await SendTS("TRIAL", data);
        // }

        // public override async Task SendWordMsgTS(string word, int serialPos, bool stim, Dictionary<string, object> extraData = null) {
        //     var data = (extraData != null) ? new Dictionary<string, object>(extraData) : new();
        //     data["word"] = word;
        //     data["serialPos"] = serialPos;
        //     data["stim"] = stim;  
        //     await SendTS("WORD", data);
        // }

        public override async Task SendExitMsgTS() {
            await SendTS("EXIT");
        }

        public override async Task SendLogMsgTS(string type, DateTime time, Dictionary<string, object> data = null) {
            await SendTS(type, data);
        }

        /// <summary>
        /// This is just like SendLogMsgTS except that it does not throw an exception if the connection is not established.
        /// Only use this if you truly don't care if the message is sent or not.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="time"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public override async Task SendLogMsgIfConnectedTS(string type, DateTime time, Dictionary<string, object> data = null) {
            try {
                await SendTS(type, data);
            } catch (UnConnectedException) { } // Ignore unconnected exceptions
        }
    }

}