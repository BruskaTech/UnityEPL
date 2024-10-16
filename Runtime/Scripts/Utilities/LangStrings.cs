//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using Unity.Collections;

using PsyForge.Extensions;
using PsyForge.Threading;

namespace PsyForge.Utilities {

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
