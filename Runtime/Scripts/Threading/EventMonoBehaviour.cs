//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

#define EVENTMONOBEHAVIOR_TASK_OPERATORS
#define EVENTMONOBEHAVIOR_MANUAL_RESULT_SET

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using UnityEPL.Threading;
using UnityEPL.Utilities;
using UnityEPL.Extensions;

using static UnityEPL.Threading.Blittability;
using UnityEPL.DataManagement;

// TODO: JPB: (refactor) Clean up EventMonoBehaviour with UniTask
//            https://github.com/Cysharp/UniTask
//            https://cysharp.github.io/UniTask/api/Cysharp.Threading.Tasks.UniTaskExtensions.html#Cysharp_Threading_Tasks_UniTaskExtensions_AsUniTask_Task_System_Boolean_
//            https://cysharp.github.io/UniTask/api/Cysharp.Threading.Tasks.UniTaskExtensions.html#Cysharp_Threading_Tasks_UniTaskExtensions_AsTask_Cysharp_Threading_Tasks_UniTask_
//            https://cysharp.github.io/UniTask/api/Cysharp.Threading.Tasks.UniTaskExtensions.html

namespace UnityEPL {

    public abstract class EventMonoBehaviour : MonoBehaviour {
        protected MainManager manager;
        protected EventReporter eventReporter;
        protected int threadID;
        protected bool awakeCompleted = false;

        protected abstract void AwakeOverride();
        protected void Awake() {
            manager = MainManager.Instance;
            eventReporter = EventReporter.Instance;
            threadID = Thread.CurrentThread.ManagedThreadId;
            AwakeOverride();
            awakeCompleted = true;
        }

        // This function is used to check if an EventMonoBehvaiour has finished it's awake call
        public bool IsAwakeCompleted() {
            return DoGet(IsAwakeCompletedHelper);
        }
        public async Task<bool> IsAwakeCompletedTS() {
            return await DoGetTS(IsAwakeCompletedHelper);
        }
        protected Bool IsAwakeCompletedHelper() {
            return awakeCompleted;
        }

        /// <summary>
        /// Guarentees that a function is being called from the main unity thread
        /// </summary>
        protected void MonoBehaviourSafetyCheck() {
            //Debug.Log($"{threadID} {Thread.CurrentThread.ManagedThreadId}");
            if (threadID != Thread.CurrentThread.ManagedThreadId) {
                ErrorNotifier.ErrorTS(new InvalidOperationException(
                    "Cannot call this function from a non-unity thread.\n" +
                    "Try using the thread safe version of this method"));
            }
        }

