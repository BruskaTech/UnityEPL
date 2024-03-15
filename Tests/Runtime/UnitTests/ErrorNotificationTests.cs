//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania

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
using UnityEngine.UI;

using UnityEPL;

namespace UnityEPLTests {

    public class ErrorNotifierTests {
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
        public IEnumerator MakeErrorNotification() {
            var inputText = "TESTING";

            Assert.Throws<Exception>(() => {
                ErrorNotifier.ErrorTS(new Exception(inputText));
            });

            yield return null; // Wait for next frame
            var actualText = TextDisplayer.Instance.transform
                .Find("Black Background").Find("Stimulus")
                .GetComponent<Text>().text;
            Assert.AreEqual(inputText, actualText);
            Assert.IsTrue(TextDisplayer.Instance.isActiveAndEnabled);
        }

        [UnityTest]
        public IEnumerator MakeWarningNotification() {
            var inputText = "TESTING";

            ErrorNotifier.WarningTS(new Exception(inputText));

            yield return null; // Wait for next frame
            var actualText = TextDisplayer.Instance.transform
                .Find("Black Background").Find("Stimulus")
                .GetComponent<Text>().text;
            Assert.AreEqual(inputText, actualText);
            Assert.IsTrue(TextDisplayer.Instance.isActiveAndEnabled);
        }

    }

}


