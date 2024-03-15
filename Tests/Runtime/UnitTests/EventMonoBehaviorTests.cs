//Copyright (c) 2024 Jefferson University
//Copyright (c) 2024 Bruska Technologies LLC
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>.

#define EVENTMONOBEHAVIOR_TASK_OPERATORS
#define EVENTMONOBEHAVIOR_MANUAL_RESULT_SET

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using UnityEPL;

namespace UnityEPLTests {

    // TODO: JPB: (refactor) Use a task instead of a thread to check for Exceptions
    //            This would make it so that we don't have to use LogAssert and dirty the logs
    //            Ex: https://stackoverflow.com/a/53460032

    public class EventMonoBehaviorTests {
        // -------------------------------------
        // Globals
        // -------------------------------------

        EMB emb;
        bool isSetup = false;

        // TODO: JPB: (bug) Things should probably never take two frames
        const double ONE_FRAME_MS = 1000.0 / 120.0;
        const double TWO_FRAMES_MS = 1000.0 / 120.0 * 2;

        // -------------------------------------
        // Setup
        // -------------------------------------

        [UnitySetUp]
        public IEnumerator Setup() {
            if (!isSetup) {
                isSetup = true;
                SceneManager.LoadScene("manager");
                yield return null; // Wait for InterfaceManager Awake call
            }

            if (emb == null) emb = new GameObject().AddComponent<EMB>();
        }


        // -------------------------------------
        // General Tests
        // -------------------------------------

        [UnityTest]
        public IEnumerator MonobehaviourSafetyCheck() {
            // This should not throw an exception because it is on the unity thread
            emb.MonoBehaviourSafetyCheckTest();

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: .*"));

            // This should throw an exception because it's on a new thread
            var thread = new Thread(() => { emb.MonoBehaviourSafetyCheckTest(); });
            thread.Start();
            thread.Join();

            yield break;
        }


        // -------------------------------------
        // DoGet Tests
        //
        // THIS IS NOT HOW YOU SHOULD USE THIS FRAMEWORK
        // Below we are verifying that Do and DoGet work
        // In order to do this, we use a mutex class to guarantee that we are not creating threading issues
        // This methodology should be avoided because it can significantly slow your code down due to the locks
        // Instead just use DoGet like the rest of the example do, once we verify that DoGet works
        // -------------------------------------

