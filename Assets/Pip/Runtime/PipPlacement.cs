using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace Pip {
public class PipPlacement : MonoBehaviour {
 public ARRaycastManager raycasts;
 public ARAnchorManager anchors;
 public Camera arCamera;
 public PipCharacter characterPrefab;
 public PipLightSensor environment;
 public Transform reticle;
 public bool editorPreview;
 [Header("Placement distance (metres)")]
 [Min(.1f)] public float minimumPlacementDistance=.25f;
 [Min(.5f)] public float maximumPlacementDistance=5f;
 readonly List<ARRaycastHit> hits=new();
 ARAnchor anchor;
 PipCharacter character;
 Pose candidate;
 bool valid, busy, reposition;
 ARAnchor flightAnchor;
 bool flying;
 string feedback;
 float feedbackUntil, nextDiagnostic;
 public string Status { get; private set; } = "Move your phone slowly to find a surface";
 public void NoticeTarget() { if(character) character.Notice(arCamera.transform.position+arCamera.transform.forward); }
 public Vector3 CompanionScreenPoint => character ? arCamera.WorldToScreenPoint(character.visual.position+Vector3.up*(.16f*character.modelScale)) : Vector3.zero;
 // Conservative projected bounds include leaves, body and tail, not just the head point.
 Renderer[] boundsRenderers;PipCharacter boundsOwner;
 public Rect CompanionScreenBounds {
  get {
   if(!character||!arCamera)return new Rect();
   if(boundsOwner!=character){boundsOwner=character;boundsRenderers=character.visual.GetComponentsInChildren<Renderer>();}
   var renderers=boundsRenderers;
   Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
   // Project each part in its own local space. A single world-aligned box around
   // the entire character invents empty corners that grow dramatically up close.
   foreach(var r in renderers){
    if(!r||!r.enabled||!r.gameObject.activeInHierarchy)continue;
    Bounds bounds=r.localBounds;var matrix=r.localToWorldMatrix;
    for(int i=0;i<8;i++){
     Vector3 corner=matrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
     var point=arCamera.WorldToScreenPoint(corner);
     if(point.z<=arCamera.nearClipPlane)return new Rect(0,0,Screen.width,Screen.height);
     Vector2 gui=new Vector2(point.x,Screen.height-point.y);min=Vector2.Min(min,gui);max=Vector2.Max(max,gui);
    }
   }
   if(float.IsInfinity(min.x))return new Rect();
   return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
  }
 }
 public bool HasCharacter => character;
 public PipDialogue Dialogue => character && character.gameObject.activeSelf && !IsPlacing && !busy ? character.Dialogue : null;
 public bool IsPlacing => !character || reposition;
 public bool IsBusy => busy;
 public bool CanCancelReposition => character && reposition && !busy;
 public string Feedback => Time.unscaledTime<feedbackUntil?feedback:null;
 public void CancelReposition() { if(CanCancelReposition) { reposition=false;valid=false;feedbackUntil=0; } }
 public bool CanPlace => IsPlacing && valid && Tracking && !busy;
 public Vector2 ScreenAimPoint => AimPoint;
 public float PlacementDistance => valid ? Vector3.Distance(arCamera.transform.position,candidate.position) : 0;
 Vector2 AimPoint => arCamera.pixelRect.center;
 void Report(string message) { feedback=message; feedbackUntil=Time.unscaledTime+5f; Status=message; }
 public bool Tracking => editorPreview || ARSession.state == ARSessionState.SessionTracking;
 public void Reposition() { if(!busy) { reposition=true;feedbackUntil=0; } }
 void Update() {
  bool tracking=Tracking && (!anchor || anchor.trackingState==TrackingState.Tracking || reposition);
  if(character) character.gameObject.SetActive(tracking);
  valid=false;
  if(!Tracking) { Status=ARSession.state==ARSessionState.Unsupported?"AR is unavailable on this device":"Tracking paused — move slowly toward the scanned area"; }
  else if(busy) Status=flying?"Pip is floating over…":"Placing Pip — hold your phone steady…";
  else if(IsPlacing) {
   valid=TrySurface(AimPoint,out candidate);
   Status=valid?(reposition?"Surface found. Tap Move Pip here.":"Surface found. Tap Place Pip here."):$"Scan more floor or table — aim inside its outline ({minimumPlacementDistance:0.##}–{maximumPlacementDistance:0.#} m)";
  } else Status=tracking?"":"Finding Pip's anchor — return to the scanned area";
  if(Time.unscaledTime<feedbackUntil && Tracking) Status=feedback;
  if(reticle) { reticle.gameObject.SetActive(valid); if(valid) reticle.SetPositionAndRotation(candidate.position,candidate.rotation); }
  WriteDeviceDiagnostic();
 }
 public PipCharacter CharacterAt(Vector2 point) {
  if(busy||!Tracking||IsPlacing||!character||!character.gameObject.activeSelf||!arCamera)return null;
  RaycastHit hit;
  return Physics.Raycast(arCamera.ScreenPointToRay(point),out hit,6)&&hit.collider.GetComponentInParent<PipCharacter>()==character?character:null;
 }
 public void Tap(Vector2 point) {
  if(busy || !Tracking) return;
  if(!IsPlacing) {
   var target=CharacterAt(point);if(target)target.React();
   return;
  }
  // Aim-to-place: the center marker and the committed pose use the exact same ray.
  // Screen taps still hit-test Pip directly once Pip has been placed.
  ConfirmPlacement();
 }

 public void ConfirmPlacement() {
  if(busy || !Tracking || !IsPlacing) return;
  if(TrySurface(AimPoint,out var pose)) Place(pose);
  else Report("No surface under the crosshair yet. Slowly scan the floor or table.");
 }
 bool TrySurface(Vector2 point,out Pose pose) {
#if UNITY_EDITOR
  if(editorPreview) {
   var plane=new Plane(Vector3.up,Vector3.zero); var ray=arCamera.ScreenPointToRay(point);
   if(plane.Raycast(ray,out float distance) && distance>=minimumPlacementDistance && distance<=maximumPlacementDistance) { pose=new Pose(ray.GetPoint(distance),Quaternion.identity); return true; }
   pose=default; return false;
  }
#endif
  // ARKit 6.3 does not support native world-ray queries. AR Foundation's Ray overload
  // falls back to ARPlaneManager's real tracked polygon intersections for this provider.
  // This uses the exact Unity camera ray that renders the crosshair and the character.
  var cameraRay=arCamera.ScreenPointToRay(point);
  if(raycasts.Raycast(cameraRay,hits,TrackableType.PlaneWithinPolygon)) {
   foreach(var hit in hits) {
    if(hit.distance<minimumPlacementDistance || hit.distance>maximumPlacementDistance) continue;
    if(Vector3.Dot(hit.pose.up,Vector3.up)<=.9f) continue;
    if(hit.trackable && hit.trackable.trackingState!=TrackingState.Tracking) continue;
    Vector3 screen=arCamera.WorldToScreenPoint(hit.pose.position);
    if(screen.z<=0 || Vector2.Distance(new Vector2(screen.x,screen.y),point)>12) continue;
    pose=hit.pose; return true;
   }
  }
  pose=default; return false;
 }
 async void Place(Pose pose) {
  busy=true; feedbackUntil=0; Status="Placing Pip — hold your phone steady…";
  ARAnchor newAnchor=null;bool flightStarted=false;
  try {
   if(!editorPreview) {
    var result=await anchors.TryAddAnchorAsync(pose);
    if(!result.status.IsSuccess()) { Report("Could not anchor Pip — try another surface"); return; }
    newAnchor=result.value;
    if(!this) { if(newAnchor) Destroy(newAnchor.gameObject); return; }
   }
   if(character){
    // Keep the existing companion and its mood; the old anchor stays alive until arrival.
    flightAnchor=newAnchor;flying=true;flightStarted=true;
    StartCoroutine(FloatTo(pose));return;
   }
   if(anchor) anchors.TryRemoveAnchor(anchor);
   anchor=newAnchor;
   character=CreateCharacter(pose,anchor?anchor.transform:null);
   reposition=false; Report("Pip is placed. Tap Pip to say hello!");
   Debug.Log("Pip placement succeeded; awaiting normal anchor tracking.");
  } catch(System.Exception ex) { Debug.LogException(ex); Report("Placement failed — try again"); }
  finally { if(!flightStarted)busy=false; }
 }
 // Root path, separate from the character's existing hover and facial animation.
 public static Vector3 FlightPoint(Vector3 from,Vector3 to,float progress){
  float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress));
  return Vector3.Lerp(from,to,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*Mathf.Min(.18f,Vector3.Distance(from,to)*.12f));
 }
 public static Quaternion FlightFacing(Vector3 from,Vector3 to,Quaternion fallback){
  Vector3 direction=Vector3.ProjectOnPlane(to-from,Vector3.up);
  return direction.sqrMagnitude>.0001f?Quaternion.LookRotation(direction,Vector3.up):fallback;
 }
 IEnumerator FloatTo(Pose pose){
  Vector3 from=character.transform.position;Quaternion facing=character.transform.rotation;
  float elapsed=0,duration=Mathf.Clamp(Vector3.Distance(from,pose.position)/1.2f,1.2f,2.8f);
  character.transform.SetParent(null,true);character.Dialogue.Close();character.SetTraveling(true);
  try{
   // Turn before taking off, rather than sliding sideways while turning.
   float turn=0;Quaternion departure=FlightFacing(from,flightAnchor?flightAnchor.transform.position:pose.position,facing);
   while(turn<.35f&&character){
    if(!Tracking){yield return null;continue;}
    turn+=Mathf.Min(Time.unscaledDeltaTime,1f/30f);
    character.transform.rotation=Quaternion.Slerp(facing,departure,Mathf.SmoothStep(0,1,turn/.35f));yield return null;
   }
   while(elapsed<duration&&character){
    if(!Tracking||(flightAnchor&&flightAnchor.trackingState!=TrackingState.Tracking)){yield return null;continue;}
    elapsed+=Mathf.Min(Time.unscaledDeltaTime,1f/30f);
    Vector3 target=flightAnchor?flightAnchor.transform.position:pose.position;
    Vector3 previous=character.transform.position;
    character.transform.position=FlightPoint(from,target,elapsed/duration);
    character.transform.rotation=FlightFacing(previous,character.transform.position,departure);
    yield return null;
   }
   if(character){
    if(flightAnchor){character.transform.SetParent(flightAnchor.transform,true);character.transform.localPosition=Vector3.zero;}
    else character.transform.position=pose.position;
    if(anchor)anchors.TryRemoveAnchor(anchor);anchor=flightAnchor;flightAnchor=null;
    Quaternion arrival=character.transform.rotation;float greet=0;
    while(greet<.4f&&character){
     if(!Tracking){yield return null;continue;}
     greet+=Mathf.Min(Time.unscaledDeltaTime,1f/30f);
     var look=FlightFacing(character.transform.position,arCamera.transform.position,arrival);
     character.transform.rotation=Quaternion.Slerp(arrival,look,Mathf.SmoothStep(0,1,greet/.4f));yield return null;
    }
    reposition=false;Report("Right here with you.");
   }
  }finally{if(character)character.SetTraveling(false);flying=false;busy=false;if(flightAnchor){if(anchors)anchors.TryRemoveAnchor(flightAnchor);flightAnchor=null;}}
 }
 void OnDestroy(){if(flying&&character)Destroy(character.gameObject);if(flightAnchor&&anchors)anchors.TryRemoveAnchor(flightAnchor);}
 PipCharacter CreateCharacter(Pose requestedPose,Transform anchorParent) {
  var created=Instantiate(characterPrefab,anchorParent,false);
  if(anchorParent) { created.transform.localPosition=Vector3.zero; created.transform.rotation=Quaternion.identity; }
  else created.transform.SetPositionAndRotation(requestedPose.position,Quaternion.identity);
  // Use the anchor's actual resulting position, not a world-space hit captured before awaiting it.
  Vector3 toward=arCamera.transform.position-created.transform.position; toward.y=0;
  if(toward.sqrMagnitude>.001f) created.transform.rotation=Quaternion.LookRotation(toward);
  created.mood.environment=environment;
  return created;
 }

 [System.Serializable] class DeviceSnapshot {
  public string timestamp, session, reason, status, orientation, cameraPosition, cameraRotation, positionControls, rotationControls, nativeViewport, managedViewport, rawPosition, rawRotation, legacyPosition, legacyRotation;
  public string characterUp,visualUp,visualLocalRotation,screenUp,projection,xrFeatures;
  public int inputTrackingState;
  public bool inputPositionEnabled,inputRotationEnabled;
  public int planeCount, nativeHits, managedHits, screenWidth, screenHeight;
  public bool planeRunning, rayRunning, nativeSupported, worldSupported, valid, busy, cameraPositionBound, cameraRotationBound;
  public float cameraOffset;
  public Rect cameraRect;
 }
 void WriteDeviceDiagnostic() {
#if UNITY_IOS && !UNITY_EDITOR
  if(!Debug.isDebugBuild || Time.unscaledTime<nextDiagnostic) return;
  nextDiagnostic=Time.unscaledTime+1;
  try {
   var planes=raycasts.GetComponent<ARPlaneManager>();
   var origin=raycasts.GetComponent<Unity.XR.CoreUtils.XROrigin>();
   var driver=arCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
   var snapshot=new DeviceSnapshot {
    timestamp=System.DateTime.UtcNow.ToString("O"),session=ARSession.state.ToString(),reason=ARSession.notTrackingReason.ToString(),status=Status,
    planeCount=planes?planes.trackables.count:0,planeRunning=planes && planes.subsystem!=null && planes.subsystem.running,
    rayRunning=raycasts.subsystem!=null && raycasts.subsystem.running,valid=valid,busy=busy,
    screenWidth=Screen.width,screenHeight=Screen.height,cameraRect=arCamera.pixelRect,orientation=Screen.orientation.ToString(),
    cameraOffset=origin?origin.CameraYOffset:-1,cameraPosition=arCamera.transform.position.ToString("F3"),cameraRotation=arCamera.transform.eulerAngles.ToString("F1")
   };
   snapshot.projection=arCamera.projectionMatrix.ToString("F3");
   if(character) {
    snapshot.characterUp=character.transform.up.ToString("F3");
    snapshot.visualUp=character.visual.up.ToString("F3");
    snapshot.visualLocalRotation=character.visual.localEulerAngles.ToString("F1");
    Vector3 p=character.visual.position;
    snapshot.screenUp=(arCamera.WorldToScreenPoint(p+Vector3.up*.1f)-arCamera.WorldToScreenPoint(p)).ToString("F1");
   }
   var xrDevices=new List<UnityEngine.XR.InputDevice>(); UnityEngine.XR.InputDevices.GetDevices(xrDevices);
   var featureText=new System.Text.StringBuilder();
   foreach(var device in xrDevices) {
    var usages=new List<UnityEngine.XR.InputFeatureUsage>();device.TryGetFeatureUsages(usages);
    foreach(var usage in usages) if(usage.type==typeof(Quaternion)) {
     Quaternion q;if(device.TryGetFeatureValue(usage.As<Quaternion>(),out q)) featureText.Append(device.name+"/"+usage.name+"="+q.eulerAngles.ToString("F1")+"; ");
    }
   }
   snapshot.xrFeatures=featureText.ToString();
   if(driver) {
    snapshot.cameraPositionBound=driver.positionInput.action!=null && driver.positionInput.action.controls.Count>0;
    snapshot.cameraRotationBound=driver.rotationInput.action!=null && driver.rotationInput.action.controls.Count>0;
    snapshot.inputPositionEnabled=driver.positionInput.action!=null && driver.positionInput.action.enabled;
    snapshot.inputRotationEnabled=driver.rotationInput.action!=null && driver.rotationInput.action.enabled;
    if(snapshot.cameraPositionBound) snapshot.rawPosition=driver.positionInput.action.ReadValue<Vector3>().ToString("F3");
    if(snapshot.cameraRotationBound) snapshot.rawRotation=driver.rotationInput.action.ReadValue<Quaternion>().eulerAngles.ToString("F1");
    if(driver.trackingStateInput.action!=null && driver.trackingStateInput.action.controls.Count>0) snapshot.inputTrackingState=driver.trackingStateInput.action.ReadValue<int>();
    snapshot.positionControls=driver.positionInput.action==null?"none":driver.positionInput.action.ToString();
    snapshot.rotationControls=driver.rotationInput.action==null?"none":driver.rotationInput.action.ToString();
   }
   var legacy=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);
   if(legacy.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,out Vector3 legacyPosition)) snapshot.legacyPosition=legacyPosition.ToString("F3");
   if(legacy.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation,out Quaternion legacyRotation)) snapshot.legacyRotation=legacyRotation.eulerAngles.ToString("F1");
   if(raycasts.subsystem!=null && raycasts.subsystem.running) {
    snapshot.nativeSupported=raycasts.subsystem.subsystemDescriptor.supportsViewportBasedRaycast;
    snapshot.worldSupported=raycasts.subsystem.subsystemDescriptor.supportsWorldBasedRaycast;
    var native=new List<ARRaycastHit>(); var managed=new List<ARRaycastHit>();
    raycasts.Raycast(AimPoint,native,TrackableType.PlaneWithinPolygon);
    raycasts.Raycast(arCamera.ScreenPointToRay(AimPoint),managed,TrackableType.PlaneWithinPolygon);
    snapshot.nativeHits=native.Count;snapshot.managedHits=managed.Count;
    if(native.Count>0) snapshot.nativeViewport=arCamera.WorldToViewportPoint(native[0].pose.position).ToString("F3");
    if(managed.Count>0) snapshot.managedViewport=arCamera.WorldToViewportPoint(managed[0].pose.position).ToString("F3");
   }
   System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath,"PipPlacementDiagnostics.json"),JsonUtility.ToJson(snapshot,true));
  } catch(System.Exception error) { Debug.LogWarning("Pip diagnostic: "+error.Message); }
#endif
 }

}
}
