//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace UnityEPL {

    public class VideoControl : EventMonoBehaviour {
        protected override void AwakeOverride() { }

        public RectTransform videoTransform;
        public VideoPlayer videoPlayer;

        protected bool skippable;
        protected string videoPath;
        protected KeyCode pauseToggleKey = KeyCode.P;
        protected KeyCode deactivateKey = KeyCode.Space;
        protected TaskCompletionSource<bool> videoFinished;

        protected void Update() {
            if (Input.GetKeyDown(pauseToggleKey)) {
                if (videoPlayer.isPlaying) {
                    videoPlayer.Pause();
                } else {
                    videoPlayer.Play();
                }
            }
            if (skippable && Input.GetKeyDown(deactivateKey)) {
                videoPlayer.Stop();
                OnLoopPointReached(videoPlayer);
            }
        }

        protected void OnEnable() {
            videoPlayer.loopPointReached += OnLoopPointReached;
            videoPlayer.errorReceived += OnErrorReceived;
        }

        protected void OnDisable() {
            // clear player
            RenderTexture.active = videoPlayer.targetTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.errorReceived -= OnErrorReceived;
        }

        // TODO: JPB: (needed) Fix FilePicker to not be a super hack
        public async Task<string> SelectVideoFile(string startingPath, SFB.ExtensionFilter[] extensions, bool skippable = false) {
            await DoWaitFor(SelectVideoFileHelper, startingPath, extensions, skippable);
            return videoPath;
        }
        public async Task<string> SelectVideoFileTS(string startingPath, SFB.ExtensionFilter[] extensions, bool skippable = false) {
            await DoWaitForTS(() => { return SelectVideoFileHelper(startingPath, extensions, skippable); });
            return videoPath;
        }
        protected Task SelectVideoFileHelper(string startingPath, SFB.ExtensionFilter[] extensions, bool skippable) {
            string[] videoPaths = new string[0];
            // Wait until a single video is selected
            while (videoPaths.Length != 1) {
                var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Select Video To Watch", startingPath, extensions, false);
                // Handle cancel case
                if (paths.Length == 1 && paths[0] != "") {
                    videoPaths = paths;
                }
            }
            var videoPath = videoPaths[0].Replace("%20", " ");
            SetVideo(videoPath, skippable);
            return Task.CompletedTask;
        }

        public void SetVideo(string videoPath, bool skippable = false, bool absolutePath=false) {
            Do(SetVideoHelper, videoPath.ToNativeText(), (Bool)skippable, (Bool)absolutePath);
        }
        public void SetVideoTS(string videoPath, bool skippable = false, bool absolutePath=false) {
            DoTS(SetVideoHelper, videoPath.ToNativeText(), (Bool)skippable, (Bool)absolutePath);
        }
        protected void SetVideoHelper(NativeText videoPath, Bool skippable, Bool absolutePath) {
            this.videoPath = videoPath.ToString();
            if (absolutePath) {
                this.videoPlayer.url = "file://" + this.videoPath;
            } else {
                this.videoPlayer.url = "file://" + Path.Combine(manager.fileManager.ExperimentRoot(), this.videoPath);
            } 
            this.skippable = skippable;
            videoPath.Dispose();
        }

        public Task PlayVideo() {
            return DoWaitFor(PlayVideoHelper);
        }
        public Task PlayVideoTS() {
            return DoWaitForTS(PlayVideoHelper);
        }
        protected async Task PlayVideoHelper() {
            videoFinished = new();

            gameObject.SetActive(true);
            GameObject textDisplay = GameObject.Find(gameObject.name).transform.Find("VideoPlayerCanvas").transform.Find("Text").gameObject;
            textDisplay.SetActive(skippable);

            videoPlayer.Play();
            await videoFinished.Task;
            gameObject.SetActive(false);
        }

        public bool IsPlaying() {
            return DoGet(IsPlayingHelper);
        }
        public async Task<bool> IsPlayingTS() {
            return await DoGetTS(IsPlayingHelper);
        }
        protected Bool IsPlayingHelper() {
            return gameObject.activeSelf;
        }

        // FOR TESTING PURPOSES ONLY
        public int videoLength {
            get { return (int)videoPlayer.length; }
        }
        // END FOR TESTING PURPOSES ONLY

        public double VideoLength() {
            return DoGet(VideoLengthHelper);
        }
        public async Task<double> VideoLengthTS() {
            return await DoGetTS(VideoLengthHelper);
        }
        protected double VideoLengthHelper() {
            return videoPlayer.length;
        }


        protected void OnLoopPointReached(VideoPlayer vp) {
            gameObject.SetActive(false);
            videoFinished.SetResult(true);
        }
        protected void OnErrorReceived(VideoPlayer vp, string message) {
            gameObject.SetActive(false);
            videoFinished.SetException(new Exception(message));
        }
    }

}