//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;

using UnityEPL.DataManagement;

namespace UnityEPL.ExternalDevices { 

    // This is just so you can use "?." syntax
    public class RamulatorWrapper {
        private RamulatorInterface ramulatorInterface;

        public RamulatorWrapper(MainManager manager) {
            ramulatorInterface = manager.gameObject.AddComponent<RamulatorInterface>();
        }

        public IEnumerator BeginNewSession() {
            yield return ramulatorInterface.BeginNewSession();
        }

        public void BeginNewTrial(int trialNumber) {
            ramulatorInterface.BeginNewTrial(trialNumber);
        }

        public void SendStateMsg(HostPcStatusMsg state, bool stateToggle, Dictionary<string, object> data = null) {
            var dict = (data != null) ? new Dictionary<string, object>(data) : new();
            foreach (var item in state.dict["data"] as Dictionary<string, object>) {
                dict.Add(item.Key, item.Value);
            }
            ramulatorInterface.SetState((string)state.dict["status"], stateToggle, dict);
        }

        public void SendMathMsg(string problem, string response, int responseTimeMs, bool correct) {
            ramulatorInterface.SendMathMessage(problem, response, responseTimeMs, correct);
        }

        public void SendExitMsg() {
            ramulatorInterface.SendExitMessage();
        }

        // Don't use this unless you have to
        public void SendMsg(string type, Dictionary<string, object> data = null) {
            ramulatorInterface.SendMessageToRamulator(new(type, -1, data));
        }
    }

    class RamulatorInterface : EventMonoBehaviour {
        protected override void AwakeOverride() { }

        //how long to wait for ramulator to connect
        const int timeoutDelay = 150;
        const int unreceivedHeartbeatsToQuit = 8;

        private int unreceivedHeartbeats = 0;
        private int packetId = 0;

        private NetMQ.Sockets.PairSocket zmqSocket;
        private const string address = "tcp://*:8889";

        void OnApplicationQuit() {
            Disconnect();
        }

        public void Disconnect() {
            StopAllCoroutines();

            if (zmqSocket != null) {
                zmqSocket.Close();
                NetMQConfig.Cleanup();
                zmqSocket = null;
            }
        }

        public IEnumerator BeginNewSession() {
            //Connect to ramulator///////////////////////////////////////////////////////////////////
            zmqSocket = new NetMQ.Sockets.PairSocket();
            zmqSocket.Bind(address);
            //Debug.Log ("socket bound");

            yield return WaitForMessage("CONNECTED", "Ramulated not connected.");

            //SendSessionEvent//////////////////////////////////////////////////////////////////////
            Dictionary<string, object> sessionData = new() {
                { "name", Config.experimentName },
                { "version", Application.version },
                { "subject", Config.subject },
                { "session_number", Config.sessionNum.ToString() },
            };
            SendMessageToRamulator(new("SESSION", -1, sessionData));
            yield return null;

            //Begin Heartbeats///////////////////////////////////////////////////////////////////////
            InvokeRepeating("SendHeartbeat", 0, 1);

            //SendReadyEvent////////////////////////////////////////////////////////////////////
            SendMessageToRamulator(new("READY", -1));
            yield return null;

            yield return WaitForMessage("START", "Start signal not received");

            InvokeRepeating("ReceiveHeartbeat", 0, 1);
        }

        private IEnumerator WaitForMessage(string containingString, string errorMessage) {
            string receivedMessage = "";
            float startTime = Time.time;
            while (receivedMessage == null || !receivedMessage.Contains(containingString)) {
                zmqSocket.TryReceiveFrameString(out receivedMessage);
                if (receivedMessage != "" && receivedMessage != null) {
                    string messageString = receivedMessage.ToString();
                    Debug.Log("received: " + messageString);
                    ReportMessage(messageString, false);
                }

                //if we have exceeded the timeout time, show warning and stop trying to connect
                if (Time.time > startTime + timeoutDelay) {
                    yield break;
                }
                yield return null;
            }
        }

        //ramulator expects this before the beginning of a new list
        public void BeginNewTrial(int trialNumber) {
            if (zmqSocket == null)
                throw new Exception("Please begin a session before beginning trials");
            Dictionary<string, object> sessionData = new() {
                { "trial", trialNumber.ToString() },
            };
            SendMessageToRamulator(new("TRIAL", -1, sessionData));
        }

        //ramulator expects this when you display words to the subject.
        //for words, stateName is "WORD"
        public void SetState(string stateName, bool stateToggle, Dictionary<string, object> sessionData) {
            sessionData.Add("name", stateName);
            sessionData.Add("value", stateToggle.ToString());
            SendMessageToRamulator(new("STATE", -1, sessionData));
        }

        public void SendMathMessage(string problem, string response, int responseTimeMs, bool correct) {
            Dictionary<string, object> mathData = new() {
                { "problem", problem },
                { "response", response },
                { "response_time_ms", responseTimeMs.ToString() },
                { "correct", correct.ToString() },
            };
            SendMessageToRamulator(new("MATH", -1, mathData));
        }


        private void SendHeartbeat() {
            SendMessageToRamulator(new("HEARTBEAT", -1));
        }

        private void ReceiveHeartbeat() {
            unreceivedHeartbeats = unreceivedHeartbeats + 1;
            Debug.Log("Unreceived heartbeats: " + unreceivedHeartbeats.ToString());

            if (unreceivedHeartbeats > unreceivedHeartbeatsToQuit) {
                CancelInvoke("ReceiveHeartbeat");
                CancelInvoke("SendHeartbeat");
                throw new Exception("Too many missed heartbeats.");
            }

            string receivedMessage = "";
            float startTime = Time.time;
            zmqSocket.TryReceiveFrameString(out receivedMessage);
            if (receivedMessage != "" && receivedMessage != null) {
                string messageString = receivedMessage.ToString();
                Debug.Log("heartbeat received: " + messageString);
                ReportMessage(messageString, false);
                unreceivedHeartbeats = 0;
            }
        }

        public void SendMessageToRamulator(NativeDataPoint dataPoint) {
            dataPoint.id = packetId++;
            var message = dataPoint.ToJSON();
            bool wouldNotHaveBlocked = zmqSocket.TrySendFrame(message, more: false);
            Debug.Log("Tried to send a message: " + message + " \nWouldNotHaveBlocked: " + wouldNotHaveBlocked.ToString());
            ReportMessage(message, true);
            dataPoint.Dispose();
        }

        public void SendExitMessage() {
            var msg = new NativeDataPoint("EXIT", -1);
            SendMessageToRamulator(msg);
        }

        private void ReportMessage(string message, bool sent) {
            Dictionary<string, object> messageDataDict = new() {
                { "message", message },
                { "sent", sent.ToString() },
            };
            EventReporter.Instance.LogTS("network", messageDataDict);
        }
    }
}