//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using PsyForge;
using PsyForge.Threading;
using PsyForge.Utilities;

namespace PsyForgeTests {

    // TODO: JPB: (refactor) Convert all EventLoopTests to async Task and remove Task.Run

    public class EventLoopTests {
        // -------------------------------------
        // Globals
        // -------------------------------------

        bool isSetup = false;

        const double DELAY_JITTER_MS = 2;
        MainManager im = new();


        // -------------------------------------
        // Setup
        // -------------------------------------
        [UnitySetUp]
        public IEnumerator Setup() {
            if (!isSetup) {
                isSetup = true;
                SceneManager.LoadScene("manager");
                yield return null; // Wait for MainManager Awake call
            }
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

        [Test]
        public void DoGetFunc() {
            Task.Run(async () => {
                var el = new EL();
                int i = el.mutex.Get();

                var result = await el.GetMutexValFunc();
                Assert.AreEqual(i, result); // Didn't mutate state
                Assert.AreEqual(el.mutex.Get(), result); // Didn't mutate state but cached value

                el.mutex.Mutate((int i) => { return i + 1; });

                result = await el.GetMutexValFunc();
                Assert.AreEqual(i + 1, result); // Didn't mutate state
                Assert.AreEqual(el.mutex.Get(), result); // Didn't mutate state but cached value
            }).Wait();
        }

        [Test]
        public void DoGetTask() {
            Task.Run(async () => {
                var el = new EL();
                int i = el.mutex.Get();

                var result = await el.GetMutexValTask();
                Assert.AreEqual(i, result); // Didn't mutate state
                Assert.AreEqual(el.mutex.Get(), result); // Didn't mutate state but cached value
            
                el.mutex.Mutate((int i) => { return i + 1; });

                result = await el.GetMutexValTask();
                Assert.AreEqual(i + 1, result); // Didn't mutate state
                Assert.AreEqual(el.mutex.Get(), result); // Didn't mutate state but cached value
            }).Wait();
        }

        // -------------------------------------
        // The rest of the tests
        // -------------------------------------

        [Test]
        public void DoAct() {
            Task.Run(async () => {
                var el = new EL();
                var i = await el.GetI();
                Debug.Log(i);

                el.IncAct();
                Debug.Log(i);
                Debug.Log(await el.GetI());

                Assert.AreEqual(i + 1, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoTask() {
            Task.Run(async () => {
                var el = new EL();
                var i = await el.GetI();

                el.IncTask();

                Assert.AreEqual(i + 1, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoInAct() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                el.DelayedIncAct(1000);

                await Task.Delay(900);
                Assert.AreEqual(i, await el.GetI());

                await Task.Delay(200);
                Assert.AreEqual(i + 1, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoInTask() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                el.DelayedIncTask(1000);

                await Task.Delay(900);
                Assert.AreEqual(i, await el.GetI());

                await Task.Delay(200);
                Assert.AreEqual(i + 1, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoRepeatingAct() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                el.IncThreeTimesAct(0, 1000, 3);

                Assert.AreEqual(i + 1, await el.GetI());

                await Task.Delay(900);
                Assert.AreEqual(i + 1, await el.GetI());

                await Task.Delay(200);
                Assert.AreEqual(i + 2, await el.GetI());

                await Task.Delay(1000);
                Assert.AreEqual(i + 3, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoRepeatingDelayedAct() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                el.IncThreeTimesAct(1000, 1000, 3);

                Assert.AreEqual(i, await el.GetI());

                await Task.Delay(900);
                Assert.AreEqual(i, await el.GetI());

                await Task.Delay(200);
                Assert.AreEqual(i + 1, await el.GetI());

                await Task.Delay(1000);
                Assert.AreEqual(i + 2, await el.GetI());

                await Task.Delay(1000);
                Assert.AreEqual(i + 3, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoRepeatingTask() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                el.IncThreeTimesTask(0, 1000, 3);

                Assert.AreEqual(i + 1, await el.GetI());

                await Task.Delay(900);
                Assert.AreEqual(i + 1, await el.GetI());

                await Task.Delay(200);
                Assert.AreEqual(i + 2, await el.GetI());

                await Task.Delay(1000);
                Assert.AreEqual(i + 3, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoRepeatingDelayedTask() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                el.IncThreeTimesTask(1000, 1000, 3);

                Assert.AreEqual(i, await el.GetI());

                await Task.Delay(900);
                Assert.AreEqual(i, await el.GetI());

                await Task.Delay(200);
                Assert.AreEqual(i + 1, await el.GetI());

                await Task.Delay(1000);
                Assert.AreEqual(i + 2, await el.GetI());

                await Task.Delay(1000);
                Assert.AreEqual(i + 3, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoWaitForAct() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                var start = Clock.UtcNow;
                await el.DelayedIncAndWaitAct(1000);

                var diff = (Clock.UtcNow - start).TotalMilliseconds;
                Assert.GreaterOrEqual(diff, 1000);
                Assert.LessOrEqual(diff, 1000 + DELAY_JITTER_MS);

                Assert.AreEqual(i + 1, await el.GetI());
            }).Wait();
        }

        [Test]
        public void DoWaitForTask() {
            Task.Run(async () => {
                var el = new EL();
                int i = await el.GetI();

                var start = Clock.UtcNow;
                await el.DelayedIncAndWaitTask(1000);

                var diff = (Clock.UtcNow - start).TotalMilliseconds;
                Assert.GreaterOrEqual(diff, 1000);
                Assert.LessOrEqual(diff, 1000 + DELAY_JITTER_MS);

                Assert.AreEqual(i + 1, await el.GetI());
            }).Wait();
        }
    }


    class EL : EventLoop {
        public Mutex<int> mutex = new Mutex<int>(0);
        protected int i = 0;


        // Test DoGet and DoGetManualTrigger with mutex

        public async Task<int> GetMutexValFunc() {
            return await DoGetTS<int>(GetMutexValFuncHelper);
        }
        protected int GetMutexValFuncHelper() {
            return mutex.Get();
        }

        public async Task<int> GetMutexValTask() {
            return await DoGetTS<int>(GetMutexValTaskHelper);
        }
        protected async Task<int> GetMutexValTaskHelper() {
            await Task.Delay(1);
            return mutex.Get();
        }


        // Test the rest of the functions with i and GetI

        public async Task<int> GetI() {
            return await DoGetTS<int>(GetIHelper);
        }
        protected int GetIHelper() {
            return i;
        }

        public void IncAct() {
            Debug.Log("IncAct");
            DoTS(IncActHelper);
        }
        protected void IncActHelper() {
            Debug.Log("IncActHelper");
            i += 1;
        }

        public void IncTask() {
            DoTS(IncTaskHelper);
        }
        protected Task IncTaskHelper() {
            i += 1;
            return Task.CompletedTask;
        }

        public void DelayedIncAct(int millisecondsDelay) {
            DoInTS(millisecondsDelay, DelayedIncActHelper);
        }
        protected void DelayedIncActHelper() {
            i += 1;
        }

        public void DelayedIncTask(int millisecondsDelay) {
            DoInTS(millisecondsDelay, DelayedIncTaskHelper);
        }
        protected Task DelayedIncTaskHelper() {
            i += 1;
            return Task.CompletedTask;
        }

        public void IncThreeTimesAct(int delayMs, int intervalMs, uint? iterations) {
            DoRepeatingTS(delayMs, intervalMs, iterations, IncThreeTimesActHelper);
        }
        protected void IncThreeTimesActHelper() {
            i += 1;
        }

        public void IncThreeTimesTask(int delayMs, int intervalMs, uint? iterations) {
            DoRepeatingTS(delayMs, intervalMs, iterations, IncThreeTimesTaskHelper);
        }
        protected Task IncThreeTimesTaskHelper() {
            i += 1;
            return Task.CompletedTask;
        }

        public async Task DelayedIncAndWaitAct(int millisecondsDelay) {
            await DoWaitForTS(DelayedIncAndWaitActHelper, millisecondsDelay);
        }
        protected void DelayedIncAndWaitActHelper(int millisecondsDelay) {
            var start = Clock.UtcNow;
            while ((Clock.UtcNow - start).TotalMilliseconds < 1000.0) ;
            i += 1;
        }

        public async Task DelayedIncAndWaitTask(int millisecondsDelay) {
            await DoWaitForTS(DelayedIncAndWaitTaskHelper, millisecondsDelay);
        }
        protected async Task DelayedIncAndWaitTaskHelper(int millisecondsDelay) {
            await Task.Delay(1000);
            i += 1;
        }
    }
}
