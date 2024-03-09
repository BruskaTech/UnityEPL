//Copyright (c) 2024 Jefferson University
//Copyright (c) 2024 Bruska Technologies LLC
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace UnityEPL {

    //this superclass implements an interface for retrieving behavioral events from a queue
    public abstract class DataReporter<T> : SingletonEventMonoBehaviour<T>
            where T : DataReporter<T> {
        public string reportingID = "Object ID not set.";

        protected Queue<DataPoint> eventQueue = new();

        public DataHandler<T> reportTo;

        protected override void AwakeOverride() {
            // this can be set in the editor, change won't appear in code search
            if (reportingID == "Object ID not set.") {
                GenerateDefaultName();
            }

            if (QualitySettings.vSyncCount == 0) {
                UnityEngine.Debug.LogWarning("vSync is off!  This will cause tearing, which will prevent meaningful reporting of frame-based time data.");
            }
        }

        protected void OnEnable() {
            if (!reportTo) {
                GameObject data = GameObject.Find("DataManager");
                if (data != null) {
                    reportTo = (DataHandler<T>)data.GetComponent("DataHandler");
                }
            }

            if (reportTo) {
                // TODO: JPB: (needed) (bug) Figure out why DataReporter::OnEnable crashes when I call AddReporterMB
                reportTo.AddReporter(this);
            }
        }

        protected void OnDisable() {
            if (reportTo) {
                eventQueue.Enqueue(new DataPoint(reportingID + "Disabled"));
                reportTo.RemoveReporter(this);
            }
        }

        /// <summary>
        /// The number of data points currently queued in this object.
        /// 
        /// Datapoints are dequeued when read. (Usually when  handled by a DataHandler.)
        /// </summary>
        /// <returns>The data point count.</returns>
        public int UnreadDataPointCount() {
            return DoGet(UnreadDataPointCountHelper);
        }
        protected int UnreadDataPointCountHelper() {
            return eventQueue.Count;
        }

        /// <summary>
        /// If you want to be responsible yourself for handling data points, instead of letting DataHandlers handle them, you can call this.
        /// 
        /// Read datapoints will be dequeueud and not read by other users of this object.
        /// </summary>
        /// <returns>The data points.</returns>
        /// <param name="count">How many data points to read.</param>
        public DataPoint[] ReadDataPoints(int count) {
            return DoGet(ReadDataPointsHelper, count);
        }
        protected DataPoint[] ReadDataPointsHelper(int count) {
            if (eventQueue.Count < count) {
                ErrorNotifier.ErrorTS(
                    new UnityException("Not enough data points! Check UnreadDataPointCount first."));
            }

            DataPoint[] dataPoints = new DataPoint[count];
            for (int i = 0; i < count; i++) {
                bool dequeued = false;
                while (!dequeued) {
                    dequeued = eventQueue.TryDequeue(out DataPoint readPoint);
                    dataPoints[i] = readPoint;
                }
            }

            return dataPoints;
        }

        public void DoReport(Dictionary<string, object> extraData = null) {
            Do(DoReportHelper, extraData);
        }
        protected void DoReportHelper(Dictionary<string, object> extraData = null) {
            var transformDict = (extraData != null) ? new Dictionary<string, object>(extraData) : new();
            transformDict.Add("positionX", transform.position.x);
            transformDict.Add("positionY", transform.position.y);
            transformDict.Add("positionZ", transform.position.z);
            transformDict.Add("rotationX", transform.rotation.eulerAngles.x);
            transformDict.Add("rotationY", transform.rotation.eulerAngles.y);
            transformDict.Add("rotationZ", transform.rotation.eulerAngles.z);
            transformDict.Add("object reporting id", reportingID);
            eventQueue.Enqueue(new DataPoint(gameObject.name + " transform", transformDict));
        }

        private void GenerateDefaultName() {
            reportingID = this.name + Guid.NewGuid();
        }
    }

}