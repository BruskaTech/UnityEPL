//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Diagnostics;
using System.Threading;

namespace UnityEPL {

    // This is a high accuracy and precision timer based on a design by Nima Ara
    // It is thread safe since all times are threadlocal
    // It will have slight inconsistencies across threads, for the same reason
    // https://nimaara.com/2016/07/06/high-resolution-clock-in-net.html
    // https://stackoverflow.com/questions/1416139/how-to-get-timestamp-of-tick-precision-in-net-c
    public static class Clock {
        private static readonly long _maxIdleTime = TimeSpan.FromSeconds(10).Ticks;
        private const long TicksMultiplier = 1000 * TimeSpan.TicksPerMillisecond;

        // TODO: JPB: (bug) Make these use a lock instead of a thread local
        //            Check if I already did this in InterfaceManager with Timestamp
        private static readonly ThreadLocal<DateTime> _startTime =
            new ThreadLocal<DateTime>(() => DateTime.UtcNow, false);

        private static readonly ThreadLocal<double> _startTimestamp =
            new ThreadLocal<double>(() => Stopwatch.GetTimestamp(), false);

        public static DateTime UtcNow {
            get {
                double endTimestamp = Stopwatch.GetTimestamp();

                var durationInTicks = (endTimestamp - _startTimestamp.Value) / Stopwatch.Frequency * TicksMultiplier;
                if (durationInTicks >= _maxIdleTime) {
                    _startTimestamp.Value = Stopwatch.GetTimestamp();
                    _startTime.Value = DateTime.UtcNow;
                    return _startTime.Value;
                }

                return _startTime.Value.AddTicks((long)durationInTicks);
            }
        }
    }

}