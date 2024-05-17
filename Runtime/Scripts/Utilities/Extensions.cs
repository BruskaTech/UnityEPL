//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;

namespace UnityEPL {
    public static class IEnumerable {
        // https://stackoverflow.com/a/30758270
        public static int GetSequenceHashCode<T>(this IEnumerable<T> sequence) {
            const int seed = 487;
            const int modifier = 31;

            unchecked {
                return sequence.Aggregate(seed, (current, item) =>
                    (current*modifier) + item.GetHashCode());
            }            
        }
    }
    
    public static class IListExtensions {
        /// <summary>
        /// Knuth (Fisher-Yates) Shuffle
        /// Returns a shuffled copy of the IList.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static IList<T> Shuffle<T>(this IList<T> list, System.Random rnd) {
            var shuf = new List<T>(list);
            for (int i = shuf.Count - 1; i > 0; i--) {
                int j = rnd.Next(i + 1);
                T tmp = shuf[i];
                shuf[i] = shuf[j];
                shuf[j] = tmp;
            }

            return shuf;
        }
        public static IList<T> Shuffle<T>(this IList<T> list) {
            return list.Shuffle(InterfaceManager.Rnd);
        }

        /// <summary>
        /// Knuth (Fisher-Yates) Shuffle
        /// Returns a shuffled copy of the List.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static List<T> Shuffle<T>(this List<T> list, System.Random rnd) {
            var shuf = new List<T>(list);
            for (int i = shuf.Count - 1; i > 0; i--) {
                int j = rnd.Next(i + 1);
                T tmp = shuf[i];
                shuf[i] = shuf[j];
                shuf[j] = tmp;
            }

