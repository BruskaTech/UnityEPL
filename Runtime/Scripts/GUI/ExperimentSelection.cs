//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PsyForge.GUI {

    /// <summary>
    /// This is attached to the dropdown menu which selects experiments.
    /// 
    /// It only needs to call PsyForge.SetExperimentName().
    /// </summary>
    [RequireComponent(typeof(Dropdown))]
    public class ExperimentSelection : EventMonoBehaviour {
        protected override void AwakeOverride() {
            Dropdown dropdown = GetComponent<Dropdown>();

            List<string> experiments = new(Config.availableExperiments);

            dropdown.AddOptions(new List<string>(new string[] { "Select Task..." }));
            dropdown.AddOptions(experiments);
            SetExperiment();
        }

        public void SetExperiment() {
            Do(SetExperimentHelper);
        }
        protected void SetExperimentHelper() {
            Dropdown dropdown = GetComponent<Dropdown>();

            if (dropdown.captionText.text != "Select Task...") {
                Config.experimentConfigName = dropdown.captionText.text;
                Config.SetupExperimentConfig();
            }
        }
    }

}