        [UnityTest]
        public IEnumerator DoGetFuncMB() {
            int i = emb.mutex.Get();

            Assert.AreEqual(i, emb.GetMutexValFuncMB()); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), emb.GetMutexValFuncMB()); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            Assert.AreEqual(i + 1, emb.GetMutexValFuncMB()); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), emb.GetMutexValFuncMB()); // Didn't mutate state but cached value
            yield break;
        }

        [UnityTest]
        public IEnumerator DoGetFuncMBSafetyCheck() {
            Exception exception = null;
            var thread = new Thread(() => {
                try {
                    emb.GetMutexValFuncMB();
                } catch (Exception e) {
                    exception = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<Exception>(exception);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);

            yield break;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        [UnityTest]
        public IEnumerator DoGetTaskMB() {
            int i = emb.mutex.Get();

            var task = emb.GetMutexValTaskMB();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            task = emb.GetMutexValTaskMB();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value
        }

        [UnityTest]
        public IEnumerator DoGetTaskMBSafetyCheck() {
            Exception exception = null;
            var thread = new Thread(async () => {
                try {
                    await emb.GetMutexValTaskMB();
                } catch (Exception e) {
                    exception = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<Exception>(exception);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);

            yield break;
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

        [UnityTest]
        public IEnumerator DoGetEnum() {
            int i = emb.mutex.Get();

            var task = emb.GetMutexValEnum();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            task = emb.GetMutexValEnum();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value
        }

        [UnityTest]
        public IEnumerator DoGetFunc() {
            int i = emb.mutex.Get();

            var task = emb.GetMutexValFunc();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            task = emb.GetMutexValFunc();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        [UnityTest]
        public IEnumerator DoGetTask() {
            int i = emb.mutex.Get();

            var task = emb.GetMutexValTask();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            task = emb.GetMutexValTask();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

#if EVENTMONOBEHAVIOR_MANUAL_RESULT_SET
        [UnityTest]
        public IEnumerator DoGetManualTriggerEnum() {
            int i = emb.mutex.Get();

            var task = emb.GetMutexValManualTriggerEnum();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            task = emb.GetMutexValManualTriggerEnum();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value
        }

        [UnityTest]
        public IEnumerator DoGetManualTriggerFunc() {
            int i = emb.mutex.Get();

            var task = emb.GetMutexValManualTriggerFunc();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            task = emb.GetMutexValManualTriggerFunc();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        [UnityTest]
        public IEnumerator DoGetManualTriggerTask() {
            int i = emb.mutex.Get();

            var task = emb.GetMutexValManualTriggerTask();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value

            emb.mutex.Mutate((int i) => { return i + 1; });

            task = emb.GetMutexValManualTriggerTask();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result); // Didn't mutate state
            Assert.AreEqual(emb.mutex.Get(), task.Result); // Didn't mutate state but cached value
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS
#endif // EVENTMONOBEHAVIOR_MANUAL_RESULT_SET


        // -------------------------------------
        // DoMB Tests
        // -------------------------------------

        [UnityTest]
        public IEnumerator DoActMB() {
            var i = emb.GetIMB();

            emb.IncActMB();

            Assert.AreEqual(i + 1, emb.GetIMB());
            yield break;
        }

        [UnityTest]
        public IEnumerator DoActMBSafetyCheck() {
            Exception exception = null;
            var thread = new Thread(() => {
                try {
                    emb.IncActMB();
                } catch (Exception e) {
                    exception = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<Exception>(exception);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);

            yield break;
        }

        [UnityTest]
        public IEnumerator DoRepeatingEnumMB() {
            var i = emb.GetIMB();

            yield return emb.IncThreeTimesEnumMB(0, 1000, 3);

            Assert.AreEqual(i + 1, emb.GetIMB());

            yield return InterfaceManager.DelayE(900);
            Assert.AreEqual(i + 1, emb.GetIMB());

            yield return InterfaceManager.DelayE(200);
            Assert.AreEqual(i + 2, emb.GetIMB());

            yield return InterfaceManager.DelayE(1000);
            Assert.AreEqual(i + 3, emb.GetIMB());
        }

        [UnityTest]
        public IEnumerator DoRepeatingDelayedEnumMB() {
            var i = emb.GetIMB();

            yield return emb.IncThreeTimesEnumMB(500, 1000, 3);

            yield return InterfaceManager.DelayE(400);
            Assert.AreEqual(i, emb.GetIMB());

            yield return InterfaceManager.DelayE(200);
            Assert.AreEqual(i + 1, emb.GetIMB());

            yield return InterfaceManager.DelayE(1000);
            Assert.AreEqual(i + 2, emb.GetIMB());

            yield return InterfaceManager.DelayE(1000);
            Assert.AreEqual(i + 3, emb.GetIMB());
        }

        [UnityTest]
        public IEnumerator DoRepeatingEnumMBSafetyCheck() {
            Exception exception = null;
            var thread = new Thread(() => {
                try {
                    var enumerator = emb.IncThreeTimesEnumMB(500, 1000, 3);
                    enumerator.MoveNext();
                    enumerator = (IEnumerator)enumerator.Current;
                    while (enumerator.MoveNext()) ;
                } catch (Exception e) {
                    exception = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<Exception>(exception);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);

            yield break;
        }

        [UnityTest]
        public IEnumerator DoRepeatingActMB() {
            var i = emb.GetIMB();

            emb.IncThreeTimesActMB(0, 1000, 3);

            Assert.AreEqual(i + 1, emb.GetIMB());

            yield return InterfaceManager.DelayE(900);
            Assert.AreEqual(i + 1, emb.GetIMB());

            yield return InterfaceManager.DelayE(200);
            Assert.AreEqual(i + 2, emb.GetIMB());

            yield return InterfaceManager.DelayE(1000);
            Assert.AreEqual(i + 3, emb.GetIMB());
        }

        [UnityTest]
        public IEnumerator DoRepeatingDelayedActMB() {
            var i = emb.GetIMB();

            emb.IncThreeTimesActMB(500, 1000, 3);

            yield return InterfaceManager.DelayE(400);
            Assert.AreEqual(i, emb.GetIMB());

            yield return InterfaceManager.DelayE(200);
            Assert.AreEqual(i + 1, emb.GetIMB());

            yield return InterfaceManager.DelayE(1000);
            Assert.AreEqual(i + 2, emb.GetIMB());

            yield return InterfaceManager.DelayE(1000);
            Assert.AreEqual(i + 3, emb.GetIMB());
        }

        [UnityTest]
        public IEnumerator DoRepeatingActMBSafetyCheck() {
            Exception exception = null;
            var thread = new Thread(() => {
                try {
                    emb.IncThreeTimesActMB(500, 1000, 3);
                } catch (Exception e) {
                    exception = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<Exception>(exception);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);

            yield break;
        }

        [UnityTest]
        public IEnumerator DoWaitForEnumMB() {
            var i = emb.GetIMB();

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitEnumMB(1000);

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + TWO_FRAMES_MS);
            Assert.AreEqual(i + 1, emb.GetIMB());
        }

        [UnityTest]
        public IEnumerator DoWaitForEnumMBSafetyCheck() {
            Exception exception = null;
            var thread = new Thread(() => {
                try {
                    var enumerator = emb.DelayedIncAndWaitEnumMB(1000);
                    enumerator.MoveNext();
                    enumerator = (IEnumerator)enumerator.Current;
                    while (enumerator.MoveNext()) ;
                } catch (Exception e) {
                    exception = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<Exception>(exception);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);

            yield break;
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        [UnityTest]
        public IEnumerator DoWaitForTaskMB() {
            var i = emb.GetIMB();

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitTaskMB(1000).ToEnumerator();

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + TWO_FRAMES_MS);

            Assert.AreEqual(i + 1, emb.GetIMB());
        }

        [UnityTest]
        public IEnumerator DoWaitForTaskMBSafetyCheck() {
            Exception exception = null;
            var thread = new Thread(async () => {
                try {
                    await emb.DelayedIncAndWaitTaskMB(1000);
                } catch (Exception e) {
                    exception = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<Exception>(exception);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);

            yield break;
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS


        // -------------------------------------
        // Do Tests
        // -------------------------------------

        [UnityTest]
        public IEnumerator DoEnum() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.IncEnum();

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }

        [UnityTest]
        public IEnumerator DoAct() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.IncAct();

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }

        [UnityTest]
        public IEnumerator DoInEnum() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.DelayedIncEnum(1000);

            yield return InterfaceManager.DelayE(900);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result);

            yield return InterfaceManager.DelayE(200);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i+1, task.Result);
        }

        [UnityTest]
        public IEnumerator DoInAct() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.DelayedIncAct(1000);

            yield return InterfaceManager.DelayE(900);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result);

            yield return InterfaceManager.DelayE(200);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }

        [UnityTest]
        public IEnumerator DoRepeatingEnum() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.IncThreeTimesEnum(0, 1000, 3);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);

            yield return InterfaceManager.DelayE(900);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);

            yield return InterfaceManager.DelayE(200);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 2, task.Result);

            yield return InterfaceManager.DelayE(1000);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 3, task.Result);
        }

        [UnityTest]
        public IEnumerator DoRepeatingDelayedEnum() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.IncThreeTimesEnum(500, 1000, 3);

            yield return InterfaceManager.DelayE(400);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result);

            yield return InterfaceManager.DelayE(200);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);

            yield return InterfaceManager.DelayE(1000);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 2, task.Result);

            yield return InterfaceManager.DelayE(1000);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 3, task.Result);
        }

        [UnityTest]
        public IEnumerator DoRepeatingAct() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.IncThreeTimesAct(0, 1000, 3);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);

            yield return InterfaceManager.DelayE(900);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);

            yield return InterfaceManager.DelayE(200);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 2, task.Result);

            yield return InterfaceManager.DelayE(1000);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 3, task.Result);
        }

        [UnityTest]
        public IEnumerator DoRepeatingDelayedAct() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            emb.IncThreeTimesAct(500, 1000, 3);

            yield return InterfaceManager.DelayE(400);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i, task.Result);

            yield return InterfaceManager.DelayE(200);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);

            yield return InterfaceManager.DelayE(1000);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 2, task.Result);

            yield return InterfaceManager.DelayE(1000);
            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 3, task.Result);
        }

        [UnityTest]
        public IEnumerator DoWaitForEnum() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitEnum(1000).ToEnumerator();

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + TWO_FRAMES_MS);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i+1, task.Result);
        }

        [UnityTest]
        public IEnumerator DoWaitForAct() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitAct(1000).ToEnumerator();

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + ONE_FRAME_MS);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        [UnityTest]
        public IEnumerator DoWaitForTask() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitTask(1000).ToEnumerator();

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + TWO_FRAMES_MS);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

