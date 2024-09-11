//Copyright (c) 2024 Columbia University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEPL.Extensions;

namespace UnityEPL.Utilities {

    public static class Scheduling {
        /// <summary>
        /// This function schedules events randomly within a total duration such that they don't overlap.
        /// <br/>It is the same random as randomly assigning spots, checking to see if there are any overlaps, and trying again until there are no overlaps.
        ///     However, it is deterministic, does NOT require repicking, and guarantees that all events will be placed (or throws an error if it's not possible)
        /// </summary>
        /// <param name="totalDurationMs"></param>
        /// <param name="eventDurationsMs"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static List<(int, int)> ScheduleEventsRandomly(int totalDurationMs, List<int> eventDurationsMs) {
            if (eventDurationsMs.Count == 0) {
                throw new ArgumentException($"eventDurationsMs must have at least one element");
            } else if (totalDurationMs < eventDurationsMs.Sum()) {
                throw new ArgumentException($"totalDurationMs ({totalDurationMs}) cannot fit all of the events in eventDurationsMs ({eventDurationsMs.Sum()})");
            }

            var randomEventDurationsMs = eventDurationsMs.Shuffle();
            var numGaps = totalDurationMs - randomEventDurationsMs.Sum();
            var fakeTimes = Enumerable.Repeat(0, numGaps).ToList();
            fakeTimes.AddRange(Enumerable.Repeat(1, randomEventDurationsMs.Count));
            fakeTimes.ShuffleInPlace();

            var realTimes = new List<(int, int)>();
            for (int i = 0; i < fakeTimes.Count; ++i) {
                if (fakeTimes[i] == 0) {
                    continue;
                }

                int startTime = i + realTimes.Select(x => x.Item2 - x.Item1).Sum() - realTimes.Count;
                realTimes.Add((startTime, startTime + randomEventDurationsMs[realTimes.Count]));
            }

            return realTimes;
        }

        /// <summary>
        /// This function schedules events (with the same duration) randomly within a total duration such that they don't overlap.
        /// <br/> It is much faster than the other ScheduleEventsRandomly function by taking advantage of the fact that all events are the same duration.
        /// <br/>It is the same random as randomly assigning spots, checking to see if there are any overlaps, and trying again until there are no overlaps.
        ///     However, it is deterministic, does NOT require repicking, and guarantees that all events will be placed (or throws an error if it's not possible)
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

