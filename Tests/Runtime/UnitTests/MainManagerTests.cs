//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

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
using UnityEPL.Extensions;
using UnityEPL.Utilities;

namespace UnityEPLTests {

    public class MainManagerTests {
        // -------------------------------------
        // Globals
        // -------------------------------------

        bool isSetup = false;

        const double ONE_FRAME_MS = 1000.0 / 120.0;

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
        // General Tests
        // -------------------------------------

        [Test]
        public void Creation() {
            Assert.AreNotEqual(null, MainManager.Instance);
        }

        // Async Delay has 9ms leniency (because it's bad)
        [UnityTest]
        public IEnumerator Delay() {
            var start = Clock.UtcNow;
            yield return MainManager.Instance.Delay(1000).ToEnumerator();
            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + ONE_FRAME_MS);
        }

        // Enumerator Delay has 9ms leniency (due to frame linking at 120fps)
        [UnityTest]
        public IEnumerator DelayE() {
            var start = Clock.UtcNow;
            yield return MainManager.Instance.DelayE(1000);
            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + ONE_FRAME_MS);
        }

        // Enumerator Delay has 3ms leniency
        [UnityTest]
        public IEnumerator IEnumeratorDelay() {
            var start = Clock.UtcNow;
            yield return MainManager.Instance.Delay(1000).ToEnumerator();
            var diff = (Clock.UtcNow - start).TotalMilliseconds;
            Assert.GreaterOrEqual(diff, 1000);
            Assert.LessOrEqual(diff, 1000 + ONE_FRAME_MS);
        }

        
    }

}


