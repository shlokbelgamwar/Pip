using UnityEngine;
using System.Reflection;
namespace Pip.Editor {
// Focused regressions for real layout boundaries and gesture routing, with no network calls.
public static class PipUIReview {
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool condition,string message){if(!condition)throw new System.Exception(message);}
 static bool Inside(Rect outer,Rect inner)=>inner.x>=outer.x-.1f&&inner.y>=outer.y-.1f&&inner.xMax<=outer.xMax+.1f&&inner.yMax<=outer.yMax+.1f;
 public static string Layouts(){
  Rect[] areas={new Rect(0,0,320,568),new Rect(0,59,390,751),new Rect(0,62,440,860),new Rect(0,177,1320,2589),new Rect(0,0,390,680)};
  int count=0;
  foreach(var safe in areas)foreach(float height in new[]{35f,110f,500f})foreach(bool detail in new[]{false,true})foreach(bool cancel in new[]{false,true})foreach(bool action in new[]{false,true})foreach(int side in new[]{0,1,2,3}) {
   float sh=safe.yMax+34;Vector3 p=side==0?new Vector3(safe.center.x,sh-safe.center.y,1):side==1?new Vector3(-500,0,1):side==2?new Vector3(0,0,-1):new Vector3(safe.center.x,sh,1);
   var l=PipUILayout.Calculate(safe,p,sh,height*safe.width/390,detail,cancel,action);
   foreach(var r in new[]{l.Status,l.More,l.Menu,l.Pause,l.Footer,l.Notice,l.Summon,l.Bubble,l.Close,l.Reading,l.Capture,l.Album})Check(Inside(safe,r),"Outside safe area: "+r+" in "+safe);
   Check(Mathf.Abs((l.Capture.xMin+l.Pause.xMax)*.5f-safe.center.x)<.1f,"Camera/pause pair not centered");
   Check(!l.Capture.Overlaps(l.Pause)&&!l.Album.Overlaps(l.Capture)&&!l.Album.Overlaps(l.Pause),"Bottom controls overlap");
   Check(!l.Bubble.Overlaps(l.More)&&!l.Bubble.Overlaps(l.Pause),"Bubble collides with controls");
   Check(Inside(l.Bubble,l.Close)&&Inside(l.Bubble,l.Reading),"Bubble contents outside bounds");
   Check(l.Close.width/l.Scale>=44&&l.Pause.width/l.Scale>=44,"Small touch target");
   if(height<=110)Check(l.Bubble.height+.1f>=height*safe.width/390+64*l.Scale,"Reply unnecessarily height-capped");
   if(action){Check(Inside(safe,l.Learn)&&!l.Bubble.Overlaps(l.Learn)&&!l.Learn.Overlaps(l.Pause)&&!l.Learn.Overlaps(l.Footer),"Explanation action overlap");Check(Mathf.Abs(l.Learn.center.x-safe.center.x)<.1f&&Mathf.Abs(l.Learn.width-(safe.width-32*l.Scale))<.1f,"Bottom action must be centered and full width");}
   var sheet=l.SheetFor(height*2*l.Scale);Check(Inside(safe,sheet)&&!sheet.Overlaps(l.Learn)&&!sheet.Overlaps(l.More),"Explanation sheet overlaps controls");
   if(cancel)Check(Inside(safe,l.Cancel)&&!l.Cancel.Overlaps(l.Summon),"Cancel/confirm overlap");count++;
  }
  var tour=new PipWalkthrough(false);Check(tour.Active&&tour.Step==0,"First-use intro missing");tour.Next();Check(tour.Step==1,"Placement guide missing");tour.Next();Check(tour.Step==1,"Guide skipped actual placement");tour.ObservePlacement(false);Check(tour.Step==1,"Guide advanced without Pip");tour.ObservePlacement(true);Check(tour.Step==2,"Guide did not recognize placement");tour.Next();Check(tour.Step==3,"Explanation step missing");tour.Next();Check(!tour.Active,"Guide did not finish");tour.Restart();Check(tour.Step==0,"Replay failed");tour.Finish();Check(!tour.Active&&!new PipWalkthrough(true).Active,"Completed guide reappears");
  return "PASS "+count+" portrait layouts, content-sized bubbles, explanation sheets and walkthrough transitions";
 }
 public static string SpeechAvoidance(){
  int count=0,placed=0;
  foreach(var safe in new[]{new Rect(0,0,320,568),new Rect(0,59,390,751),new Rect(0,62,440,860)}){
   float sh=safe.yMax+34;var l=PipUILayout.Calculate(safe,new Vector3(safe.center.x,sh-safe.center.y,1),sh,80,false,false,true);float s=l.Scale;
   foreach(float x in new[]{-.1f,.1f,.3f,.5f,.7f,.9f,1.1f})foreach(float y in new[]{.05f,.25f,.5f,.75f,.95f})foreach(float width in new[]{180f,260f,300f})foreach(float height in new[]{60f,120f,240f})foreach(bool sheet in new[]{false,true}){
    var body=new Rect(safe.x+safe.width*x-45*s,safe.y+safe.height*y-80*s,90*s,160*s);Rect r;
    if(l.TrySpeechPosition(body,width*s,height*s,sheet,out r)){
     Check(Inside(safe,r),"Speech leaves viewport");Check(!r.Overlaps(body),"Speech covers Pip");
     Check(!r.Overlaps(l.More)&&!r.Overlaps(l.Learn),"Speech covers control");
     Check(!r.Overlaps(l.Pause)&&!r.Overlaps(l.Capture)&&!r.Overlaps(l.Album)&&!r.Overlaps(l.Footer),"Speech covers bottom controls/feedback");placed++;
    }count++;
   }
   Rect result;Check(!l.TrySpeechPosition(safe,200*s,100*s,false,out result),"Full-screen Pip must not be covered");
   Check(l.TrySpeechPosition(new Rect(safe.center.x-50*s,safe.center.y-60*s,100*s,120*s),200*s,60*s,false,out result),"Normal Pip has no speech space");
  }
  return "PASS "+count+" viewport/character/control collision cases ("+placed+" fitted), including no-room fallback";
 }
 public static string PetGestures(){
  var g=new PipPetGesture();g.Begin(Vector2.zero,0);
  Check(!g.Move(Vector2.one,1,true),"Rest/jitter counts as pet");
  g.Begin(Vector2.zero,0);Check(!g.Move(new Vector2(100,0),1,true),"Single swipe counts as pet");
  g.Begin(Vector2.zero,0);Check(!g.Move(new Vector2(25,0),.2f,true),"First stroke counts as pet");
  Check(!g.Move(new Vector2(-25,0),.4f,true),"One reversal counts as pet");
  Check(g.Move(new Vector2(25,0),.6f,true)&&g.Recognized,"Rubbing not recognized");
  Check(!g.Move(new Vector2(-25,0),.8f,true),"One rub emitted multiple pets");
  g.Cancel();Check(!g.Recognized&&!g.Move(Vector2.zero,1,true),"Cancellation retains pet");
  g.Begin(Vector2.zero,0);g.Move(new Vector2(25,0),.05f,true);g.Move(new Vector2(-25,0),.1f,true);
  Check(!g.Move(new Vector2(25,0),.15f,true),"Very fast flick counts as pet");
  g.Begin(Vector2.zero,0);g.Move(new Vector2(25,0),.2f,true);g.Move(new Vector2(-25,0),.4f,false);
  Check(!g.Move(new Vector2(25,0),.6f,true),"Outside-character strokes count as pet");
  g.Begin(Vector2.zero,0);g.Move(new Vector2(0,25),.2f,true);g.Move(new Vector2(0,-25),.4f,true);
  Check(g.Move(new Vector2(0,25),.6f,true),"Vertical rubbing not recognized");
  return "PASS taps/jitter, swipe, horizontal/vertical rubbing, minimum duration, one event per gesture, cancellation and off-character rejection";
 }
 public static string PetResponses(){
  var p=Object.FindFirstObjectByType<PipPlacement>();Check(p&&p.Dialogue,"Stage preview first");
  var c=p.Dialogue.GetComponent<PipCharacter>();var d=c.Dialogue;
  c.mood.Touch();c.mood.Touch();c.mood.Touch();Check(c.mood.Current==Mood.Angry,"Pokes did not annoy");
  d.Close();c.Pet();Check(c.mood.Current==Mood.Content&&d.Expression==Mood.Content&&d.IsVisible,"Pet did not soothe and respond happily");
  var line=d.Text;c.Pet();Check(d.Text==line,"Pet spam replaced dialogue");
  d.SayWithExpression("A pinned explanation.",PipDialogueKind.Explanation,Mood.Curious);
  c.Pet();Check(d.Kind==PipDialogueKind.Explanation&&d.Text=="A pinned explanation.","Pet destroyed reading sheet");
  return "PASS pokes remain distinct, pet soothes, happy pet response, response cooldown and pinned reading preserved";
 }
 public static string MemoryVariants(){
  string folder=System.IO.Path.Combine(Application.temporaryCachePath,"PipVariantsReview-"+System.Guid.NewGuid().ToString("N"));var image=new Texture2D(4,6,TextureFormat.RGB24,false);
  try{
   var bytes=image.EncodeToJPG();var store=new PipMemoryStore(folder);
   var e=store.Save(bytes,bytes,4,6,"case","Original words",true,bytes,bytes);
   Check(store.HasClean(e)&&store.DisplayPath(e)==store.ImagePath(e),"Default overlay version missing");
   store.SetOverlay(e,false);Check(store.DisplayPath(e)==store.CleanImagePath(e)&&store.DisplayPath(e,true)==store.CleanThumbnailPath(e),"Overlay-off does not select clean files");
   var restored=new PipMemoryStore(folder);e=restored.Entries[0];Check(e.hidePipOverlay&&restored.DisplayPath(e)==restored.CleanImagePath(e),"Per-memory choice did not persist");
   restored.SetOverlay(e,true);Check(restored.DisplayPath(e)==restored.ImagePath(e)&&e.remark=="Original words","Overlay-on did not restore original");
   var styled=restored.Save(bytes,bytes,4,6,"case","Exact quote",true,bytes,bytes,bytes,bytes);
   restored.SetStyle(styled,"Keepsake");Check(restored.DisplayPath(styled)==restored.KeepsakePath(styled)&&restored.DisplayPath(styled,true)==restored.KeepsakePath(styled,true),"Keepsake routing failed");
   var reload=new PipMemoryStore(folder);styled=reload.Entries.Find(e2=>e2.id==styled.id);Check(reload.Style(styled)=="Keepsake","Keepsake choice lost on reload");
   reload.SetStyle(styled,"Clean");Check(reload.DisplayPath(styled)==reload.CleanImagePath(styled),"Clean style failed");
   reload.SetStyle(styled,"Pip’s take");Check(reload.DisplayPath(styled)==reload.ImagePath(styled)&&styled.remark=="Exact quote","Quote style failed");
   var old=restored.Save(bytes,bytes,4,6,"old","Old words");Check(!restored.HasClean(old)&&restored.DisplayPath(old)==restored.ImagePath(old),"Legacy photo compatibility failed");
   bool rejected=false;try{restored.SetOverlay(old,false);}catch(System.InvalidOperationException){rejected=true;}Check(rejected,"Unavailable clean version silently selected");
   return "PASS clean/overlay image and thumbnail routing, persistent per-memory choice, original quote preservation and legacy photo compatibility";
  }finally{Object.DestroyImmediate(image);if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);}
 }
 public static string MemoriesStorage(){
  string folder=System.IO.Path.Combine(Application.temporaryCachePath,"PipMemoryReview-"+System.Guid.NewGuid().ToString("N"));
  var image=new Texture2D(4,6,TextureFormat.RGB24,false);
  try{
   var bytes=image.EncodeToJPG();var store=new PipMemoryStore(folder);
   Check(store.Entries.Count==0,"New gallery not empty");
   var first=store.Save(bytes,bytes,4,6,"earbud case","A tiny hotel. 🌿");
   var second=store.Save(bytes,bytes,4,6,"room","Adventure headquarters!");
   var restored=new PipMemoryStore(folder);Check(restored.Entries.Count==2&&restored.Entries[0].id==second.id,"Saved gallery not restored newest-first");
   Check(restored.Entries[1].remark=="A tiny hotel. 🌿","Caption metadata changed");
   Check(System.IO.File.ReadAllBytes(restored.ImagePath(first)).Length==bytes.Length,"Captured image changed");
   System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"broken.json"),"not json");restored.Reload();Check(restored.Entries.Count==2,"Corrupt record broke gallery");
   System.IO.File.Delete(restored.ThumbnailPath(first));restored.Reload();Check(restored.Entries.Count==1,"Incomplete memory not skipped");
   bool rejected=false;try{restored.ImagePath(new PipMemoryStore.Entry{id="../../escape"});}catch(System.ArgumentException){rejected=true;}Check(rejected,"Unsafe file path accepted");
   return "PASS local capture persistence, thumbnail records, newest-first reload, Unicode captions, corrupt/incomplete recovery and safe paths";
  }finally{Object.DestroyImmediate(image);if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);}
 }
 public static string ShakeScanning(){
  var g=new PipShakeGesture();Vector3 still=new Vector3(0,-1,0);var pose=Quaternion.identity;
  g.Sample(still,Vector3.zero,pose,0,true,true);
  g.Sample(still+Vector3.right*2,Vector3.zero,pose,.1f,true,true);
  Check(!g.Pending,"Single bump triggered scan");
  g.Sample(still-Vector3.right*2,Vector3.zero,pose,.25f,true,true);Check(!g.Pending,"Two peaks triggered scan");
  g.Sample(still+Vector3.right*2,Vector3.zero,pose,.4f,true,true);Check(g.Pending,"Deliberate shake not queued");
  Check(!g.Sample(still,Vector3.zero,pose,.5f,true,false),"Captured without tracking");
  g.Sample(still,Vector3.zero,pose,.8f,true,true);
  Check(!g.Sample(still,new Vector3(.1f,0,0),pose,1.2f,true,true),"Moving phone captured");
  Check(!g.Sample(still,new Vector3(.1f,0,0),pose,1.6f,true,true),"Capture skipped settle interval");
  Check(g.Sample(still,new Vector3(.1f,0,0),pose,2f,true,true)&&!g.Pending,"Settled shake did not produce one capture");
  g.Sample(still+Vector3.right*2,Vector3.zero,pose,2.1f,true,true);g.Sample(still-Vector3.right*2,Vector3.zero,pose,2.3f,true,true);g.Sample(still+Vector3.right*2,Vector3.zero,pose,2.5f,true,true);
  Check(!g.Pending,"Shake cooldown failed");
  g.Sample(still+Vector3.right*2,Vector3.zero,pose,10f,false,true);g.Sample(still-Vector3.right*2,Vector3.zero,pose,10.2f,false,true);g.Sample(still+Vector3.right*2,Vector3.zero,pose,10.4f,false,true);
  Check(!g.Pending,"Disabled shake queued scan");
  g.Sample(still-Vector3.right*2,Vector3.zero,pose,11,true,true);g.Sample(still+Vector3.right*2,Vector3.zero,pose,11.2f,true,true);g.Sample(still-Vector3.right*2,Vector3.zero,pose,11.4f,true,true);
  Check(g.Pending,"Later shake did not queue");g.Sample(still,Vector3.zero,pose,20,true,false);Check(!g.Pending,"Pending shake never timed out");
  var gate=new PipLookGate();var view=new byte[768];gate.AcceptCurrent(Vector3.zero,pose,view);
  Check(!gate.Sample(Vector3.zero,pose,view,4),"Manual capture immediately retriggered auto scan");
  return "PASS bump rejection, three alternating peaks, settle/tracking requirement, single capture, cooldown, disabled state, timeout and automatic duplicate suppression";
 }
 public static string MotionSuppression(){
  var floor=new byte[768];var shifted=new byte[768];var added=new byte[768];var room=new byte[768];
  var random=new System.Random(17);
  for(int i=0;i<floor.Length;i++){floor[i]=(byte)random.Next(30,160);room[i]=(byte)random.Next(25,225);}
  for(int y=0;y<24;y++)for(int x=0;x<32;x++)shifted[y*32+x]=floor[y*32+Mathf.Max(0,x-2)];
  System.Array.Copy(floor,added,768);
  for(int y=7;y<17;y++)for(int x=10;x<22;x++)added[y*32+x]=235;
  var gate=new PipLookGate();gate.Sample(Vector3.zero,Quaternion.identity,floor,0);
  Check(!gate.Sample(new Vector3(.02f,0,0),Quaternion.Euler(0,3,0),shifted,1)&&!gate.Sample(new Vector3(.02f,0,0),Quaternion.Euler(0,3,0),shifted,5),"Slight camera movement scanned textured floor");
  Check(!gate.Sample(new Vector3(.03f,0,0),Quaternion.Euler(0,4,0),room,6)&&!gate.Sample(new Vector3(.03f,0,0),Quaternion.Euler(0,4,0),room,10),"Broad change with slight movement triggered room scan");
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,added,11)&&gate.Sample(Vector3.zero,Quaternion.identity,added,13),"Introduced object not recognized");
  Check(!gate.Sample(new Vector3(.8f,0,0),Quaternion.Euler(0,45,0),room,14)&&gate.Sample(new Vector3(.8f,0,0),Quaternion.Euler(0,45,0),room,16),"Settled new room not inspected");
  return "PASS registered floor shift, slight-movement broad-change rejection, introduced object and settled new-room trigger";
 }
 public static string RepositionFlight(){
  Vector3 from=new Vector3(-1,.1f,0),to=new Vector3(2,.4f,1);
  Check(Vector3.Distance(PipPlacement.FlightPoint(from,to,0),from)<.0001f,"Flight starts with a teleport");
  Check(Vector3.Distance(PipPlacement.FlightPoint(from,to,1),to)<.0001f,"Flight misses destination");
  var middle=PipPlacement.FlightPoint(from,to,.5f);Check(middle.y>Vector3.Lerp(from,to,.5f).y,"Flight has no floating arc");
  for(int i=0;i<=100;i++){var p=PipPlacement.FlightPoint(from,to,i/100f);Check(p.x>=from.x-.001f&&p.x<=to.x+.001f,"Flight overshoots");}
  Check(PipPlacement.FlightPoint(from,from,.5f)==from,"Zero-distance flight drifts");
  foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}){
   var facing=PipPlacement.FlightFacing(Vector3.zero,direction+Vector3.up,Quaternion.identity);
   Check(Vector3.Dot(facing*Vector3.forward,direction)>.999f,"Flight does not face travel direction");
   Check(Vector3.Dot(facing*Vector3.up,Vector3.up)>.999f,"Flight tilted sideways");
  }
  var fallback=Quaternion.Euler(0,45,0);Check(Quaternion.Angle(PipPlacement.FlightFacing(Vector3.zero,Vector3.up,fallback),fallback)<.001f,"Vertical movement lost heading");
  return "PASS exact flight endpoints, arc, no overshoot, four travel headings, gravity upright and vertical heading fallback";
 }
 public static string ShakyHands(){
  var floor=new byte[768];var item=new byte[768];var random=new System.Random(43);
  for(int i=0;i<768;i++)floor[i]=(byte)random.Next(30,150);
  System.Array.Copy(floor,item,768);for(int y=7;y<17;y++)for(int x=10;x<22;x++)item[y*32+x]=235;
  var gate=new PipLookGate();gate.Sample(Vector3.zero,Quaternion.identity,floor,0);bool scanned=false;
  for(int n=0;n<=10;n++){
   int shift=n%3-1;var frame=new byte[768];
   for(int y=0;y<24;y++)for(int x=0;x<32;x++)frame[y*32+x]=item[y*32+Mathf.Clamp(x-shift,0,31)];
   bool hit=gate.Sample(new Vector3(n%2==0?.025f:-.025f,0,0),Quaternion.Euler(0,n%2==0?3:-3,0),frame,1+n*.25f);
   if(hit)scanned=true;
  }
  Check(scanned,"Hand tremor prevented new object recognition");
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,item,5),"Tremor scan immediately duplicated");
  gate=new PipLookGate();gate.Sample(Vector3.zero,Quaternion.identity,floor,0);
  for(int n=0;n<16;n++)Check(!gate.Sample(Vector3.right*n*.02f,Quaternion.Euler(0,n*2,0),item,1+n*.25f),"Continuous pan was treated as a held object");
  gate=new PipLookGate();gate.Sample(Vector3.zero,Quaternion.identity,floor,0);gate.Sample(Vector3.zero,Quaternion.identity,item,1);
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,floor,1.25f),"Absent object triggered");
  Check(gate.Sample(Vector3.zero,Quaternion.identity,item,3),"One noisy sample erased the hold");
  gate=new PipLookGate();gate.Sample(Vector3.zero,Quaternion.identity,floor,0);gate.Sample(Vector3.zero,Quaternion.identity,item,1);
  gate.Sample(Vector3.zero,Quaternion.identity,floor,2);Check(!gate.Sample(Vector3.zero,Quaternion.identity,item,3),"Removed object kept stale dwell");
  return "PASS new object with registered tremor, duplicate suppression, continuous pan rejection, transient noise and object removal";
 }
 public static string NewViews(){
  var gate=new PipLookGate();var floor=new byte[768];var item=new byte[768];var place=new byte[768];
  for(int i=0;i<floor.Length;i++){floor[i]=80;item[i]=(byte)(i<192?200:80);place[i]=(byte)(i%2==0?160:40);}
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,floor,0)&&!gate.Sample(Vector3.zero,Quaternion.identity,floor,5),"Placement floor triggered inspection");
  Check(!gate.Sample(Vector3.one,Quaternion.Euler(0,90,0),floor,6),"Pose alone triggered inspection");
  var brighter=new byte[768];for(int i=0;i<brighter.Length;i++)brighter[i]=110;
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,brighter,7),"Exposure change triggered inspection");
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,item,8),"New object skipped dwell");gate.Pause();
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,item,11),"Pause preserved stale dwell");
  Check(gate.Sample(Vector3.zero,Quaternion.identity,item,13),"New object did not trigger after hold");
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,item,16),"Repeated object triggered");
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,floor,17),"Returning to placement floor triggered");
  Check(!gate.Sample(Vector3.one,Quaternion.Euler(0,50,0),place,18)&&gate.Sample(Vector3.one,Quaternion.Euler(0,50,0),place,20),"New scene did not trigger");
  Check(!gate.Sample(Vector3.zero,Quaternion.identity,item,23),"Recently seen object triggered again");
  gate.RetryCurrent();Check(!gate.Sample(Vector3.zero,Quaternion.identity,item,24)&&gate.Sample(Vector3.zero,Quaternion.identity,item,26),"Explicit retry could not inspect same view");
  gate.Reset();Check(!gate.Sample(Vector3.zero,Quaternion.identity,place,30)&&!gate.Sample(Vector3.zero,Quaternion.identity,place,33),"Reposition baseline triggered");
  return "PASS silent placement baseline, pose/exposure suppression, new object/scene dwell, seen-view memory, pause, retry and reposition";
 }
 public static string Expressions(){
  var p=Object.FindFirstObjectByType<PipPlacement>();var v=Object.FindFirstObjectByType<PipVision>();
  Check(p&&p.editorPreview&&p.Dialogue,"Stage the editor preview first");var d=p.Dialogue;
  foreach(Mood expression in System.Enum.GetValues(typeof(Mood))){
   d.SayWithExpression("A line with intentional acting.",PipDialogueKind.Reaction,expression);
   Check(d.ActingMood==expression,"Dialogue expression lost");
   d.Reveal();Check(d.ActingMood==expression,"Reveal changed expression");
   d.Close();Check(d.ActingMood==d.GetComponent<PipCharacter>().mood.Current,"Ambient mood did not resume");
  }
  Check(PipVision.ReplyExpression("Angry",Mood.Content)==Mood.Angry,"Model expression not parsed");
  Check(PipVision.ReplyExpression(null,Mood.Sleepy)==Mood.Sleepy&&PipVision.ReplyExpression("99",Mood.Curious)==Mood.Curious,"Invalid expression not rejected");
  typeof(PipVision).GetField("lastReactionExpression",F).SetValue(v,Mood.Angry);
  typeof(PipVision).GetField("lastExplanationExpression",F).SetValue(v,Mood.Curious);
  v.ShowReaction();Check(d.ActingMood==Mood.Angry,"Cached reaction lost expression");
  v.KnowMore();Check(d.ActingMood==Mood.Curious,"Cached explanation lost expression");
  v.ShowReaction();Check(d.ActingMood==Mood.Angry,"Back to Pip lost expression");
  return "PASS dialogue expression priority, reveal, ambient return, model enum validation and cached explanation/back";
 }
 public static string Interactions(){
  var p=Object.FindFirstObjectByType<PipPlacement>();var ui=Object.FindFirstObjectByType<PipInput>();var v=Object.FindFirstObjectByType<PipVision>();
  Check(p&&p.editorPreview&&p.Dialogue,"Stage the editor preview first");
  var d=p.Dialogue;
  d.Say(new string('a',320),PipDialogueKind.Explanation);d.Reveal();typeof(PipDialogue).GetField("expires",F).SetValue(d,-1f);typeof(PipDialogue).GetMethod("Update",F).Invoke(d,null);
  Check(d.IsVisible&&d.Pinned,"Explanation expired while reading");
  d.Close();Check(!d.IsVisible,"Close did not immediately dismiss");
  v.ShowReaction();Check(d.Kind==PipDialogueKind.Reaction&&d.IsVisible,"Back to Pip did not restore reaction");
  v.KnowMore();Check(d.Kind==PipDialogueKind.Explanation&&!v.Busy,"Cached explanation sent a request");
  // Outside-menu tap would hit the world; ensure it is consumed and only dismisses.
  p.Reposition();Check(p.IsPlacing&&p.CanCancelReposition,"Cannot enter reposition");
  Check(string.IsNullOrEmpty(p.Feedback),"Reposition shows stale placement feedback");
  typeof(PipVision).GetMethod("Update",F).Invoke(v,null);Check(v.HasReaction,"Reposition cleared captured context before confirmation");
  typeof(PipInput).GetField("menu",F).SetValue(ui,true);
  var point=new Vector2(Screen.width*.45f,Screen.height*.45f);
  typeof(PipInput).GetMethod("BeginPointer",F).Invoke(ui,new object[]{point});
  typeof(PipInput).GetMethod("EndPointer",F).Invoke(ui,new object[]{point});
  Check(p.IsPlacing,"Menu dismissal accidentally placed Pip");Check(!(bool)typeof(PipInput).GetField("menu",F).GetValue(ui),"Menu did not close");
  // A drag that begins in the world must not become an aim-to-place tap on release.
  typeof(PipInput).GetMethod("BeginPointer",F).Invoke(ui,new object[]{point});
  var end=point+new Vector2(80,80);typeof(PipInput).GetMethod("MovePointer",F).Invoke(ui,new object[]{end,80f});typeof(PipInput).GetMethod("EndPointer",F).Invoke(ui,new object[]{end});
  Check(p.IsPlacing,"World drag accidentally summoned Pip");
  p.CancelReposition();Check(!p.IsPlacing&&p.HasCharacter,"Cancel lost Pip");
  typeof(PipVision).GetMethod("Update",F).Invoke(v,null);Check(v.HasReaction,"Cancel lost the previous reaction");
  NewViews();
  return "PASS pinned reading, immediate close, cached explanation/back, modal dismissal, drag routing, cancel reposition and paused dwell";
 }
}
}
