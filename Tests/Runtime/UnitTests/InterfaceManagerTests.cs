//Copyright (c) 2024 Jefferson University
//Copyright (c) 2024 Bruska Technologies LLC
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using UnityEPL;

namespace UnityEPLTests {

    public class InterfaceManagerTests {
        // -------------------------------------
        // Globals
        // -------------------------------------

        bool isSetup = false;

        const double ONE_FRAME_MS = 1000.0 / 120.0;
        const double DELAY_JITTER_MS = 9;
        // TODO: JPB: (bug) The acceptable jitter for InterfaceManager.Delay() should be less than 9ms

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
        }

        // -------------------------------------
        // General Tests
        // -------------------------------------

        [Test]
        public void Creation() {
            Assert.AreNotEqual(null, InterfaceManager.Instance);
        }

        // Async Delay has 9ms leniency (because it's bad)
        [Test]
        public void Delay() {
            Task.Run(async () => {
                var start = Clock.UtcNow;
                await InterfaceManager.Delay(1000);
                var diff = (Clock.UtcNow - start).TotalMilliseconds;
                Assert.GreaterOrEqual(diff, 1000);
                Assert.LessOrEqual(diff, 1000 + DELAY_JITTER_MS);
            }).Wait();
        }

        // Enumerator Delay has 9ms leniency (due to frame linking at 120fps)
        [UnityTest]
        public IEnumerator DelayE() {
            var start = Clock.UtcNow;
            yield return InterfaceManager.DelayE(1000);
            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + ONE_FRAME_MS);
        }

        // Enumerator Delay has 3ms leniency
        [UnityTest]
        public IEnumerator IEnumeratorDelay() {
            var start = Clock.UtcNow;
            yield return InterfaceManager.Delay(1000).ToEnumerator();
            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1003);
        }

        
    }

}


