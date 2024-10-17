//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

using SFB;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PsyForge.GUI {

    public class FileSelector : EventMonoBehaviour {
        protected override void AwakeOverride() { }

        //public FileSelector Builder() {

        //}

        //// TODO: JPB: SelectFiles thread safe
        //public async Task<string[]> SelectFilesHelper(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        //public async Task<NativeArray<NativeText>> SelectFilesHelper(string title, string directory, ExtensionFilter[] extensions, bool multiselect) {
        //    return await SFB.StandaloneFileBrowser.OpenFilePanel(title, directory, extensions, multiselect);
        //}

        
    }

}