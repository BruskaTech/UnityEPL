//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEPL.Extensions;

namespace UnityEPL.Threading {

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

    public static class NativeExtensions {
        public static NativeText ToNativeText(this string s) {
            return new NativeText(s, Allocator.Persistent);
        }
        public static string ToStringAndDispose(this NativeText nativeText) {
            var s = nativeText.ToString();
            nativeText.Dispose();
            return s;
        }
        public static NativeArray<NativeText> ToNativeArray(this string[] strings) {
            var nativeArray = new NativeArray<NativeText>(strings.Length, Allocator.Persistent);
            for (int i = 0; i < strings.Length; i++) {
                nativeArray[i] = strings[i].ToNativeText();
            }
            return nativeArray;
        }
        public static string[] ToArrayAndDispose(this NativeArray<NativeText> nativeArray) {
            var strings = new string[nativeArray.Length];
            for (int i = 0; i < nativeArray.Length; i++) {
                strings[i] = nativeArray[i].ToString();
                nativeArray[i].Dispose();
            }
            nativeArray.Dispose();
            return strings;
        }
        public static NativeArray<NativeText> ToNativeArray(this List<string> strings) {
            var nativeArray = new NativeArray<NativeText>(strings.Count, Allocator.Persistent);
            for (int i = 0; i < strings.Count; i++) {
                nativeArray[i] = strings[i].ToNativeText();
            }
            return nativeArray;
        }
        public static List<string> ToListAndDispose(this NativeArray<NativeText> nativeArray) {
            var strings = new List<string>(nativeArray.Length);
            for (int i = 0; i < nativeArray.Length; i++) {
                strings.Add(nativeArray[i].ToString());
                nativeArray[i].Dispose();
            }
            nativeArray.Dispose();
            return strings;
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

        public double ConvertToMillisecondsSinceEpoch() {
            return Value.ConvertToMillisecondsSinceEpoch();
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
}