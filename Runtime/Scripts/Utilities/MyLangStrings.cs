//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

namespace UnityEPL {
    public static partial class LangStrings {
        public static LangString Blank() { return GenForCurrLang(""); }
        public static LangString NewLine() { return GenForCurrLang("\n"); }

        public static LangString Error() { return new( new() {
            { Language.English, "Error" },
        }); }
        public static LangString Warning() { return new( new() {
            { Language.English, "Warning" },
        }); }
        public static LangString ShowInstructionVideo() { return new( new() {
            { Language.English, "Press any key to show instruction video" },
        }); }
        public static LangString MicrophoneTestTitle() { return new( new() {
            { Language.English, "Microphone Test" },
        }); }
        public static LangString MicrophoneTest() { return new( new() {
            { Language.English, "Press any key to record a sound after the beep." },
        }); }
        public static LangString ConfirmStart() { return new( new() {
            { Language.English, "Please let the experimenter know if you have any questions about the task.\n\n" +
                "If you think you understand, please explain the task to the experimenter in your own words.\n\n" +
                "Press any key to continue to start." },
        }); }
        public static LangString ExperimentQuit() { return new( new() {
            { Language.English, "Do you want to quit?\n\nPress Y to Quit.\nPress N to Resume." },
        }); }
        public static LangString ExperimentPaused() { return new( new() {
            { Language.English, "<b>Paused</b>\n\nPress P to unpause." },
        }); }
        public static LangString RepeatIntroductionVideo() { return new( new() {
            { Language.English, "Press Y to continue, \n Press N to replay instructional video." },
        }); }
        public static LangString RepeatMicTest() { return new( new() {
            { Language.English, "Did you hear the recording ? \n(Y = Continue / N = Try Again)." },
        }); }
        public static LangString SlideControlLine() { return new( new() {
            { Language.English, "\n\n(go backward) '<-'   |   '->' (go forward) " },
        }); }
        public static LangString MicrophoneTestRecording() { return new( new() {
            { Language.English, "Recording..." },
        }); }
        public static LangString MicrophoneTestPlaying() { return new( new() {
            { Language.English, "Playing..." },
        }); }
        public static LangString SubjectSessionConfirmation(string subject, int sessionNum, string experimentName) { return new( new() {
            { Language.English, $"Running {subject} in session {sessionNum} of {experimentName}."
                + "\nPress Y to continue, N to quit." },
        }); }
        public static LangString VerbalRecallDisplay() { return new( new() {
            { Language.English, "*****" },
        }); }
        public static LangString ElememConnection() { return new( new() {
            { Language.English, "Waiting for Elemem connection..." },
        }); }
        public static LangString RamulatorConnection() { return new( new() {
            { Language.English, "Waiting for Ramulator connection..." },
        }); }
    }
}