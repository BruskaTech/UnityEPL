//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania (James Bruska)

//This file is part of PsyForge.
//PsyForge is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//PsyForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with PsyForge. If not, see <https://www.gnu.org/licenses/>. 

// using System.Collections.Generic;
// using UnityEngine;

// namespace PsyForge.DataManagement {

//     [AddComponentMenu("PsyForge/Reporters/World Data Reporter")]
//     public class WorldDataReporter : DataReporter<WorldDataReporter> {
//         public bool reportTransform = true;
//         public int framesPerTransformReport = 30;
//         public bool reportView = true;
//         public int framesPerViewReport = 30;

//         private Dictionary<Camera, bool> camerasToInViewfield = new();

//         void Update() {
//             if (reportTransform) CheckTransformReport();
//             if (reportView) CheckViewReport();
//         }

//         void Start() {
//             if (reportView && GetComponent<Collider>() == null) {
//                 ErrorNotifier.ErrorTS(
//                     new UnityException("You have selected enter/exit viewfield reporting for " + gameObject.name + " but there is no collider on the object. " +
//                                        "This feature uses collision detection to compare with camera bounds and other objects.  Please add a collider or " +
//                                        "unselect viewfield enter/exit reporting."));
//             }
//         }


//         public void DoTransformReport(Dictionary<string, object> extraData = null) {
//             Do(DoTransformReportHelper, extraData);
//         }
//         public void DoTransformReportHelper(Dictionary<string, object> extraData = null) {
//             var transformDict = (extraData != null) ? new Dictionary<string, object>(extraData) : new();
//             transformDict.Add("positionX", transform.position.x);
//             transformDict.Add("positionY", transform.position.y);
//             transformDict.Add("positionZ", transform.position.z);
//             transformDict.Add("rotationX", transform.position.x);
//             transformDict.Add("rotationY", transform.position.y);
//             transformDict.Add("rotationZ", transform.position.z);
//             transformDict.Add("scaleX", transform.position.x);
//             transformDict.Add("scaleY", transform.position.y);
//             transformDict.Add("scaleZ", transform.position.z);
//             transformDict.Add("object reporting id", reportingID);
//             eventQueue.Enqueue(new DataPoint(gameObject.name + " transform", transformDict));
//         }

//         private void CheckTransformReport() {
//             if (Time.frameCount % framesPerTransformReport == 0) {
//                 DoTransformReport();
//             }
//         }

//         private void CheckViewReport() {
//             if (Time.frameCount % framesPerViewReport == 0) {
//                 DoViewReport();
//             }
//         }

//         //untested accuraccy, requires collider
//         private void DoViewReport() {
//             Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

//             foreach (Camera thisCamera in cameras) {
//                 Plane[] frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(thisCamera);
//                 Collider objectCollider = GetComponent<Collider>();

//                 // raycast to center mass
//                 if (!Physics.Linecast(thisCamera.transform.position, gameObject.transform.position, out RaycastHit lineOfSightHit)){
//                     continue;
//                 }
//                 bool lineOfSight = lineOfSightHit.collider.Equals(gameObject.GetComponent<Collider>());
//                 bool inView = GeometryUtility.TestPlanesAABB(frustrumPlanes, objectCollider.bounds) && lineOfSight;

//                 if (!reportView)
//                     continue;

//                 Dictionary<string, object> dataDict = new() {
//                     { "cameraName", thisCamera.name },
//                     { "isInView", inView },
//                 };
//                 var eventName = gameObject.name.ToLower() + "InView";
//                 eventQueue.Enqueue(new DataPoint(eventName, dataDict));
//             }
//         }
//     }

// }