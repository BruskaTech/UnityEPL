//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

//#define NETWORKINTERFACE_DEBUG_MESSAGES

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace UnityEPL {
    public abstract class NetworkInterface : EventLoop {
        private TcpClient tcpClient;
        private NetworkStream stream;

        private bool stopListening = false;
        private readonly List<(string, TaskCompletionSource<JObject>)> receiveRequests = new();

        private readonly static int connectionTimeoutMs = 5000;
        private readonly static int sendTimeoutMs = 5000;
        private readonly static int receiveTimeoutMs = 5000;

        ~NetworkInterface() {
            DisconnectHelper();
        }

        public async Task<bool> IsConnectedTS() {
            return await DoGetTS<Bool>(IsConnectedHelper);
        }
        private Bool IsConnectedHelper() {
            return tcpClient?.Connected ?? false;
        }
        protected bool IsConnectedUnchecked() {
            return IsConnectedHelper();
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
                var newLineIndex = 0;
                while (newLineIndex != messageBuffer.Length) {
                    newLineIndex = messageBuffer.IndexOf("\n", newLineIndex) + 1;
                    string message = messageBuffer.Substring(0, newLineIndex);

                    JObject json;
                    try {
                        // Check if value is a valid json
                        json = JObject.Parse(message);
                        // Remove it from the message buffer if it is valid
                        messageBuffer = messageBuffer.Substring(newLineIndex);
                        newLineIndex = 0;
                    } catch {
                        continue;
                    }
                    
                    // Report the message and send it to the waiting tasks
                    var msgType = json.GetValue("type").Value<string>();
                    ReportNetworkMessageTS(msgType, message, Clock.UtcNow, false);
                    for (int i = receiveRequests.Count - 1; i >= 0; i--) {
                        var (type, tcs) = receiveRequests[i];
                        if (type == msgType) {
                            receiveRequests.RemoveAt(i);
                            tcs.SetResult(json);
                        }
                    }

                    // Handle network error messages
                    if (msgType.Contains("ERROR")) {
                        ErrorNotifier.ErrorTS(new Exception($"Error received from {this.GetType().Name} is {msgType}: {json.GetValue("data").Value<string>("error")}"));
                    }

                    // Handle network exit messge
                    if (msgType == "EXIT") {
                        DisconnectTS();
                    }
                }
            }
        }

        protected virtual Task<JObject> ReceiveTS(string type) {
            return ReceiveJsonTS(type);
        }
        protected Task<JObject> ReceiveJsonTS(string type) {
            return DoGetRelaxedTS(ReceiveJsonHelper, type.ToNativeText());
        }
        private Task<JObject> ReceiveJsonHelper(NativeText type) {
            if (tcpClient == null || stream == null) { 
                throw new Exception($"Tried to receive {this.GetType().Name} network message \"{type}\" before connecting.");
            }
            TaskCompletionSource<JObject> tcs = new();
            receiveRequests.Add((type.ToString(), tcs));
            var timeoutMessage = $"{this.GetType().Name} didn't receive message after waiting {receiveTimeoutMs}ms";
            type.Dispose();
            return tcs.Task.Timeout(receiveTimeoutMs, new(), timeoutMessage);
        }

        // TODO: JPB: (needed) Make NetworkInterface::Send use Mutex
        protected virtual async Task SendTS(string type, Dictionary<string, object> data = null) {
            await SendJsonTS(type, data);
        }
        protected async Task SendJsonTS(string type, Dictionary<string, object> data = null) {
            await DoWaitForTS(() => { SendJsonHelper(type, data); });
        }
        private Task SendJsonHelper(string type, Dictionary<string, object> data = null) {
            if (tcpClient == null || stream == null) { 
                ErrorNotifier.ErrorTS(new Exception($"Tried to send {this.GetType().Name} network message \"{type}\" before connecting."));
            }
            DataPoint point = new DataPoint(type, data);
            string message = point.ToJSON();

            Byte[] buffer = Encoding.UTF8.GetBytes(message + "\n");
            var timeoutMessage = $"{this.GetType().Name} didn't receive message after waiting {1000}ms";
            var cts = new CancellationTokenSource();
            Task sendTask;
            try {
                sendTask = stream.WriteAsync(buffer, 0, buffer.Length, cts.Token);
            } catch (SocketException) {
                var name = this.GetType().Name;
                throw new IOException($"The network interface {name} closed before the {type} message could be sent");
            }

            ReportNetworkMessageTS(type, message, point.time, true);
            return sendTask.Timeout(1000, cts, timeoutMessage);
        }

        protected Task<JObject> SendAndReceiveTS(string sendType, string receiveType) {
            return SendAndReceive(sendType, null, receiveType);
        }
        protected async Task<JObject> SendAndReceive(string sendType, Dictionary<string, object> sendData, string receiveType) {
            var recvTask = ReceiveTS(receiveType);
            await SendTS(sendType, sendData);
            return await recvTask;
        }

        protected void ReportNetworkMessageTS(string type, string message, DateTime time, bool sent) {
            Dictionary<string, object> dict = new() {
                { "interface", this.GetType().Name },
                { "message", message },
                { "ip", ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString() },
                { "sent", sent },
            };

            var sendStr = sent ? "Sending" : "Received";
#if NETWORKINTERFACE_DEBUG_MESSAGES
            UnityEngine.Debug.Log($"{this.GetType().Name} {sendStr} Network Message: {type}\n{string.Join(Environment.NewLine, message)}");
#endif // NETWORKINTERFACE_DEBUG_MESSAGES

            if (Config.logNetworkMessages) {
                manager.eventReporter.LogLocalTS("network", time, dict);
            }
        }
    }

}