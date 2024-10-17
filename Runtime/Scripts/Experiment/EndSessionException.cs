//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Runtime.Serialization;

namespace PsyForge.Experiment {

    // Using exceptions for ending trials is better because
    // 1) You may want to end a practice or the real trials at any time
    // 2) There is no hard timing constraint when ending a trial (so exceptions are okay)
    // TODO: JPB: (feature) Think about adding EndCurrentTrial to end a specific trial at any time
    //            This can be done by the user by adding a try catch into the TrialStates
    //            This may be a bad idea for state management reasons
    [Serializable]
    public class EndSessionException : Exception {
        public EndSessionException() 
            : base() { }
        public EndSessionException(string message) 
            : base(message) { }

        protected EndSessionException(SerializationInfo info, StreamingContext context) 
            : base (info, context) { }
    }

}