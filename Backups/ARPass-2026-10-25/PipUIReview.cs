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
   foreach(var r in new[]{l.Status,l.More,l.Menu,l.Pause,l.Footer,l.Notice,l.Summon,l.Bubble,l.Close,l.Reading})Check(Inside(safe,r),"Outside safe area: "+r+" in "+safe);
   Check(!l.Bubble.Overlaps(l.More)&&!l.Bubble.Overlaps(l.Pause),"Bubble collides with controls");
   Check(Inside(l.Bubble,l.Close)&&Inside(l.Bubble,l.Reading),"Bubble contents outside bounds");
   Check(l.Close.width/l.Scale>=44&&l.Pause.width/l.Scale>=44,"Small touch target");
   if(!l.Offscreen && sh-p.y-safe.y>200*l.Scale)Check(l.Bubble.yMax+16*l.Scale<=sh-p.y+.1f,"Speech bubble is below Pip");
   if(action){Check(Inside(safe,l.Learn)&&!l.Bubble.Overlaps(l.Learn)&&!l.Learn.Overlaps(l.Pause)&&!l.Learn.Overlaps(l.Footer),"Explanation action overlap");Check(Mathf.Abs(l.Learn.center.x-safe.center.x)<.1f&&Mathf.Abs(l.Learn.width-(safe.width-32*l.Scale))<.1f,"Bottom action must be centered and full width");}
   if(cancel)Check(Inside(safe,l.Cancel)&&!l.Cancel.Overlaps(l.Summon),"Cancel/confirm overlap");count++;
  }
  var tour=new PipWalkthrough(false);Check(tour.Active&&tour.Step==0,"First-use intro missing");tour.Next();Check(tour.Step==1,"Placement guide missing");tour.Next();Check(tour.Step==1,"Guide skipped actual placement");tour.ObservePlacement(false);Check(tour.Step==1,"Guide advanced without Pip");tour.ObservePlacement(true);Check(tour.Step==2,"Guide did not recognize placement");tour.Next();Check(tour.Step==3,"Explanation step missing");tour.Next();Check(!tour.Active,"Guide did not finish");tour.Restart();Check(tour.Step==0,"Replay failed");tour.Finish();Check(!tour.Active&&!new PipWalkthrough(true).Active,"Completed guide reappears");
  return "PASS "+count+" portrait layouts, above-Pip bubbles and walkthrough transitions";
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
  var gate=new PipLookGate();var sample=new byte[]{32,32};gate.Sample(Vector3.zero,Quaternion.identity,sample,0);gate.Pause();Check(!gate.Sample(Vector3.zero,Quaternion.identity,sample,3),"Pause preserved stale dwell");
  Check(gate.Sample(Vector3.zero,Quaternion.identity,sample,5),"Dwell did not restart");gate.Pause();Check(!gate.Sample(Vector3.zero,Quaternion.identity,sample,8),"Pause retriggered identical shown view");
  return "PASS pinned reading, immediate close, cached explanation/back, modal dismissal, drag routing, cancel reposition and paused dwell";
 }
}
}
