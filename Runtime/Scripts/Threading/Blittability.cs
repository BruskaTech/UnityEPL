//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System;
using Unity.Collections.LowLevel.Unsafe;

using PsyForge.Utilities;

namespace PsyForge.Threading {
    public static class Blittability {
        // AssertBlittable

        public static bool IsPassable(Type t) {
            Type genericTypeDefinition = null;
            try {
                genericTypeDefinition = t.GetGenericTypeDefinition();
            } catch (InvalidOperationException) { }
            return UnsafeUtility.IsBlittable(t)
                | typeof(Mutex<>) == genericTypeDefinition;
        }

        // TODO: JPB: (feature) Maybe use IComponentData from com.unity.entities when it releases
        //            This will also allow for bool and char to be included in the structs
        //            https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.IComponentData.html
        public static void AssertBlittable<T>()
                where T : struct {
            if (!IsPassable(typeof(T))) {
                throw new ArgumentException($"The first argument is not a blittable type ({typeof(T)}).");
            }
        }
        public static void AssertBlittable<T, U>()
                where T : struct
                where U : struct {
            if (!IsPassable(typeof(T))) {
                throw new ArgumentException($"The first argument is not a blittable type ({typeof(T)}).");
            } else if (!IsPassable(typeof(U))) {
                throw new ArgumentException($"The second argument is not a blittable type ({typeof(U)}).");
            }
        }
        public static void AssertBlittable<T, U, V>()
                where T : struct
                where U : struct
                where V : struct {
            if (!IsPassable(typeof(T))) {
                throw new ArgumentException($"The first argument is not a blittable type ({typeof(T)}).");
            } else if (!IsPassable(typeof(U))) {
                throw new ArgumentException($"The second argument is not a blittable type ({typeof(U)}).");
            } else if (!IsPassable(typeof(V))) {
                throw new ArgumentException($"The third argument is not a blittable type ({typeof(V)}).");
            }
        }
        public static void AssertBlittable<T, U, V, W>()
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            if (!IsPassable(typeof(T))) {
                throw new ArgumentException($"The first argument is not a blittable type ({typeof(T)}).");
            } else if (!IsPassable(typeof(U))) {
                throw new ArgumentException($"The second argument is not a blittable type ({typeof(U)}).");
            } else if (!IsPassable(typeof(V))) {
                throw new ArgumentException($"The third argument is not a blittable type ({typeof(V)}).");
            } else if (!IsPassable(typeof(W))) {
                throw new ArgumentException($"The fourth argument is not a blittable type ({typeof(W)}).");
            }
        }
        public static void AssertBlittable<T, U, V, W, Z>()
                where T : struct
                where U : struct
                where V : struct
                where W : struct {
            if (!IsPassable(typeof(T))) {
                throw new ArgumentException($"The first argument is not a blittable type ({typeof(T)}).");
            } else if (!IsPassable(typeof(U))) {
                throw new ArgumentException($"The second argument is not a blittable type ({typeof(U)}).");
            } else if (!IsPassable(typeof(V))) {
                throw new ArgumentException($"The third argument is not a blittable type ({typeof(V)}).");
            } else if (!IsPassable(typeof(W))) {
                throw new ArgumentException($"The fourth argument is not a blittable type ({typeof(W)}).");
            } else if (!IsPassable(typeof(Z))) {
                throw new ArgumentException($"The fifth argument is not a blittable type ({typeof(Z)}).");
            }
        }
    }
}