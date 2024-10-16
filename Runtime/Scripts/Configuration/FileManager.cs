//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEPL {

    //////////
    // Classes to manage the filesystem in
    // which experiment data is stored
    /////////
    // TODO: JPB: (needed) (refactor) Decide if FileManager should be an EventLoop
    public static class FileManager {

        public static string BasePath() {

#if UNITY_EDITOR
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
#else
            return Path.GetFullPath(".");
#endif
        }

        public static string DataPath() {
            return Config.dataPath ?? Path.Combine(BasePath(), "data");
        }

        public static string ExperimentPath() {
            string experiment;

            try {
                experiment = Config.experimentName;
            } catch (MissingFieldException e) {
                throw new MissingFieldException("No experiment selected", e);
            }

            return Path.Combine(DataPath(), experiment);
        }

        public static string ParticipantPath(string participant) {
            string dir = ExperimentPath();
            dir = Path.Combine(dir, participant);
            return dir;
        }

        public static string ParticipantPath() {
            string dir = ExperimentPath();

            if (Config.subject == null) {
                throw new MissingFieldException("No participant selected");
            }

            dir = Path.Combine(dir, Config.subject);
            return dir;
        }

        public static string SessionPath(string participant, int session) {
            string dir = ParticipantPath(participant);
            dir = Path.Combine(dir, "session_" + session.ToString());
            return dir;
        }

#nullable enable
        public static string? SessionPath() {
            if (Config.sessionNum == null) {
                // return null and don't use ErrorTS because of EventReporter::DoWrite
                return null;
            }

            string dir = ParticipantPath();
            dir = Path.Combine(dir, "session_" + Config.sessionNum);
            return dir;
        }

        public static string? PriorSessionPath() {
            if (Config.sessionNum == null) {
                // return null and don't use ErrorTS because of EventReporter::DoWrite
                return null;
            } else if (Config.sessionNum == 0) {
                return null;
            }

            string dir = ParticipantPath();
            dir = Path.Combine(dir, "session_" + (Config.sessionNum - 1));
            return dir;
        }
#nullable disable

        public static bool isValidParticipant(string code) {
            if (Config.isTest) {
                return true;
            }

            string id = Config.participantIdRegex ?? ".*";
            if (id == "") { id = ".*"; }
            string prefix = Config.participantIdPrefixRegex ?? "";
            string postfix = Config.participantIdPostfixRegex ?? "";
            
            Regex rx = new Regex(@"^" + prefix + id + postfix + @"$");

            return rx.IsMatch(code);
        }

        public static string GetWordList() {
            string root = BasePath();
            return Path.Combine(root, Config.wordpool);
        }
        public static string GetPracticeWordList() {
            string root = BasePath();
            return Path.Combine(root, Config.practiceWordpool);
        }

        internal static void CreateSession(string participant, int session) {
            var dir = SessionPath(participant, session);
            if (dir == null) {
                throw new Exception("No session selected");
            }
            Directory.CreateDirectory(dir);
        }

        internal static void CreateParticipant() {
            Directory.CreateDirectory(ParticipantPath());
        }
        internal static void CreateExperiment() {
            Directory.CreateDirectory(ExperimentPath());
        }
        internal static void CreateDataFolder() {
            Directory.CreateDirectory(DataPath());
        }

        public static string ConfigPath() {
            string root = BasePath();
            return Path.Combine(root, "configs");
        }

        public static int CurrentSession(string participant) {
            int nextSessionNumber = 0;
            Debug.Log(SessionPath(participant, nextSessionNumber));
            while (Directory.Exists(SessionPath(participant, nextSessionNumber))) {
                nextSessionNumber++;
            }
            return nextSessionNumber;
        }
    }

}