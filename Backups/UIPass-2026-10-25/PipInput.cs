using UnityEngine;
namespace Pip {
// Lightweight portrait AR overlay. All pointer gestures are owned from their beginning.
public class PipInput : MonoBehaviour {
 public PipPlacement placement;
 PipVision vision;
 PipUITheme theme;
 bool pointerDown;
 Vector2 pointerPosition;
 PipWalkthrough walkthrough;
 Rect guideRect;
 bool GuideModal => walkthrough!=null && walkthrough.Active && walkthrough.Step!=1;
 bool menu,gestureOwned,readingGesture,dragged,bubbleInitialized;
 Vector2 pointerStart,readingScroll,bubblePosition,bubbleVelocity,bubbleTarget;
 float pointerTime,scrollLimit;
 string shownText;
 Rect previousSafe;
 PipUILayout layout;
 PipTypography typography;
 const PipTypeRole text=PipTypeRole.Body,small=PipTypeRole.Caption,title=PipTypeRole.Label,button=PipTypeRole.Action,body=PipTypeRole.Speech;
 readonly Color mint=new Color(.71f,.91f,.80f),ink=new Color(.15f,.14f,.13f,.84f),white=new Color(.98f,.97f,.94f);
 Rect Safe => new Rect(Screen.safeArea.x,Screen.height-Screen.safeArea.yMax,Screen.safeArea.width,Screen.safeArea.height);
 float S => Mathf.Min(Safe.width/390f,Safe.height/680f);
 PipDialogue Dialogue => placement.Dialogue;
 bool Located => placement.Tracking && (!placement.HasCharacter || placement.IsPlacing || Dialogue);
 bool HasAction => Dialogue && vision.CanKnowMore && (!Dialogue.IsVisible || Dialogue.Kind==PipDialogueKind.Reaction || Dialogue.Kind==PipDialogueKind.Explanation);
 bool HasBubble => Dialogue && Dialogue.IsVisible;
 void Awake() {
  vision=GetComponent<PipVision>();if(!vision)vision=gameObject.AddComponent<PipVision>();vision.placement=placement;
  walkthrough=new PipWalkthrough(PlayerPrefs.GetInt(PipWalkthrough.PreferenceKey,0)==1);
  Application.targetFrameRate=60;Screen.sleepTimeout=SleepTimeout.NeverSleep;
 }
 bool ShowFeedback => !Located || placement.IsBusy || vision.Busy || !vision.Ready || vision.LastError!=null || (!placement.IsPlacing && layout.Offscreen) || (placement.IsPlacing && (!string.IsNullOrEmpty(placement.Feedback)));
 bool OverControl(Vector2 p) => GuideModal || (walkthrough.Active&&guideRect.Contains(p)) || (ShowFeedback&&layout.Footer.Contains(p)) || layout.More.Contains(p) || (placement.IsPlacing ? layout.Summon.Contains(p)||(placement.CanCancelReposition&&layout.Cancel.Contains(p)) : layout.Pause.Contains(p)||(HasBubble&&layout.Bubble.Contains(p))||(HasAction&&layout.Learn.Contains(p)));
 void BeginPointer(Vector2 screenPoint) {
  var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);pointerDown=true;pointerPosition=p;pointerStart=p;pointerTime=Time.unscaledTime;dragged=false;
  gestureOwned=menu||OverControl(p);readingGesture=!walkthrough.Active&&!menu&&HasBubble&&layout.Reading.Contains(p);
 }
 void MovePointer(Vector2 screenPoint,float screenDeltaY) {
  var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);pointerPosition=p;dragged|=Vector2.Distance(pointerStart,p)>10*S;
  if(readingGesture&&dragged) {readingScroll.y=Mathf.Clamp(readingScroll.y+screenDeltaY,0,scrollLimit);if(Dialogue)Dialogue.HoldReading();}
 }
 void EndPointer(Vector2 screenPoint) {
  var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);
  bool tap=!dragged&&Vector2.Distance(pointerStart,p)<10*S&&Time.unscaledTime-pointerTime<.65f;
  if(tap) {
   if(menu&&!layout.Menu.Contains(p)&&!layout.More.Contains(p))menu=false;
   else if(readingGesture&&Dialogue)Dialogue.Reveal();
   else if(!gestureOwned&&!menu&&!OverControl(p))placement.Tap(screenPoint);
  }
  pointerDown=false;gestureOwned=false;readingGesture=false;
 }
 void Update() {
  if(!placement)return;
  walkthrough.ObservePlacement(placement.HasCharacter&&!placement.IsPlacing);
  vision.InteractionPaused=walkthrough.Active||menu||readingGesture;
  if((menu||walkthrough.Active)&&Dialogue)Dialogue.HoldReading();
  if(bubbleInitialized)bubblePosition=Vector2.SmoothDamp(bubblePosition,bubbleTarget,ref bubbleVelocity,.16f,Mathf.Infinity,Time.unscaledDeltaTime);
  if(Input.touchCount>0) {
   var t=Input.GetTouch(0);
   if(t.phase==TouchPhase.Began)BeginPointer(t.position);
   else if(t.phase==TouchPhase.Moved)MovePointer(t.position,t.deltaPosition.y);
   else if(t.phase==TouchPhase.Ended)EndPointer(t.position);
   else if(t.phase==TouchPhase.Canceled){gestureOwned=false;readingGesture=false;}
  }
#if UNITY_EDITOR
  if(Input.GetMouseButtonDown(0))BeginPointer(Input.mousePosition);
  if(Input.GetMouseButton(0))MovePointer(Input.mousePosition,Input.GetAxis("Mouse Y")*12);
  if(Input.GetMouseButtonUp(0))EndPointer(Input.mousePosition);
#endif
 }
 void Prepare() { if(typography==null){theme=new PipUITheme();typography=new PipTypography(theme);} }
 void Label(Rect r,string value,PipTypeRole role,Color color,TextAnchor? alignment=null) {
  var anchor=alignment??(role==PipTypeRole.Action||role==PipTypeRole.SecondaryAction?TextAnchor.MiddleCenter:role==PipTypeRole.Label?TextAnchor.MiddleLeft:TextAnchor.UpperLeft);
  typography.Draw(r,value,role,color,S,anchor);
 }
 void Panel(Rect r,Color color,float radius=18){GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,Mathf.Min(radius*S,r.height*.5f));}
 void Card(Rect r,Color fill,float radius=20) {
  Panel(r,new Color(1,1,1,.12f),radius);
  Panel(new Rect(r.x+.6f*S,r.y+.6f*S,r.width-1.2f*S,r.height-1.2f*S),fill,radius-.6f);
 }
 bool Hit(Rect r,bool enabled=true,bool blocked=false) {
  bool old=GUI.enabled;GUI.enabled=old&&enabled&&!blocked;bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=old;return hit;
 }
 bool Pressed(Rect r)=>pointerDown&&r.Contains(pointerStart)&&r.Contains(pointerPosition)&&!dragged;
 bool Pill(Rect r,string value,bool primary=false,bool enabled=true,bool blocked=false) {
  Color bg=primary?new Color(.22f,.34f,.28f):theme.Glass,fg=white;
  if(!enabled){bg=new Color(.2f,.2f,.18f,.38f);fg=new Color(1,1,1,.43f);}
  else if(!blocked&&Pressed(r))bg=Color.Lerp(bg,Color.white,.12f);
  Panel(r,bg,22);Label(r,value,button,fg);return Hit(r,enabled,blocked);
 }
 bool Circle(Rect r,int icon,bool enabled,bool blocked) {
  Rect visual=new Rect(r.center.x-21*S,r.center.y-21*S,42*S,42*S);
  var fill=theme.Control;if(Pressed(r)&&enabled&&!blocked)fill.a=.66f;
  Panel(visual,fill,21);DrawIcon(r,icon,enabled?white:new Color(1,1,1,.35f));return Hit(r,enabled,blocked);
 }
 // 0 more, 1 pause, 2 play, 3 retry, 4 close, 5 move, 6 hello, 7 help.
 void DrawIcon(Rect r,int icon,Color color) {
  Vector2 c=r.center;
  if(icon==0){for(int i=-1;i<=1;i++)Panel(new Rect(c.x+i*6*S-1.6f*S,c.y-1.6f*S,3.2f*S,3.2f*S),color,2);}
  else if(icon==1){Panel(new Rect(c.x-5*S,c.y-7*S,3*S,14*S),color,1.5f);Panel(new Rect(c.x+2*S,c.y-7*S,3*S,14*S),color,1.5f);}
  else if(icon==2){GUI.color=color;GUI.DrawTexture(new Rect(c.x-4*S,c.y-7*S,11*S,14*S),theme.Play);GUI.color=Color.white;}
  else if(icon==4){Line(c+new Vector2(-4,-4)*S,c+new Vector2(4,4)*S,1.2f*S,color);Line(c+new Vector2(4,-4)*S,c+new Vector2(-4,4)*S,1.2f*S,color);}
  else if(icon==5){for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var n=new Vector2(-d.y,d.x);Line(c,c+d*8*S,1.3f*S,color);Line(c+d*8*S,c+(d*5+n*3)*S,1.3f*S,color);Line(c+d*8*S,c+(d*5-n*3)*S,1.3f*S,color);}}
  else {
   float start=icon==3?-.2f:0,end=icon==3?Mathf.PI*1.7f:Mathf.PI*2;
   for(int i=0;i<32;i++){float a=start+(end-start)*i/32,b=start+(end-start)*(i+1)/32;Line(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*8*S,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*8*S,1.2f*S,color);}
   if(icon==7)Label(r,"?",small,color,TextAnchor.MiddleCenter);
   if(icon==6){Panel(new Rect(c.x-4*S,c.y-3*S,2*S,2*S),color,1);Panel(new Rect(c.x+2*S,c.y-3*S,2*S,2*S),color,1);Line(c+new Vector2(-3,3)*S,c+new Vector2(0,5)*S,1.1f*S,color);Line(c+new Vector2(0,5)*S,c+new Vector2(3,3)*S,1.1f*S,color);}
   if(icon==3){var tip=c+new Vector2(Mathf.Cos(end),Mathf.Sin(end))*8*S;Line(tip,tip+new Vector2(-4,0)*S,1.2f*S,color);Line(tip,tip+new Vector2(0,-4)*S,1.2f*S,color);}
  }
 }
 void Line(Vector2 a,Vector2 b,float width,Color color){var m=GUI.matrix;GUI.color=Color.white;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),width),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,width*.5f);GUI.matrix=m;GUI.color=Color.white;}
 void Reticle(Vector2 c,float progress,bool ready) {
  float r=17*S,l=7*S;Color color=ready?mint:new Color(1,1,1,.75f);
  for(int i=0;i<4;i++){float x=i%2==0?-1:1,y=i<2?-1:1;var p=c+new Vector2(x*r,y*r);Line(p,p-new Vector2(x*l,0),2*S,color);Line(p,p-new Vector2(0,y*l),2*S,color);}
  for(int i=0;i<Mathf.RoundToInt(progress*48);i++){float a=(i/48f*360-90)*Mathf.Deg2Rad,b=((i+1)/48f*360-90)*Mathf.Deg2Rad;Line(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*25*S,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*25*S,2*S,mint);}
 }
 string Hint {
  get {
   if(!Located)return placement.Status=="AR is unavailable on this device"?"AR is unavailable on this device":"Move slowly toward the scanned area";
   if(placement.IsPlacing) {
    if(placement.IsBusy)return "Hold steady. Pip is on the way.";
    if(!string.IsNullOrEmpty(placement.Feedback))return placement.Feedback;
    return placement.CanPlace?(placement.HasCharacter?"Choose a spot, then tap Move Pip here":"Aim here, then tap Summon Pip"):"Slowly scan a floor or tabletop";
   }
   if(!vision.Ready)return "Looking isn't connected. You can still tap Pip.";
   if(vision.Busy)return "One little moment…";
   if(vision.LastError!=null)return "Tap retry when you're ready to look again";
   if(HasBubble&&Dialogue.Kind==PipDialogueKind.Explanation)return "No rush. Close this when you're done.";
   if(!vision.Watching)return "Tap play when curiosity calls";
   if(layout.Offscreen)return DirectionHint();
   return vision.Progress>0?"Hold steady. Almost there…":"Show Pip something. Hold for 2 seconds.";
  }
 }
 string DirectionHint() {
  Vector3 p=placement.CompanionScreenPoint;float dx=p.x-Safe.center.x;if(p.z<0)dx=-dx;
  if(Mathf.Abs(dx)>Safe.width*.12f || p.z<0)return dx<0?"Pip is to your left  ‹":"Pip is to your right  ›";
  return Screen.height-p.y<Safe.center.y?"Tilt up to find Pip":"Tilt down to find Pip";
 }
 void OnGUI() {
  if(!placement||!vision)return;Prepare();
  float w=Mathf.Min(248*S,Safe.width-32*S);
  float content=HasBubble?typography.Measure(Dialogue.Text,body,w-48*S,S).Height:40*S;
  layout=PipUILayout.Calculate(Safe,placement.CompanionScreenPoint,Screen.height,content,HasBubble&&Dialogue.Pinned,placement.CanCancelReposition,HasAction);
  bubbleTarget=layout.Bubble.position;
  if(!bubbleInitialized || previousSafe!=Safe || shownText!=(Dialogue?Dialogue.Text:null)) {bubblePosition=bubbleTarget;bubbleVelocity=Vector2.zero;bubbleInitialized=true;previousSafe=Safe;}
  layout.SetBubblePosition(bubblePosition,HasAction);
  if(GuideModal){DrawWalkthrough();return;}
  if(Circle(layout.More,menu?4:0,true,false)){menu=!menu;vision.InteractionPaused=walkthrough.Active||menu;}
  if(menu){DrawMenu();return;}
  // A modal menu owns all input; the underlying controls cannot activate through it.
  bool blocked=menu||GuideModal;
  if(placement.IsPlacing) {
   Vector2 aim=placement.ScreenAimPoint;aim.y=Screen.height-aim.y;if(!menu)Reticle(aim,0,placement.CanPlace);
   if(Pill(layout.Summon,placement.IsBusy?"Summoning…":placement.HasCharacter?"Move Pip here":"Summon Pip",true,placement.CanPlace,blocked))placement.ConfirmPlacement();
   if(placement.CanCancelReposition&&Pill(layout.Cancel,"Cancel",false,true,blocked))placement.CancelReposition();
  } else {
   if(!menu&&Located&&vision.Ready&&vision.Watching&&!vision.Busy&&!(HasBubble&&Dialogue.Pinned)){Vector2 aim=placement.ScreenAimPoint;aim.y=Screen.height-aim.y;Reticle(aim,vision.Progress,true);}
   if(HasBubble&&!walkthrough.Active)DrawBubble(blocked);
   if(HasAction&&!walkthrough.Active) {
    string action=vision.ShowingExplanation?"Back to Pip":"Tell me more";
    if(!HasBubble&&!string.IsNullOrEmpty(vision.Subject))action="Last look";
    if(Pill(layout.Learn,action,false,!vision.Busy,blocked)){if(vision.ShowingExplanation)vision.ShowReaction();else vision.KnowMore();}
   }
   bool canPause=vision.Ready&&Located;
   int pauseIcon=vision.LastError!=null?3:!vision.Watching?2:1;
   if(Circle(layout.Pause,pauseIcon,canPause,blocked)){vision.ToggleWatching();if(Dialogue&&Dialogue.Kind==PipDialogueKind.Error)Dialogue.Close();}

  }
  if(ShowFeedback&&!walkthrough.Active){Panel(layout.Footer,new Color(.04f,.07f,.06f,.82f),16);Label(new Rect(layout.Footer.x+10*S,layout.Footer.y,layout.Footer.width-20*S,layout.Footer.height),Hint,small,white,TextAnchor.MiddleCenter);}
  if(walkthrough.Active)DrawWalkthrough();
  if(menu)DrawMenu();
 }
 void DrawBubble(bool blocked) {
  Rect box=layout.Bubble;
  if(!layout.Offscreen){float x=Mathf.Clamp(placement.CompanionScreenPoint.x,box.x+20*S,box.xMax-20*S);GUI.color=ink;GUI.DrawTexture(new Rect(x-7*S,box.yMax,14*S,9*S),theme.Tail);GUI.color=Color.white;}
  Card(box,ink,20);
  string heading="Pip";
  Label(new Rect(box.x+18*S,box.y+10*S,box.width-68*S,24*S),heading,title,mint);
  DrawIcon(layout.Close,4,new Color(1,1,1,.5f));
  bool old=GUI.enabled;GUI.enabled=old&&!blocked;
  if(GUI.Button(layout.Close,GUIContent.none,GUIStyle.none)){Dialogue.Close();bubbleInitialized=false;}
  GUI.enabled=old;if(!HasBubble)return;
  if(shownText!=Dialogue.Text){shownText=Dialogue.Text;readingScroll=Vector2.zero;}
  Rect reading=layout.Reading;
  float h=Mathf.Max(reading.height,typography.Measure(Dialogue.Text,body,reading.width-12*S,S).Height);
  if(h>reading.height)reading.height=Mathf.Max(18*S,reading.height-18*S);
  scrollLimit=Mathf.Max(0,h-reading.height);
  readingScroll.y=Mathf.Clamp(readingScroll.y,0,scrollLimit);
  GUI.enabled=old&&!blocked;
  readingScroll=GUI.BeginScrollView(reading,readingScroll,new Rect(0,0,reading.width-12*S,h),false,false,GUIStyle.none,GUIStyle.none);
  typography.Draw(new Rect(0,0,reading.width-12*S,h),Dialogue.Text,body,white,S,TextAnchor.UpperLeft,Dialogue.VisibleText.Length);GUI.EndScrollView();GUI.enabled=old;
  if(scrollLimit>0){
   var rail=new Rect(reading.xMax-2*S,reading.y,2*S,reading.height);Panel(rail,new Color(1,1,1,.08f),1);
   float thumb=Mathf.Max(22*S,reading.height*reading.height/h),y=reading.y+readingScroll.y/scrollLimit*(reading.height-thumb);
   Panel(new Rect(rail.x,y,rail.width,thumb),new Color(1,1,1,.4f),1);
  }
  string hint=scrollLimit>1?"Swipe for more":"";
  Label(new Rect(box.x+18*S,box.yMax-18*S,box.width-36*S,15*S),hint,small,new Color(.78f,.84f,.80f));
 }
 bool MenuRow(Rect r,string label,int icon,bool enabled) {
  if(Pressed(r)&&enabled)Panel(r,new Color(1,1,1,.06f),8);
  Color fg=enabled?white:new Color(1,1,1,.33f);DrawIcon(new Rect(r.x+10*S,r.y,32*S,r.height),icon,fg);
  Label(new Rect(r.x+52*S,r.y,r.width-64*S,r.height),label,PipTypeRole.SecondaryAction,fg,TextAnchor.MiddleLeft);return Hit(r,enabled);
 }
 void DrawMenu() {
  GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(0,0,0,.12f),0,0);
  Rect r=layout.Menu;Card(r,new Color(.16f,.15f,.14f,.94f),20);
  Label(new Rect(r.x+20*S,r.y+8*S,r.width-40*S,24*S),"Pip",title,mint);
  if(MenuRow(new Rect(r.x+8*S,r.y+36*S,r.width-16*S,44*S),"Move Pip",5,placement.HasCharacter&&!placement.IsBusy&&!vision.Busy&&!placement.IsPlacing)){placement.Reposition();menu=false;}
  if(MenuRow(new Rect(r.x+8*S,r.y+84*S,r.width-16*S,44*S),vision.Watching?"Pause looking":"Resume looking",vision.Watching?1:2,vision.Ready&&Located&&!placement.IsPlacing)){vision.ToggleWatching();menu=false;}
  if(MenuRow(new Rect(r.x+8*S,r.y+132*S,r.width-16*S,44*S),"Say hello",6,Dialogue&&!vision.Busy)){Dialogue.Chat();menu=false;}
  if(MenuRow(new Rect(r.x+8*S,r.y+180*S,r.width-16*S,44*S),"How it works",7,true)){walkthrough.Restart();menu=false;}
  for(int i=0;i<3;i++)Panel(new Rect(r.x+52*S,r.y+(82+48*i)*S,r.width-72*S,.5f*S),new Color(1,1,1,.07f),0);
  Label(new Rect(r.x+20*S,r.y+236*S,r.width-40*S,64*S),"When looking is on, a camera frame is sent to Google Gemini through our server.",small,new Color(.80f,.79f,.75f));
 }
 void CompleteWalkthrough() {
  walkthrough.Finish();PlayerPrefs.SetInt(PipWalkthrough.PreferenceKey,1);PlayerPrefs.Save();
  vision.InteractionPaused=false;
  if(Dialogue){Dialogue.Close();bubbleInitialized=false;}
 }
 void DrawWalkthrough() {
  float w=Mathf.Min(354*S,Safe.width-32*S),contentWidth=w-48*S;
  string notice="Looking sends a frame to Google Gemini through our server. Pause whenever you like.";
  float headingHeight=typography.Measure(walkthrough.Heading,PipTypeRole.Heading,contentWidth,S).Height;
  float copyHeight=typography.Measure(walkthrough.Copy,text,contentWidth,S).Height;
  float noteHeight=walkthrough.Step==2?typography.Measure(notice,small,contentWidth,S).Height:0;
  float bodyEnd=48*S+headingHeight+12*S+copyHeight;
  float contentEnd=bodyEnd+(walkthrough.Step==2?16*S+noteHeight:0);
  float h=contentEnd+(walkthrough.Step==1?16*S+20*S+24*S:24*S+44*S+24*S);
  guideRect=new Rect(Safe.center.x-w/2,walkthrough.Step==1?Safe.y+76*S:Safe.yMax-h-20*S,w,h);
  if(GuideModal)GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(0,0,0,.10f),0,0);
  Card(guideRect,ink,24);
  Label(new Rect(guideRect.x+24*S,guideRect.y+18*S,w-104*S,20*S),"Getting started",title,mint);
  var skip=new Rect(guideRect.xMax-80*S,guideRect.y+6*S,64*S,44*S);Label(skip,"Skip",PipTypeRole.SecondaryAction,new Color(.85f,.86f,.82f));
  if(Hit(skip)){CompleteWalkthrough();return;}
  Label(new Rect(guideRect.x+24*S,guideRect.y+48*S,contentWidth,headingHeight),walkthrough.Heading,PipTypeRole.Heading,white);
  Label(new Rect(guideRect.x+24*S,guideRect.y+48*S+headingHeight+12*S,contentWidth,copyHeight),walkthrough.Copy,text,white);
  if(walkthrough.Step==2)Label(new Rect(guideRect.x+24*S,guideRect.y+bodyEnd+16*S,contentWidth,noteHeight),notice,small,new Color(.80f,.81f,.77f));
  if(walkthrough.Step==1)Label(new Rect(guideRect.x+24*S,guideRect.y+contentEnd+16*S,contentWidth,20*S),placement.CanPlace?"Surface found. Summon Pip below.":"Looking for a surface…",small,mint);
  else if(Pill(new Rect(guideRect.x+24*S,guideRect.y+contentEnd+24*S,contentWidth,44*S),walkthrough.Step==3?"Ready":walkthrough.Step==0?"Find a spot":"Continue",false)){
   if(walkthrough.Step==3)CompleteWalkthrough();else walkthrough.Next();
  }
  for(int i=0;i<4;i++)Panel(new Rect(guideRect.center.x-24*S+i*13*S,guideRect.yMax-11*S,7*S,2*S),i==walkthrough.Step?mint:new Color(1,1,1,.2f),1);
 }
 void OnDestroy(){if(theme!=null)theme.Dispose();}
 void OnDisable(){if(vision)vision.InteractionPaused=false;gestureOwned=false;readingGesture=false;menu=false;}
}}