            var eventTimesMs = new List<(int,int)>();
            while (eventTimesMs.Count < numEvents) {
                // Loop constants
                var numGaps = eventTimesMs.Count + 1;
                var numRemainingEvents = numEvents - eventTimesMs.Count;

                // Find the gaps big enough to fit another event
                var gaps = new List<(int, int)>();
                if (eventTimesMs.Count == 0) {
                    // If there are no events, then the entire duration is a gap
                    gaps.Add((0, totalDurationMs));
                } else {
                    // Add the gap from the start to the first event, if it's big enough
                    if (eventTimesMs[0].Item1 >= eventDurationMs) {
                        gaps.Add((0, eventTimesMs[0].Item1));
                    }
                    // Add the gap from the last event to the end, if it's big enough
                    var lastEventEndTime = eventTimesMs[eventTimesMs.Count - 1].Item2;
                    if (totalDurationMs - lastEventEndTime >= eventDurationMs) {
                        gaps.Add((lastEventEndTime, totalDurationMs));
                    }
                    // Add the gaps between the events, if they're big enough
                    for (int i = 1; i < eventTimesMs.Count; ++i) {
                        var gap = (eventTimesMs[i - 1].Item2, eventTimesMs[i].Item1);
                        int gapSize = gap.Item2 - gap.Item1;
                        if (gapSize >= eventDurationMs) {
                            gaps.Add(gap);
                        }
                    }
                }
                gaps.Sort();

                // Determine the available positions in the gaps
                int numPossibleEventsInGaps = gaps.Select(g => (g.Item2 - g.Item1) / eventDurationMs).Sum();
                var availablePositions = new List<(int, int)>();
                foreach (var gap in gaps) {
                    int gapSize = gap.Item2 - gap.Item1;
                    int numPossibleEventsInGap = gapSize / eventDurationMs;
                    int numPossibleEventsInOtherGaps = numPossibleEventsInGaps - numPossibleEventsInGap;
                    int numGapsNeeded = Math.Max(1, numRemainingEvents - numPossibleEventsInOtherGaps);
                    var availablePositionsInGap = AvailablePositions(gapSize, eventDurationMs, numGapsNeeded);
                    foreach (var availablePosition in availablePositionsInGap) {
                        // Convert it back to it's real position in the total duration
                        availablePositions.Add((gap.Item1 + availablePosition.Item1, gap.Item1 + availablePosition.Item2));
                    }
                }

                // Pick the start time at random from the available position
                // In order to have an equal chance of picking any available position we:
                //   1) Compress the values into a single duration
                //   2) Pick a random number between 0 and that duration
                //   3) Expand that back out to where that should be in the original available positions.
                var compressedAvailablePositions = availablePositions.Select(g => g.Item2 - g.Item1 + 1).ToList();
                int totalCompressedAvailablePositions = compressedAvailablePositions.Sum();
                int compressedPosition = Random.Rnd.Next(0, totalCompressedAvailablePositions);

                int decompressedPosition = compressedPosition;
                for (int i = 0; i < compressedAvailablePositions.Count; ++i) {
                    decompressedPosition -= compressedAvailablePositions[i];
                    if (decompressedPosition < 0) {
                        int position = decompressedPosition + compressedAvailablePositions[i];
                        int startTime = availablePositions[i].Item1 + position;
                        eventTimesMs.Add((startTime, startTime + eventDurationMs));
                        eventTimesMs.Sort();
                        break;
                    }
                }
            }

            return eventTimesMs;
        }

        /// <summary>
        /// This function finds all the available positions (inclusive) within a total number of positions such that
        ///     it guarantees all of the items can all be placed in the future if one of these positions is chosen
        /// <br/> Make sure to rerun it for each item placed to get the correct available positions
        /// <br/> The easiest way to understand is with an example:
        /// <br/> Let's say there are 13 total positions, each item is 5 positions wide, and there are 2 items,
        ///     then the available positions, as a tuple of start and end positions, would be: (0, 3), (5, 8)
        /// <br/> If the first item were to be placed at the unlisted index of 4 (consuming positions [4, 5, 6, 7, 8]), 
        ///     then there would be 4 spots to the left of it [0, 1, 2, 3]
        ///     and 4 spots to the right of it [9, 10, 11, 12].
        /// <br/> Neither of the sides would be enough to place the second item with a width of 5
        ///     without overlapping the first item. So you have to pick a value from the available positions.
        /// </summary>
        /// <param name="numTotalPositions"></param>
        /// <param name="itemWidth"></param>
        /// <param name="numItems"></param>
        /// <returns>A list of tuples of valid start and end positions</returns>
        private static List<(int, int)> AvailablePositions(int numTotalPositions, int itemWidth, int numItems) {
            if (numItems == 0) {
                throw new ArgumentException($"numItems ({numItems}) must be greater than 0");
            } else if (numTotalPositions < itemWidth * numItems) {
                throw new ArgumentException($"numTotalPositions ({numTotalPositions}) cannot fit the numItems ({numItems}) at their itemWidth ({itemWidth})");
            }

            var positions = new List<(int, int)>();
            int numPerBlock = numTotalPositions - itemWidth * numItems + 1;
            for (int i = 0; i < numItems; ++i) {
                int startPos = itemWidth * i;
                int endPos = startPos + numPerBlock - 1;
                // Add a new position or combine overlapping positions
                if (i > 0 && startPos <= positions[positions.Count-1].Item2) {
                    positions[positions.Count-1] = (positions[positions.Count-1].Item1, endPos);
                } else {
                    positions.Add((startPos, endPos));
                }
            }
            return positions;
        }
    }
}