        /// <summary>
        /// Retuns whether the calling function is on the main unity thread
        /// </summary>
        /// <returns>if calling function is on unity thread</returns>
        public bool OnUnityThread() {
            return threadID == Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Run an iterator function that might throw an exception.
        /// Handle the exception by using the ErrorNotifier
        /// Handle pausing as well
        /// https://www.jacksondunstan.com/articles/3718
        /// If for some reason this project needs a to define a custom Enumerator instead of using this function,
        /// it is also defined in this webpage (this may also be more efficient...)
        /// </summary>
        /// <param name="enumerator">Iterator function to run</param>
        /// <returns>An enumerator that runs the given enumerator</returns>
        /// /// TODO: JPB: (needed) Implement pausing in MakeEventEnumerator
        private IEnumerator MakeEventEnumerator(IEnumerator enumerator, StackTrace stackTrace = null, bool unpausable = false) {
            object current = null;
            while (true) {
                try {
                    if (unpausable || Time.timeScale != 0) {
                        if (enumerator.MoveNext() == false) {
                            break;
                        }
                        current = enumerator.Current;
                    }
                } catch (Exception e) {
                    var e2 = new Exception(e.Message, e);
                    if (stackTrace != null) { e2.SetStackTrace(stackTrace); }
                    UnityEngine.Debug.Log(e2);
                    ErrorNotifier.ErrorTS(e2);
                    yield break;
                }
                yield return current;
            }
        }
        /// <summary>
        /// This replaces the normal MonoBehaviour::StartCoroutine and adds other features,
        /// such as exception handling and pausing
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        protected Coroutine StartCoroutine(IEnumerator enumerator, bool unpausable = false) {
            StackTrace stackTrace = Config.debugEventLoopExtendedStackTrace ? new(true) : null;
            return base.StartCoroutine(MakeEventEnumerator(enumerator, stackTrace, unpausable));
        }

        // -------------------------------------
        // Unity Wait functions
        // -------------------------------------

        /// <summary>
        /// Converts an IEnumerator to an awaitable task that runs in a coroutine
        /// Only call this internally!
        /// TODO: JPB: (needed) Decide if this should be more efficient and call StartCoroutine right away
        ///            or if it should be put into queue
        /// </summary>
        /// <param name="enumerator">The enumerator to be turned to a task</param>
        /// <returns>The task to await</returns>
        protected async Task ToCoroutineTask(IEnumerator enumerator) {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(TaskTrigger(tcs, enumerator));
            await tcs.Task;
        }

        /// <summary>
        /// Acts just like Unity's WaitWhile class
        /// </summary>
        /// <param name="conditional">The condition to wait while it's true</param>
        /// <returns>The task to await</returns>
        protected async Task DoWaitWhile(Func<bool> conditional) {
            while (conditional()) {
                await Awaitable.NextFrameAsync();
            }
            // await ToCoroutineTask(new WaitWhile(conditional)); // This is bad because it uses another Task in TaskCompletionSource
        }

        /// <summary>
        /// Acts just like Unity's WaitUntil class
        /// </summary>
        /// <param name="conditional">The condition to wait until it's true</param>
        /// <returns>The task to await</returns>
        protected async Task DoWaitUntil(Func<bool> conditional) {
            while (!conditional()) {
                await Awaitable.NextFrameAsync();
            }
            // await ToCoroutineTask(new WaitUntil(conditional)); // This is bad because it uses another Task in TaskCompletionSource
        }


        // -------------------------------------
        // Do
        // Acts just like a function call, but guarentees thread safety
        // -------------------------------------
        // TODO: JPB: (feature) Add support for cancellation tokens in EventMonoBehavior Do functions
        protected void Do(Action func) {
            MonoBehaviourSafetyCheck();
            try {
                func();
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
            }
        }
        protected void Do<T>(Action<T> func, T t) {
            MonoBehaviourSafetyCheck();
            try {
                func(t);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
            }
        }
        protected void Do<T, U>(Action<T, U> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            try {
                func(t, u);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
            }
        }
        protected void Do<T, U, V>(Action<T, U, V> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            try {
                func(t, u, v);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
            }
        }
        protected void Do<T, U, V, W>(Action<T, U, V, W> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            try {
                func(t, u, v, w);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
            }
        }


        // -------------------------------------
        // DoTS
        // -------------------------------------
        // TODO: JPB: (feature) Add support for cancellation tokens in EventMonoBehavior Do functions
        private void DoHelper(IEnumerator enumerator) {
            StackTrace stackTrace = Config.debugEventLoopExtendedStackTrace ? new(true) : null;
            manager.events.Enqueue(MakeEventEnumerator(enumerator, stackTrace: stackTrace));
        }

        protected void DoTS(Func<IEnumerator> func) {
            DoHelper(func());
        }
        protected void DoTS<T>(Func<T, IEnumerator> func, T t)
                where T : struct {
            AssertBlittable<T>();
            DoHelper(func(t));
        }
        protected void DoTS<T, U>(Func<T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            DoHelper(func(t, u));
        }
        protected void DoTS<T, U, V>(Func<T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            DoHelper(func(t, u, v));
        }
        protected void DoTS<T, U, V, W>(Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            DoHelper(func(t, u, v, w));
        }

        protected void DoTS(Action func) {
            DoHelper(EnumeratorCaller(func));
        }
        protected void DoTS<T>(Action<T> func, T t)
                where T : struct {
            AssertBlittable<T>();
            DoHelper(EnumeratorCaller(func, t));
        }
        protected void DoTS<T, U>(Action<T, U> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            DoHelper(EnumeratorCaller(func, t, u));
        }
        protected void DoTS<T, U, V>(Action<T, U, V> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            DoHelper(EnumeratorCaller(func, t, u, v));
        }
        protected void DoTS<T, U, V, W>(Action<T, U, V, W> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            DoHelper(EnumeratorCaller(func, t, u, v, w));
        }


        // -------------------------------------
        // DoIn
        // -------------------------------------

        protected IEnumerator DoIn(int millisecondsDelay, Func<IEnumerator> func) {
            MonoBehaviourSafetyCheck();
            yield return DelayedEnumeratorCaller(millisecondsDelay, func());
        }
        protected IEnumerator DoIn<T>(int millisecondsDelay, Func<T, IEnumerator> func, T t) {
            MonoBehaviourSafetyCheck();
            yield return DelayedEnumeratorCaller(millisecondsDelay, func(t));
        }
        protected IEnumerator DoIn<T, U>(int millisecondsDelay, Func<T, U, IEnumerator> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            yield return DelayedEnumeratorCaller(millisecondsDelay, func(t, u));
        }
        protected IEnumerator DoIn<T, U, V>(int millisecondsDelay, Func<T, U, V, IEnumerator> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            yield return DelayedEnumeratorCaller(millisecondsDelay, func(t, u, v));
        }
        protected IEnumerator DoIn<T, U, V, W>(int millisecondsDelay, Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            yield return DelayedEnumeratorCaller(millisecondsDelay, func(t, u, v, w));
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task DoIn(int millisecondsDelay, Func<Task> func) {
            MonoBehaviourSafetyCheck();
            try {
                await manager.Delay(millisecondsDelay);
                await func();
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoIn<T>(int millisecondsDelay, Func<T, Task> func, T t) {
            MonoBehaviourSafetyCheck();
            try {
                await manager.Delay(millisecondsDelay);
                await func(t);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoIn<T, U>(int millisecondsDelay, Func<T, U, Task> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            try {
                await manager.Delay(millisecondsDelay);
                await func(t, u);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoIn<T, U, V>(int millisecondsDelay, Func<T, U, V, Task> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            try {
                await manager.Delay(millisecondsDelay);
                await func(t, u, v);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoIn<T, U, V, W>(int millisecondsDelay, Func<T, U, V, W, Task> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            try {
                await manager.Delay(millisecondsDelay);
                await func(t, u, v, w);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS


        // -------------------------------------
        // DoInTS
        // -------------------------------------

        protected void DoInTS(int millisecondsDelay, Func<IEnumerator> func) {
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func()));
        }
        protected void DoInTS<T>(int millisecondsDelay, Func<T, IEnumerator> func, T t)
                where T : struct {
            AssertBlittable<T>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func(t)));
        }
        protected void DoInTS<T, U>(int millisecondsDelay, Func<T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func(t, u)));
        }
        protected void DoInTS<T, U, V>(int millisecondsDelay, Func<T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func(t, u, v)));
        }
        protected void DoInTS<T, U, V, W>(int millisecondsDelay, Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func(t, u, v, w)));
        }

        protected void DoInTS(int millisecondsDelay, Action func) {
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func));
        }
        protected void DoInTS<T>(int millisecondsDelay, Action<T> func, T t)
                where T : struct {
            AssertBlittable<T>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func, t));
        }
        protected void DoInTS<T, U>(int millisecondsDelay, Action<T, U> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func, t, u));
        }
        protected void DoInTS<T, U, V>(int millisecondsDelay, Action<T, U, V> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func, t, u, v));
        }
        protected void DoInTS<T, U, V, W>(int millisecondsDelay, Action<T, U, V, W> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            DoHelper(DelayedEnumeratorCaller(millisecondsDelay, func, t, u, v, w));
        }


        // -------------------------------------
        // DoRepeating
        // -------------------------------------

        protected IEnumerator DoRepeating(int delayMs, int intervalMs, uint? iterations, Func<IEnumerator> func) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();

            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                yield return MakeEventEnumerator(func());
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func));
            }

            yield return cts;
        }
        protected IEnumerator DoRepeating<T>(int delayMs, int intervalMs, uint? iterations, Func<T, IEnumerator> func, T t) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();

            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                yield return MakeEventEnumerator(func(t));
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t));
            }

            yield return cts;
        }
        protected IEnumerator DoRepeating<T, U>(int delayMs, int intervalMs, uint? iterations, Func<T, U, IEnumerator> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();

            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                yield return MakeEventEnumerator(func(t, u));
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t, u));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u));
            }

            yield return cts;
        }
        protected IEnumerator DoRepeating<T, U, V>(int delayMs, int intervalMs, uint? iterations, Func<T, U, V, IEnumerator> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();

            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                yield return MakeEventEnumerator(func(t, u, v));
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t, u, v));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v));
            }

            yield return cts;
        }
        protected IEnumerator DoRepeating<T, U, V, W>(int delayMs, int intervalMs, uint? iterations, Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();

            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                yield return MakeEventEnumerator(func(t, u, v, w));
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t, u, v, w));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v, w));
            }

            yield return cts;
        }

        protected CancellationTokenSource DoRepeating(int delayMs, int intervalMs, uint? iterations, Action func) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                func();
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func));
            }

            return cts;
        }
        protected CancellationTokenSource DoRepeating<T>(int delayMs, int intervalMs, uint? iterations, Action<T> func, T t) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                func(t);
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t));
            }

            return cts;
        }
        protected CancellationTokenSource DoRepeating<T, U>(int delayMs, int intervalMs, uint? iterations, Action<T, U> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                func(t, u);
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t, u));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u));
            }

            return cts;
        }
        protected CancellationTokenSource DoRepeating<T, U, V>(int delayMs, int intervalMs, uint? iterations, Action<T, U, V> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                func(t, u, v);
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t, u, v));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v));
            }

            return cts;
        }
        protected CancellationTokenSource DoRepeating<T, U, V, W>(int delayMs, int intervalMs, uint? iterations, Action<T, U, V, W> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            if (delayMs == 0) {
                var startTime = Clock.UtcNow;
                func(t, u, v, w);
                delayMs = intervalMs - (int)(Clock.UtcNow - startTime).TotalMilliseconds;
                if (delayMs < 0) {
                    throw new TimeoutException("DoRepeating execution took longer than the interval assigned");
                } else if (iterations > 1) {
                    DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations - 1, func, t, u, v, w));
                }
            } else {
                DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v, w));
            }

            return cts;
        }


        // -------------------------------------
        // DoRepeatingTS
        // -------------------------------------

        protected CancellationTokenSource DoRepeatingTS(int delayMs, int intervalMs, uint? iterations, Func<IEnumerator> func) {
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T>(int delayMs, int intervalMs, uint? iterations, Func<T, IEnumerator> func, T t)
                where T : struct {
            AssertBlittable<T>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T, U>(int delayMs, int intervalMs, uint? iterations, Func<T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T, U, V>(int delayMs, int intervalMs, uint? iterations, Func<T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T, U, V, W>(int delayMs, int intervalMs, uint? iterations, Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v, w));
            return cts;
        }

        protected CancellationTokenSource DoRepeatingTS(int delayMs, int intervalMs, uint? iterations, Action func) {
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T>(int delayMs, int intervalMs, uint? iterations, Action<T> func, T t)
                where T : struct {
            AssertBlittable<T>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T, U>(int delayMs, int intervalMs, uint? iterations, Action<T, U> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T, U, V>(int delayMs, int intervalMs, uint? iterations, Action<T, U, V> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v));
            return cts;
        }
        protected CancellationTokenSource DoRepeatingTS<T, U, V, W>(int delayMs, int intervalMs, uint? iterations, Action<T, U, V, W> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            if (intervalMs <= 0) { throw new ArgumentOutOfRangeException($"intervalMs <= 0 ({intervalMs})"); }

            CancellationTokenSource cts = new();
            DoHelper(RepeatingEnumeratorCaller(cts, delayMs, intervalMs, iterations, func, t, u, v, w));
            return cts;
        }


        // -------------------------------------
        // DoWaitFor
        // -------------------------------------

        protected IEnumerator DoWaitFor(Func<IEnumerator> func) {
            MonoBehaviourSafetyCheck();
            yield return MakeEventEnumerator(func());
        }
        protected IEnumerator DoWaitFor<T>(Func<T, IEnumerator> func, T t) {
            MonoBehaviourSafetyCheck();
            yield return MakeEventEnumerator(func(t));
        }
        protected IEnumerator DoWaitFor<T, U>(Func<T, U, IEnumerator> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            yield return MakeEventEnumerator(func(t, u));
        }
        protected IEnumerator DoWaitFor<T, U, V>(Func<T, U, V, IEnumerator> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            yield return MakeEventEnumerator(func(t, u, v));
        }
        protected IEnumerator DoWaitFor<T, U, V, W>(Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            yield return MakeEventEnumerator(func(t, u, v, w));
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task DoWaitFor(Func<Task> func) {
            MonoBehaviourSafetyCheck();
            try {
                await func();
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoWaitFor<T>(Func<T, Task> func, T t) {
            MonoBehaviourSafetyCheck();
            try {
                await func(t);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoWaitFor<T, U>(Func<T, U, Task> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            try {
                await func(t, u);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoWaitFor<T, U, V>(Func<T, U, V, Task> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            try {
                await func(t, u, v);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task DoWaitFor<T, U, V, W>(Func<T, U, V, W, Task> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            try {
                await func(t, u, v, w);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

        // -------------------------------------
        // DoWaitForTS
        // -------------------------------------

        private async Task DoWaitForHelper(IEnumerator enumerator) {

            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, enumerator));
            await tcs.Task;
        }

        protected async Task DoWaitForTS(Func<IEnumerator> func) {
            await DoWaitForHelper(func());
        }
        protected async Task DoWaitForTS<T>(Func<T, IEnumerator> func, T t)
                where T : struct {
            AssertBlittable<T>();
            await DoWaitForHelper(func(t));
        }
        protected async Task DoWaitForTS<T, U>(Func<T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            await DoWaitForHelper(func(t, u));
        }
        protected async Task DoWaitForTS<T, U, V>(Func<T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            await DoWaitForHelper(func(t, u, v));
        }
        protected async Task DoWaitForTS<T, U, V, W>(Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            await DoWaitForHelper(func(t, u, v, w));
        }

        protected async Task DoWaitForTS(Action func) {
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T>(Action<T> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T, U>(Action<T, U> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T, U, V>(Action<T, U, V> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T, U, V, W>(Action<T, U, V, W> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v, w));
            await tcs.Task;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task DoWaitForTS(Func<Task> func) {
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T>(Func<T, Task> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T, U>(Func<T, U, Task> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T, U, V>(Func<T, U, V, Task> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v));
            await tcs.Task;
        }
        protected async Task DoWaitForTS<T, U, V, W>(Func<T, U, V, W, Task> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v, w));
            await tcs.Task;
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS


        // -------------------------------------
        // DoGet
        // -------------------------------------

        protected Z DoGet<Z>(Func<Z> func) {
            MonoBehaviourSafetyCheck();
            try {
                return func();
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected Z DoGet<T, Z>(Func<T, Z> func, T t) {
            MonoBehaviourSafetyCheck();
            try {
                return func(t);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected Z DoGet<T, U, Z>(Func<T, U, Z> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            try {
                return func(t, u);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected Z DoGet<T, U, V, Z>(Func<T, U, V, Z> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            try {
                return func(t, u, v);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected Z DoGet<T, U, V, W, Z>(Func<T, U, V, W, Z> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            try {
                return func(t, u, v, w);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task<Z> DoGet<Z>(Func<Task<Z>> func) {
            MonoBehaviourSafetyCheck();
            try {
                return await func();
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task<Z> DoGet<T, Z>(Func<T, Task<Z>> func, T t) {
            MonoBehaviourSafetyCheck();
            try {
                return await func(t);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task<Z> DoGet<T, U, Z>(Func<T, U, Task<Z>> func, T t, U u) {
            MonoBehaviourSafetyCheck();
            try {
                return await func(t, u);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task<Z> DoGet<T, U, V, Z>(Func<T, U, V, Task<Z>> func, T t, U u, V v) {
            MonoBehaviourSafetyCheck();
            try {
                return await func(t, u, v);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
        protected async Task<Z> DoGet<T, U, V, W, Z>(Func<T, U, V, W, Task<Z>> func, T t, U u, V v, W w) {
            MonoBehaviourSafetyCheck();
            try {
                return await func(t, u, v, w);
            } catch (Exception e) {
                ErrorNotifier.ErrorTS(e);
                throw e; // This is a duplication, but C# can't tell Error always throws an exception
            }
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

        // -------------------------------------
        // DoGetTS
        // -------------------------------------

        private async Task<Z> DoGetHelper<Z>(IEnumerator enumerator)
                where Z : struct {
            AssertBlittable<Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, enumerator));
            return await tcs.Task;
        }

        protected async Task<Z> DoGetTS<Z>(Func<IEnumerator> func)
                where Z : struct {
            return await DoGetHelper<Z>(func());
        }
        protected async Task<Z> DoGetTS<T, Z>(Func<T, IEnumerator> func, T t)
                where T : struct
                where Z : struct {
            AssertBlittable<T>();
            return await DoGetHelper<Z>(func(t));
        }
        protected async Task<Z> DoGetTS<T, U, Z>(Func<T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct
                where Z : struct {
            AssertBlittable<T, U>();
            return await DoGetHelper<Z>(func(t, u));
        }
        protected async Task<Z> DoGetTS<T, U, V, Z>(Func<T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct
                where Z : struct {
            AssertBlittable<T, U, V>();
            return await DoGetHelper<Z>(func(t, u, v));
        }
        protected async Task<Z> DoGetTS<T, U, V, W, Z>(Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct
                where Z : struct {
            AssertBlittable<T, U, V, W>();
            return await DoGetHelper<Z>(func(t, u, v, w));
        }

        protected async Task<Z> DoGetTS<Z>(Func<Z> func)
                where Z : struct {
            AssertBlittable<Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, Z>(Func<T, Z> func, T t)
                where T : struct
                where Z : struct {
            AssertBlittable<T, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, U, Z>(Func<T, U, Z> func, T t, U u)
                where T : struct
                where U : struct
                where Z : struct {
            AssertBlittable<T, U, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, U, V, Z>(Func<T, U, V, Z> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct
                where Z : struct {
            AssertBlittable<T, U, V, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, U, V, W, Z>(Func<T, U, V, W, Z> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct
                where Z : struct {
            AssertBlittable<T, U, V, W, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v, w));
            return await tcs.Task;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task<Z> DoGetTS<Z>(Func<Task<Z>> func)
                where Z : struct {
            AssertBlittable<Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, Z>(Func<T, Task<Z>> func, T t)
                where T : struct
                where Z : struct {
            AssertBlittable<T, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, U, Z>(Func<T, U, Task<Z>> func, T t, U u)
                where T : struct
                where U : struct
                where Z : struct {
            AssertBlittable<T, U, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, U, V, Z>(Func<T, U, V, Task<Z>> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct
                where Z : struct {
            AssertBlittable<T, U, V, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetTS<T, U, V, W, Z>(Func<T, U, V, W, Task<Z>> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct
                where Z : struct {
            AssertBlittable<T, U, V, W, Z>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v, w));
            return await tcs.Task;
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

        // -------------------------------------
        // DoGetRelaxedTS
        // -------------------------------------
        // Only use these methods if you create the value in the function and you know it isn't used anywhere else
        // TODO: JPB: (needed) Figure out how to handle non-blittable return types in DoGet
        //            And add tests for these

        private async Task<Z> DoGetRelaxedHelper<Z>(IEnumerator enumerator) {
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, enumerator));
            return await tcs.Task;
        }

        protected async Task<Z> DoGetRelaxedTS<Z>(Func<IEnumerator> func) {
            return await DoGetRelaxedHelper<Z>(func());
        }
        protected async Task<Z> DoGetRelaxedTS<T, Z>(Func<T, IEnumerator> func, T t)
                where T : struct {
            AssertBlittable<T>();
            return await DoGetRelaxedHelper<Z>(func(t));
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, Z>(Func<T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            return await DoGetRelaxedHelper<Z>(func(t, u));
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, V, Z>(Func<T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            return await DoGetRelaxedHelper<Z>(func(t, u, v));
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, V, W, Z>(Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            return await DoGetRelaxedHelper<Z>(func(t, u, v, w));
        }

        protected async Task<Z> DoGetRelaxedTS<Z>(Func<Z> func) {
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, Z>(Func<T, Z> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, Z>(Func<T, U, Z> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, V, Z>(Func<T, U, V, Z> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, V, W, Z>(Func<T, U, V, W, Z> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v, w));
            return await tcs.Task;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task<Z> DoGetRelaxedTS<Z>(Func<Task<Z>> func) {
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, Z>(Func<T, Task<Z>> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, Z>(Func<T, U, Task<Z>> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, V, Z>(Func<T, U, V, Task<Z>> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetRelaxedTS<T, U, V, W, Z>(Func<T, U, V, W, Task<Z>> func, T t, U u, V v, W w)
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            AssertBlittable<T, U, V, W>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(TaskTrigger(tcs, func, t, u, v, w));
            return await tcs.Task;
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

#if EVENTMONOBEHAVIOR_MANUAL_RESULT_SET
        // -------------------------------------
        // DoWaitForManualTriggerTS
        // 
        // User is responsible for triggering these TaskCompletionSources
        // Do NOT use this unless you really know what you're doing (and there is no other option)
        // 
        // // JPB: This is currently used in the InputManager
        // -------------------------------------

        protected async Task DoWaitForManualTriggerTS(Func<TaskCompletionSource<bool>, IEnumerator> func) {
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(func(tcs));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T>(Func<TaskCompletionSource<bool>, T, IEnumerator> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(func(tcs, t));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T, U>(Func<TaskCompletionSource<bool>, T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(func(tcs, t, u));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T, U, V>(Func<TaskCompletionSource<bool>, T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(func(tcs, t, u, v));
            await tcs.Task;
        }

        protected async Task DoWaitForManualTriggerTS(Action<TaskCompletionSource<bool>> func) {
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T>(Action<TaskCompletionSource<bool>, T> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T, U>(Action<TaskCompletionSource<bool>, T, U> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T, U, V>(Action<TaskCompletionSource<bool>, T, U, V> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u, v));
            await tcs.Task;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task DoWaitForManualTriggerTS(Func<TaskCompletionSource<bool>, Task> func) {
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T>(Func<TaskCompletionSource<bool>, T, Task> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T, U>(Func<TaskCompletionSource<bool>, T, U, Task> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u));
            await tcs.Task;
        }
        protected async Task DoWaitForManualTriggerTS<T, U, V>(Func<TaskCompletionSource<bool>, T, U, V, Task> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<bool>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u, v));
            await tcs.Task;
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

        // -------------------------------------
        // DoGetManualTriggerTS
        // 
        // User is responsible for triggering these TaskCompletionSources with the result
        // Do NOT use this unless you really know what you're doing (and there is no other option)
        // 
        // JPB: This is currently used in the InputManager
        // 
        // TODO: JPB: (bug) There is a bug in these that is does not guarentee that the returned result is actually blittable
        // It only guarentees that it is a struct
        // The struct could have a reference (aka a class) in it, which would create an unsafe reference across threads
        // The calling passed in IEnumerator/Func has to call AssertBlittable on the result
        // -------------------------------------

        protected async Task<Z> DoGetManualTriggerTS<Z>(Func<TaskCompletionSource<Z>, IEnumerator> func) {
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(func(tcs));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, Z>(Func<TaskCompletionSource<Z>, T, IEnumerator> func, T t)
                where T : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(func(tcs, t));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, U, Z>(Func<TaskCompletionSource<Z>, T, U, IEnumerator> func, T t, U u)
                where T : struct
                where U : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(func(tcs, t, u));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, U, V, Z>(Func<TaskCompletionSource<Z>, T, U, V, IEnumerator> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(func(tcs, t, u, v));
            return await tcs.Task;
        }

        protected async Task<Z> DoGetManualTriggerTS<Z>(Action<TaskCompletionSource<Z>> func)
                where Z : struct {
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, Z>(Action<TaskCompletionSource<Z>, T> func, T t)
                where T : struct
                where Z : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, U, Z>(Action<TaskCompletionSource<Z>, T, U> func, T t, U u)
                where T : struct
                where U : struct
                where Z : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, U, V, Z>(Action<TaskCompletionSource<Z>, T, U, V> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct
                where Z : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u, v));
            return await tcs.Task;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        protected async Task<Z> DoGetManualTriggerTS<Z>(Func<TaskCompletionSource<Z>, Task> func)
                where Z : struct {
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, Z>(Func<TaskCompletionSource<Z>, T, Task> func, T t)
                where T : struct
                where Z : struct {
            AssertBlittable<T>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, U, Z>(Func<TaskCompletionSource<Z>, T, U, Task> func, T t, U u)
                where T : struct
                where U : struct
                where Z : struct {
            AssertBlittable<T, U>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u));
            return await tcs.Task;
        }
        protected async Task<Z> DoGetManualTriggerTS<T, U, V, Z>(Func<TaskCompletionSource<Z>, T, U, V, Task> func, T t, U u, V v)
                where T : struct
                where U : struct
                where V : struct
                where Z : struct {
            AssertBlittable<T, U, V>();
            var tcs = new TaskCompletionSource<Z>();
            manager.events.Enqueue(EnumeratorCaller(func, tcs, t, u, v));
            return await tcs.Task;
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS
#endif // EVENTMONOBEHAVIOR_MANUAL_RESULT_SET


        // -------------------------------------
        // EnumeratorCaller
        // -------------------------------------

        private IEnumerator EnumeratorCaller(Action func) {
            func();
            yield break;
        }
        private IEnumerator EnumeratorCaller<T>(Action<T> func, T t) {
            func(t);
            yield break;
        }
        private IEnumerator EnumeratorCaller<T, U>(Action<T, U> func, T t, U u) {
            func(t, u);
            yield break;
        }
        private IEnumerator EnumeratorCaller<T, U, V>(Action<T, U, V> func, T t, U u, V v) {
            func(t, u, v);
            yield break;
        }
        private IEnumerator EnumeratorCaller<T, U, V, W>(Action<T, U, V, W> func, T t, U u, V v, W w) {
            func(t, u, v, w);
            yield break;
        }

#if EVENTMONOBEHAVIOR_MANUAL_RESULT_SET
        private IEnumerator EnumeratorCaller<T>(Func<T, Task> func, T t) {
            yield return func(t).ToEnumerator();
        }
        private IEnumerator EnumeratorCaller<T, U>(Func<T, U, Task> func, T t, U u) {
            yield return func(t, u).ToEnumerator();
        }
        private IEnumerator EnumeratorCaller<T, U, V>(Func<T, U, V, Task> func, T t, U u, V v) {
            yield return func(t, u, v).ToEnumerator();
        }
        private IEnumerator EnumeratorCaller<T, U, V, W>(Func<T, U, V, W, Task> func, T t, U u, V v, W w) {
            yield return func(t, u, v, w).ToEnumerator();
        }
#endif // EVENTMONOBEHAVIOR_MANUAL_RESULT_SET


        // -------------------------------------
        // DelayedEnumeratorCaller
        // -------------------------------------

        private IEnumerator DelayedEnumeratorCaller(int millisecondsDelay, IEnumerator func) {
            yield return manager.DelayE(millisecondsDelay);
            yield return func;
        }
        private IEnumerator DelayedEnumeratorCaller(int millisecondsDelay, Action func) {
            yield return manager.DelayE(millisecondsDelay);
            func();
        }
        private IEnumerator DelayedEnumeratorCaller<T>(int millisecondsDelay, Action<T> func, T t) {
            yield return manager.DelayE(millisecondsDelay);
            func(t);
        }
        private IEnumerator DelayedEnumeratorCaller<T, U>(int millisecondsDelay, Action<T, U> func, T t, U u) {
            yield return manager.DelayE(millisecondsDelay);
            func(t, u);
        }
        private IEnumerator DelayedEnumeratorCaller<T, U, V>(int millisecondsDelay, Action<T, U, V> func, T t, U u, V v) {
            yield return manager.DelayE(millisecondsDelay);
            func(t, u, v);
        }
        private IEnumerator DelayedEnumeratorCaller<T, U, V, W>(int millisecondsDelay, Action<T, U, V, W> func, T t, U u, V v, W w) {
            yield return manager.DelayE(millisecondsDelay);
            func(t, u, v, w);
        }


        // -------------------------------------
        // RepeatingEnumeratorCaller
        // -------------------------------------

        private IEnumerator RepeatingEnumeratorCaller(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Func<IEnumerator> func) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                yield return MakeEventEnumerator(func());
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Func<T, IEnumerator> func, T t) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                yield return MakeEventEnumerator(func(t));
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T, U>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Func<T, U, IEnumerator> func, T t, U u) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                yield return MakeEventEnumerator(func(t, u));
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T, U, V>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Func<T, U, V, IEnumerator> func, T t, U u, V v) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                yield return MakeEventEnumerator(func(t, u, v));
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T, U, V, W>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Func<T, U, V, W, IEnumerator> func, T t, U u, V v, W w) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                yield return MakeEventEnumerator(func(t, u, v, w));
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }

        private IEnumerator RepeatingEnumeratorCaller(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Action func) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                func();
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Action<T> func, T t) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                func(t);
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T, U>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Action<T, U> func, T t, U u) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                func(t, u);
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T, U, V>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Action<T, U, V> func, T t, U u, V v) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                func(t, u, v);
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }
        private IEnumerator RepeatingEnumeratorCaller<T, U, V, W>(CancellationTokenSource cts, int delayMs, int intervalMs, uint? iterations, Action<T, U, V, W> func, T t, U u, V v, W w) {
            if (delayMs != 0) { yield return manager.DelayE(delayMs); }

            uint totalIterations = iterations ?? uint.MaxValue;
            var initTime = Clock.UtcNow;
            for (int i = 0; i < totalIterations; ++i) {
                if (cts.IsCancellationRequested) { break; }
                func(t, u, v, w);
                var delayTime = (i + 1) * intervalMs - (Clock.UtcNow - initTime).TotalMilliseconds;
                if (delayTime < 0) { throw new TimeoutException("DoRepeating execution took longer than the interval assigned"); }
                yield return manager.DelayE((int)delayTime);
            }
        }


        // -------------------------------------
        // TaskTrigger
        // Enumerator, Action, Function, and Task to Enumerator
        // -------------------------------------

        private IEnumerator TaskTrigger(TaskCompletionSource<bool> tcs, IEnumerator func) {
            yield return func;
            tcs.SetResult(true);
        }

        private IEnumerator TaskTrigger(TaskCompletionSource<bool> tcs, Action func) {
            func();
            tcs.SetResult(true);
            yield break;
        }
        private IEnumerator TaskTrigger<T>(TaskCompletionSource<bool> tcs, Action<T> func, T t) {
            func(t);
            tcs.SetResult(true);
            yield break;
        }
        private IEnumerator TaskTrigger<T, U>(TaskCompletionSource<bool> tcs, Action<T, U> func, T t, U u) {
            func(t, u);
            tcs.SetResult(true);
            yield break;
        }
        private IEnumerator TaskTrigger<T, U, V>(TaskCompletionSource<bool> tcs, Action<T, U, V> func, T t, U u, V v) {
            func(t, u, v);
            tcs.SetResult(true);
            yield break;
        }
        private IEnumerator TaskTrigger<T, U, V, W>(TaskCompletionSource<bool> tcs, Action<T, U, V, W> func, T t, U u, V v, W w) {
            func(t, u, v, w);
            tcs.SetResult(true);
            yield break;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        private IEnumerator TaskTrigger(TaskCompletionSource<bool> tcs, Func<Task> func) {
            yield return func().ToEnumerator();
            tcs.SetResult(true);
        }
        private IEnumerator TaskTrigger<T>(TaskCompletionSource<bool> tcs, Func<T, Task> func, T t) {
            yield return func(t).ToEnumerator();
            tcs.SetResult(true);
        }
        private IEnumerator TaskTrigger<T, U>(TaskCompletionSource<bool> tcs, Func<T, U, Task> func, T t, U u) {
            yield return func(t, u).ToEnumerator();
            tcs.SetResult(true);
        }
        private IEnumerator TaskTrigger<T, U, V>(TaskCompletionSource<bool> tcs, Func<T, U, V, Task> func, T t, U u, V v) {
            yield return func(t, u, v).ToEnumerator();
            tcs.SetResult(true);
        }
        private IEnumerator TaskTrigger<T, U, V, W>(TaskCompletionSource<bool> tcs, Func<T, U, V, W, Task> func, T t, U u, V v, W w) {
            yield return func(t, u, v, w).ToEnumerator();
            tcs.SetResult(true);
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

        private IEnumerator TaskTrigger<Z>(TaskCompletionSource<Z> tcs, IEnumerator func) {
            yield return func;
            Z ret = (Z)func.Current;
            tcs.SetResult(ret);
        }

        private IEnumerator TaskTrigger<Z>(TaskCompletionSource<Z> tcs, Func<Z> func) {
            Z ret = func();
            tcs.SetResult(ret);
            yield break;
        }
        private IEnumerator TaskTrigger<T, Z>(TaskCompletionSource<Z> tcs, Func<T, Z> func, T t) {
            Z ret = func(t);
            tcs.SetResult(ret);
            yield break;
        }
        private IEnumerator TaskTrigger<T, U, Z>(TaskCompletionSource<Z> tcs, Func<T, U, Z> func, T t, U u) {
            Z ret = func(t, u);
            tcs.SetResult(ret);
            yield break;
        }
        private IEnumerator TaskTrigger<T, U, V, Z>(TaskCompletionSource<Z> tcs, Func<T, U, V, Z> func, T t, U u, V v) {
            Z ret = func(t, u, v);
            tcs.SetResult(ret);
            yield break;
        }
        private IEnumerator TaskTrigger<T, U, V, W, Z>(TaskCompletionSource<Z> tcs, Func<T, U, V, W, Z> func, T t, U u, V v, W w) {
            Z ret = func(t, u, v, w);
            tcs.SetResult(ret);
            yield break;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        private IEnumerator TaskTrigger<Z>(TaskCompletionSource<Z> tcs, Func<Task<Z>> func) {
            var task = func();
            yield return task.ToEnumerator();
            Z ret = task.Result;
            tcs.SetResult(ret);
        }
        private IEnumerator TaskTrigger<T, Z>(TaskCompletionSource<Z> tcs, Func<T, Task<Z>> func, T t) {
            var task = func(t);
            yield return task.ToEnumerator();
            Z ret = task.Result;
            tcs.SetResult(ret);
        }
        private IEnumerator TaskTrigger<T, U, Z>(TaskCompletionSource<Z> tcs, Func<T, U, Task<Z>> func, T t, U u) {
            var task = func(t, u);
            yield return task.ToEnumerator();
            Z ret = task.Result;
            tcs.SetResult(ret);
        }
        private IEnumerator TaskTrigger<T, U, V, Z>(TaskCompletionSource<Z> tcs, Func<T, U, V, Task<Z>> func, T t, U u, V v) {
            var task = func(t, u, v);
            yield return task.ToEnumerator();
            Z ret = task.Result;
            tcs.SetResult(ret);
        }
        private IEnumerator TaskTrigger<T, U, V, W, Z>(TaskCompletionSource<Z> tcs, Func<T, U, V, W, Task<Z>> func, T t, U u, V v, W w) {
            var task = func(t, u, v, w);
            yield return task.ToEnumerator();
            Z ret = task.Result;
            tcs.SetResult(ret);
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS
    }

}