//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;

namespace UnityEPL {

    // Information on blittability
    // https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types

    // TODO: JPB: (refactor) Remove Bool and Char structs once we have blittable bools/chars or use IComponentData

    public struct Bool {
        private byte _val;
        public Bool(bool b) {
            if (b) {
                _val = 1;
            } else {
                _val = 0;
            }
        }

        public static implicit operator bool(Bool b) => b._val == 1;
        public static implicit operator Bool(bool b) => new Bool(b);

        public static bool operator true(Bool b) => b._val == 1;
        public static bool operator false(Bool b) => b._val == 0;

        public static bool operator ==(Bool x, Bool y) => x._val == y._val;
        public static bool operator !=(Bool x, Bool y) => !(x == y);
        public static bool operator ==(Bool x, bool y) => (y && x) || (!x && !y);
        public static bool operator !=(Bool x, bool y) => !(x == y);
        public static bool operator ==(bool x, Bool y) => (y && x) || (!x && !y);
        public static bool operator !=(bool x, Bool y) => !(x == y);

        public override string ToString() => ((bool)this).ToString();

        public override bool Equals(object obj) {
            return (obj is Bool b) && (_val == b._val);
        }

        public override int GetHashCode() {
            return HashCode.Combine(_val);
        }
    }

    public struct Char {
        private UInt16 _val;
        public Char(char c) {
            _val = (UInt16)c;
        }
        public static implicit operator char(Char c) => (char)c._val;
        public static implicit operator Char(char c) => new Char(c);
        public override string ToString() => ((char)_val).ToString();
    }

    public static class UnityEplExtensions {
        public static NativeText ToNativeText(this string s) {
            return new NativeText(s, Allocator.Persistent);
        }
    }

    // Blittable DateTime
    public struct BlitDateTime {
        private long ticks;

        public DateTime Value {
            get { return new(ticks); }
            set { ticks = value.Ticks; }
        }

        public static implicit operator BlitDateTime(DateTime dt) {
            return new() { Value = dt };
        }
        public static implicit operator DateTime(BlitDateTime bdt) {
            return bdt.Value;
        }

        public override string ToString() {
            return new DateTime(ticks).ToString();
        }
        public string ToString(string format) {
            return new DateTime(ticks).ToString(format);
        }
        public string ToString(IFormatProvider formatProvider) {
            return new DateTime(ticks).ToString(formatProvider);
        }
        public string ToString(string format, IFormatProvider formatProvider) {
            return new DateTime(ticks).ToString(format, formatProvider);
        }
    }

    

    public static class ByteArray {
        private static byte[] ObjectToByteArray(object obj) {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream()) {
                bf.Serialize(ms, obj);

                return ms.ToArray();
            }
        }
        private static object ByteArrayToObject(byte[] arrBytes) {
            using (var memStream = new MemoryStream()) {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
    }

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