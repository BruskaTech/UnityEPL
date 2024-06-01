//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 


using System;
using System.Collections.Generic;

namespace UnityEPL {

    public enum Language {
        English = 0,
        German = 1,
        Spanish = 2,
    }

    public static partial class LanguageSwitcher {
        public static Language language {get; private set;} = Language.English ;

        public static LangString showInstructionVideo { get { return new( new() {
            { Language.English, "Press any key to show instruction video" },
        }); } }


    }

    public class LangString {
        public override string ToString() {
            var language = LanguageSwitcher.language;
            if (!strings.ContainsKey(language)) {
                // TODO: JPB: (needed) Change language enum number to the name in the error.
                throw new Exception($"The current {nameof(LangString)} does not have a {language} option available.");
            }
            return strings[language];
        }

        private readonly Dictionary<Language, string> strings;

        // Make a langstring enumerable constructor that uses a dictionary, for convenience
        public LangString(Dictionary<Language, string> strings) {
            if (strings.Count <= 0) { throw new ArgumentException($"{nameof(LangString)} must have at least one language provided"); }
            this.strings = strings;
        }
    }

}
