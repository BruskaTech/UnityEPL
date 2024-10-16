//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.Threading.Tasks;
using UnityEngine;
using UnityEPL.DataManagement;

namespace UnityEPL.ExternalDevices {
    public abstract class SyncBox : EventMonoBehaviour {
        private bool continuousPulsing = false;
        private int lastFrameCount = -1;

        public abstract Task Init();
        protected abstract Task PulseInternals();
        public abstract Task TearDown();

        protected override void AwakeOverride() { }
        protected void OnDestroy() {
            StopContinuousPulsing();
            TearDown();
        }

        public async Task Pulse() {
            EventReporter.Instance.LogTS("syncbox", new() {
                { "pulse", "on" }
            });
            await PulseInternals();
        }

        public void StartContinuousPulsing() {
            DoTS(StartContinuousPulsingHelper);
        }
        private async void StartContinuousPulsingHelper() {
            continuousPulsing = true;
            while (continuousPulsing) {
                if (lastFrameCount == Time.frameCount) {
                    throw new System.Exception($"SyncBox ({this.GetType().Name}) is pulsing too fast (or has no delays in it). You can only pulse once per frame.");
                }
                lastFrameCount = Time.frameCount;
                await Pulse();
            }
        }
        public void StopContinuousPulsing() {
            continuousPulsing = false;
        }
        public bool IsContinuousPulsing() {
            return continuousPulsing;
        }
    }
}