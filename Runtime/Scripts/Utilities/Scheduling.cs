//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEPL.Utilities {

    public static class Scheduling {
        /// <summary>
        /// This function finds all the available positions (inclusive) within a total number of positions such that
        /// it guarantees all of the items can all be placed in the future if one of these positions is chosen
        /// <br/> The easiest way to understand is with an example:
        /// <br/> If there are 9 total positions, each item is 4 positions wide, and there are 2 items,
        /// then the available positions would be: (0, 1), (4, 5)
        /// <br/> If the first item were to be placed at index 2 [2, 3, 4, 5], then there would be 2 spots to the left of it [0, 1] and 3 spots to the right of it [6, 7, 8].
        /// <br/> Neither of the sides would be enough to place the second item with a width of 4 without overlapping the first item
        /// </summary>
        /// <param name="numTotalPositions"></param>
        /// <param name="itemWidth"></param>
        /// <param name="numItems"></param>
        /// <returns></returns>
        public static List<(int, int)> AvailablePositions(int numTotalPositions, int itemWidth, int numItems) {
            if (numItems == 0) {
                throw new ArgumentException($"numItems ({numItems}) must be greater than 0");
            } else if (numTotalPositions < itemWidth * numItems) {
                throw new ArgumentException($"numTotalPositions ({numTotalPositions}) cannot fit the numItems ({numItems}) at their itemWidth ({itemWidth})");
            }

            var gaps = new List<(int, int)>();
            int numPerBlock = numTotalPositions - itemWidth * numItems + 1;
            for (int i = 0; i < numItems; ++i) {
                int startPos = itemWidth * i;
                int endPos = startPos + numPerBlock - 1;
                gaps.Add((startPos, endPos));
            }
            return gaps;
        }

        /// <summary>
        /// This function schedules events (with the same duration) randomly within a total duration such that they don't overlap
        /// It is the same "random" as just picking a random spot for each and checking if it overlaps with any of the others and repicking it if it does;
        /// however, it is deterministic, does NOT require repicking, and guarantees that all events will be placed (or throws an error if it's not possible)
        /// </summary>
        /// <param name="totalDurationMs"></param>
        /// <param name="eventDurationMs"></param>
        /// <param name="numEvents"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static List<(int, int)> ScheduleEventsRandomly(int totalDurationMs, int eventDurationMs, int numEvents) {
            if (numEvents == 0) {
                throw new ArgumentException($"numEvents ({numEvents}) must be greater than 0");
            } else if (totalDurationMs < eventDurationMs * numEvents) {
                throw new ArgumentException($"totalDurationMs ({totalDurationMs}) cannot fit the numEvents ({numEvents}) at their eventDurationMs ({eventDurationMs})");
            }

            var startTimes = new List<(int,int)>();
            while (startTimes.Count < numEvents) {
                // Loop constants
                var numGaps = startTimes.Count + 1;
                var numRemainingEvents = numEvents - startTimes.Count;

                // Find the gaps big enough to fit another event
                var usableGaps = new List<(int, int)>();
                if (startTimes.Count == 0) {
                    // If there are no events, then the entire duration is a gap
                    usableGaps.Add((0, totalDurationMs));
                } else {
                    // Add the gap from the start to the first event, if it's big enough
                    if (startTimes[0].Item1 >= eventDurationMs) {
                        usableGaps.Add((0, startTimes[0].Item1));
                    }
                    // Add the gap from the last event to the end, if it's big enough
                    var lastEventEndTime = startTimes[startTimes.Count - 1].Item2;
                    if (totalDurationMs - lastEventEndTime >= eventDurationMs) {
                        usableGaps.Add((lastEventEndTime, totalDurationMs));
                    }
                    // Add the gaps between the events, if they're big enough
                    for (int i = 1; i < startTimes.Count; ++i) {
                        var gap = (startTimes[i - 1].Item2, startTimes[i].Item1);
                        int gapSize = gap.Item2 - gap.Item1;
                        if (gapSize >= eventDurationMs) {
                            usableGaps.Add(gap);
                        }
                    }
                }
                usableGaps.Sort();

                // Determine the available positions in the usable gaps
                int numPossibleEventsInUsableGaps = usableGaps.Select(g => (g.Item2 - g.Item1) / eventDurationMs).Sum();
                var availablePositions = new List<(int, int)>();
                foreach (var gap in usableGaps) {
                    int gapSize = gap.Item2 - gap.Item1;
                    int numPossibleEventsInGap = gapSize / eventDurationMs;
                    int numPossibleEventsInOtherGaps = numPossibleEventsInUsableGaps - numPossibleEventsInGap;
                    int numGapsNeeded = Math.Max(1, numRemainingEvents - numPossibleEventsInOtherGaps);
                    var availablePositionsInGap = AvailablePositions(gapSize, eventDurationMs, numGapsNeeded);
                    foreach (var availablePosition in availablePositionsInGap) {
                        // Convert it back to it's real position in the total duration
                        availablePositions.Add((gap.Item1 + availablePosition.Item1, gap.Item1 + availablePosition.Item2));
                    }
                }

                // Pick the start time at random from the available position
                // In order to have an equal chance of picking any available position we:
                // compress the values into a single duration, pick a random number between 0 and that duration,
                // and then expand that back out to where that should be in the original available positions.
                var compressedAvailablePositions = availablePositions.Select(g => g.Item2 - g.Item1 + 1).ToList();
                int totalCompressedAvailablePositions = compressedAvailablePositions.Sum();
                int compressedPosition = Random.Rnd.Next(0, totalCompressedAvailablePositions);

                int decompressedPosition = compressedPosition;
                for (int i = 0; i < compressedAvailablePositions.Count; ++i) {
                    decompressedPosition -= compressedAvailablePositions[i];
                    if (decompressedPosition < 0) {
                        int position = decompressedPosition + compressedAvailablePositions[i];
                        int startTime = availablePositions[i].Item1 + position;
                        startTimes.Add((startTime, startTime + eventDurationMs));
                        startTimes.Sort();
                        break;
                    }
                }
            }

            return startTimes;
        }

    }
}