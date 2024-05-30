//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEPL {

    /// <summary>
    /// A class for time related functions.
    /// </summary>
    public static class Timing {
        // TODO: JPB: (feature) Make InterfaceManager.Delay() pause aware
        // https://devblogs.microsoft.com/pfxteam/cooperatively-pausing-async-methods/
#if !UNITY_WEBGL || UNITY_EDITOR // System.Threading
        public static async Task Delay(int millisecondsDelay) {
            if (millisecondsDelay < 0) {
                throw new ArgumentOutOfRangeException($"millisecondsDelay <= 0 ({millisecondsDelay})");
            } else if (millisecondsDelay == 0) {
                return;
            }

            await Task.Delay(millisecondsDelay);
        }

        public static async Task Delay(int millisecondsDelay, CancellationToken cancellationToken) {
            if (millisecondsDelay < 0) {
                throw new ArgumentOutOfRangeException($"millisecondsDelay <= 0 ({millisecondsDelay})");
            } else if (millisecondsDelay == 0) {
                return;
            }

            await Task.Delay(millisecondsDelay, cancellationToken);
        }

        public static IEnumerator DelayE(int millisecondsDelay) {
            //yield return new WaitForSeconds(millisecondsDelay / 1000.0f);
            yield return Delay(millisecondsDelay).ToEnumerator();
        }

        public static IEnumerator DelayE(int millisecondsDelay, CancellationToken cancellationToken) {
            yield return Delay(millisecondsDelay, cancellationToken).ToEnumerator();
        }
#else
        public static async Task Delay(int millisecondsDelay) {
            if (millisecondsDelay < 0) {
                throw new ArgumentOutOfRangeException($"millisecondsDelay <= 0 ({millisecondsDelay})"); }
            else if (millisecondsDelay == 0) {
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            float seconds = ((float)millisecondsDelay) / 1000;
            Instance.StartCoroutine(WaitForSeconds(seconds, tcs));
            await tcs.Task;
        }

        public static async Task Delay(int millisecondsDelay, CancellationToken cancellationToken) {
            if (millisecondsDelay < 0) {
                throw new ArgumentOutOfRangeException($"millisecondsDelay <= 0 ({millisecondsDelay})"); }
            else if (millisecondsDelay == 0) {
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            float seconds = ((float)millisecondsDelay) / 1000;
            Instance.StartCoroutine(WaitForSeconds(seconds, cancellationToken, tcs));
            await tcs.Task;
        }

        public static IEnumerator DelayE(int millisecondsDelay) {
            yield return InterfaceManager.Delay(millisecondsDelay).ToEnumerator();
        }

        public static IEnumerator DelayE(int millisecondsDelay, CancellationToken cancellationToken) {
            yield return InterfaceManager.Delay(millisecondsDelay, cancellationToken).ToEnumerator();
        }

        protected static IEnumerator WaitForSeconds(float seconds, TaskCompletionSource<bool> tcs) {
            yield return new WaitForSeconds(seconds);
            tcs?.SetResult(true);
        }

        protected static IEnumerator WaitForSeconds(float seconds, CancellationToken cancellationToken, TaskCompletionSource<bool> tcs) {
            var endTime = Time.fixedTime + seconds;
            Console.WriteLine(seconds);
            Console.WriteLine(Time.fixedTime);
            Console.WriteLine(endTime);
            while (Time.fixedTime < endTime) {
                if (cancellationToken.IsCancellationRequested) {
                    Console.WriteLine("CANCELLED");
                    tcs?.SetResult(false);
                    yield break;
                }
                yield return null;
            }
            tcs?.SetResult(true);
        }
#endif
    
    }
}
