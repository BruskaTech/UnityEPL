//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections;
using UnityEngine;

namespace UnityEPL.Utilities {
    /// <summary>
    /// This allows you to call coroutines from a normal function (it blocks)
    /// This can only be used on the main thread (due to Unity restrictions)
    /// If you need to get around this, then add a UnityCoroutineRunner to InterfaceManager.
    /// This would need to be made thread safe though.
    /// </summary>
    public class UnityCoroutineRunner : MonoBehaviour {
        public static UnityCoroutineRunner Generate() {
            var gameObject = new GameObject();
            gameObject.isStatic = true;
            return gameObject.AddComponent<UnityCoroutineRunner>();
        }

        // This is blocking
        public void RunCoroutine(IEnumerator enumerator) {
            this.StartCoroutine(enumerator);
        }

        UnityCoroutineRunner() { }
        ~UnityCoroutineRunner() { Destroy(transform.gameObject); }
    }
}