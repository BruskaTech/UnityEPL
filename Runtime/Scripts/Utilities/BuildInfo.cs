//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.IO;
using System.Reflection;

namespace UnityEPL.Utilities {

    /// <summary>
    /// Get the date and time of the build.
    /// </summary>
    public static class BuildInfo {
        /// <summary>
        /// Get the date and time of the build.
        /// Uses the last write time of the assembly file.
        /// </summary>
        /// <returns></returns>
        public static System.DateTime Date() {
            var assembly = Assembly.GetExecutingAssembly();
            var filePath = assembly.Location;
            var buildTime = File.GetLastWriteTime(filePath);
            return buildTime;
        }
    }

}
