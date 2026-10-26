using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using UnityEditor.XR.Management;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management.Metadata;
namespace Pip.Editor {
public static class PipProjectSetup {
 const string Root="Assets/Pip/Generated";
 static Material Mat(string name,Color color,bool emission=false) {
  var material=new Material(Shader.Find("Standard")); material.color=color;
  if(emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*.5f); }
  AssetDatabase.CreateAsset(material,$"{Root}/{name}.mat"); return material;
 }
 static Transform Part(string name,Transform parent,Vector3 position,Vector3 scale,Material mat) {
  var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat; Object.DestroyImmediate(go.GetComponent<Collider>()); return go.transform;
 }
 static void AddMouth(Transform visual) {
  if(visual.Find("Small lips")) return;
  var lips=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Lips.mat");
  if(!lips) lips=Mat("Lips",new Color(.64f,.30f,.28f));
  var mouth=new GameObject("Small lips").transform;
  mouth.SetParent(visual,false); mouth.localPosition=new Vector3(0,-.023f,.0655f);
  var smile=mouth.gameObject.AddComponent<LineRenderer>();
  smile.useWorldSpace=false; smile.positionCount=13; smile.widthMultiplier=.0022f;
  smile.numCapVertices=5; smile.numCornerVertices=3; smile.generateLightingData=true;
  smile.sharedMaterial=lips; smile.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  for(int i=0;i<13;i++) {
   float x=(i/12f-.5f)*.019f;
   smile.SetPosition(i,new Vector3(x,.003f*Mathf.Pow(x/.0095f,2)-.0015f,0));
  }
  Part("Soft lower lip",mouth,new Vector3(0,-.002f,.0003f),new Vector3(.012f,.0025f,.0024f),lips);
 }
 static void AddFace(PipCharacter character) {
  AddMouth(character.visual);
  var face=character.GetComponent<PipFace>();
  if(!face) face=character.gameObject.AddComponent<PipFace>();
  character.face=face;
  face.leftEye=character.leftEye; face.rightEye=character.rightEye;
  var browMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Brows.mat");
  if(!browMat) browMat=Mat("Brows",new Color(.27f,.21f,.16f));
  face.leftBrow=character.visual.Find("Left eyebrow");
  if(!face.leftBrow) face.leftBrow=Part("Left eyebrow",character.visual,new Vector3(-.032f,.045f,.055f),new Vector3(.021f,.0035f,.0035f),browMat);
  face.rightBrow=character.visual.Find("Right eyebrow");
  if(!face.rightBrow) face.rightBrow=Part("Right eyebrow",character.visual,new Vector3(.032f,.045f,.055f),new Vector3(.021f,.0035f,.0035f),browMat);
  var mouth=character.visual.Find("Small lips");
  face.lips=mouth.GetComponent<LineRenderer>();
  face.lowerLip=mouth.Find("Soft lower lip");
  face.mouthInterior=mouth.Find("Mouth opening");
  if(!face.mouthInterior) face.mouthInterior=Part("Mouth opening",mouth,new Vector3(0,-.0015f,-.0008f),new Vector3(.008f,.008f,.001f),browMat);
  var eyeMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Eyes.mat");
  for(int side=0;side<2;side++) {
   string name=side==0?"Left expressive eyelid":"Right expressive eyelid";
   var lidTransform=character.visual.Find(name);
   if(!lidTransform) {
    lidTransform=new GameObject(name).transform; lidTransform.SetParent(character.visual,false);
    lidTransform.localPosition=new Vector3(side==0?-.032f:.032f,.015f,.069f);
   }
   var lid=lidTransform.GetComponent<LineRenderer>();
   if(!lid) lid=lidTransform.gameObject.AddComponent<LineRenderer>();
   lid.useWorldSpace=false; lid.sharedMaterial=eyeMat; lid.numCapVertices=4; lid.generateLightingData=true;
   lid.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   if(side==0) face.leftLid=lid; else face.rightLid=lid;
  }
  face.PreviewContent();
 }
 [MenuItem("Pip/Update facial expressions")]
 public static void UpdateFacialExpressions() {
  string path=Root+"/Pip.prefab";
  var root=PrefabUtility.LoadPrefabContents(path);
  try { AddFace(root.GetComponent<PipCharacter>()); PrefabUtility.SaveAsPrefabAsset(root,path); }
  finally { PrefabUtility.UnloadPrefabContents(root); }
  AssetDatabase.SaveAssets(); Debug.Log("Pip coordinated eyes, eyebrows and lips saved.");
 }
 [MenuItem("Pip/Update small lips")]
 public static void UpdateSmallLips() {
  string path=Root+"/Pip.prefab";
  var root=PrefabUtility.LoadPrefabContents(path);
  try {
   AddMouth(root.GetComponent<PipCharacter>().visual);
   PrefabUtility.SaveAsPrefabAsset(root,path);
  } finally { PrefabUtility.UnloadPrefabContents(root); }
  AssetDatabase.SaveAssets(); Debug.Log("Pip small lips saved to character prefab.");
 }
 [MenuItem("Pip/Generate scenes and iOS settings")]
 public static void Generate() {
  Directory.CreateDirectory(Root);
  // Deterministic generation: keep existing assets until explicitly regenerating.
  if(AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Pip.prefab")) { Debug.Log("Pip already generated. Use existing scenes; delete Generated only to regenerate."); return; }
  var cream=Mat("Cream",new Color(1,.93f,.76f)); var teal=Mat("Teal",new Color(.05f,.48f,.48f)); var leaf=Mat("Leaf",new Color(.4f,.7f,.27f)); var eye=Mat("Eyes",new Color(.035f,.07f,.08f)); var glow=Mat("Glow",new Color(.4f,1,.65f),true);
  var root=new GameObject("Pip"); var character=root.AddComponent<PipCharacter>(); character.mood=root.AddComponent<PipMood>();
  character.visual=new GameObject("Visual (replaceable model)").transform; character.visual.SetParent(root.transform,false);
  Part("Body",character.visual,Vector3.zero,new Vector3(.15f,.17f,.13f),cream);
  character.leftLeaf=Part("Left leaf",character.visual,new Vector3(-.035f,.1f,0),new Vector3(.035f,.11f,.018f),leaf);
  character.rightLeaf=Part("Right leaf",character.visual,new Vector3(.035f,.1f,0),new Vector3(.035f,.11f,.018f),leaf);
  character.leftEye=Part("Left eye",character.visual,new Vector3(-.032f,.015f,.06f),new Vector3(.026f,.038f,.016f),eye);
  character.rightEye=Part("Right eye",character.visual,new Vector3(.032f,.015f,.06f),new Vector3(.026f,.038f,.016f),eye);
  AddMouth(character.visual);
  character.scarf=Part("Scarf",character.visual,new Vector3(0,-.05f,0),new Vector3(.16f,.035f,.14f),teal);
  Part("Scarf end",character.scarf,new Vector3(.32f,-1,0),new Vector3(.25f,2,.3f),teal);
  character.tail=Part("Tail",character.visual,new Vector3(0,-.01f,-.1f),new Vector3(.045f,.045f,.1f),glow); character.tailRenderer=character.tail.GetComponent<Renderer>();
  AddFace(character);
  var collider=character.visual.gameObject.AddComponent<SphereCollider>(); collider.radius=.115f;
  var prefab=PrefabUtility.SaveAsPrefabAsset(root,$"{Root}/Pip.prefab"); Object.DestroyImmediate(root);
  CreateScene(prefab,false,teal); CreateScene(prefab,true,teal); Configure(); AssetDatabase.SaveAssets();
 }
 static void CreateScene(GameObject prefab,bool preview,Material guideMaterial) {
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var light=new GameObject("Estimated room light").AddComponent<Light>(); light.type=LightType.Directional; light.transform.rotation=Quaternion.Euler(45,-30,0); light.shadows=LightShadows.None;
  var rig=new GameObject("XR Origin"); var origin=rig.AddComponent<XROrigin>(); origin.CameraYOffset=0;
  var offset=new GameObject("Camera Offset"); offset.transform.SetParent(rig.transform,false); origin.CameraFloorOffsetObject=offset;
  var cameraObject=new GameObject("AR Camera"); cameraObject.tag="MainCamera"; cameraObject.transform.SetParent(offset.transform,false);
  var camera=cameraObject.AddComponent<Camera>(); camera.nearClipPlane=.05f; camera.farClipPlane=20; origin.Camera=camera;
  var source=cameraObject.AddComponent<ARCameraManager>(); source.requestedFacingDirection=CameraFacingDirection.World;
  var background=cameraObject.AddComponent<ARCameraBackground>();
  var driver=cameraObject.AddComponent<PipARCameraPoseDriver>();
  driver.positionInput=new InputActionProperty(new InputAction("Position",InputActionType.Value,binding:"<HandheldARInputDevice>/devicePosition",expectedControlType:"Vector3"));
  driver.rotationInput=new InputActionProperty(new InputAction("Rotation",InputActionType.Value,binding:"<HandheldARInputDevice>/deviceRotation",expectedControlType:"Quaternion"));
  driver.trackingStateInput=new InputActionProperty(new InputAction("Tracking",InputActionType.Value,expectedControlType:"Integer"));
  var planes=rig.AddComponent<ARPlaneManager>(); planes.requestedDetectionMode=PlaneDetectionMode.Horizontal;
  var rays=rig.AddComponent<ARRaycastManager>(); var anchors=rig.AddComponent<ARAnchorManager>();
  var session=new GameObject("AR Session"); session.AddComponent<ARSession>(); session.AddComponent<ARInputManager>();
  var systems=new GameObject("Pip systems"); var sensing=systems.AddComponent<PipLightSensor>(); sensing.source=source; sensing.roomLight=light;
  var placement=systems.AddComponent<PipPlacement>(); placement.raycasts=rays; placement.anchors=anchors; placement.arCamera=camera; placement.characterPrefab=prefab.GetComponent<PipCharacter>(); placement.environment=sensing; placement.editorPreview=preview;
  systems.AddComponent<PipInput>().placement=placement;
  var guides=systems.AddComponent<PipPlaneGuide>(); guides.manager=planes; guides.lineMaterial=guideMaterial; guides.placement=placement;
  var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.name="Surface reticle"; marker.transform.localScale=new Vector3(.055f,.001f,.055f); Object.DestroyImmediate(marker.GetComponent<Collider>()); marker.GetComponent<Renderer>().sharedMaterial=guideMaterial; placement.reticle=marker.transform;
  if(preview) {
   session.SetActive(false); source.enabled=false; background.enabled=false; planes.enabled=false; rays.enabled=false; anchors.enabled=false; guides.enabled=false;
   cameraObject.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>().enabled=false;
   camera.transform.position=new Vector3(0,.65f,-1); camera.transform.LookAt(new Vector3(0,.1f,0));
   var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name="SIMULATED floor"; floor.transform.localScale=Vector3.one*.4f;
  }
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),$"{Root}/{(preview?"PipPreview":"PipAR")}.unity");
 }
 [MenuItem("Pip/Configure iOS")]
 public static void Configure() {
  PlayerSettings.companyName="Shlok"; PlayerSettings.productName="Pip";
  if(string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS)))
   PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.shlok.pip");
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
  PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.iOS,1);
  PlayerSettings.iOS.targetOSVersionString="18.0";
  PlayerSettings.insecureHttpOption=InsecureHttpOption.NotAllowed;
  PlayerSettings.iOS.cameraUsageDescription="Pip uses the camera to find surfaces and share your room in augmented reality.";
  PlayerSettings.iOS.appleEnableAutomaticSigning=true;
  PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,false); PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{UnityEngine.Rendering.GraphicsDeviceType.Metal});
  var settings=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.iOS);
  if(settings==null || settings.AssignedSettings==null) {
   XRGeneralSettingsPerBuildTarget store;
   if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out store)) {
    store=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
    AssetDatabase.CreateAsset(store,Root+"/XRSettings.asset");
    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,store,true);
   }
   if(!store.HasSettingsForBuildTarget(BuildTargetGroup.iOS)) store.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
   store.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
   settings=store.SettingsForBuildTarget(BuildTargetGroup.iOS);
   EditorUtility.SetDirty(store);
  }
  if(settings!=null && settings.AssignedSettings!=null) {
   bool ok=XRPackageMetadataStore.AssignLoader(settings.AssignedSettings,"UnityEngine.XR.ARKit.ARKitLoader",BuildTargetGroup.iOS);
   if(!ok) throw new System.Exception("Could not assign the ARKit loader.");
   settings.InitManagerOnStart=true;
   EditorUtility.SetDirty(settings); EditorUtility.SetDirty(settings.AssignedSettings);
   Debug.Log("Pip ARKit loader assigned: "+ok);
  }
  // ARKit's Editor updater skips batch mode. Explicitly persist this before compilation.
  var target=UnityEditor.Build.NamedBuildTarget.iOS;
  var defines=PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(x=>!string.IsNullOrEmpty(x)).ToList();
  if(!defines.Contains("UNITY_XR_ARKIT_LOADER_ENABLED")) {
   defines.Add("UNITY_XR_ARKIT_LOADER_ENABLED");
   PlayerSettings.SetScriptingDefineSymbols(target,string.Join(";",defines));
  }
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene($"{Root}/PipAR.unity",true)};
  var serialized=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
  serialized.FindProperty("activeInputHandler").intValue=2;
  serialized.ApplyModifiedPropertiesWithoutUndo();
  QualitySettings.vSyncCount=0;
 }
 [MenuItem("Pip/Export Xcode project")]
 public static void Export() {
  Configure();
#if !UNITY_IOS || !UNITY_XR_ARKIT_LOADER_ENABLED
  throw new System.InvalidOperationException("Select iOS, run Pip/Configure iOS, and wait for script compilation before exporting. ARKit must be compiled with UNITY_XR_ARKIT_LOADER_ENABLED.");
#else
  var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Builds/iOS",BuildTarget.iOS,BuildOptions.Development);
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Pip iOS export failed: "+report.summary.result);
  var plist=new UnityEditor.iOS.Xcode.PlistDocument();var plistPath="Builds/iOS/Info.plist";plist.ReadFromFile(plistPath);
  plist.root.SetString("NSPhotoLibraryAddUsageDescription","Save the memories you choose to your photo library.");
  var projectPath=UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath("Builds/iOS");
  var nativeProject=new UnityEditor.iOS.Xcode.PBXProject();nativeProject.ReadFromFile(projectPath);
  nativeProject.AddFrameworkToProject(nativeProject.GetUnityFrameworkTargetGuid(),"Photos.framework",false);nativeProject.WriteToFile(projectPath);
  plist.root.values.Remove("NSLocalNetworkUsageDescription");
  UnityEditor.iOS.Xcode.PlistElement ats;
  var transport=plist.root.values.TryGetValue("NSAppTransportSecurity",out ats)?ats.AsDict():plist.root.CreateDict("NSAppTransportSecurity");
  transport.values.Remove("NSAllowsLocalNetworking");plist.WriteToFile(plistPath);
#endif
 }
}}
