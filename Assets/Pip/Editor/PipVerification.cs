using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Pip.Editor {
// Batch integration check of the explicitly simulated scene, not ARKit/device validation.
public static class PipVerification {
 static double started;
 static int step;
 static PipPlacement placement;
 static PipCharacter character;
 static Vector3 initial;
 public static void Run() {
  SessionState.SetBool("PipVerify",true);
  EditorSceneManager.OpenScene("Assets/Pip/Generated/PipPreview.unity");
  EditorApplication.EnterPlaymode();
 }
 [InitializeOnLoadMethod]
 static void Subscribe() { EditorApplication.playModeStateChanged+=state=>{
  if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("PipVerify",false)) {
   started=EditorApplication.timeSinceStartup; step=0; EditorApplication.update+=Tick;
  }
 }; }
 static void Check(bool pass,string message) { if(!pass) throw new System.Exception(message); Debug.Log("PIP VERIFIED: "+message); }
 static void Tick() {
  try {
   if(EditorApplication.timeSinceStartup-started>35) throw new System.Exception("Preview verification timeout");
   if(!Application.isPlaying) return;
   float elapsed=(float)(EditorApplication.timeSinceStartup-started);
   if(step==0 && elapsed>2) {
    placement=Object.FindFirstObjectByType<PipPlacement>(); Check(placement && placement.editorPreview,"Explicit simulation scene");
    placement.Tap(new Vector2(Screen.width*.5f,Screen.height*.5f)); step++; started=EditorApplication.timeSinceStartup;
   } else if(step==1 && elapsed>2) {
    character=Object.FindFirstObjectByType<PipCharacter>(); Check(character && placement.HasCharacter,"Summon creates Pip on simulated floor");
    if(!SystemInfo.graphicsDeviceType.Equals(UnityEngine.Rendering.GraphicsDeviceType.Null)) {
     var camera=Camera.main; var rt=new RenderTexture(720,960,24); camera.targetTexture=rt; camera.Render();
     var previous=RenderTexture.active; RenderTexture.active=rt;
     var image=new Texture2D(720,960,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,720,960),0,0); image.Apply();
     System.IO.Directory.CreateDirectory("Evidence"); System.IO.File.WriteAllBytes("Evidence/PipPreview.png",image.EncodeToPNG());
     camera.targetTexture=null; RenderTexture.active=previous; rt.Release(); Object.Destroy(rt); Object.Destroy(image);
    }
    initial=character.visual.position; character.mood.responseSeconds=1; character.mood.minimumMoodSeconds=.1f; character.mood.inactivitySeconds=999;
    placement.environment.smoothingSeconds=.1f; placement.environment.simulatedBrightness=0;
    step++; started=EditorApplication.timeSinceStartup;
   } else if(step==2 && elapsed>7) {
    Check(character.mood.Current==Mood.Sleepy,"Dim simulated light becomes Sleepy");
    Check(Vector3.Distance(initial,character.visual.position)>.0001f,"Autonomous procedural movement");
    character.React(); Check(character.mood.energy>0,"Touch changes internal energy");
    placement.environment.simulatedBrightness=1; step++; started=EditorApplication.timeSinceStartup;
   } else if(step==3 && elapsed>7) {
    Check(character.mood.Current==Mood.Curious,"Bright simulated light becomes Curious");
    placement.Reposition(); placement.Tap(new Vector2(Screen.width*.55f,Screen.height*.5f)); step++; started=EditorApplication.timeSinceStartup;
   } else if(step==4 && elapsed>1) {
    Check(Object.FindObjectsByType<PipCharacter>(FindObjectsSortMode.None).Length==1,"Reposition keeps one companion");
    SessionState.SetBool("PipVerify",false); EditorApplication.update-=Tick;
    Debug.Log("PIP PREVIEW INTEGRATION PASSED (not physical AR validation)"); EditorApplication.Exit(0);
   }
  } catch(System.Exception ex) {
   Debug.LogException(ex); SessionState.SetBool("PipVerify",false); EditorApplication.update-=Tick; EditorApplication.Exit(1);
  }
 }
}}
