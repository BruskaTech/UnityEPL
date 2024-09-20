//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Linq;
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
    public abstract class ExperimentConstants {
        public ExperimentConstants() {
            ValidateClass();
        }

        private void ValidateClass() {
            var currentType = GetType();

            // Get all public property Getters of the current class (including inherited properties)
            var allProperties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var allPropertyGetters = allProperties.Select(property => property.GetGetMethod());

            // Check that all properties are virtual (or overridden) and not final
            var allMistypedGetters = allProperties
                .Where(property => !property.GetGetMethod().IsVirtual || property.GetGetMethod().IsFinal);
            if (allMistypedGetters.Count() > 0) {
                throw new Exception(
                    $"ExperimentConstants classes must only contain public virtual (or overridden) properties.\nThese properties of {currentType.Name} are not virtual or are final: "
                    + string.Join(", ", allMistypedGetters.Select(property => property.Name)));
            }

            // Check that no properties have Setters
            var allPropertySetters = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Select(property => property.GetSetMethod()).Where(setter => setter != null);
            if (allPropertySetters.Count() > 0) {
                throw new Exception(
                    $"ExperimentConstants classes must not contain any public setters.\nThese properties of {currentType.Name} have public setters: "
                    + string.Join(", ", allPropertySetters.Select(setter => setter.Name)));
            }

            // Get all public members of the current class (including inherited members)
            // Ignore the constructor, the propertyGetters, the default methods (Equals, GetHashCode, GetType, ToString), and the ToDict method
            var allMembers = currentType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(member => member.MemberType != MemberTypes.Constructor
                    && !allPropertyGetters.Contains(member)
                    && !(new string[5] {"Equals", "GetHashCode", "GetType", "ToString", "ToDict"}.Contains(member.Name) && member.MemberType == MemberTypes.Method));

            // Check that there are no static values
            // Ignore the default methods (Equals, ReferenceEquals)
            var allStaticMembers = currentType.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(member => !(new string[2] {"Equals", "ReferenceEquals"}.Contains(member.Name) && member.MemberType == MemberTypes.Method));
            if (allStaticMembers.Count() > 0) {
                throw new Exception(
                    $"ExperimentConstants classes must not contain any static members.\nThese static members of {currentType.Name} were found: "
                    + string.Join(", ", allStaticMembers.Select(member => member.Name)));
            }

            // Check that every base property is overridden in the derived class
            var baseProperties = currentType.BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var currentProperties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var nonOverridenProperties = baseProperties
                .Where(baseProperty => !currentProperties.Any(currentProperty =>
                    currentProperty.Name == baseProperty.Name
                    && currentProperty.PropertyType == baseProperty.PropertyType
                    && currentProperty.DeclaringType != currentType.BaseType));
            if (nonOverridenProperties.Count() > 0) {
                throw new Exception(
                    $"ExperimentConstants classes must override all properties from the base class.\nThese properties of {currentType.Name} are not overridden: "
                    + string.Join(", ", nonOverridenProperties.Select(property => property.Name)));
            }
        }

        public Dictionary<string, object> ToDict() {
            return Reflection.ReadOnlyValuesToDict(this);
        }
    }
}