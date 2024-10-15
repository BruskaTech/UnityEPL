//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using UnityEPL.DataManagement;
using UnityEPL.GUI;

namespace UnityEPL.Experiment {
    public class ExperimentSession<TrialType> {
        // public int NumTrials { get; protected set; } = 0;
        // public TrialType Trial { get; protected set; }
        public uint TrialNum = 1;
        public bool isPractice = false;

        protected MainManager manager;
        protected TextDisplayer textDisplayer;
        protected EventReporter eventReporter;
        protected InputManager inputManager;

        public ExperimentSession() {
            manager = MainManager.Instance;
            textDisplayer = TextDisplayer.Instance;
            eventReporter = EventReporter.Instance;
            inputManager = InputManager.Instance;
        }
    }
}