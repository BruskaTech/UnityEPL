//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

//#define NETWORKINTERFACE_DEBUG_MESSAGES

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;

using Newtonsoft.Json.Linq;

using UnityEPL.DataManagement;
using UnityEPL.Threading;
using UnityEPL.Extensions;
using System.Diagnostics;

namespace UnityEPL.Utilities {
    public class UnConnectedException : IOException {
        public UnConnectedException() { }
        public UnConnectedException(string message) : base(message) { }
        public UnConnectedException(string message, Exception inner) : base(message, inner) { }
    }

    public abstract class NetworkInterface : EventLoop {
        private TcpClient tcpClient;
        private NetworkStream stream;

        private bool stopListening = false;
        private readonly List<(string, TaskCompletionSource<JObject>)> receiveRequests = new();

        private readonly static int connectionTimeoutMs = 5000;
        private readonly static int sendTimeoutMs = 5000;
        private readonly static int receiveTimeoutMs = 5000;

        private int packetId = 0;

        ~NetworkInterface() {
            DisconnectHelper();
        }

        public async Task<bool> IsConnectedTS() {
            return await DoGetTS(IsConnectedHelper);
        }
        private Bool IsConnectedHelper() {
            return tcpClient?.Connected ?? false;
        }

        public async Task ConnectTS(string ip, int port) {
            await DoWaitForTS(ConnectHelper, ip.ToNativeText(), port);
        }
        private async Task ConnectHelper(NativeText ip, int port) {
            tcpClient = new TcpClient { SendTimeout = sendTimeoutMs };

            Task connectTask = tcpClient.ConnectAsync(ip.ToStringAndDispose(), port);
            var timeoutMessage = $"{GetType().Name} connection attempt timed out after {connectionTimeoutMs}ms";
            try {
                await connectTask.Timeout(connectionTimeoutMs, new(), timeoutMessage);
                stream = tcpClient.GetStream();
            } catch (Exception e) {
                throw new Exception($"{GetType().Name} connection attempt failed with \"{e.Message}\"", e);
            }

            stopListening = false;
            DoListenerForever();
        }

        protected void DisconnectTS() {
            DoTS(DisconnectHelper);
        }
        private void DisconnectHelper() {
            if (tcpClient?.Connected ?? false) {
                stopListening = true;
                stream.Close();
                stream = null;
                tcpClient.Close();
                tcpClient = null;
            }
        }

        protected virtual void DoListenerForever() {
            DoTS(ListenerHelperJson);
        }
        private async Task ListenerHelperJson() {
            var buffer = new byte[8192];
            string messageBuffer = "";
            while (!stopListening && !cts.Token.IsCancellationRequested) {
                try {
                    var bytesRead = await stream.ReadAsync(buffer, cts.Token);
                    messageBuffer += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                } catch (SocketException) {
                    var name = this.GetType().Name;
                    throw new IOException($"The network interface {name} disconnected prematurely");
                }

                // Extract a full message from the byte buffer, and leave remaining characters in string buffer.
                // Also, if there is more than one message in the buffer, report both.
                var newLineIndex = messageBuffer.IndexOf("\n") + 1;
                while (newLineIndex != 0) {
                    string message = messageBuffer.Substring(0, newLineIndex);

                    JObject json;
                    try {
                        // Check if value is a valid json
                        json = JObject.Parse(message);
                        // Remove it from the message buffer if it is valid
                        messageBuffer = messageBuffer.Substring(newLineIndex);
                        // Set the index of the next newline
                        newLineIndex = messageBuffer.IndexOf("\n") + 1;
                    } catch {
                        EventReporter.Instance.LogTS("invalid network json", new() {
                            { "interface", this.GetType().Name },
                            { "ip", ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString() },
                            { "message", message },
                        });
                        continue;
                    }
                    
                    // Report the message and send it to the waiting tasks
                    string msgType = json.GetValue("type").Value<string>();
                    bool idPresent = json.TryGetValue("id", out JToken idJToken);
                    int msgId = idPresent ? idJToken.Value<int>() : -1;
                    var dataPoint = new NativeDataPoint(msgType, msgId, Clock.UtcNow, json);
                    ReportNetworkMessage(dataPoint, false);
                    dataPoint.Dispose();
                    for (int i = receiveRequests.Count - 1; i >= 0; i--) {
                        var (type, tcs) = receiveRequests[i];
                        if (type == msgType) {
                            receiveRequests.RemoveAt(i);
                            tcs.SetResult(json);
                        }
                    }

                    // Handle network error messages
                    if (msgType.Contains("ERROR")) {
                        throw new Exception($"Error received from {this.GetType().Name} is {msgType}: {json.GetValue("data").Value<string>("error")}");
                    }

                    // Handle network exit messge
                    if (msgType == "EXIT") {
                        DisconnectTS();
                    }
                }
            }
        }

