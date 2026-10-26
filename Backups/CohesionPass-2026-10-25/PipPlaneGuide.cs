using UnityEngine;
using UnityEngine.XR.ARFoundation;
namespace Pip {
// Lightweight boundary outlines show the actual provider polygons, not fabricated planes.
public class PipPlaneGuide : MonoBehaviour {
 public ARPlaneManager manager;
 public Material lineMaterial;
 public PipPlacement placement;
 void Update() {
  foreach(var plane in manager.trackables) {
   var line=plane.GetComponent<LineRenderer>();
   if(!line) { line=plane.gameObject.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.loop=true; line.widthMultiplier=.003f; line.sharedMaterial=lineMaterial; }
   line.enabled=placement.IsPlacing && placement.Tracking && plane.trackingState==UnityEngine.XR.ARSubsystems.TrackingState.Tracking;
   if(!line.enabled) continue;
   var boundary=plane.boundary; line.positionCount=boundary.Length;
   for(int i=0;i<boundary.Length;i++) line.SetPosition(i,new Vector3(boundary[i].x,.002f,boundary[i].y));
  }
 }
}}
