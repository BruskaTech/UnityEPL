//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PsyForge {
    public static class Reflection {
        /// <summary>
        /// Get all constant values in the class as a dictionary
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, object> ConstValuesToDict<T>(T obj) {
            var dict = new Dictionary<string, object>();

            // Get all public static fields of the struct using reflection
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static);

            // Iterate through all static fields and check if they are constants
            foreach (FieldInfo field in fields) {
                // Only add fields that are constants
                if (field.IsLiteral && !field.IsInitOnly) {
                    dict[field.Name] = field.GetValue(obj);
                }
            }

            return dict;
        }

        /// <summary>
        /// Get all readonly values in the class as a dictionary
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, object> ReadOnlyValuesToDict<T>(T obj) {
            var dict = new Dictionary<string, object>();

            // Get all public instance fields of the class (including inherited fields) using reflection
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            // Iterate through all instance fields and add readonly fields
            // We go in reverse order so that we can override values if they are redefined in a subclass
            foreach (FieldInfo field in fields.Reverse()) {
                if (field.IsInitOnly) {
                    dict[field.Name] = field.GetValue(obj);
                }
            }

            return dict;
        }

        /// <summary>
        /// Get all Getter only values in the class as a dictionary
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, object> GetterOnlyValuesToDict<T>(T obj) {
            var dict = new Dictionary<string, object>();

            // Get all public instance properties of the class (including inherited properties) using reflection
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (PropertyInfo property in properties) {
                // Get the 'get' accessor method info
                MethodInfo getMethod = property.GetGetMethod();
                
                // Check if the property has a getter but no setter, is virtual or overriden, and is not sealed
                if (property.CanRead && !property.CanWrite && getMethod.IsVirtual && !getMethod.IsFinal) {
                    dict.Add(property.Name, property.GetValue(obj));
                }
            }

            return dict;
        }
    }
}