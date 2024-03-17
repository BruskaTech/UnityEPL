//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.IO;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;


public static class FlexibleConfig {
    public static IDictionary<string, object> LoadFromText(string json) {
        JObject cfg = JObject.Parse(json);
        return CastToStatic(cfg);
    }

    public static void WriteToText(object data, string filename) {
        JsonSerializer serializer = new JsonSerializer();

        using (StreamWriter sw = new StreamWriter(filename))
        using (JsonWriter writer = new JsonTextWriter(sw)) {
            serializer.Serialize(writer, data);
        }
    }

    public static IDictionary<string, object> CastToStatic(JObject cfg) {
        // casts a JObject consisting of simple types (int, bool, string,
        // float, and single dimensional arrays) to a C# expando object, obviating
        // the need for casts to work in C# native types

        var settings = new ExpandoObject();  // dynamic

        foreach (JProperty prop in cfg.Properties()) {
            // convert from JObject types to .NET internal types
            // and add to dynamic settings object
            // if JSON contains arrays, we need to peek at the
            // type of the contents to get the right cast, as
            // C# doesn't implicitly cast the contents of an
            // array when casting the array

            if (prop.Value is JArray) {
                JTokenType jType = JTokenType.None;

                foreach (JToken child in prop.Value.Children()) {
                    if (jType == JTokenType.None) {
                        jType = child.Type;
                    } else if (jType != child.Type) {
                        throw new Exception("Mixed type arrays not supported. If using floats, make sure that they all have a decimal point.");
                    }
                }

                Type cType = JTypeConversion((int)jType);
                if (cType == typeof(string)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<string[]>());
                } else if (cType == typeof(int)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<int[]>());
                } else if (cType == typeof(float)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<float[]>());
                } else if (cType == typeof(bool)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<bool[]>());
                }
            } else {
                Type cType = JTypeConversion((int)prop.Value.Type);
                if (cType == typeof(string)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<string>());
                } else if (cType == typeof(int)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<int>());
                } else if (cType == typeof(float)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<float>());
                } else if (cType == typeof(bool)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<bool>());
                }
            }
        }
        return (IDictionary<string, object>)settings;
    }

    public static Type JTypeConversion(int t) {
        return t switch {
            6 => typeof(int),
            7 => typeof(float),
            8 => typeof(string),
            9 => typeof(bool),
            _ => throw new Exception("Unsupported Type"),
        };
    }
}

// This is a dynamic version of the FlexibleConfig class
// This cannot be used right now because WebGL does not support the dynamic keyword
// The moment it does, this version of the class should be used instead of the one above
// Other code will need to be restructured when this is used
// Make sure it stays thread safe though

//public static class FlexibleConfig {
//    public static dynamic LoadFromText(string json) {
//        JObject cfg = JObject.Parse(json);
//        return CastToStatic(cfg);
//    }

//    public static void WriteToText(dynamic data, string filename) {
//    JsonSerializer serializer = new JsonSerializer();

//    using (StreamWriter sw = new StreamWriter(filename))
//      using (JsonWriter writer = new JsonTextWriter(sw))
//      {
//        serializer.Serialize(writer, data);
//      }
//    }

//    private static dynamic CastToStatic(JObject cfg) {
//        // casts a JObject consisting of simple types (int, bool, string,
//        // float, and single dimensional arrays) to a C# expando object, obviating
//        // the need for casts to work in C# native types

//        dynamic settings = new ExpandoObject();

//        foreach(JProperty prop in cfg.Properties()) {
//            // convert from JObject types to .NET internal types
//            // and add to dynamic settings object
//            // if JSON contains arrays, we need to peek at the
//            // type of the contents to get the right cast, as
//            // C# doesn't implicitly cast the contents of an
//            // array when casting the array

//            if(prop.Value is Newtonsoft.Json.Linq.JArray) {
//                JTokenType jType = JTokenType.None;

//                foreach(JToken child in prop.Value.Children()) {
//                    if(jType == JTokenType.None) {
//                        jType = child.Type;
//                    }
//                    else if (jType != child.Type) {
//                        throw new Exception("Mixed type arrays not supported");
//                    }
//                }

//                Type cType = JTypeConversion((int)jType);
//                if(cType  == typeof(string)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<string[]>());
//                }
//                else if(cType == typeof(int)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<int[]>());
//                }
//                else if(cType == typeof(float)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<float[]>());
//                }
//                else if(cType == typeof(bool)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<bool[]>());
//                }
//            }
//            else {
//                Type cType = JTypeConversion((int)prop.Value.Type);
//                if(cType == typeof(string)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<string>());
//                }
//                else if(cType == typeof(int)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<int>());
//                }
//                else if(cType == typeof(float)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<float>());
//                }
//                else if(cType == typeof(bool)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<bool>());
//                }
//            }
//        }

//        return settings;
//    }

//    private static Type JTypeConversion(int t) {
//        switch(t) {
//            case 6:
//                return typeof(int);
//            case 7:
//                return typeof(float);
//            case 8:
//                return typeof(string);
//            case 9:
//                return typeof(bool);
//            default:
//                throw new Exception("Unsupported Type");
//        }
//    }
//}
