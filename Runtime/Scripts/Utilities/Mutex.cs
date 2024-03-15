//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Threading;

namespace UnityEPL {

    public class Mutex<T> {
        private T val;
        private Mutex mutex;

        public Mutex(T val) {
            this.val = val;
            this.mutex = new Mutex();
        }

        public void Mutate(Func<T, T> mutator) {
            mutex.WaitOne();
            val = mutator(val);
            mutex.ReleaseMutex();
        }

        public ReadOnly<T> Get() {
            mutex.WaitOne();
            var ret = new ReadOnly<T>(val);
            mutex.ReleaseMutex();
            return ret;
        }

        public ReadOnly<T> MutateGet(Func<T, T> mutator) {
            mutex.WaitOne();
            val = mutator(val);
            var ret = new ReadOnly<T>(val);
            mutex.ReleaseMutex();
            return ret;
        }
    }

    public ref struct ReadOnly<T> {
        private T val;

        public ReadOnly(T val) {
            this.val = val;
        }

        public static implicit operator T(ReadOnly<T> ro) {
            // TODO: JPB: (bug) Use Wait to check against Mutex
            return ro.val;
        }
    }

}
