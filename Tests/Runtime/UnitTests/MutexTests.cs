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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using UnityEPL;

namespace UnityEPLTests {

    public class MutexTests {
        [Test]
        public void MutexSafety() {
            Mutex<Int32> m = new(0);
            var a = m.Get();
            a = new ReadOnly<Int32>(1);
            Assert.AreNotEqual((Int32)m.Get(), (Int32)a);
        }

        [Test]
        public void MutexMutation() {
            Mutex<Int32> m = new(0);
            Assert.AreEqual(0, (int)m.Get());
            m.Mutate((int i) => { return i + 1; });
            Assert.AreEqual(1, (int)m.Get());
        }
    }

}
