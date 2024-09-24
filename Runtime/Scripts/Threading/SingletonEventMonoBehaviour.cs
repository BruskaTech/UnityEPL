//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;

namespace UnityEPL {

    public abstract class SingletonEventMonoBehaviour<Self> : EventMonoBehaviour
            where Self : SingletonEventMonoBehaviour<Self> {

        protected static bool IsInstatiated { get; private set; } = false;
        private static Self _Instance;
        public static Self Instance {
            get {
                if (_Instance == null) {
                    throw new InvalidOperationException($"{typeof(Self).Name} SingletonEventMonoBehavior has not initialized. Accessed before it's awake method has been called.");
                }
                return _Instance;
            }
        }

        protected SingletonEventMonoBehaviour() {
            _Instance = (Self)this;
        }

        protected new void Awake() {
            if (IsInstatiated) {
                ErrorNotifier.ErrorTS(new InvalidOperationException($"Cannot create multiple {typeof(Self).Name} Objects"));
            }
            IsInstatiated = true;
            DontDestroyOnLoad(this.gameObject);

            base.Awake();
        }
    }
}