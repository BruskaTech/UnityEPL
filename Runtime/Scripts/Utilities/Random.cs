//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Threading;

namespace PsyForge.Utilities {

    /// <summary>
    /// Static class for random number generation.
    /// </summary>
    public static class Random {
        private static ThreadLocal<int> rndSeed = new(() => { return Environment.TickCount; });
        private static ThreadLocal<System.Random> rnd = new(() => { return new(RndSeed); });
        private static ThreadLocal<int> stableRndSeed = null;
        private static ThreadLocal<System.Random> stableRnd = null;

        /// <summary>
        /// Get the seed for Rnd for this thread.
        /// </summary>
        public static int RndSeed { get { return rndSeed.Value; } }

        /// <summary>
        /// Get the random number generator.
        /// This is a thread-local random number generator that is randomly seeded once per thread.
        /// It is useful for generating truly random numbers.
        /// </summary>
        public static System.Random Rnd { get { return rnd.Value; } }

        /// <summary>
        /// Get/Set the seed for the stable random number generator for this thread.
        /// </summary>
        public static int StableRndSeed { 
            get { 
                return stableRndSeed.Value;
            } 
            set { 
                if (stableRndSeed != null) {
                    throw new InvalidOperationException("StableRnd seed already set for this thread.");
                }
                stableRndSeed = new(() => value);
                stableRnd = new(() => new(value));
            }    
        }

        /// <summary>
        /// Get the stable random number generator.
        /// This is a thread-local random number generator that is seeded once per thread with a function call.
        /// It is useful for generating reproducible random numbers
        /// </summary>
        public static System.Random StableRnd { get {
            return stableRnd?.Value ?? throw new InvalidOperationException("StableRnd seed not set for this thread. Please assign a value to StableRndSeed first.");
        } }
    }

}
