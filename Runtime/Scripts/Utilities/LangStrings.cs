//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 


using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEPL {

    // TODO: JPB: (needed) Figure out partial enum for Language so it is extensible to the user.
    public enum Language {
        English = 0,
        German = 1,
        Spanish = 2,
    }

    // Maybe change name to LangStrs or LangCtrl and chage LangString to LangStr
    public static partial class LangStrings {
        public static Language Language {get; private set;} = Language.English;

        /// <summary>
        /// Set the current language for the LangStrings
        /// </summary>
        /// <param name="lang">The language to set</param>
        public static void SetLanguage(Language lang) {
            Language = lang;
        }

        /// <summary>
        /// Generate a LangString for all languages
        /// ONLY USE THIS FUNCTION IF YOU ARE SURE THE STRING IS THE SAME FOR ALL LANGUAGES
        /// Examples of this would be: numbers, file paths, subject ids, rich text formatting, etc.
        /// </summary>
        /// <param name="val">The string value for all languages</param>
        /// <returns>A LangString with the value for all languages</returns>
        public static LangString GenForAllLangs(string val) {
            Dictionary<Language, string> strings = new();
            foreach (Language lang in Enum.GetValues(typeof(Language))) {
                strings.Add(lang, val);
            }
            return new(strings);
        }
        
        /// <summary>
        /// Generate a LangString for the current language
        /// ONLY USE THIS FUNCTION IF YOU ARE SURE THE STRING IS THE SAME FOR ALL LANGUAGES
        /// Examples of this would be: numbers, file paths, subject ids, rich text formatting, etc.
        /// </summary>
        /// <param name="val">The string value for the current language</param>
        /// <returns>A LangString with the value for the current language</returns>
        public static LangString GenForCurrLang(string val) {
            return new(new() { { Language, val } });
        }

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
        public static LangString TrialPrompt(uint trialNum) { return new( new() {
            { Language.English, $"Press any key to start Trial {trialNum}." },
        }); }
        public static LangString PracticeTrialPrompt(uint trialNum) { return new( new() {
            { Language.English, $"Press any key to start Practice Trial {trialNum}." },
        }); }
        public static LangString ExperimentQuit() { return new( new() {
            { Language.English, "Do you want to quit\nPress Y to Quit, N to Resume." },
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

    public class LangString {
        private readonly Dictionary<Language, string> strings;

        // Make a langstring enumerable constructor that uses a dictionary, for convenience
        public LangString(Dictionary<Language, string> strings) {
            if (strings.Count <= 0) { throw new ArgumentException($"{nameof(LangString)} must have at least one language provided"); }
            this.strings = strings;
        }
        
        public override string ToString() {
            var language = LangStrings.Language;
            if (!strings.ContainsKey(language)) {
                throw new Exception($"The current {nameof(LangString)} does not have a {Enum.GetName(typeof(Language), language)} option available.\n{strings.ToJSON()}");
            }
            return strings[language];
        }
        public static implicit operator string(LangString str) {
            return str.ToString();
        }
        public NativeText ToNativeText() {
            return ToString().ToNativeText();
        }
        public static LangString operator +(LangString str1, LangString str2) {
            Dictionary<Language, string> strings = new();
            foreach (Language lang in Enum.GetValues(typeof(Language))) {
                if (str1.strings.ContainsKey(lang) && str2.strings.ContainsKey(lang)) {
                    strings.Add(lang, str1.strings[lang] + str2.strings[lang]);
                }
            }
            return new(strings);
        }

        /// <summary>
        /// Color the text of the LangString using RichText
        /// </summary>
        /// <param name="color">The color to use in RichText format</param>
        /// <returns>A new LangString with the color applied</returns>
        public LangString Color(string color) {
            Dictionary<Language, string> strings = new();
            foreach (Language lang in Enum.GetValues(typeof(Language))) {
                if (this.strings.ContainsKey(lang)) {
                    strings.Add(lang, $"<color={color}>{this.strings[lang]}</color>");
                }
            }
            return new(strings);
        }
    }

}
