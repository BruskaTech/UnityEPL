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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using UnityEPL;

namespace UnityEPLTests {

    public class SingletonEventMonoBehaviorTests {
        // -------------------------------------
        // Globals
        // -------------------------------------

        bool isSetup = false;

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

        [UnityTest]
        public IEnumerator Creation() {
            var semb = new GameObject().AddComponent<SEMB>();
            Assert.AreNotEqual(null, semb);
            GameObject.Destroy(semb);

            yield break;
        }

        [UnityTest]
        public IEnumerator Singleton() {
            var semb = new GameObject().AddComponent<SEMB2>();
            Assert.AreNotEqual(null, semb);

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: .*"));
            var failing = new GameObject().AddComponent<SEMB2>();

            GameObject.Destroy(semb);
            GameObject.Destroy(failing);

            yield break;
        }

        class SEMB : SingletonEventMonoBehaviour<SEMB> {
            protected override void AwakeOverride() { }
        }

        class SEMB2 : SingletonEventMonoBehaviour<SEMB2> {
            protected override void AwakeOverride() { }
        }

    }

}


