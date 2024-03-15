//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEPL {

    //////////
    // Classes to manage the filesystem in
    // which experiment data is stored
    /////////
    // TODO: JPB: (needed) (refactor) Decide if FileManager should be an EventLoop
    public class FileManager {

        InterfaceManager manager;

        public FileManager(InterfaceManager _manager) {
            manager = _manager;
        }

        public virtual string ExperimentRoot() {

#if UNITY_EDITOR
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
#else
            return Path.GetFullPath(".");
#endif
        }

        public string DataPath() {
            try {
                return Config.dataPath;
            } catch {
                return Path.Combine(ExperimentRoot(), "data");
            }
        }

        public string ExperimentPath() {
            string experiment;

            try {
                experiment = Config.experimentName;
            } catch (MissingFieldException) {
                ErrorNotifier.ErrorTS(new Exception("No experiment selected"));
                return null;
            }

            return Path.Combine(DataPath(), experiment);
        }

        public string ParticipantPath(string participant) {
            string dir = ExperimentPath();
            dir = Path.Combine(dir, participant);
            return dir;
        }

        public string ParticipantPath() {
            string dir = ExperimentPath();
            string participant;

            try {
                participant = Config.subject;
            } catch (MissingFieldException) {
                ErrorNotifier.ErrorTS(new Exception("No participant selected"));
                return null;
            }

            dir = Path.Combine(dir, participant);
            return dir;
        }

        public string SessionPath(string participant, int session) {
            string dir = ParticipantPath(participant);
            dir = Path.Combine(dir, "session_" + session.ToString());
            return dir;
        }

        public string SessionPath() {
            string session;
            try {
                session = Config.sessionNum.ToString();
            } catch (MissingFieldException) {
                return null;
            }

            string dir = ParticipantPath();
            dir = Path.Combine(dir, "session_" + session);
            return dir;
        }

        public bool isValidParticipant(string code) {
            if (Config.isTest) {
                return true;
            }

            string prefix;
            try {
                prefix = Config.prefix;
            } catch (MissingFieldException) {
                return false;
            }

            if (prefix == "any") {
                return true;
            }

            Regex rx = new Regex(@"^" + prefix + @"\d{1,4}[A-Z]?$");

            return rx.IsMatch(code);
        }

        public string GetWordList() {
            string root = ExperimentRoot();
            return Path.Combine(root, Config.wordpool);
        }
        public string GetPracticeWordList() {
            string root = ExperimentRoot();
            return Path.Combine(root, Config.practiceWordpool);
        }

        public void CreateSession() {
            Directory.CreateDirectory(SessionPath());
        }

        public void CreateParticipant() {
            Directory.CreateDirectory(ParticipantPath());
        }
        public void CreateExperiment() {
            Directory.CreateDirectory(ExperimentPath());
        }
        public void CreateDataFolder() {
            Directory.CreateDirectory(DataPath());
        }

        public string ConfigPath() {
            string root = ExperimentRoot();
            return Path.Combine(root, "configs");
        }

        public int CurrentSession(string participant) {
            int nextSessionNumber = 0;
            Debug.Log(SessionPath(participant, nextSessionNumber));
            while (Directory.Exists(SessionPath(participant, nextSessionNumber))) {
                nextSessionNumber++;
            }
            return nextSessionNumber;
        }
    }

}