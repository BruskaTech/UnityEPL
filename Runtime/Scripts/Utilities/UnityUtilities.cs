//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using UnityEngine;
using TMPro;
using System.Collections.Generic;

public static class UnityUtilities {
    public static bool IsMacOS() {
        return Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer;
    }

    public static float FindMaxFittingFontSize(List<string> strings, TextMeshProUGUI textComponent) {
        string oldText = textComponent.text;
        bool oldAutosizing = textComponent.enableAutoSizing;
        textComponent.enableAutoSizing = true;
        float maxFontSize = 300;
        foreach (var str in strings) {
            textComponent.text = str;
            textComponent.ForceMeshUpdate();
            if (textComponent.fontSize < maxFontSize) {
                maxFontSize = textComponent.fontSize;
            }
        }
        textComponent.enableAutoSizing = oldAutosizing;
        textComponent.text = oldText;
        textComponent.ForceMeshUpdate();

        return maxFontSize;
    }
}
