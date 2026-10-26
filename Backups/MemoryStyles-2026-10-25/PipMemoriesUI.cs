using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Pip {
public partial class PipInput {
 PipMemoryStore memories;
 bool memoriesOpen,capturingMoment,momentOverlay;
 Texture2D momentBackground,momentPortrait;
 string momentQuote,momentSubject;
 int memoryPage;
 PipMemoryStore.Entry selectedMemory;
 Texture2D memoryFull;
 bool photoSaving;string photoSaveMessage;
 readonly Dictionary<string,Texture2D> memoryThumbs=new Dictionary<string,Texture2D>();
 string memoryNotice;
 float memoryNoticeUntil;
 Rect MemoriesButton=>new Rect(layout.Album.x,layout.Album.y-(placement.IsPlacing?60*S:0),48*S,48*S);
 Rect CaptureButton=>placement.IsPlacing?new Rect(Safe.center.x-24*S,layout.Pause.y-60*S,48*S,48*S):layout.Capture;
 string MemoryNotice=>Time.unscaledTime<memoryNoticeUntil?memoryNotice:null;
 void EnsureMemories(){if(memories==null)memories=new PipMemoryStore(Path.Combine(Application.persistentDataPath,"PipMemories"));}
 void MemoryMessage(string message){memoryNotice=message;memoryNoticeUntil=Time.unscaledTime+4;}
 void OpenMemories(){
  try{EnsureMemories();memories.Reload();memoryPage=0;selectedMemory=null;memoriesOpen=true;menu=false;CancelPointer();vision.InteractionPaused=true;}
  catch(Exception){MemoryMessage("Couldn't open memories. Please try again.");}
 }
 void CloseMemories(){memoriesOpen=false;ClearMemoryTextures();selectedMemory=null;CancelPointer();vision.InteractionPaused=GuideModal||menu;}
 void ClearMemoryTextures(){foreach(var texture in memoryThumbs.Values)if(texture)Destroy(texture);memoryThumbs.Clear();if(memoryFull)Destroy(memoryFull);memoryFull=null;}
 void CaptureMoment(){
  if(capturingMoment||memoriesOpen||GuideModal)return;
  menu=false;menuIn.T=0;StartCoroutine(SaveMoment());
 }
 IEnumerator SaveMoment(){
  capturingMoment=true;momentOverlay=false;vision.InteractionPaused=true;
  yield return new WaitForEndOfFrame();
  bool ready=false;
  try{
   if(!isActiveAndEnabled)throw new IOException("Capture cancelled");
   momentBackground=ScreenCapture.CaptureScreenshotAsTexture();
   if(!momentBackground)throw new IOException("Capture unavailable");
   momentQuote=Dialogue?(HasBubble?Dialogue.Text:Dialogue.LastSpokenText):"";momentSubject=vision.Subject;
   momentPortrait=CapturePipPortrait();momentOverlay=true;ready=true;
  }catch(Exception){MemoryMessage("Couldn't save that moment. Please try again.");ClearMomentCapture();}
  if(!ready){capturingMoment=false;vision.InteractionPaused=memoriesOpen||GuideModal||menu;yield break;}
  // The second frame renders the frozen photo and its frozen portrait/quote overlay.
  yield return new WaitForEndOfFrame();
  Texture2D photo=null,thumb=null;
  try{
   if(!isActiveAndEnabled)throw new IOException("Capture cancelled");
   photo=ScreenCapture.CaptureScreenshotAsTexture();if(!photo)throw new IOException("Capture unavailable");
   int w=Mathf.Min(360,photo.width),h=Mathf.Max(1,Mathf.RoundToInt(photo.height*w/(float)photo.width));
   var target=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
   try{Graphics.Blit(photo,target);RenderTexture.active=target;thumb=new Texture2D(w,h,TextureFormat.RGB24,false);thumb.ReadPixels(new Rect(0,0,w,h),0,0);thumb.Apply();}
   finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
   EnsureMemories();memories.Save(photo.EncodeToJPG(90),thumb.EncodeToJPG(80),photo.width,photo.height,momentSubject,momentQuote,true,momentBackground.EncodeToJPG(90),MemoryThumbnail(momentBackground));
   MemoryMessage("Saved to Memories");
  }catch(Exception){MemoryMessage("Couldn't save that moment. Please try again.");}
  finally{if(photo)Destroy(photo);if(thumb)Destroy(thumb);ClearMomentCapture();capturingMoment=false;vision.InteractionPaused=memoriesOpen||GuideModal||menu;}
 }
 void CancelMomentCapture(){if(capturingMoment)StopAllCoroutines();ClearMomentCapture();capturingMoment=false;}
 void OnApplicationPause(bool paused){if(paused&&capturingMoment){CancelMomentCapture();if(vision)vision.InteractionPaused=memoriesOpen||GuideModal||menu;}}
 void ClearMomentCapture(){if(momentBackground)Destroy(momentBackground);if(momentPortrait)Destroy(momentPortrait);momentBackground=null;momentPortrait=null;momentOverlay=false;}
 Texture2D CapturePipPortrait(){
  var character=Dialogue?Dialogue.GetComponent<PipCharacter>():null;if(!character||!character.visual)return null;
  var children=character.visual.GetComponentsInChildren<Transform>(true);var layers=new int[children.Length];
  var go=new GameObject("Pip memory portrait"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;
  var target=RenderTexture.GetTemporary(384,384,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
  Texture2D result=null;
  try{
   for(int i=0;i<children.Length;i++){layers[i]=children[i].gameObject.layer;children[i].gameObject.layer=31;}
   Vector3 center=character.visual.TransformPoint(new Vector3(0,.035f,0)),forward=character.visual.forward;
   go.transform.position=center+forward*.8f;go.transform.rotation=Quaternion.LookRotation(-forward,character.visual.up);
   camera.orthographic=true;camera.orthographicSize=.22f*character.modelScale;camera.aspect=1;camera.nearClipPlane=.01f;camera.farClipPlane=2;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;camera.allowHDR=false;camera.targetTexture=target;
   camera.Render();RenderTexture.active=target;result=new Texture2D(384,384,TextureFormat.RGBA32,false);result.ReadPixels(new Rect(0,0,384,384),0,0);result.Apply();return result;
  }catch{if(result)Destroy(result);return null;}
  finally{for(int i=0;i<children.Length;i++)children[i].gameObject.layer=layers[i];RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);Destroy(go);}
 }
 void DrawMomentOverlay(){
  if(!momentBackground)return;
  A=1;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),momentBackground,ScaleMode.StretchToFill);
  float s=S,width=Safe.width-32*s,portrait=momentPortrait?86*s:0,inset=20*s,textWidth=width-2*inset-(portrait>0?portrait+12*s:0);
  string quote=string.IsNullOrEmpty(momentQuote)?"Quiet moment":momentQuote;float textScale=s;
  float maxText=Mathf.Max(70*s,Safe.height*.46f-64*s);
  for(int i=0;i<30&&typography.Measure(quote,text,textWidth,textScale).Height>maxText;i++)textScale*=.97f;
  float textHeight=typography.Measure(quote,text,textWidth,textScale).Height;
  float height=Mathf.Max(portrait>0?118*s:80*s,textHeight+64*s);
  var box=new Rect(Safe.x+16*s,Safe.yMax-16*s-height,width,height);Card(box,new Color(.14f,.13f,.12f,.94f),22,.8f);
  if(momentPortrait)GUI.DrawTexture(new Rect(box.x+inset,box.center.y-portrait*.5f,portrait,portrait),momentPortrait,ScaleMode.ScaleToFit);
  float x=box.x+inset+(portrait>0?portrait+12*s:0);
  Label(new Rect(x,box.y+18*s,textWidth,20*s),"Pip",title,mint);
  typography.Draw(new Rect(x,box.y+44*s,textWidth,textHeight),quote,text,C(white),textScale);
 }
 byte[] MemoryThumbnail(Texture2D source){
  int w=Mathf.Min(360,source.width),h=Mathf.Max(1,Mathf.RoundToInt(source.height*w/(float)source.width));
  var target=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;Texture2D result=null;
  try{Graphics.Blit(source,target);RenderTexture.active=target;result=new Texture2D(w,h,TextureFormat.RGB24,false);result.ReadPixels(new Rect(0,0,w,h),0,0);result.Apply();return result.EncodeToJPG(80);}
  finally{if(result)Destroy(result);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
 }
 void SaveMemoryToPhotos(){
  if(photoSaving||selectedMemory==null||!PipPhotos.Supported)return;
  photoSaving=true;photoSaveMessage=null;
  try{PipPhotos.Save(memories.DisplayPath(selectedMemory),gameObject.name);}
  catch(Exception){photoSaving=false;photoSaveMessage="Couldn't save. Please try again.";}
 }
 [UnityEngine.Scripting.Preserve]
 public void OnPhotoSaved(string result){
  photoSaving=false;
  photoSaveMessage=result=="saved"?"Saved to Photos":result=="denied"?"Allow photo saving in Settings, then try again.":"Couldn't save. Please try again.";
  MemoryMessage(photoSaveMessage);
 }
 void DrawMemoryExport(float x,float width){
  float s=S;var row=new Rect(x,Safe.yMax-154*s,width,44*s);bool available=memories.HasClean(selectedMemory);
  Panel(row,new Color(1,1,1,.035f),12);Label(new Rect(row.x+14*s,row.y,row.width-82*s,row.height),available?"Pip overlay":"Original capture",PipTypeRole.SecondaryAction,available?white:muted,TextAnchor.MiddleLeft);
  if(available){
   bool on=!selectedMemory.hidePipOverlay;var track=new Rect(row.xMax-58*s,row.center.y-11*s,40*s,22*s);
   Panel(track,on?new Color(.40f,.65f,.51f,1):new Color(.32f,.32f,.30f,1),11);
   Panel(new Rect(track.x+(on?21:3)*s,track.y+3*s,16*s,16*s),white,8);
   if(Hit(row,!photoSaving)){try{memories.SetOverlay(selectedMemory,!on);ClearMemoryTextures();photoSaveMessage=null;}catch(Exception){photoSaveMessage="Couldn't change this memory. Try again.";}}
  }
  if(Pill(new Rect(x,Safe.yMax-98*s,width,48*s),photoSaving?"Saving…":"Save to Photos",true,PipPhotos.Supported&&!photoSaving))SaveMemoryToPhotos();
  Label(new Rect(x,Safe.yMax-40*s,width,36*s),photoSaveMessage??(PipPhotos.Supported?"Saves the version shown above.":"Photo saving is available on iPhone."),small,muted,TextAnchor.UpperCenter);
 }
 Texture2D MemoryTexture(PipMemoryStore.Entry e,bool full){
  try{
   if(full&&memoryFull)return memoryFull;
   Texture2D cached;if(!full&&memoryThumbs.TryGetValue(e.id,out cached))return cached;
   string path=memories.DisplayPath(e,!full);
   if(new FileInfo(path).Length>32*1024*1024)return null;
   var t=new Texture2D(2,2,TextureFormat.RGB24,false);
   if(!ImageConversion.LoadImage(t,File.ReadAllBytes(path))){Destroy(t);return null;}
   if(full)memoryFull=t;else memoryThumbs[e.id]=t;return t;
  }catch(Exception){return null;}
 }
 string MemoryDate(PipMemoryStore.Entry e){DateTime time;return DateTime.TryParse(e.createdUtc,out time)?time.ToLocalTime().ToString("MMM d · h:mm tt"):"Saved moment";}
 static string ShortMemoryText(string value,int limit){if(string.IsNullOrEmpty(value)||value.Length<=limit)return value;int end=value.LastIndexOf(' ',limit-1,limit);return value.Substring(0,end>limit/2?end:limit-1)+"…";}
 void DrawMemoryControls(){
  if(Circle(MemoriesButton,9,true,false))OpenMemories();
  if(Circle(CaptureButton,8,!capturingMoment&&!placement.IsBusy,false))CaptureMoment();
 }
 void DrawMemories(){
  A=1;Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.105f,.10f,.095f,1),0);
  float s=S,x=Safe.x+20*s,w=Safe.width-40*s;
  var tabs=new Rect(x,Safe.y+12*s,w,44*s);Panel(tabs,new Color(1,1,1,.05f),22);
  var live=new Rect(tabs.x,tabs.y,tabs.width*.5f,tabs.height);var saved=new Rect(live.xMax,live.y,live.width,live.height);
  Panel(saved,new Color(.23f,.29f,.25f,1),22);Label(live,"Live",PipTypeRole.SecondaryAction,muted);Label(saved,"Memories",button,mint);
  if(Hit(live)){CloseMemories();return;}
  float top=tabs.yMax+24*s;
  if(selectedMemory!=null){
   if(Circle(new Rect(Safe.xMax-64*s,top-5*s,44*s,44*s),4,true,false)){selectedMemory=null;if(memoryFull)Destroy(memoryFull);memoryFull=null;return;}
   Label(new Rect(x,top,w-52*s,30*s),"A moment with Pip",PipTypeRole.Heading,white);
   Label(new Rect(x,top+36*s,w,22*s),MemoryDate(selectedMemory),small,muted);
   string caption=selectedMemory.withPipOverlay?"":ShortMemoryText(selectedMemory.remark,160);
   float captionHeight=string.IsNullOrEmpty(caption)?0:typography.Measure(caption,text,w,s).Height;
   float imageTop=top+70*s,imageBottom=Safe.yMax-170*s-captionHeight-(captionHeight>0?18*s:0);
   var photoRect=new Rect(x,imageTop,w,Mathf.Max(80*s,imageBottom-imageTop));
   Panel(photoRect,new Color(1,1,1,.025f),16);var photo=MemoryTexture(selectedMemory,true);
   if(photo)GUI.DrawTexture(photoRect,photo,ScaleMode.ScaleToFit);else Label(photoRect,"This image couldn't be opened.",text,muted,TextAnchor.MiddleCenter);
   if(captionHeight>0)Label(new Rect(x,photoRect.yMax+18*s,w,captionHeight),caption,text,white);
   DrawMemoryExport(x,w);return;
  }
  Label(new Rect(x,top,w,32*s),"Little moments",PipTypeRole.Heading,white);
  Label(new Rect(x,top+40*s,w,24*s),"Saved only on this iPhone",small,muted);
  if(memories.Entries.Count==0){
   var empty=new Rect(x,top+110*s,w,200*s);DrawIcon(new Rect(empty.center.x-24*s,empty.y,48*s,48*s),8,mint);
   Label(new Rect(x,empty.y+66*s,w,30*s),"Make your first memory",body,white,TextAnchor.MiddleCenter);
   Label(new Rect(x+16*s,empty.y+110*s,w-32*s,72*s),"Return to Live and tap the camera button to keep a moment with Pip.",text,muted,TextAnchor.UpperCenter);return;
  }
  int pages=(memories.Entries.Count+3)/4;memoryPage=Mathf.Clamp(memoryPage,0,pages-1);
  float gridTop=top+84*s,gap=12*s,cellW=(w-gap)*.5f,cellH=Mathf.Min(236*s,(Safe.yMax-gridTop-84*s-gap)*.5f);
  for(int i=0;i<4;i++){
   int index=memoryPage*4+i;if(index>=memories.Entries.Count)break;var e=memories.Entries[index];
   var cell=new Rect(x+(i%2)*(cellW+gap),gridTop+(i/2)*(cellH+gap),cellW,cellH);
   Card(cell,new Color(1,1,1,.035f),16,.25f);var image=new Rect(cell.x+6*s,cell.y+6*s,cell.width-12*s,cell.height-40*s);
   var texture=MemoryTexture(e,false);if(texture)GUI.DrawTexture(image,texture,e.withPipOverlay?ScaleMode.ScaleToFit:ScaleMode.ScaleAndCrop);
   Label(new Rect(cell.x+10*s,cell.yMax-28*s,cell.width-20*s,20*s),MemoryDate(e),small,muted);
   if(Hit(cell)){selectedMemory=e;photoSaveMessage=null;if(memoryFull)Destroy(memoryFull);memoryFull=null;return;}
  }
  float footer=Safe.yMax-64*s;
  Label(new Rect(x+w*.25f,footer,w*.5f,44*s),(memoryPage+1)+" / "+pages,small,muted,TextAnchor.MiddleCenter);
  if(Pill(new Rect(x,footer,90*s,44*s),"Previous",false,memoryPage>0)){memoryPage--;ClearMemoryTextures();}
  if(Pill(new Rect(x+w-90*s,footer,90*s,44*s),"Next",false,memoryPage<pages-1)){memoryPage++;ClearMemoryTextures();}
 }
}
}