            return shuf;
        }
        public static List<T> Shuffle<T>(this List<T> list) {
            return list.Shuffle(InterfaceManager.Rnd);
        }

        /// <summary>
        /// Knuth (Fisher-Yates) Shuffle
        /// Shuffles the element order of the specified list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static IList<T> ShuffleInPlace<T>(this IList<T> list, System.Random rnd) {
            var count = list.Count;
            for (int i = 0; i < count; ++i) {
                int r = rnd.Next(i, count);
                T tmp = list[i];
                list[i] = list[r];
                list[r] = tmp;
            }
            return list;
        }
        public static IList<T> ShuffleInPlace<T>(this IList<T> list) {
            return list.ShuffleInPlace(InterfaceManager.Rnd);
        }

        /// <summary>
        /// Knuth (Fisher-Yates) Shuffle
        /// Shuffles the element order of the specified list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static List<T> ShuffleInPlace<T>(this List<T> list, System.Random rnd) {
            var count = list.Count;
            for (int i = 0; i < count; ++i) {
                int r = rnd.Next(i, count);
                T tmp = list[i];
                list[i] = list[r];
                list[r] = tmp;
            }
            return list;
        }
        public static List<T> ShuffleInPlace<T>(this List<T> list) {
            return list.ShuffleInPlace(InterfaceManager.Rnd);
        }

        // https://stackoverflow.com/a/30758270
        public static int GetSequenceHashCode<T>(this IList<T> sequence) {
            const int seed = 487;
            const int modifier = 31;

            unchecked {
                return sequence.Aggregate(seed, (current, item) =>
                    (current*modifier) + item.GetHashCode());
            }            
        }
    }

    public static class ArrayExtensions {
        /// <summary>
        /// Knuth (Fisher-Yates) Shuffle
        /// Shuffles the element order of the specified array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T[] ShuffleInPlace<T>(this T[] array, System.Random rnd) {
            int n = array.Length;
            while (n > 1) {
                _ = rnd.Next(2);
                int k = rnd.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return array;
        }
        public static T[] ShuffleInPlace<T>(this T[] array) {
            return array.ShuffleInPlace(InterfaceManager.Rnd);
        }

        // https://stackoverflow.com/a/30758270
        public static int GetSequenceHashCode<T>(this T[] sequence) {
            const int seed = 487;
            const int modifier = 31;

            unchecked {
                return sequence.Aggregate(seed, (current, item) =>
                    (current*modifier) + item.GetHashCode());
            }            
        }
    }

    public static class CollectionExtensions {
        /// <summary>
        /// Allows List constructor to take a items or a list of items that gets expanded
        /// https://stackoverflow.com/a/63374611
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToAdd"></param>
        public static void Add<T>(this ICollection<T> collection, IEnumerable<T> itemsToAdd) {
            foreach (var item in itemsToAdd) {
                collection.Add(item);
            }
        }
    }

    public static class EnumeratorExtensions {
        /// <summary>
        /// Convert an IEnumerator to an IEnumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator) {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }

    public static class TaskExtensions {
        /// <summary>
        /// Convert awaitable task to an IEnumerator
        /// https://forum.unity.com/threads/async-await-inside-a-coroutine.952110/
        /// </summary>
        /// <param name="task"></param>
        public static IEnumerator ToEnumerator(this Task task) {
            while(!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) { ErrorNotifier.ErrorTS(task.Exception.InnerException); }
            else if (task.IsCanceled) { ErrorNotifier.ErrorTS(new OperationCanceledException("ToEnumerator")); }
        }

        /// <summary>
        /// Convert awaitable task to an IEnumerator
        /// https://forum.unity.com/threads/async-await-inside-a-coroutine.952110/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        public static IEnumerator ToEnumerator<T>(this Task<T> task) {
            while (!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) { ErrorNotifier.ErrorTS(task.Exception.InnerException); }
            else if (task.IsCanceled) { ErrorNotifier.ErrorTS(new OperationCanceledException("ToEnumerator")); }
        }

        /// <summary>
        /// Create a timeout for a task that throws a TimeoutException if the task takes too long
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="timeoutMessage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public static async Task Timeout(this Task task, int timeoutMs, CancellationTokenSource cts, string timeoutMessage = null) {
            if (timeoutMs < 0) {
                throw new ArgumentException($"The timeoutMs cannot be negative, but it was {timeoutMs}");
            }
            Task timeoutTask = InterfaceManager.Delay(timeoutMs);
            var completedTask = await Task.WhenAny(task, timeoutTask);
            await completedTask; // Propagates exceptions thrown in the task
            if (completedTask == timeoutTask) {
                cts?.Cancel();
                var msg = timeoutMessage ?? $"Task Timed out after {timeoutMs}ms";
                throw new TimeoutException(msg);
            }
        }

        /// <summary>
        /// Create a timeout for a task that throws a TimeoutException if the task takes too long
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <param name="task"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="timeoutMessage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public static async Task<Z> Timeout<Z>(this Task<Z> task, int timeoutMs, CancellationTokenSource cts, string timeoutMessage = null) {
            if (timeoutMs < 0) {
                throw new ArgumentException($"The timeoutMs cannot be negative, but it was {timeoutMs}");
            }
            Task timeoutTask = InterfaceManager.Delay(timeoutMs);
            var completedTask = await Task.WhenAny(task, timeoutTask);
            await completedTask; // Propagates exceptions thrown in the task
            if (completedTask == timeoutTask) {
                cts?.Cancel();
                var msg = timeoutMessage ?? $"Task Timed out after {timeoutMs}ms";
                throw new TimeoutException(msg);
            }
            return await task;
        }

        /// <summary>
        /// Create a timeout for a task and logs if the task taskes too long
        /// Unlike the normal Timeout, this one does not throw an exception if the task times out
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task TimeoutQuiet(this Task task, int timeoutMs, CancellationTokenSource cts) {
            if (timeoutMs < 0) {
                throw new ArgumentException($"The timeoutMs cannot be negative, but it was {timeoutMs}");
            }
            Task timeoutTask = InterfaceManager.Delay(timeoutMs);
            var completedTask = await Task.WhenAny(task, timeoutTask);
            await completedTask; // Propagates exceptions thrown in the task
            if (completedTask == timeoutTask) {
                cts?.Cancel();
                EventReporter.Instance.LogTS("TimeoutQuiet", new() {
                    { "timeoutMs", timeoutMs }
                });
            }
        }
    }

    public static class MonoBehaviourExtensions {
        /// <summary>
        /// Quits the game
        /// </summary>
        /// <param name="monoBehaviour"></param>
        public static void Quit(this MonoBehaviour monoBehaviour) {
            Debug.Log("Quitting");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public static class AudioSourceExtentions {
        /// <summary>
        /// Plays a provided audio clip.
        /// </summary>
        /// <param name="audioSource">The audio source</param>
        /// <param name="audioClip">The audio clip to play</param>
        /// <returns>The audio clip length</returns>
        public static float Play(this AudioSource audioSource, AudioClip audioClip) {
            audioSource.clip = audioClip;
            audioSource.Play();
            return audioClip.length;
        }
    }

    public static class RandomExtensions {
        public static double NextDouble(this System.Random random, double max) {
            return random.NextDouble() * max;
        }
        public static double NextDouble(this System.Random random, double min, double max) {
            return random.NextDouble() * (max - min) + min;
        }
        public static float NextFloat(this System.Random random) {
            return (float) random.NextDouble();
        }
        public static float NextFloat(this System.Random random, float max) {
            return random.NextFloat() * max;
        }
        public static float NextFloat(this System.Random random, float min, float max) {
            return random.NextFloat() * (max - min) + min;
        }
    }
}