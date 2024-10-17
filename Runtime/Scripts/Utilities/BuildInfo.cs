//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

// https://stackoverflow.com/a/68072397
// See https://docs.unity3d.com/Manual/PlatformDependentCompilation.html

using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace PsyForge.Utilities {

    public class BuildInfo : ScriptableObject {
        [SerializeField] private string packageVersion;
        [SerializeField] private string packageCommitHash;
        [SerializeField] private string applicationCommitHash;
        [SerializeField] private string buildDateTime;

        private static BuildInfo Instance() {
            var instance = Resources.Load<BuildInfo>($"{nameof(BuildInfo)}");
            if (instance == null) {
                throw new Exception("PackageVersion asset not found at runtime!");
            }
            return instance;
        }

        public static string PackageVersion() {
            return Instance().packageVersion;
        }
        public static string ApplicationVersion() {
            return Application.version;
        }
        public static string UnityVersion() {
            return Application.unityVersion;
        }
        public static string PackageCommitHash() {
            return Instance().packageCommitHash;
        }
        public static string ApplicationCommitHash() {
            return Instance().applicationCommitHash;
        }
        public static string BuildDateTime() {
            return Instance().buildDateTime;
        }


#if UNITY_EDITOR
        private static UnityEditor.PackageManager.PackageInfo PackageInfo() {
            // Get the path of the Package (this script itself belongs to the package's assemblies)
            var assembly = typeof(BuildInfo).Assembly;
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
        }
        private static string PackageFolder() {
            return Path.Combine(new string[2] { PackageInfo().assetPath, "Resources" });
        }
        private static string PackagePath() {
            return Path.Combine(new string[2] { PackageFolder(), $"{nameof(BuildInfo)}.asset" });
        }
        private static string GetGitCommitHash(string gitRootPath) {
            try {
                ProcessStartInfo processInfo = new ProcessStartInfo("git", "rev-parse HEAD") {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = gitRootPath//Application.dataPath // Set this to the root directory of your Unity project
                };

                using (Process process = Process.Start(processInfo)) {
                    process.WaitForExit();
                    if (process.ExitCode == 0) {
                        return process.StandardOutput.ReadToEnd().Trim();
                    } else {
                        throw new Exception("Failed to get Git commit hash.");
                    }
                }
            } catch (Exception e) {
                throw new Exception($"Exception when retrieving Git commit hash: {e.Message}");
            }
        }

        [InitializeOnLoadMethod] // Called automatically after open Unity and each recompilation
        private static void Init() {
            // Removing the callback before adding it makes sure it is only added once at a time
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object obj) {
            BuildInfo asset = AssetDatabase.LoadAssetAtPath<BuildInfo>(PackagePath());
            if (asset == null) {
                asset = CreateInstance<BuildInfo>();
                asset.name = nameof(BuildInfo);
                asset.hideFlags = HideFlags.NotEditable; // Make it non-editable via the Inspector

                Directory.CreateDirectory(PackageFolder());
                AssetDatabase.CreateAsset(asset, PackagePath());
            }

            // Set the version and ensure it's saved
            asset.packageVersion = PackageInfo().version;
            asset.packageCommitHash = GetGitCommitHash(PackageInfo().resolvedPath);
            asset.applicationCommitHash = GetGitCommitHash(Application.dataPath);
            asset.buildDateTime = DateTime.Now.ToString();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }

}