#if EVENTMONOBEHAVIOR_MANUAL_RESULT_SET
        [UnityTest]
        public IEnumerator DoWaitForManualTriggerEnum() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitManualTriggerEnum(1000).ToEnumerator();

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + TWO_FRAMES_MS);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }

        [UnityTest]
        public IEnumerator DoWaitForManualTriggerAct() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitManualTriggerAct(1000).ToEnumerator();

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + ONE_FRAME_MS);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
        [UnityTest]
        public IEnumerator DoWaitForManualTriggerTask() {
            var task = emb.GetI();
            yield return task.ToEnumerator();
            var i = task.Result;

            var start = Clock.UtcNow;
            yield return emb.DelayedIncAndWaitManualTriggerTask(1000).ToEnumerator();

            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + TWO_FRAMES_MS);

            task = emb.GetI();
            yield return task.ToEnumerator();
            Assert.AreEqual(i + 1, task.Result);
        }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS
#endif // EVENTMONOBEHAVIOR_MANUAL_RESULT_SET


        // -------------------------------------
        // EventMonoBehavior Helper Class
        // -------------------------------------

        class EMB : EventMonoBehaviour {
            protected override void AwakeOverride() { }

            public Mutex<int> mutex = new Mutex<int>(0);
            protected int i = 0;

            // General functions

            public void MonoBehaviourSafetyCheckTest() {
                MonoBehaviourSafetyCheck();
            }

            // -------------------------------------
            // DoGet Tests
            // Test DoGetMB, DoGet and DoGetManualTrigger with mutex
            // -------------------------------------

            public int GetMutexValFuncMB() {
                return DoGet(GetMutexValFuncMBHelper);
            }
            protected int GetMutexValFuncMBHelper() {
                return mutex.Get();
            }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
            public Task<int> GetMutexValTaskMB() {
                return DoGet<int>(GetMutexValTaskMBHelper);
            }
            protected async Task<int> GetMutexValTaskMBHelper() {
                await InterfaceManager.Delay(1);
                return mutex.Get();
            }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

            public Task<int> GetMutexValEnum() {
                return DoGetTS<int>(GetMutexValEnumHelper);
            }
            protected IEnumerator<int> GetMutexValEnumHelper() {
                yield return mutex.Get();
            }

            public Task<int> GetMutexValFunc() {
                return DoGetTS<int>(GetMutexValFuncHelper);
            }
            protected int GetMutexValFuncHelper() {
                return mutex.Get();
            }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
            public Task<int> GetMutexValTask() {
                return DoGetTS<int>(GetMutexValTaskHelper);
            }
            protected async Task<int> GetMutexValTaskHelper() {
                await InterfaceManager.Delay(1);
                return mutex.Get();
            }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

            public Task<int> GetMutexValManualTriggerEnum() {
                return DoGetManualTriggerTS<int>(GetMutexValManualTriggerEnumHelper);
            }
            protected IEnumerator GetMutexValManualTriggerEnumHelper(TaskCompletionSource<int> tcs) {
                tcs.SetResult(mutex.Get());
                yield return null;
            }

#if EVENTMONOBEHAVIOR_MANUAL_RESULT_SET
            public Task<int> GetMutexValManualTriggerFunc() {
                return DoGetManualTriggerTS<int>(GetMutexValManualTriggerFuncHelper);
            }
            protected void GetMutexValManualTriggerFuncHelper(TaskCompletionSource<int> tcs) {
                tcs.SetResult(mutex.Get());
            }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
            public Task<int> GetMutexValManualTriggerTask() {
                return DoGetManualTriggerTS<int>(GetMutexValManualTriggerTaskHelper);
            }
            protected Task GetMutexValManualTriggerTaskHelper(TaskCompletionSource<int> tcs) {
                tcs.SetResult(mutex.Get());
                return tcs.Task;
            }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS
#endif // EVENTMONOBEHAVIOR_MANUAL_RESULT_SET


            // -------------------------------------
            // Other DoMB Functions
            // -------------------------------------

            public int GetIMB() {
                return DoGet(GetIMBHelper);
            }
            protected int GetIMBHelper() {
                return i;
            }

            public void IncActMB() {
                Do(IncActMBHelper);
            }
            protected void IncActMBHelper() {
                i += 1;
            }

            public IEnumerator IncThreeTimesEnumMB(int delayMs, int intervalMs, uint? iterations) {
                yield return DoRepeating(delayMs, intervalMs, iterations, IncThreeTimesEnumMBHelper);
            }
            protected IEnumerator IncThreeTimesEnumMBHelper() {
                i += 1;
                yield break;
            }

            public void IncThreeTimesActMB(int delayMs, int intervalMs, uint? iterations) {
                DoRepeating(delayMs, intervalMs, iterations, IncThreeTimesActMBHelper);
            }
            protected void IncThreeTimesActMBHelper() {
                i += 1;
            }

            public IEnumerator DelayedIncAndWaitEnumMB(int millisecondsDelay) {
                yield return DoWaitFor(DelayedIncAndWaitEnumMBHelper, millisecondsDelay);
            }
            protected IEnumerator DelayedIncAndWaitEnumMBHelper(int millisecondsDelay) {
                yield return InterfaceManager.DelayE(1000);
                i += 1;
            }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
            public Task DelayedIncAndWaitTaskMB(int millisecondsDelay) {
                return DoWaitFor(DelayedIncAndWaitTaskMBHelper, millisecondsDelay);
            }
            protected async Task DelayedIncAndWaitTaskMBHelper(int millisecondsDelay) {
                await InterfaceManager.Delay(1000);
                i += 1;
            }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS

            // -------------------------------------
            // Other Do Functions
            // -------------------------------------

            public async Task<int> GetI() {
                return await DoGetTS<int>(GetIHelper);
            }
            protected IEnumerator<int> GetIHelper() {
                yield return i;
            }

            public void IncEnum() {
                DoTS(IncEnumHelper);
            }
            protected IEnumerator IncEnumHelper() {
                i += 1;
                yield break;
            }

            public void IncAct() {
                DoTS(IncActHelper);
            }
            protected void IncActHelper() {
                i += 1;
            }

            public void DelayedIncEnum(int millisecondsDelay) {
                DoInTS(millisecondsDelay, DelayedIncEnumHelper);
            }
            protected IEnumerator DelayedIncEnumHelper() {
                i += 1;
                yield break;
            }

            public void DelayedIncAct(int millisecondsDelay) {
                DoInTS(millisecondsDelay, DelayedIncActHelper);
            }
            protected void DelayedIncActHelper() {
                i += 1;
            }

            public void IncThreeTimesEnum(int delayMs, int intervalMs, uint? iterations) {
                DoRepeatingTS(delayMs, intervalMs, iterations, IncThreeTimesEnumHelper);
            }
            protected IEnumerator IncThreeTimesEnumHelper() {
                i += 1;
                yield break;
            }

            public void IncThreeTimesAct(int delayMs, int intervalMs, uint? iterations) {
                DoRepeatingTS(delayMs, intervalMs, iterations, IncThreeTimesActHelper);
            }
            protected void IncThreeTimesActHelper() {
                i += 1;
            }

            public async Task DelayedIncAndWaitEnum(int millisecondsDelay) {
                await DoWaitForTS(DelayedIncAndWaitEnumHelper, millisecondsDelay);
            }
            protected IEnumerator DelayedIncAndWaitEnumHelper(int millisecondsDelay) {
                yield return InterfaceManager.DelayE(1000);
                i += 1;
            }

            public async Task DelayedIncAndWaitAct(int millisecondsDelay) {
                await DoWaitForTS(DelayedIncAndWaitActHelper, millisecondsDelay);
            }
            protected void DelayedIncAndWaitActHelper(int millisecondsDelay) {
                var start = Clock.UtcNow;
                while ((Clock.UtcNow - start).TotalMilliseconds < 1000.0) ;
                i += 1;
            }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
            public Task DelayedIncAndWaitTask(int millisecondsDelay) {
                return DoWaitForTS(DelayedIncAndWaitTaskHelper, millisecondsDelay);
            }
            protected async Task DelayedIncAndWaitTaskHelper(int millisecondsDelay) {
                await InterfaceManager.Delay(1000);
                i += 1;
            }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS


#if EVENTMONOBEHAVIOR_MANUAL_RESULT_SET
            public Task DelayedIncAndWaitManualTriggerEnum(int millisecondsDelay) {
                return DoWaitForManualTriggerTS(DelayedIncAndWaitManualTriggerEnumHelper, millisecondsDelay);
            }
            protected IEnumerator DelayedIncAndWaitManualTriggerEnumHelper(TaskCompletionSource<bool> tcs, int millisecondsDelay) {
                yield return InterfaceManager.DelayE(1000);
                i += 1;
                tcs.SetResult(true);
            }

            public Task DelayedIncAndWaitManualTriggerAct(int millisecondsDelay) {
                return DoWaitForManualTriggerTS(DelayedIncAndWaitManualTriggerActHelper, millisecondsDelay);
            }
            protected void DelayedIncAndWaitManualTriggerActHelper(TaskCompletionSource<bool> tcs, int millisecondsDelay) {
                var start = Clock.UtcNow;
                while ((Clock.UtcNow - start).TotalMilliseconds < 1000.0) ;
                i += 1;
                tcs.SetResult(true);
            }

#if EVENTMONOBEHAVIOR_TASK_OPERATORS
            public Task DelayedIncAndWaitManualTriggerTask(int millisecondsDelay) {
                return DoWaitForManualTriggerTS(DelayedIncAndWaitManualTriggerTaskHelper, millisecondsDelay);
            }
            protected async Task DelayedIncAndWaitManualTriggerTaskHelper(TaskCompletionSource<bool> tcs, int millisecondsDelay) {
                await InterfaceManager.Delay(1000);
                i += 1;
                tcs.SetResult(true);
            }
#endif // EVENTMONOBEHAVIOR_TASK_OPERATORS
#endif // EVENTMONOBEHAVIOR_MANUAL_RESULT_SET
        }
    }

}


