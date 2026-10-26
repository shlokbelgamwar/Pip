using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Pip {
public partial class PipInput {
 PipMemoryStore memories;
 bool memoriesOpen,capturingMoment,momentOverlay,momentKeepsake;
 string momentDate;
 Texture2D momentBackground,momentPortrait;
 string momentQuote,momentSubject;
 int memoryPage;
 PipMemoryStore.Entry selectedMemory;
 Texture2D memoryFull;
 bool photoSaving;string photoSaveMessage;
 readonly Dictionary<string,Texture2D> memoryThumbs=new Dictionary<string,Texture2D>();
 string memoryNotice;
 // A saved photo shrinks into the album button, so success is shown rather than announced.
 Texture2D flyThumb;
 float flyT=-1,albumPulse,styleSlide=-1;
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
  if(capturingMoment||memoriesOpen||GuideModal||flyT>=0)return;
  menu=false;menuIn.T=0;StartCoroutine(SaveMoment());
 }
 IEnumerator SaveMoment(){
  capturingMoment=true;momentOverlay=false;momentKeepsake=false;momentDate=DateTime.Now.ToString("MMM d, yyyy");vision.InteractionPaused=true;
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
  Texture2D photo=null,keepsake=null;
  try{photo=ScreenCapture.CaptureScreenshotAsTexture();if(!photo)throw new IOException("Capture unavailable");momentKeepsake=true;}
  catch(Exception){MemoryMessage("Couldn't save that moment. Please try again.");}
  if(photo){
   yield return new WaitForEndOfFrame();
   try{
    if(!isActiveAndEnabled)throw new IOException("Capture cancelled");
    keepsake=ScreenCapture.CaptureScreenshotAsTexture();if(!keepsake)throw new IOException("Capture unavailable");
    EnsureMemories();memories.Save(photo.EncodeToJPG(90),MemoryThumbnail(photo),photo.width,photo.height,momentSubject,momentQuote,true,momentBackground.EncodeToJPG(90),MemoryThumbnail(momentBackground),keepsake.EncodeToJPG(90),MemoryThumbnail(keepsake));
    StartSavedFlight(photo);photo=null;
   }catch(Exception){MemoryMessage("Couldn't save that moment. Please try again.");}
  }
  if(photo)Destroy(photo);if(keepsake)Destroy(keepsake);ClearMomentCapture();capturingMoment=false;vision.InteractionPaused=memoriesOpen||GuideModal||menu;
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
  float s=S;
  // Photo styling uses the full captured canvas, independent of the phone's safe area.
  var canvas=new Rect(0,0,Screen.width,Screen.height);
  float margin=24*s,width=Mathf.Min(canvas.width-(momentKeepsake?80:48)*s,360*s),portrait=momentPortrait?56*s:0,inset=16*s;
  float textWidth=width-2*inset-(portrait>0?portrait+12*s:0);
  string quote=string.IsNullOrEmpty(momentQuote)?"Quiet moment":momentQuote;float textScale=s*.90f;
  float maxText=canvas.height*.28f;
  for(int i=0;i<40&&typography.Measure(quote,text,textWidth,textScale).Height>maxText;i++)textScale*=.97f;
  float textHeight=typography.Measure(quote,text,textWidth,textScale).Height;
  float height=Mathf.Max(88*s,textHeight+52*s);
  float footer=momentKeepsake?48*s:0;
  var box=new Rect(canvas.center.x-width*.5f,canvas.yMax-margin-footer-height,width,height);
  if(momentKeepsake){
   Color frame=new Color(mint.r,mint.g,mint.b,.85f);float line=1.5f*s;
   Panel(new Rect(margin,margin,canvas.width-2*margin,line),frame,0);
   Panel(new Rect(margin,canvas.height-margin-line,canvas.width-2*margin,line),frame,0);
   Panel(new Rect(margin,margin,line,canvas.height-2*margin),frame,0);
   Panel(new Rect(canvas.width-margin-line,margin,line,canvas.height-2*margin),frame,0);
   Panel(new Rect(margin+line,canvas.height-margin-40*s,canvas.width-2*margin-2*line,40*s),new Color(ink.r,ink.g,ink.b,.88f),0);
   Label(new Rect(margin+14*s,canvas.height-margin-37*s,100*s,32*s),"with Pip",title,mint,TextAnchor.MiddleLeft);
   // Placed at its measured width so the date sits flush right on the same line as the signature.
   float dateW=Width(momentDate,title)+2*s;
   Label(new Rect(canvas.width-margin-14*s-dateW,canvas.height-margin-37*s,dateW,32*s),momentDate,title,new Color(white.r,white.g,white.b,.8f),TextAnchor.MiddleLeft);
  }
  Card(box,new Color(ink.r,ink.g,ink.b,.92f),18,.5f);
  if(momentPortrait)GUI.DrawTexture(new Rect(box.x+inset,box.center.y-portrait*.5f,portrait,portrait),momentPortrait,ScaleMode.ScaleToFit);
  float x=box.x+inset+(portrait>0?portrait+12*s:0);
  Label(new Rect(x,box.y+10*s,textWidth,24*s),"Pip",title,mint,TextAnchor.MiddleLeft);
  typography.Draw(new Rect(x,box.y+36*s,textWidth,textHeight),quote,text,C(white),textScale);
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
// One grouped control: the chosen style sits on the accent pill and slides between options.
 void DrawMemoryExport(float x,float width){
  float s=S;var row=new Rect(x,Safe.yMax-154*s,width,44*s);bool available=memories.HasClean(selectedMemory);
  Panel(row,well,22);
  if(available){
   string[] styles=memories.HasKeepsake(selectedMemory)?new[]{"Clean","Pip’s take","Keepsake"}:new[]{"Clean","Pip’s take"};
   int chosenIndex=Mathf.Max(0,System.Array.IndexOf(styles,memories.Style(selectedMemory)));
   if(styleSlide<0)styleSlide=chosenIndex;
   styleSlide=Mathf.Lerp(styleSlide,chosenIndex,1-Mathf.Exp(-Time.unscaledDeltaTime*18));
   float tw=row.width/styles.Length;
   var slider=new Rect(row.x+styleSlide*tw+3*s,row.y+3*s,tw-6*s,row.height-6*s);
   Panel(slider,new Color(mint.r,mint.g,mint.b,.32f),19);Panel(new Rect(slider.x+.8f*s,slider.y+.8f*s,slider.width-1.6f*s,slider.height-1.6f*s),accent,19);
   for(int i=0;i<styles.Length;i++){
    var tab=new Rect(row.x+i*tw,row.y,tw,row.height);bool chosen=i==chosenIndex;
    Label(tab,styles[i],PipTypeRole.SecondaryAction,chosen?mint:muted);
    if(Hit(tab,!photoSaving)&&!chosen){try{memories.SetStyle(selectedMemory,styles[i]);ClearMemoryTextures();photoSaveMessage=null;}catch(Exception){photoSaveMessage="Couldn't change this memory. Try again.";}}
   }
  }else Label(row,"Original capture",PipTypeRole.SecondaryAction,muted);
  if(Pill(new Rect(x,Safe.yMax-98*s,width,48*s),photoSaving?"Saving…":"Save to Photos",true,PipPhotos.Supported&&!photoSaving))SaveMemoryToPhotos();
  Label(new Rect(x,Safe.yMax-40*s,width,36*s),photoSaveMessage??(PipPhotos.Supported?"Saves the version shown above.":"Photo saving is available on iPhone."),small,muted,TextAnchor.UpperCenter);
 }
 static Rect Fit(Rect box,float aspect){float w=box.width,h=w/Mathf.Max(.01f,aspect);if(h>box.height){h=box.height;w=h*aspect;}return new Rect(box.center.x-w*.5f,box.center.y-h*.5f,w,h);}
 static Rect LerpRect(Rect a,Rect b,float t)=>new Rect(Mathf.Lerp(a.x,b.x,t),Mathf.Lerp(a.y,b.y,t),Mathf.Lerp(a.width,b.width,t),Mathf.Lerp(a.height,b.height,t));
 // Photos are drawn with the same rounded corners and soft shadow as every other surface.
 void Photo(Rect r,Texture2D t,float radius,ScaleMode mode=ScaleMode.StretchToFill){
  Shadow(r,radius,.6f);
  if(t)GUI.DrawTexture(r,t,mode,false,0,C(Color.white),0,radius*S);else Panel(r,well,radius);
 }
 void StartSavedFlight(Texture2D photo){ClearFlight();flyThumb=photo;flyT=0;}
 void ClearFlight(){if(flyThumb)Destroy(flyThumb);flyThumb=null;flyT=-1;}
 void StepMemoryMotion(float dt){
  if(flyT>=0){flyT+=dt/.7f;if(flyT>=1){ClearFlight();albumPulse=1;}}
  albumPulse=Mathf.MoveTowards(albumPulse,0,dt/.6f);
 }
 // A brief shutter flash, then the saved photo shrinks into the album button.
 void DrawSavedFlight(){
  if(!flyThumb||flyT<0)return;
  float t=Mathf.Clamp01(flyT),a=A;A=1;
  if(t<.2f)Panel(new Rect(0,0,Screen.width,Screen.height),new Color(1,1,1,.4f*(1-t/.2f)),0);
  float m=Mathf.Clamp01((t-.14f)/.86f),e=m<.5f?4*m*m*m:1-Mathf.Pow(-2*m+2,3)*.5f;
  float aspect=flyThumb.width/(float)flyThumb.height;
  var from=Fit(new Rect(Safe.x+32*S,Safe.y+72*S,Safe.width-64*S,Safe.height-220*S),aspect);
  var to=Fit(new Rect(MemoriesButton.center.x-18*S,MemoriesButton.center.y-18*S,36*S,36*S),aspect);
  var r=LerpRect(from,to,e);float radius=Mathf.Lerp(16,6,e);
  A=1-Mathf.Clamp01((m-.86f)/.14f);
  Photo(r,flyThumb,radius);
  GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,C(new Color(1,1,1,.9f)),Mathf.Lerp(3,1.5f,e)*S,radius*S);
  A=a;
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
  if(albumPulse>0)Arc(MemoriesButton.center,(21+9*(1-albumPulse))*S,0,Mathf.PI*2,2*S,new Color(mint.r,mint.g,mint.b,.85f*albumPulse),48);
  if(Circle(CaptureButton,8,!capturingMoment&&!placement.IsBusy&&flyT<0,false))CaptureMoment();
 }
 // Memories mirrors Live: the top-right circle sits where the menu button was and takes you back.
 void DrawMemories(){
  A=1;Panel(new Rect(0,0,Screen.width,Screen.height),backdrop,0);
  float s=S,x=Safe.x+20*s,w=Safe.width-40*s;var close=layout.More;
  if(selectedMemory!=null){DrawMemoryViewer(x,w,close);return;}
  Label(new Rect(x,close.y,w-60*s,close.height),"Memories",PipTypeRole.Heading,white,TextAnchor.MiddleLeft);
  if(Circle(close,4,true,false)){CloseMemories();return;}
  Label(new Rect(x,close.yMax+2*s,w,22*s),"Saved only on this iPhone",small,muted);
  float top=close.yMax+40*s;
  if(memories.Entries.Count==0){
   var empty=new Rect(x,top+90*s,w,200*s);
   Panel(new Rect(empty.center.x-32*s,empty.y-8*s,64*s,64*s),well,32);DrawIcon(new Rect(empty.center.x-24*s,empty.y,48*s,48*s),8,mint);
   Label(new Rect(x,empty.y+74*s,w,30*s),"Make your first memory",body,white,TextAnchor.MiddleCenter);
   Label(new Rect(x+16*s,empty.y+112*s,w-32*s,72*s),"Tap the camera in Live to keep a moment with Pip.",text,muted,TextAnchor.UpperCenter);return;
  }
  const int perPage=6;
  int pages=(memories.Entries.Count+perPage-1)/perPage;memoryPage=Mathf.Clamp(memoryPage,0,pages-1);
  float gap=10*s,cellW=(w-2*gap)/3,aspect=Screen.width/(float)Mathf.Max(1,Screen.height),labelH=24*s;
  float footerH=pages>1?64*s:0,room=Safe.yMax-top-footerH-8*s;
  float imageH=Mathf.Min(cellW/aspect,(room-gap)*.5f-labelH);
  for(int i=0;i<perPage;i++){
   int index=memoryPage*perPage+i;if(index>=memories.Entries.Count)break;var e=memories.Entries[index];
   var cell=new Rect(x+(i%3)*(cellW+gap),top+(i/3)*(imageH+labelH+gap),cellW,imageH+labelH);
   var texture=MemoryTexture(e,false);
   var image=new Rect(cell.x,cell.y,cellW,imageH);
   if(texture)image=Fit(image,texture.width/(float)texture.height);
   if(Pressed(cell))image=Scale(image,.96f);
   Photo(image,texture,12,ScaleMode.ScaleAndCrop);
   Label(new Rect(cell.x,image.yMax+4*s,cellW,20*s),ShortMemoryDate(e),small,muted,TextAnchor.UpperCenter);
   if(Hit(cell)){selectedMemory=e;photoSaveMessage=null;styleSlide=-1;if(memoryFull)Destroy(memoryFull);memoryFull=null;return;}
  }
  if(pages>1){
   float y=Safe.yMax-56*s;
   if(Circle(new Rect(x,y,44*s,44*s),10,memoryPage>0,false)){memoryPage--;ClearMemoryTextures();}
   if(Circle(new Rect(x+w-44*s,y,44*s,44*s),11,memoryPage<pages-1,false)){memoryPage++;ClearMemoryTextures();}
   Label(new Rect(x+56*s,y,w-112*s,44*s),(memoryPage+1)+" of "+pages,small,muted,TextAnchor.MiddleCenter);
  }
 }
 void DrawMemoryViewer(float x,float w,Rect close){
  float s=S;
  var back=new Rect(Safe.x+16*s,close.y,close.width,close.height);
  if(Circle(back,10,true,false)){selectedMemory=null;if(memoryFull)Destroy(memoryFull);memoryFull=null;return;}
  if(Circle(close,4,true,false)){CloseMemories();return;}
  Label(new Rect(back.xMax+8*s,close.y,close.x-back.xMax-16*s,close.height),MemoryDate(selectedMemory),title,white,TextAnchor.MiddleCenter);
  string caption=selectedMemory.withPipOverlay?"":ShortMemoryText(selectedMemory.remark,160);
  float captionHeight=string.IsNullOrEmpty(caption)?0:typography.Measure(caption,text,w,s).Height;
  float imageTop=close.yMax+16*s,imageBottom=Safe.yMax-170*s-captionHeight-(captionHeight>0?16*s:0);
  var area=new Rect(x,imageTop,w,Mathf.Max(80*s,imageBottom-imageTop));
  var photo=MemoryTexture(selectedMemory,true);
  var photoRect=photo?Fit(area,photo.width/(float)photo.height):area;
  Photo(photoRect,photo,16);
  if(!photo)Label(area,"This image couldn't be opened.",text,muted,TextAnchor.MiddleCenter);
  if(captionHeight>0)Label(new Rect(x,photoRect.yMax+16*s,w,captionHeight),caption,text,white);
  DrawMemoryExport(x,w);
 }
 string ShortMemoryDate(PipMemoryStore.Entry e){DateTime time;return DateTime.TryParse(e.createdUtc,out time)?time.ToLocalTime().ToString("MMM d"):"Saved";}
}
}
