﻿//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections.Generic;
using System;
using System.Threading;

using UnityEPL.Utilities;
using UnityEPL.Threading;
using UnityEPL.Extensions;

namespace UnityEPL.DataManagement {
    
    //These datapoints represent behavioral events
    //data about the event is stored in a dictionary

    /// <summary>
    /// The data point struct represents a single piece of data that you might want to keep about your project.
    /// It is used primarily in logging
    /// It is also completely blittable (safe to pass into TS functions)
    /// </summary>
    public struct DataPoint {
        public readonly string type;
        public readonly BlitDateTime time;
        public readonly int thisID;
        public readonly string json;

        private static volatile int id = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DataPoint"/> class.  This represents a piece of data that you might want to keep about your project.
        /// If you get an error with a data type, try using ToString() on the object before passing it in.
        /// </summary>
        /// <param name="type">A short description of the data.</param>
        /// <param name="time">The time when the datapoint occured.</param>
        /// <param name="data">The actual data that you might want to analyze later.  Each element of the data is a key-value pair, the key representing its name. The value can be any C# object.</param>
        public DataPoint(string type, DateTime time, Dictionary<string, object> data = null) {
            this.type = type;
            this.time = time;
            this.thisID = Interlocked.Increment(ref id);
            this.json = "";
            this.json = GenJSON(data ?? new());
        }
        public DataPoint(string type, Dictionary<string, object> data = null) :
            this(type, Clock.UtcNow, data) { }

        /// <summary>
        /// Returns a JSON string representing this datapoint.
        /// 
        /// Strings conforming to certain formats will be converted to corresponding types.  For example, if a string looks like a number it will be represented as a JSON number type. 
        /// </summary>
        /// <returns>The json.</returns>
        public string ToJSON() {
            return json;
        }

        /// <summary>
        /// Sets the JSON string representing this datapoint.
        /// 
        /// Strings conforming to certain formats will be converted to corresponding types.  For example, if a string looks like a number it will be represented as a JSON number type. 
        /// </summary>
        /// <returns>The json.</returns>
        private string GenJSON(Dictionary<string, object> data) {
            double unixTimestamp = time.ConvertToMillisecondsSinceEpoch();

            Dictionary<string, object> dataPointjson = new() {
                { "type", type },
                { "time", unixTimestamp.ToString() },
                { "data", data }
            };
            return dataPointjson.ToJSON();
        }
    }

}