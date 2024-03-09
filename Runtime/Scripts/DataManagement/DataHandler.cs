//Copyright (c) 2024 Jefferson University
//Copyright (c) 2024 Bruska Technologies LLC
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace UnityEPL {

    public abstract class DataHandler<T> : EventMonoBehaviour
            where T : DataReporter<T> {
        protected List<DataReporter<T>> reportersToHandle = new();
        protected Queue<DataReporter<T>> toAdd = new();
        protected Queue<DataReporter<T>> toRemove = new();
        protected Queue<DataPoint> eventQueue = new();

        // TODO: JPB: (needed) (bug) DataHandler Update is overriden by child classes
        protected virtual void Update() {
            DataReporter<T> result;

            if (toAdd.TryDequeue(out result)) {
                reportersToHandle.Add(result);
            }

            foreach (DataReporter<T> reporter in reportersToHandle) {
                if (reporter.UnreadDataPointCount() > 0) {
                    DataPoint[] newPoints = reporter.ReadDataPoints(reporter.UnreadDataPointCount());
                    HandleDataPoints(newPoints);
                }
            }

            if (toRemove.TryDequeue(out result)) {
                if (!reportersToHandle.Remove(result)) {
                    toRemove.Enqueue(result);
                }
            }
        }

        // TODO: JPB: (needed) (bug) Make QueuePoint use a blittable type instead of DataPoint
        //            Or at least have it use Mutex
        public void QueuePoint(DataPoint data) {
            Do(QueuePointHelper, data);
        }
        public void QueuePointTS(DataPoint data) {
            DoTS(() => { QueuePointHelper(data); });
        }
        protected void QueuePointHelper(DataPoint data) {
            eventQueue.Enqueue(data);
        }

        public void AddReporter(DataReporter<T> add) {
            Do(AddReporterHelper, add);
        }
        public void AddReporterHelper(DataReporter<T> add) {
            toAdd.Enqueue(add);
        }

        public void RemoveReporter(DataReporter<T> remove) {
            Do(RemoveReporterHelper, remove);
        }
        public void RemoveReporterHelper(DataReporter<T> remove) {
            toRemove.Enqueue(remove);
        }

        protected abstract void HandleDataPoints(DataPoint[] dataPoints);
    }

}