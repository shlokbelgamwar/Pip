using UnityEngine;
using UnityEngine.XR.ARFoundation;
namespace Pip {
// Lightweight boundary outlines show the actual provider polygons, not fabricated planes.
public class PipPlaneGuide : MonoBehaviour {
 public ARPlaneManager manager;
 public Material lineMaterial;
 public PipPlacement placement;
 // Outlines share the placement ring's mint so the whole placement moment reads as one system.
 Material tinted;
 Material Tinted(){if(!tinted&&lineMaterial){tinted=new Material(lineMaterial);tinted.color=new Color(.62f,.88f,.76f,.6f);}return tinted?tinted:lineMaterial;}
 void OnDestroy(){if(tinted)Destroy(tinted);}
 void Update() {
  foreach(var plane in manager.trackables) {
   var line=plane.GetComponent<LineRenderer>();
   if(!line) { line=plane.gameObject.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.loop=true; line.widthMultiplier=.0025f; line.sharedMaterial=Tinted(); }
   line.enabled=placement.IsPlacing && placement.Tracking && plane.trackingState==UnityEngine.XR.ARSubsystems.TrackingState.Tracking;
   if(!line.enabled) continue;
   var boundary=plane.boundary; line.positionCount=boundary.Length;
   for(int i=0;i<boundary.Length;i++) line.SetPosition(i,new Vector3(boundary[i].x,.002f,boundary[i].y));
  }
 }
}}
