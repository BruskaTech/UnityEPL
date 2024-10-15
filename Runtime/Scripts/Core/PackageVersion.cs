//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

// https://stackoverflow.com/a/68072397
// See https://docs.unity3d.com/Manual/PlatformDependentCompilation.html

using UnityEngine;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace UnityEPL {

    public class PackageVersion : ScriptableObject {
        [SerializeField] private string version;

        public static string Version() {
            var packageVersion = Resources.Load<PackageVersion>($"{nameof(PackageVersion)}");
            if (packageVersion == null) {
                throw new Exception("PackageVersion asset not found at runtime!");
            }
            return packageVersion.version;
        }

#if UNITY_EDITOR
        private static UnityEditor.PackageManager.PackageInfo PackageInfo() {
            // Get the path of the Package (this script itself belongs to the package's assemblies)
            var assembly = typeof(PackageVersion).Assembly;
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
        }
        private static string PackageFolder() {
            return Path.Combine(new string[2] { PackageInfo().assetPath, "Resources" });
        }
        private static string PackagePath() {
            return Path.Combine(new string[2] { PackageFolder(), $"{nameof(PackageVersion)}.asset" });
        }

        [InitializeOnLoadMethod] // Called automatically after open Unity and each recompilation
        private static void Init() {
            // Removing the callback before adding it makes sure it is only added once at a time
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object obj) {
            var version = PackageInfo().version;

            // Check if the asset already exists, if so, update it instead of recreating it
            PackageVersion asset = AssetDatabase.LoadAssetAtPath<PackageVersion>(PackagePath());
            if (asset == null) {
                asset = CreateInstance<PackageVersion>();
                asset.name = nameof(PackageVersion);
                asset.hideFlags = HideFlags.NotEditable; // Make it non-editable via the Inspector

                Directory.CreateDirectory(PackageFolder());
                AssetDatabase.CreateAsset(asset, PackagePath());
            }

            // Set the version and ensure it's saved
            asset.version = version;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }

}