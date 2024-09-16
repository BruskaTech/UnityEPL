//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEPL.Experiment {
    /// <summary>
    /// Class to hold constants for an experiment.
    /// All values in this class should be 'public readonly'
    /// Any other types of values will be ignored when logging the values.
    /// 
    /// I wish I could use 'public const' but I can't because it would fall apart 
    ///     if you make an experiment that acts as a base class for other experiments.
    /// This is because you have to access constants through the type (not an instance)
    ///     and you can't do it throught a type parameter or you get the error:
    /// Cannot do non-virtual member lookup in 'Constants' because it is a type parameter (CS0704)
    /// 
    /// I also wish I could have at least used a readonly struct here, 
    ///     but I can't because you can't inherit from a struct.
    /// </summary>
    public class ExperimentConstants {
        public Dictionary<string, object> ToDict() {
            return GetReadOnlyValues();
        }

        /// <summary>
        /// Get all constant values in the class as a dictionary
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, object> GetConstValues() {
            var dict = new Dictionary<string, object>();

            // Get all public static fields of the struct using reflection
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Static);

            // Iterate through all static fields and check if they are constants
            foreach (FieldInfo field in fields) {
                // Only add fields that are constants
                if (field.IsLiteral && !field.IsInitOnly) {
                    dict.Add(field.Name, field.GetRawConstantValue());
                }
            }

            return dict;
        }

        /// <summary>
        /// Get all readonly values in the class as a dictionary
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, object> GetReadOnlyValues() {
            var dict = new Dictionary<string, object>();

            // Get all public instance fields of the class (including inherited fields) using reflection
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            // Iterate through all instance fields and add readonly fields
            foreach (FieldInfo field in fields) {
                if (field.IsInitOnly) {
                    dict.Add(field.Name, field.GetValue(this));
                }
            }

            return dict;
        }
    }
}