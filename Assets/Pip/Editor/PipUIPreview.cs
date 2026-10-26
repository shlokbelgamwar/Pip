using UnityEngine;
using UnityEditor;
namespace Pip.Editor {
// Stages the existing preview scene in Play Mode only; never changes the AR build scene.
public static class PipUIPreview {
 [MenuItem("Pip/Stage UI preview (Play Mode)")]
 public static void Stage() {
  var p=Object.FindFirstObjectByType<PipPlacement>();
  if(!Application.isPlaying || !p || !p.editorPreview){Debug.LogWarning("Open PipPreview and enter Play Mode first.");return;}
  if(!p.HasCharacter)p.ConfirmPlacement();
  var c=Object.FindFirstObjectByType<PipCharacter>();if(!c)return;
  var cam=p.arCamera;cam.transform.position=new Vector3(0,.64f,-1.25f);cam.transform.LookAt(new Vector3(0,.15f,0));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.77f,.81f,.78f);cam.fieldOfView=52;
  c.transform.position=new Vector3(.13f,0,0);c.transform.rotation=Quaternion.LookRotation(Vector3.back);c.mood.enabled=false;c.enabled=false;c.visual.localPosition=Vector3.up*c.hoverHeight;c.visual.localRotation=Quaternion.Euler(0,-10,0);c.visual.localScale=Vector3.one*c.modelScale;c.face.PreviewContent();
  var floor=GameObject.Find("SIMULATED floor");if(floor)floor.SetActive(false);
  var previous=GameObject.Find("UI PREVIEW props");if(previous)Object.DestroyImmediate(previous);
  var root=new GameObject("UI PREVIEW props");
  RenderSettings.ambientLight=new Color(.65f,.64f,.59f);
  Prop(root,"Desk",PrimitiveType.Cube,new Vector3(0,-.065f,0),new Vector3(1.8f,.1f,2),new Color(.62f,.43f,.27f));
  Prop(root,"Earbud case",PrimitiveType.Sphere,new Vector3(-.13f,.027f,-.035f),new Vector3(.23f,.12f,.14f),new Color(.96f,.95f,.91f));
  Prop(root,"Lid seam",PrimitiveType.Cube,new Vector3(-.13f,.048f,-.09f),new Vector3(.17f,.002f,.002f),new Color(.56f,.57f,.55f));
  Prop(root,"Notebook",PrimitiveType.Cube,new Vector3(-.35f,-.004f,-.5f),new Vector3(.25f,.015f,.32f),new Color(.22f,.31f,.29f));
  Prop(root,"Pages",PrimitiveType.Cube,new Vector3(-.35f,.004f,-.5f),new Vector3(.24f,.008f,.31f),new Color(.86f,.83f,.72f));
  Prop(root,"Wall",PrimitiveType.Cube,new Vector3(0,.7f,1.2f),new Vector3(3,2,.05f),new Color(.72f,.77f,.73f));
  Prop(root,"Plant pot",PrimitiveType.Cylinder,new Vector3(-.4f,.06f,.6f),new Vector3(.19f,.14f,.19f),new Color(.72f,.65f,.51f));
  Prop(root,"Foliage",PrimitiveType.Sphere,new Vector3(-.4f,.29f,.6f),new Vector3(.38f,.4f,.22f),new Color(.25f,.43f,.22f));
  var input=Object.FindFirstObjectByType<PipInput>();
  var uiFlags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  var tour=(PipWalkthrough)typeof(PipInput).GetField("walkthrough",uiFlags).GetValue(input);tour.Finish();
  var d=c.Dialogue;d.AllowIdle=false;d.enabled=false;d.Say("A tiny hotel for your earbuds. Do they get room service?",PipDialogueKind.Reaction);d.Advance();
  var v=Object.FindFirstObjectByType<PipVision>();v.enabled=false;
  // The placeholder unlocks the real UI; cached explanation prevents any network call.
  var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  typeof(PipVision).GetField("lastImage",f).SetValue(v,new byte[]{1});
  typeof(PipVision).GetField("captureOwner",f).SetValue(v,d);
  typeof(PipVision).GetField("lastReaction",f).SetValue(v,d.Text);
  typeof(PipVision).GetField("lastSubject",f).SetValue(v,"earbud charging case");
  typeof(PipVision).GetField("lastExplanation",f).SetValue(v,"This case stores and recharges wireless earbuds. Think of it as a pocket-sized hotel where the guests wake up fully charged.");
 }
 static void Prop(GameObject root,string name,PrimitiveType shape,Vector3 position,Vector3 scale,Color color){
  var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(root.transform);go.transform.position=position;go.transform.localScale=scale;
  var m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",shape==PrimitiveType.Sphere?.5f:.1f);go.GetComponent<Renderer>().material=m;
 }
}}
