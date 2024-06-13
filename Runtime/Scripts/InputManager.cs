//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace UnityEPL {

    public class InputManager : SingletonEventMonoBehaviour<InputManager> {
        private struct KeyRequest {
            public TaskCompletionSource<KeyCode> tcs;
            public List<KeyCode> keyCodes;
            public Timer timer;
            public bool unpausable;

            public KeyRequest(TaskCompletionSource<KeyCode> tcs, List<KeyCode> keyCodes, TimeSpan timeout, bool unpausable) {
                this.tcs = tcs;
                this.keyCodes = keyCodes;
                this.timer = new Timer(timeout);
                this.unpausable = unpausable;
            }
        }

        LinkedList<KeyRequest> tempKeyRequests = new();
        protected override void AwakeOverride() { }

        void Update() {
            // TODO: JPB: (refactor) Use new unity input system for key input
            //            Keyboard.current.anyKey.wasPressedThisFrame

            // Remove timed out requests
            var node = tempKeyRequests.First;
            while (node != null) {
                var keyReq = node.Value;
                if (keyReq.timer.IsFinished()) {
                    keyReq.tcs.SetCanceled();
                    tempKeyRequests.Remove(node);
                }
                node = node.Next;
            }
            // Check for button presses
            // TODO: JPB: (refactor) The keysystem can be improved by keeping track of which keys were requested and only looping through those
            foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode))) {
                if (Input.GetKeyDown(vKey)) {
                    node = tempKeyRequests.First;
                    while (node != null) {
                        var keyReq = node.Value;
                        if ((keyReq.unpausable || Time.timeScale != 0) &&
                            (keyReq.keyCodes.Count == 0 || keyReq.keyCodes.Exists(x => x == vKey))) {
                            keyReq.tcs.SetResult(vKey);
                            tempKeyRequests.Remove(node);
                        }
                        node = node.Next;
                    }
                }
            }
        }

        public bool GetKeyDown(KeyCode key, bool unpausable = false) {
            return DoGet<KeyCode, Bool, Bool>(GetKeyDownHelper, key, unpausable);
        }
        protected Bool GetKeyDownHelper(KeyCode key, Bool unpausable) {
            if (!unpausable && Time.timeScale == 0) { return false; }
            return Input.GetKeyDown(key);
        }
        public KeyCode GetKeyDown(KeyCode[] keys, bool unpausable = false) {
            return DoGet<KeyCode[], Bool, KeyCode>(GetKeyDownHelper, keys, unpausable);
        }
        public KeyCode GetKeyDownHelper(KeyCode[] keys, Bool unpausable) {
            if (!unpausable && Time.timeScale == 0) { return KeyCode.None; }

            foreach (KeyCode key in keys) {
                if (Input.GetKeyDown(key)) {
                    return key;
                }
            }
            return KeyCode.None;
        }

        public bool GetKey(KeyCode key, bool unpausable = false) {
            return DoGet<KeyCode, Bool, Bool>(GetKeyHelper, key, unpausable);
        }
        protected Bool GetKeyHelper(KeyCode key, Bool unpausable) {
            if (!unpausable && Time.timeScale == 0) { return false; }
            return Input.GetKey(key);
        }
        public KeyCode GetKey(List<KeyCode> keys, bool unpausable = false) {
            return DoGet<KeyCode[], Bool, KeyCode>(GetKeyHelper, keys.ToArray(), unpausable);
        }
        public KeyCode GetKey(KeyCode[] keys, bool unpausable = false) {
            return DoGet<KeyCode[], Bool, KeyCode>(GetKeyHelper, keys, unpausable);
        }
        public KeyCode GetKeyHelper(KeyCode[] keys, Bool unpausable) {
            if (!unpausable && Time.timeScale == 0) { return KeyCode.None; }

            foreach (KeyCode key in keys) {
                if (Input.GetKey(key)) {
                    return key;
                }
            }
            return KeyCode.None;
        }

        public async Task<KeyCode> WaitForKey(bool unpausable = false, CancellationToken ct = default) {
            return await DoGet<Bool, CancellationToken, KeyCode>(WaitForKeyHelper, unpausable, ct);
        }
        protected async Task<KeyCode> WaitForKeyHelper(Bool unpausable, CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                if (!unpausable && Time.timeScale == 0) { continue; }
                foreach (KeyCode vKey in Enum.GetValues(typeof(KeyCode))) {
                    if (Input.GetKeyDown(vKey)) {
                        return vKey;
                    };
                }
                await Awaitable.NextFrameAsync();
            }
            return KeyCode.None;
        }
        public async Task WaitForKey(KeyCode key, bool unpausable = false, CancellationToken ct = default) {
            await DoWaitFor<KeyCode, Bool, CancellationToken>(WaitForKeyHelper, key, unpausable, ct);
        }
        protected async Task WaitForKeyHelper(KeyCode key, Bool unpausable, CancellationToken ct) {
            // This first await is needed when WaitForKey is used in a tight loop.
            // If it is, then it will repeatedly be checked over and over on the same frame, causing the program to hang
            // It does add a one frame delay, but if you are using an await in the first place, you are probably not concerned about that
            await Awaitable.NextFrameAsync();
            while (!ct.IsCancellationRequested && !GetKeyDownHelper(key, unpausable)) {
                await Awaitable.NextFrameAsync();
            }
        }
        public async Task<KeyCode> WaitForKey(List<KeyCode> keys, bool unpausable = false, CancellationToken ct = default) {
            return await WaitForKey(keys.ToArray(), unpausable, ct);
        }
        public async Task<KeyCode> WaitForKey(KeyCode[] keys, bool unpausable = false, CancellationToken ct = default) {
            return await DoGet<KeyCode[], Bool, CancellationToken, KeyCode>(WaitForKeyHelper, keys, unpausable, ct);
        }
        protected async Task<KeyCode> WaitForKeyHelper(KeyCode[] keys, Bool unpausable, CancellationToken ct) {
            // This first await is needed when WaitForKey is used in a tight loop.
            // If it is, then it will repeatedly be checked over and over on the same frame, causing the program to hang
            // It does add a one frame delay, but if you are using an await in the first place, you are probably not concerned about that
            await Awaitable.NextFrameAsync();
            var retKey = GetKeyDownHelper(keys, unpausable);
            while (!ct.IsCancellationRequested && retKey == KeyCode.None) {
                await Awaitable.NextFrameAsync();
                retKey = GetKeyDownHelper(keys, unpausable);
            }
            return retKey;
        }

        
        /// <summary>
        /// Waits for any key to be pressed and returns the KeyCode of the pressed key.
        /// A thread safe version of the WaitForKey function. 
        /// </summary>
        /// <param name="unpausable">Indicates whether the keypress is effected by game pauses.</param>
        /// <returns>A task that returns the KeyCode of the pressed key.</returns>
        [Obsolete("This is a bad paradigm. Make something in your experiment than handles it instead.")]
        public async Task<KeyCode> WaitForKeyTS(bool unpausable = false) {
            TimeSpan dur = DateTime.MaxValue - Clock.UtcNow - TimeSpan.FromDays(1);
            return (await WaitForKeyTS(dur, unpausable)).Value;
        }
        /// <summary>
        /// Waits for any key to be pressed within a specified duration.
        /// </summary>
        /// <param name="duration">The maximum duration to wait for the key press.</param>
        /// <param name="unpausable">Indicates whether the keypress is effected by game pauses.</param>
        /// <returns>The KeyCode of the key that was pressed, or null if the duration was exceeded.</returns>
        [Obsolete("This is a bad paradigm. Make something in your experiment than handles it instead.")]
        public async Task<KeyCode?> WaitForKeyTS(TimeSpan duration, bool unpausable = false) {
            try {
                return await DoGetManualTriggerTS<NativeArray<KeyCode>, TimeSpan, Bool, KeyCode>(WaitForKeyHelper, new(), duration, unpausable);
            } catch (TaskCanceledException) {
                return null;
            }
        }
        /// <summary>
        /// Waits for one of the provided keys to be pressed and returns the KeyCode of the pressed key.
        /// A thread safe version of the WaitForKey function.
        /// </summary>
        /// <param name="keyCodes">The list of keys to wait for.</param>
        /// <param name="unpausable">Indicates whether the keypress is effected by game pauses.</param>
        /// <returns>A task that returns the KeyCode of the pressed key.</returns>
        [Obsolete("This is a bad paradigm. Make something in your experiment than handles it instead.")]
        public async Task<KeyCode> WaitForKeyTS(List<KeyCode> keyCodes, bool unpausable = false) {
            TimeSpan dur = DateTime.MaxValue - Clock.UtcNow - TimeSpan.FromDays(1);
            return (await WaitForKeyTS(keyCodes, dur, unpausable)).Value;
        }
        /// <summary>
        /// Waits for one of the provided keys to be pressed within a specified duration.
        /// </summary>
        /// <param name="keyCodes">The list of keys to wait for.</param>
        /// <param name="duration">The maximum duration to wait for the key press.</param>
        /// <param name="unpausable">Indicates whether the keypress is effected by game pauses.</param>
        /// <returns>The KeyCode of the key that was pressed, or null if the duration was exceeded.</returns>
        [Obsolete("This is a bad paradigm. Make something in your experiment than handles it instead.")]
        public async Task<KeyCode?> WaitForKeyTS(List<KeyCode> keyCodes, TimeSpan duration, bool unpausable = false) {
            var nativeKeyCodes = keyCodes.ToNativeArray(AllocatorManager.Persistent);
            try {
                return await DoGetManualTriggerTS<NativeArray<KeyCode>, TimeSpan, Bool, KeyCode>(WaitForKeyHelper, nativeKeyCodes, duration, unpausable);
            } catch (TaskCanceledException) {
                return null;
            } 
        }
        protected IEnumerator WaitForKeyHelper(TaskCompletionSource<KeyCode> tcs, NativeArray<KeyCode> keyCodes, TimeSpan duration, Bool unpausable) {
            var keyCodesList = keyCodes.ToList();
            foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode))) {
                if (Input.GetKeyDown(vKey) && (keyCodesList.Count == 0 || keyCodesList.Exists(x => x == vKey))) {
                    tcs.SetResult(vKey);
                    yield break;
                }
            }
            tempKeyRequests.AddLast(new KeyRequest(tcs, keyCodesList, duration, unpausable));
        }
    }


}