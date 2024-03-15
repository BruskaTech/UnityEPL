//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;

namespace UnityEPL {

    public abstract class SingletonEventMonoBehaviour<T> : EventMonoBehaviour
            where T : SingletonEventMonoBehaviour<T> {

        protected static bool IsInstatiated { get; private set; } = false;
        private static T _Instance;
        public static T Instance {
            get {
                if (_Instance == null) {
                    throw new InvalidOperationException($"{typeof(T).Name} SingletonEventMonoBehavior has not initialized. Accessed before it's awake method has been called.");
                }
                return _Instance;
            }
            private set { }
        }

        protected SingletonEventMonoBehaviour() {
            if (typeof(T) == typeof(InterfaceManager)) {
                _Instance = (T)this;
            }
        }

        protected new void Awake() {
            if (IsInstatiated) {
                ErrorNotifier.ErrorTS(new InvalidOperationException($"Cannot create multiple {typeof(T).Name} Objects"));
            }
            IsInstatiated = true;
            _Instance = (T)this;
            DontDestroyOnLoad(this.gameObject);

            base.Awake();
        }
    }
}