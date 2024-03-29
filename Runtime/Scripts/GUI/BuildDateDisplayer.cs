//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEPL {

    [RequireComponent(typeof(UnityEngine.UI.Text))]
    public class BuildDateDisplayer : EventMonoBehaviour {
        protected override void AwakeOverride() {}

        [Tooltip("Date/time format.")]
        public string format = "g";    // see: https://msdn.microsoft.com/en-us/library/az4se3k1%28v=vs.110%29.aspx

        void Start() {
            GetComponent<UnityEngine.UI.Text>().text = BuildInfo.ToString(format);
        }
    }
}