        protected async Task<JObject> ReceiveJsonTS(string type) {
            return await DoGetRelaxedTS(ReceiveJsonHelper, type.ToNativeText());
        }
        private async Task<JObject> ReceiveJsonHelper(NativeText type) {
            if (tcpClient == null || stream == null) { 
                throw new Exception($"Tried to receive {this.GetType().Name} network message \"{type}\" before connecting.");
            }
            TaskCompletionSource<JObject> tcs = new();
            receiveRequests.Add((type.ToString(), tcs));
            var timeoutMessage = $"{this.GetType().Name} didn't receive message after waiting {receiveTimeoutMs}ms";
            type.Dispose();
            return await tcs.Task.Timeout(receiveTimeoutMs, new(), timeoutMessage);
        }

        protected async Task SendJsonTS(string type, Dictionary<string, object> data) {
            var dataPoint = new NativeDataPoint(type, -1, Clock.UtcNow, data);
            await DoWaitForTS(SendJsonHelper, dataPoint);
        }
        private async Task SendJsonHelper(NativeDataPoint dataPoint) {
            if (tcpClient == null || stream == null) {
                throw new UnConnectedException($"Tried to send {this.GetType().Name} network message \"{dataPoint.type}\" before connecting.");
            }
            dataPoint.id = packetId++;
            string message = dataPoint.ToJSON();

            Byte[] buffer = Encoding.UTF8.GetBytes(message + "\n");
            var timeoutMessage = $"{this.GetType().Name} didn't receive message after waiting {1000}ms";
            var cts = new CancellationTokenSource();
            Task sendTask;
            try {
                sendTask = stream.WriteAsync(buffer, 0, buffer.Length, cts.Token);
            } catch (SocketException exception) {
                var name = this.GetType().Name;
                throw new IOException($"The network interface {name} closed before the {dataPoint.type} message could be sent", exception);
            }

            ReportNetworkMessage(dataPoint, true);
            await sendTask.Timeout(1000, cts, timeoutMessage);
            dataPoint.Dispose();
        }

        protected async Task<JObject> SendAndReceiveJsonTS(string sendType, string receiveType) {
            return await SendAndReceiveJsonTS(sendType, null, receiveType);
        }
        protected async Task<JObject> SendAndReceiveJsonTS(string sendType, Dictionary<string, object> sendData, string receiveType) {
            var recvTask = ReceiveJsonTS(receiveType);
            await SendJsonTS(sendType, sendData);
            return await recvTask;
        }

        protected void ReportNetworkMessage(NativeDataPoint dataPoint, bool sent) {
            Dictionary<string, object> dict = new() {
                { "interface", this.GetType().Name },
                { "message", dataPoint.ToJSON() },
                { "ip", ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString() },
                { "sent", sent },
            };

            var sendStr = sent ? "Sending" : "Received";
#if NETWORKINTERFACE_DEBUG_MESSAGES
            UnityEngine.Debug.Log($"{this.GetType().Name} {sendStr} Network Message: {type}\n{string.Join(Environment.NewLine, message)}");
#endif // NETWORKINTERFACE_DEBUG_MESSAGES

            if (Config.logNetworkMessages) {
                EventReporter.Instance.LogLocalTS("network", Clock.UtcNow, dict);
            }
        }
    }

}