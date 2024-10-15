//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Threading;
using UnityEngine.Networking;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;

using UnityEPL.Threading;

namespace UnityEPL.Utilities {

#if !UNITY_WEBGL // Microphone
    public class SoundRecorder : EventMonoBehaviour {
        protected override void AwakeOverride() { }

        private AudioClip recording;
        private int startSample;
        private float startTime;
        private bool isRecording = false;
        private string nextOutputPath;

        private const int SECONDS_IN_MEMORY = 1200;
        private const int SAMPLE_RATE = 44100;

        protected void OnEnable() {
            //TODO: enable cycling through devices
            try {
                recording = Microphone.Start("", true, SECONDS_IN_MEMORY, SAMPLE_RATE);
            } catch (Exception e) { // TODO
                ErrorNotifier.ErrorTS(e);
            }
        }

        protected void OnDisable() {
            // device = null;
            if (isRecording)
                StopRecording();
            Microphone.End("");
        }

        protected void OnApplicationQuit() {
            if (isRecording)
                StopRecording();
        }

        // TODO: JPB: (needed) (refactor) SoundRecorder should add a function that does start and stop
        //            Record(int duration, string outputFilePath)

        //using the system's default device

        public void StartRecording(string outputFilePath) {
            Do(StartRecordingHelper, outputFilePath.ToNativeText());
        }
        public void StartRecordingTS(string outputFilePath) {
            DoTS(StartRecordingHelper, outputFilePath.ToNativeText());
        }
        protected void StartRecordingHelper(NativeText outputFilePath) {
            if (isRecording) {
                throw new UnityException("Already recording.  Please StopRecording first.");
            }

            eventReporter.LogTS("recording start");
            nextOutputPath = outputFilePath.ToString();
            startSample = Microphone.GetPosition("");
            startTime = Time.unscaledTime;
            isRecording = true;
            outputFilePath.Dispose();
        }
      
        public AudioClip StopRecording() {
            return DoGet(StopRecordingHelper);
        }
        public async Task<AudioClip> StopRecordingTS() {
            return await DoGetRelaxedTS(StopRecordingHelper);
        }
        protected AudioClip StopRecordingHelper() {
            if (!isRecording) {
                throw new UnityException("Not recording.  Please StartRecording first.");
            }
            isRecording = false;
            eventReporter.LogTS("recording stop");

            float recordingLength = Time.unscaledTime - startTime;

            int outputLength = Mathf.RoundToInt(SAMPLE_RATE * recordingLength);
            AudioClip croppedClip = AudioClip.Create("cropped recording", outputLength, 1, SAMPLE_RATE, false);

            float[] saveData = GetLastSamples(outputLength);

            croppedClip.SetData(saveData, 0);
            SaveWave.Save(nextOutputPath, croppedClip);
            return croppedClip;
        }

        public float[] GetLastSamples(int howManySamples) {
            return DoGet(GetLastSamplesHelper, howManySamples);
        }
        public async Task<float[]> GetLastSamplesTS(int howManySamples) {
            return await DoGetRelaxedTS(GetLastSamplesHelper, howManySamples);
        }
        public float[] GetLastSamplesHelper(int howManySamples) {
            float[] lastSamples = new float[howManySamples];
            if (startSample < recording.samples - howManySamples) {
                recording.GetData(lastSamples, startSample);
            } else {
                float[] tailData = new float[recording.samples - startSample];
                recording.GetData(tailData, startSample);
                float[] headData = new float[howManySamples - tailData.Length];
                recording.GetData(headData, 0);
                for (int i = 0; i < tailData.Length; i++)
                    lastSamples[i] = tailData[i];
                for (int i = 0; i < headData.Length; i++)
                    lastSamples[tailData.Length + i] = headData[i];
            }
            return lastSamples;
        }

        // TODO: JPB: (bug) Fix AudioClipFromDatapathHelper and make it public
        private AudioClip AudioClipFromDatapath(string datapath) {
            return DoGet(AudioClipFromDatapathHelper, datapath.ToNativeText());
        }
        private async Task<AudioClip> AudioClipFromDatapathTS(string datapath) {
            return await DoGetRelaxedTS(AudioClipFromDatapathHelper, datapath.ToNativeText());
        }
        protected AudioClip AudioClipFromDatapathHelper(NativeText datapath) {
            string url = "file:///" + datapath.ToString();
            UnityWebRequest audioFile = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
            //audioFile.timeout = 10; // timeout in ten seconds
            audioFile.SendWebRequest();
            while (!audioFile.isDone) {

                // FIXME

                Debug.Log("blocking");
                // block
            }

            switch (audioFile.result) {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ConnectionError:
                    throw new Exception($"{audioFile.responseCode}\n{audioFile.error}");
            }

            datapath.Dispose();
            return DownloadHandlerAudioClip.GetContent(audioFile);
        }
    }
#else
    public class SoundRecorder : EventMonoBehaviour {
        protected override void AwakeOverride() { }

        public void StartRecording(string outputFilePath) { throw new NotImplementedException(); }
        public void StartRecordingTS(string outputFilePath) { throw new NotImplementedException(); }

        public async Task<AudioClip> StopRecording() { throw new NotImplementedException(); }
        public async Task<AudioClip> StopRecordingTS() { throw new NotImplementedException(); }

        public async Task<float[]> GetLastSamples(int howManySamples) { throw new NotImplementedException(); }

        protected AudioClip AudioClipFromDatapathHelper(string datapath) { throw new NotImplementedException(); }
    }
#endif // !UNITY_WEBGL

}