using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace Pip {
public class PipVision : MonoBehaviour {
 [Serializable] class Config { public string endpoint,token; }
 [Serializable] class Request { public string image,mood,mode,subject; }
 [Serializable] class Reply { public string expression; public string remark; public string @object; public bool certain; }
 public PipPlacement placement;
 public bool InteractionPaused;
 public string LastError { get; private set; }
 public bool ShowingExplanation => placement.Dialogue && placement.Dialogue.Kind==PipDialogueKind.Explanation;
 public bool HasReaction => lastImage!=null && !string.IsNullOrEmpty(lastReaction);
 public void ShowReaction() { if(HasReaction && placement.Dialogue) placement.Dialogue.SayWithExpression(lastReaction,PipDialogueKind.Reaction,lastReactionExpression); }
 public bool Busy { get; private set; }
 public bool Watching { get; private set; }=true;
 public bool CanKnowMore => lastImage!=null && !Busy && placement.Dialogue;
 readonly PipShakeGesture shake=new PipShakeGesture();
 bool manualReady;float manualDeadline;
 public bool ShakePending => shake.Pending||manualReady;
 public float Progress => shake.Pending?shake.Progress:gate.Progress;
 public string Subject => lastSubject;
 public bool Ready => config!=null && !string.IsNullOrEmpty(config.endpoint) && !string.IsNullOrEmpty(config.token);
 readonly PipLookGate gate=new PipLookGate();
 Config config;
 UnityWebRequest activeRequest;
 byte[] lastImage;
 PipDialogue captureOwner;
 string lastSubject="",lastExplanation,lastReaction;
 Mood lastReactionExpression=Mood.Content,lastExplanationExpression=Mood.Curious;
 public static Mood ReplyExpression(string value,Mood fallback) {
  Mood parsed;return Enum.TryParse<Mood>(value,true,out parsed)&&Enum.IsDefined(typeof(Mood),parsed)?parsed:fallback;
 }
 float nextSample,nextRequest;
 void Awake() {
  try { config=JsonUtility.FromJson<Config>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"pip-vision.json"))); }
  catch(Exception) { config=null; }
 }
 public void ToggleWatching() {
  if(!Watching && !Ready)return;
  bool retry=!Watching&&LastError!=null;
  Watching=!Watching;LastError=null;
  if(retry)gate.RetryCurrent();else gate.Pause();
  if(!Watching) CancelActivity();
  else if(placement.Dialogue && placement.Dialogue.Kind==PipDialogueKind.Error) placement.Dialogue.Close();
 }
 public void CompanionPetted(){CancelActivity();gate.Pause();}
 void CancelActivity() {
  shake.Cancel();manualReady=false;
  if(activeRequest!=null) { activeRequest.Abort();activeRequest.Dispose();activeRequest=null; }
  StopAllCoroutines();Busy=false;
  if(placement && placement.Dialogue) placement.Dialogue.ExternalActivity=false;
 }
 public void KnowMore() {
  if(!CanKnowMore) return;
  if(!string.IsNullOrEmpty(lastExplanation)) { placement.Dialogue.SayWithExpression(lastExplanation,PipDialogueKind.Explanation,lastExplanationExpression);return; }
  StartCoroutine(Learn());
 }
 void Update() {
  var dialogue=placement.Dialogue;
  float now=Time.unscaledTime;
  bool canShake=dialogue&&Ready&&!Busy&&!InteractionPaused&&(Watching||LastError!=null)&&!(dialogue.IsVisible&&dialogue.Kind==PipDialogueKind.Explanation)&&now>=nextRequest;
#if UNITY_IOS && !UNITY_EDITOR
  if(!canShake||now>manualDeadline)manualReady=false;
  if(placement.arCamera&&shake.Sample(Input.acceleration,placement.arCamera.transform.position,placement.arCamera.transform.rotation,now,canShake,placement.Tracking)){manualReady=true;manualDeadline=now+5;}
#endif
  if(manualReady&&canShake&&placement.Tracking){
   Watching=true;LastError=null;if(dialogue.Kind==PipDialogueKind.Error)dialogue.Close();
  }
  if(!dialogue || !placement.Tracking) {
   gate.Reset();
   return;
  }
  if(lastImage!=null && captureOwner!=dialogue) { lastImage=null;lastSubject="";lastExplanation=null;lastReaction=null; }
  dialogue.AllowIdle=!Watching && !dialogue.Pinned;
  if(InteractionPaused || (dialogue.IsVisible && dialogue.Pinned)) { gate.Pause();return; }
  if(shake.Pending){gate.Pause();return;}
  if(!Watching || !Ready || Busy || Time.unscaledTime<nextRequest || Time.unscaledTime<nextSample) return;
  nextSample=Time.unscaledTime+.25f;
#if UNITY_EDITOR
  if(placement.editorPreview) return;
#endif
  var source=placement.arCamera.GetComponent<ARCameraManager>();
  if(!source || !source.TryAcquireLatestCpuImage(out var image)) { gate.Pause();return; }
  byte[] signature=null;
  try {
   // Average a coarse grid across the view locally. Includes objects entering away
   // from the center, while smoothing tiny texture changes on the placement floor.
   if(image.planeCount>0) {
    var plane=image.GetPlane(0);signature=new byte[32*24];
    for(int y=0;y<24&&signature!=null;y++)for(int x=0;x<32;x++) {
     int sum=0;
     for(int dy=0;dy<3&&signature!=null;dy++)for(int dx=0;dx<3;dx++){
      int px=Mathf.Clamp(Mathf.RoundToInt(image.width*(.1f+(x+.25f+dx*.25f)*.8f/32)),0,image.width-1);
      int py=Mathf.Clamp(Mathf.RoundToInt(image.height*(.1f+(y+.25f+dy*.25f)*.8f/24)),0,image.height-1);
      int index=py*plane.rowStride+px*plane.pixelStride;
      if(index>=plane.data.Length){signature=null;break;}sum+=plane.data[index];
     }
     if(signature==null)break;signature[y*32+x]=(byte)(sum/9);
    }
   }
  } finally { image.Dispose(); }
  if(signature==null) { gate.Pause();return; }
  var camera=placement.arCamera.transform;
  if(manualReady){
   manualReady=false;gate.AcceptCurrent(camera.position,camera.rotation,signature);StartCoroutine(Observe());
  } else if(gate.Sample(camera.position,camera.rotation,signature,Time.unscaledTime)) StartCoroutine(Observe());
 }
 IEnumerator Observe() {
  var dialogue=placement.Dialogue;if(!dialogue) yield break;
  Busy=true;dialogue.ExternalActivity=true;nextRequest=Time.unscaledTime+8;placement.NoticeTarget();
  try {
   var source=placement.arCamera.GetComponent<ARCameraManager>();
   if(!source || !source.TryAcquireLatestCpuImage(out var image)) { Fail(dialogue,"I blinked at the wrong moment. Let's try looking again!");yield break; }
   var parameters=new XRCpuImage.ConversionParams {
    inputRect=new RectInt(0,0,image.width,image.height),
    outputDimensions=new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(image.width*Mathf.Min(1f,768f/image.width))),Mathf.Max(1,Mathf.RoundToInt(image.height*Mathf.Min(1f,768f/image.width)))),
    outputFormat=TextureFormat.RGB24,transformation=XRCpuImage.Transformation.None
   };
   XRCpuImage.AsyncConversion conversion=default;bool started=false;
   try { conversion=image.ConvertAsync(parameters);started=true; }
   catch(Exception) { Fail(dialogue,"My looking glasses blinked. Try again?"); }
   finally { image.Dispose(); }
   if(!started) yield break;
   byte[] jpeg=null;
   try {
    float deadline=Time.realtimeSinceStartup+10;
    while(!conversion.status.IsDone() && Time.realtimeSinceStartup<deadline) yield return null;
    CaptureDiagnostic("conversion",conversion.status.ToString());
    if(conversion.status==XRCpuImage.AsyncConversionStatus.Ready) {
     var texture=new Texture2D(parameters.outputDimensions.x,parameters.outputDimensions.y,TextureFormat.RGB24,false);
     try { texture.LoadRawTextureData(conversion.GetData<byte>());texture.Apply();jpeg=texture.EncodeToJPG(75); }
     finally { Destroy(texture); }
    }
   } finally { conversion.Dispose(); }
   if(jpeg==null) { Fail(dialogue,"My eyes blinked! Try looking again?");yield break; }
   CaptureDiagnostic("jpeg-ready",jpeg.Length.ToString());
   if(!Current(dialogue)) yield break;
   yield return Send(jpeg,"react","",dialogue);
  } finally { Busy=false;if(dialogue) dialogue.ExternalActivity=false; }
 }
 IEnumerator Learn() {
  var dialogue=placement.Dialogue;Busy=true;dialogue.ExternalActivity=true;
  try { yield return Send(lastImage,"learn",lastSubject,dialogue); }
  finally { Busy=false;if(dialogue) dialogue.ExternalActivity=false; }
 }
 bool Current(PipDialogue dialogue) => dialogue && placement.Dialogue==dialogue;
 IEnumerator Send(byte[] jpeg,string mode,string subject,PipDialogue dialogue) {
  using(var web=new UnityWebRequest(config.endpoint,"POST")) {
   var payload=new Request { image=Convert.ToBase64String(jpeg),mood=dialogue.MoodLabel,mode=mode,subject=subject };
   activeRequest=web;
   web.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
   web.downloadHandler=new DownloadHandlerBuffer();web.SetRequestHeader("Content-Type","application/json");web.SetRequestHeader("Authorization","Bearer "+config.token);web.timeout=35;
   CaptureDiagnostic("request-host",new Uri(config.endpoint).Host+" mode="+mode);
   float started=Time.realtimeSinceStartup;yield return web.SendWebRequest();
   activeRequest=null;
   CaptureDiagnostic("upload",web.responseCode+" "+web.result+" elapsed="+(Time.realtimeSinceStartup-started).ToString("F1")+" error="+(web.error??"none"));
   if(!Current(dialogue)) yield break;
   if(web.result!=UnityWebRequest.Result.Success) {
    Fail(dialogue,web.responseCode==429?"My looking glasses need a break. Let's try later!":"Can't reach my looking glasses. Check your internet and try again!");yield break;
   }
   Reply reply=null;try { reply=JsonUtility.FromJson<Reply>(web.downloadHandler.text); } catch(Exception) { }
   if(reply==null || string.IsNullOrWhiteSpace(reply.remark)) { Fail(dialogue,"I lost my train of thought. Let's look again?");yield break; }
   Mood expression=ReplyExpression(reply.expression,mode=="learn"?Mood.Curious:ReplyExpression(dialogue.MoodLabel,Mood.Content));
   if(mode=="react") { lastReactionExpression=expression;captureOwner=dialogue;lastImage=jpeg;lastSubject=reply.@object??"";lastExplanation=null;lastReaction=reply.remark; }
   else {lastExplanation=reply.remark;lastExplanationExpression=expression;}
   LastError=null;dialogue.SayWithExpression(reply.remark,mode=="learn"?PipDialogueKind.Explanation:PipDialogueKind.Reaction,expression);
  }
 }
 void Fail(PipDialogue dialogue,string text) { Watching=false;LastError=text;gate.Reset();if(Current(dialogue)) dialogue.Say(text,PipDialogueKind.Error); }
 void CaptureDiagnostic(string stage,string detail) {
  if(!Debug.isDebugBuild) return;
  try { File.AppendAllText(Path.Combine(Application.persistentDataPath,"PipVisionDiagnostics.txt"),DateTime.UtcNow.ToString("O")+" "+stage+" "+detail+"\n"); } catch(Exception) { }
 }
 void OnDisable() { CancelActivity();gate.Reset();lastImage=null;if(placement && placement.Dialogue) placement.Dialogue.ExternalActivity=false; }
}}
