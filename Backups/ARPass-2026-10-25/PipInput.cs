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
 readonly Color mint=new Color(.71f,.91f,.80f),ink=new Color(.15f,.14f,.13f,.84f),white=new Color(.98f,.97f,.94f),muted=new Color(.80f,.81f,.77f);
 // Motion: each tween eases toward its target once per frame in Update; drawing reads the eased value.
 struct Tween {
  public float T;
  public void Step(bool on,float inTime,float outTime,float dt){T=Mathf.MoveTowards(T,on?1:0,dt/Mathf.Max(.001f,on?inTime:outTime));}
  public float Out => 1-(1-T)*(1-T)*(1-T);
 }
 Tween bubbleIn,menuIn,guideIn,guideContent,footerIn,hintIn,ghostOut;
 float A=1; // opacity of the layer currently being drawn
 float shownProgress,pulse,footerWidth,footerTarget,guideHeight,guideTarget;
 readonly float[] dotWidth={1,0,0,0};
 int lastStep=-2;
 string lastHint="",ghostText,ghostSubject;
 int ghostVisible;
 Rect ghostRect,footerRect;
 float ghostTailX,ghostScroll,ghostHeight;
 bool ghostTail,wasBusy,wasPlaced,hadError;
 string lastSaid;
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
 bool OverControl(Vector2 p) => GuideModal || (walkthrough.Active&&guideRect.Contains(p)) || (ShowFeedback&&footerRect.Contains(p)) || layout.More.Contains(p) || (placement.IsPlacing ? layout.Summon.Contains(p)||(placement.CanCancelReposition&&layout.Cancel.Contains(p)) : layout.Pause.Contains(p)||(HasBubble&&layout.Bubble.Contains(p))||(HasAction&&layout.Learn.Contains(p)));
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
   else if(t.phase==TouchPhase.Canceled){gestureOwned=false;readingGesture=false;pointerDown=false;}
  }
#if UNITY_EDITOR
  if(Input.GetMouseButtonDown(0))BeginPointer(Input.mousePosition);
  if(Input.GetMouseButton(0))MovePointer(Input.mousePosition,Input.GetAxis("Mouse Y")*12);
  if(Input.GetMouseButtonUp(0))EndPointer(Input.mousePosition);
#endif
  Animate(Time.unscaledDeltaTime);
  Feel();
 }
 void Animate(float dt) {
  if(walkthrough.Step!=lastStep) {
   bool moved=lastStep<0||lastStep>=4||(lastStep==1)!=(walkthrough.Step==1);
   if(moved){guideIn.T=0;guideHeight=guideTarget=0;}
   guideContent.T=0;lastStep=walkthrough.Step;
  }
  bubbleIn.Step(HasBubble,.24f,.14f,dt);
  menuIn.Step(menu,.2f,.14f,dt);
  guideIn.Step(walkthrough.Active,.3f,.16f,dt);
  guideContent.Step(true,.26f,.26f,dt);
  footerIn.Step(layout.Safe.width>0&&ShowFeedback&&!walkthrough.Active,.2f,.24f,dt);
  hintIn.Step(true,.2f,.2f,dt);
  ghostOut.Step(false,1,.16f,dt);
  float k=1-Mathf.Exp(-dt*16);
  if(guideHeight>0)guideHeight=Mathf.Lerp(guideHeight,guideTarget,k);
  footerWidth=Mathf.Lerp(footerWidth,footerTarget,k);
  shownProgress=Mathf.Lerp(shownProgress,vision.Progress,1-Mathf.Exp(-dt*18));
  pulse=Mathf.MoveTowards(pulse,0,dt/.55f);
  for(int i=0;i<dotWidth.Length;i++)dotWidth[i]=Mathf.Lerp(dotWidth[i],i==walkthrough.Step?1:0,k);
 }
 // Haptics and the capture pulse follow state changes, so every source of a change feels the same.
 void Feel() {
  bool busy=vision.Busy;
  if(busy&&!wasBusy&&shownProgress>.5f){pulse=1;PipHaptics.Soft();}
  wasBusy=busy;
  bool placed=placement.HasCharacter&&!placement.IsPlacing;
  if(placed&&!wasPlaced)PipHaptics.Medium();
  wasPlaced=placed;
  bool error=vision.LastError!=null;
  if(error&&!hadError)PipHaptics.Warning();
  hadError=error;
  string said=HasBubble?Dialogue.Text:null;
  if(said!=null&&said!=lastSaid&&Dialogue.Kind==PipDialogueKind.Reaction)PipHaptics.Light();
  lastSaid=said;
 }
 void Prepare() { if(typography==null){theme=new PipUITheme();typography=new PipTypography(theme);} }
 Color C(Color c)=>new Color(c.r,c.g,c.b,c.a*A);
 void Label(Rect r,string value,PipTypeRole role,Color color,TextAnchor? alignment=null) {
  var anchor=alignment??(role==PipTypeRole.Action||role==PipTypeRole.SecondaryAction?TextAnchor.MiddleCenter:role==PipTypeRole.Label?TextAnchor.MiddleLeft:TextAnchor.UpperLeft);
  typography.Draw(r,value,role,C(color),S,anchor);
 }
 float Width(string value,PipTypeRole role)=>typography.Measure(value,role,10000,S).Style.CalcSize(new GUIContent(value)).x;
 void Panel(Rect r,Color color,float radius=18){if(A<=.002f)return;GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,C(color),0,Mathf.Min(radius*S,r.height*.5f));}
 // A few widening, faint layers read as a soft shadow and lift surfaces off the camera feed.
 void Shadow(Rect r,float radius,float strength=1) {
  for(int i=1;i<=3;i++){float g=i*2.2f*S;Panel(new Rect(r.x-g*.5f,r.y-g*.5f+i*1.4f*S,r.width+g,r.height+g),new Color(0,0,0,.055f*strength),radius+g*.5f/S);}
 }
 void Card(Rect r,Color fill,float radius=20) {
  Shadow(r,radius);
  Panel(r,new Color(1,1,1,.12f),radius);
  Panel(new Rect(r.x+.6f*S,r.y+.6f*S,r.width-1.2f*S,r.height-1.2f*S),fill,radius-.6f);
 }
 static Rect Scale(Rect r,float s)=>new Rect(r.center.x-r.width*s*.5f,r.center.y-r.height*s*.5f,r.width*s,r.height*s);
 bool Hit(Rect r,bool enabled=true,bool blocked=false) {
  bool old=GUI.enabled;GUI.enabled=old&&enabled&&!blocked;bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=old;
  if(hit)PipHaptics.Light();return hit;
 }
 bool Pressed(Rect r)=>pointerDown&&r.Contains(pointerStart)&&r.Contains(pointerPosition)&&!dragged;
 bool Pill(Rect r,string value,bool primary=false,bool enabled=true,bool blocked=false) {
  Color bg=primary?new Color(.20f,.33f,.27f,.96f):theme.Glass,fg=white;
  bool down=enabled&&!blocked&&Pressed(r);
  if(!enabled){bg=new Color(.2f,.2f,.18f,.38f);fg=new Color(1,1,1,.43f);}
  else if(down)bg=Color.Lerp(bg,Color.white,.1f);
  Rect v=down?Scale(r,.97f):r;
  if(enabled)Shadow(v,22,primary?1:.7f);
  if(primary&&enabled){Panel(v,new Color(mint.r,mint.g,mint.b,.32f),22);Panel(new Rect(v.x+.8f*S,v.y+.8f*S,v.width-1.6f*S,v.height-1.6f*S),bg,22);}
  else Panel(v,bg,22);
  Label(v,value,button,fg);return Hit(r,enabled,blocked);
 }
 bool Circle(Rect r,int icon,bool enabled,bool blocked) {
  bool down=Pressed(r)&&enabled&&!blocked;
  Rect visual=new Rect(r.center.x-21*S,r.center.y-21*S,42*S,42*S);if(down)visual=Scale(visual,.92f);
  var fill=theme.Control;if(down)fill.a=.66f;
  Shadow(visual,21,.6f);Panel(visual,fill,21);DrawIcon(r,icon,enabled?white:new Color(1,1,1,.35f));return Hit(r,enabled,blocked);
 }
 // 0 more, 1 pause, 2 play, 3 retry, 4 close, 5 move, 6 hello, 7 help.
 void DrawIcon(Rect r,int icon,Color color) {
  Vector2 c=r.center;
  if(icon==0){for(int i=-1;i<=1;i++)Panel(new Rect(c.x+i*6*S-1.6f*S,c.y-1.6f*S,3.2f*S,3.2f*S),color,2);}
  else if(icon==1){Panel(new Rect(c.x-5*S,c.y-7*S,3*S,14*S),color,1.5f);Panel(new Rect(c.x+2*S,c.y-7*S,3*S,14*S),color,1.5f);}
  else if(icon==2){GUI.color=C(color);GUI.DrawTexture(new Rect(c.x-4*S,c.y-7*S,11*S,14*S),theme.Play);GUI.color=Color.white;}
  else if(icon==4){Line(c+new Vector2(-4,-4)*S,c+new Vector2(4,4)*S,1.2f*S,color);Line(c+new Vector2(4,-4)*S,c+new Vector2(-4,4)*S,1.2f*S,color);}
  else if(icon==5){for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var n=new Vector2(-d.y,d.x);Line(c,c+d*8*S,1.3f*S,color);Line(c+d*8*S,c+(d*5+n*3)*S,1.3f*S,color);Line(c+d*8*S,c+(d*5-n*3)*S,1.3f*S,color);}}
  else {
   float start=icon==3?-.2f:0,end=icon==3?Mathf.PI*1.7f:Mathf.PI*2;
   Arc(c,8*S,start,end,1.2f*S,color,32);
   if(icon==7)Label(r,"?",small,color,TextAnchor.MiddleCenter);
   if(icon==6){Panel(new Rect(c.x-4*S,c.y-3*S,2*S,2*S),color,1);Panel(new Rect(c.x+2*S,c.y-3*S,2*S,2*S),color,1);Line(c+new Vector2(-3,3)*S,c+new Vector2(0,5)*S,1.1f*S,color);Line(c+new Vector2(0,5)*S,c+new Vector2(3,3)*S,1.1f*S,color);}
   if(icon==3){var tip=c+new Vector2(Mathf.Cos(end),Mathf.Sin(end))*8*S;Line(tip,tip+new Vector2(-4,0)*S,1.2f*S,color);Line(tip,tip+new Vector2(0,-4)*S,1.2f*S,color);}
  }
 }
 // Rotation is applied in the current local space (GUIUtility.RotateAroundPivot rotates in screen space,
 // which skews strokes whenever a surface is scaled during its animation).
 void Line(Vector2 a,Vector2 b,float width,Color color){if(A<=.002f)return;var m=GUI.matrix;GUI.color=Color.white;GUI.matrix=m*Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one)*Matrix4x4.Translate(-(Vector3)a);GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,Vector2.Distance(a,b),width),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,C(color),0,width*.5f);GUI.matrix=m;GUI.color=Color.white;}
 void Arc(Vector2 c,float radius,float start,float end,float width,Color color,int segments) {
  for(int i=0;i<segments;i++){float a=start+(end-start)*i/segments,b=start+(end-start)*(i+1)/segments;Line(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,color);}
 }
 // Corners draw in as the hold completes, so the marker visibly focuses on what Pip will look at.
 void Reticle(Vector2 c,float progress,bool ready) {
  float r=(17-4*progress)*S,l=(7+2*progress)*S;Color color=ready?mint:new Color(1,1,1,.75f);
  for(int i=0;i<4;i++){float x=i%2==0?-1:1,y=i<2?-1:1;var p=c+new Vector2(x*r,y*r);Line(p,p-new Vector2(x*l,0),2*S,color);Line(p,p-new Vector2(0,y*l),2*S,color);}
  if(progress>.01f){
   Arc(c,25*S,0,Mathf.PI*2,2*S,new Color(1,1,1,.14f*Mathf.Clamp01(progress*4)),48);
   int n=Mathf.RoundToInt(progress*48);if(n>0)Arc(c,25*S,-Mathf.PI*.5f,-Mathf.PI*.5f+Mathf.PI*2*n/48f,2*S,mint,n);
  }
 }
 void Pulse(Vector2 c) {
  if(pulse<=0)return;float e=1-pulse;
  Arc(c,(25+16*(1-(1-e)*(1-e)))*S,0,Mathf.PI*2,2*S,new Color(mint.r,mint.g,mint.b,.85f*pulse),48);
 }
 void Spinner(Vector2 c,float radius,Color color) {
  float t=Time.unscaledTime*5.5f;Arc(c,radius,t,t+Mathf.PI*1.35f,1.6f*S,color,14);
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
  // The layout reserves room for a footer line; replies no longer have one, so the bottom inset matches the sides.
  float content=HasBubble?typography.Measure(Dialogue.Text,body,w-48*S,S).Height-10*S:40*S;
  layout=PipUILayout.Calculate(Safe,placement.CompanionScreenPoint,Screen.height,content,HasBubble&&Dialogue.Pinned,placement.CanCancelReposition,HasAction);
  bubbleTarget=layout.Bubble.position;
  if(!bubbleInitialized || previousSafe!=Safe || shownText!=(Dialogue?Dialogue.Text:null)) {bubblePosition=bubbleTarget;bubbleVelocity=Vector2.zero;bubbleInitialized=true;previousSafe=Safe;}
  layout.SetBubblePosition(bubblePosition,HasAction);
  A=1;
  if(GuideModal){DrawWalkthrough(false);return;}
  // A modal menu owns all input: everything underneath fades back and cannot activate through it.
  bool blocked=menu;
  A=1-menuIn.Out;
  if(placement.IsPlacing) {
   Vector2 aim=placement.ScreenAimPoint;aim.y=Screen.height-aim.y;Reticle(aim,0,placement.CanPlace);
   if(Pill(layout.Summon,placement.IsBusy?"Summoning…":placement.HasCharacter?"Move Pip here":"Summon Pip",true,placement.CanPlace,blocked))placement.ConfirmPlacement();
   if(placement.CanCancelReposition&&Pill(layout.Cancel,"Cancel",false,true,blocked))placement.CancelReposition();
  } else {
   Vector2 aim=placement.ScreenAimPoint;aim.y=Screen.height-aim.y;
   if(Located&&vision.Ready&&vision.Watching&&!vision.Busy&&!(HasBubble&&Dialogue.Pinned))Reticle(aim,shownProgress,true);
   Pulse(aim);
   if(!HasBubble)DrawGhost();
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
  DrawFeedback();
  if(walkthrough.Active)DrawWalkthrough(blocked);
  A=1;
  if(menuIn.T>.001f)DrawMenu();
  if(Circle(layout.More,menu?4:0,true,false)){menu=!menu;vision.InteractionPaused=walkthrough.Active||menu;}
 }
 void DrawFeedback() {
  bool show=ShowFeedback&&!walkthrough.Active;
  if(show){string hint=Hint;if(hint!=lastHint){if(footerIn.T>.3f)hintIn.T=0;lastHint=hint;}}
  if(footerIn.T<=.001f){footerRect=new Rect();return;}
  // The hint keeps its last words while fading out, so it never flickers to an unrelated message.
  bool spin=placement.IsBusy||vision.Busy;
  float textWidth=Mathf.Min(Width(lastHint,small),layout.Footer.width-(spin?58:36)*S);
  footerTarget=Mathf.Min(layout.Footer.width,textWidth+(spin?58:36)*S);
  if(footerIn.T<.2f||footerWidth<=0)footerWidth=footerTarget;
  float fw=footerWidth,e=footerIn.Out;
  footerRect=new Rect(layout.Footer.center.x-fw*.5f,layout.Footer.y+(1-e)*6*S,fw,layout.Footer.height);
  float a=A;A*=e;
  Shadow(footerRect,19,.7f);Panel(footerRect,new Color(.15f,.14f,.13f,.80f),19);
  Rect label=new Rect(footerRect.x+(spin?40:18)*S,footerRect.y,footerRect.width-(spin?58:36)*S,footerRect.height);
  if(spin)Spinner(new Vector2(footerRect.x+24*S,footerRect.center.y),6*S,mint);
  A*=hintIn.Out;Label(label,lastHint,small,white,spin?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);
  A=a;
 }
 string SubjectName() {
  string s=vision.Subject;if(string.IsNullOrWhiteSpace(s))return null;s=s.Trim();
  return char.ToUpperInvariant(s[0])+s.Substring(1);
 }
 // Draws the name tag. Explanations carry the subject beside Pip's name so the reader keeps context.
 void Tag(Rect box,string subject) {
  Rect tag=new Rect(box.x+18*S,box.y+10*S,box.width-68*S,24*S);
  Label(tag,"Pip",title,mint);
  if(string.IsNullOrEmpty(subject))return;
  float pipW=Width("Pip",title),dotX=tag.x+pipW+7*S;
  Panel(new Rect(dotX,tag.center.y-1.25f*S,2.5f*S,2.5f*S),new Color(1,1,1,.35f),2);
  float room=tag.width-pipW-17*S;string shown=subject;
  // Drop whole words before ever cutting one; a single long word is the only case that gets clipped.
  while(Width(shown,small)>room){int cut=shown.TrimEnd('…').LastIndexOf(' ');if(cut<=0)break;shown=shown.Substring(0,cut).TrimEnd(',',' ')+"…";}
  Label(new Rect(dotX+10*S,tag.y,room,tag.height),shown,small,muted,TextAnchor.MiddleLeft);
 }
 void Tail(Rect box,float x) {
  // The pointer shares the card's rim and overlaps it slightly, so the two read as one surface.
  GUI.color=C(new Color(1,1,1,.12f));GUI.DrawTexture(new Rect(x-7.8f*S,box.yMax-.6f*S,15.6f*S,10.2f*S),theme.Tail);
  GUI.color=C(ink);GUI.DrawTexture(new Rect(x-7*S,box.yMax-.6f*S,14*S,9*S),theme.Tail);GUI.color=Color.white;
 }
 void Appear(float e,Vector2 pivot,float rise) {
  GUIUtility.ScaleAroundPivot(Vector2.one*(.94f+.06f*e),pivot);
  GUI.matrix=Matrix4x4.Translate(new Vector3(0,(1-e)*rise*S,0))*GUI.matrix;
 }
 void DrawBubble(bool blocked) {
  Rect box=layout.Bubble;
  float tailX=Mathf.Clamp(placement.CompanionScreenPoint.x,box.x+20*S,box.xMax-20*S);bool tail=!layout.Offscreen;
  if(shownText!=Dialogue.Text){shownText=Dialogue.Text;readingScroll=Vector2.zero;bubbleIn.T=Mathf.Min(bubbleIn.T,.45f);}
  var matrix=GUI.matrix;float a=A;
  A*=Mathf.Clamp01(bubbleIn.T*1.8f);Appear(bubbleIn.Out,new Vector2(tailX,box.yMax+9*S),8);
  string subject=Dialogue.Kind==PipDialogueKind.Explanation?SubjectName():null;
  Card(box,ink,20);if(tail)Tail(box,tailX);Tag(box,subject);
  DrawIcon(layout.Close,4,new Color(1,1,1,.5f));
  bool old=GUI.enabled;GUI.enabled=old&&!blocked;
  bool closed=GUI.Button(layout.Close,GUIContent.none,GUIStyle.none);
  GUI.enabled=old;
  if(closed){PipHaptics.Light();Remember(box,tailX,tail,subject,layout.Reading.height);Dialogue.Close();bubbleInitialized=false;GUI.matrix=matrix;A=a;return;}
  Rect reading=layout.Reading;
  float h=Mathf.Max(reading.height,typography.Measure(Dialogue.Text,body,reading.width-12*S,S).Height);
  scrollLimit=Mathf.Max(0,h-reading.height);
  readingScroll.y=Mathf.Clamp(readingScroll.y,0,scrollLimit);
  GUI.enabled=old&&!blocked;
  readingScroll=GUI.BeginScrollView(reading,readingScroll,new Rect(0,0,reading.width-12*S,h),false,false,GUIStyle.none,GUIStyle.none);
  DrawReading(reading,h,readingScroll.y,Dialogue.Text,Dialogue.VisibleText.Length);
  GUI.EndScrollView();GUI.enabled=old;
  if(scrollLimit>0){
   var rail=new Rect(reading.xMax-2*S,reading.y,2*S,reading.height);Panel(rail,new Color(1,1,1,.08f),1);
   float thumb=Mathf.Max(22*S,reading.height*reading.height/h),y=reading.y+readingScroll.y/scrollLimit*(reading.height-thumb);
   Panel(new Rect(rail.x,y,rail.width,thumb),new Color(1,1,1,.4f),1);
  }
  GUI.matrix=matrix;A=a;
  Remember(box,tailX,tail,subject,reading.height);
 }
 // Text that continues past the visible area fades at that edge instead of being cut mid-line.
 void DrawReading(Rect reading,float h,float scroll,string value,int visible) {
  float fade=30*S,top=scroll>1?scroll:float.NegativeInfinity,bottom=scroll<h-reading.height-1?scroll+reading.height:float.PositiveInfinity;
  typography.Draw(new Rect(0,0,reading.width-12*S,h),value,body,C(white),S,TextAnchor.UpperLeft,visible,top,bottom,fade);
 }
 void Remember(Rect box,float tailX,bool tail,string subject,float readingHeight) {
  ghostRect=box;ghostTailX=tailX;ghostTail=tail;ghostSubject=subject;ghostText=Dialogue.Text;ghostVisible=Dialogue.VisibleText.Length;ghostScroll=readingScroll.y;ghostHeight=readingHeight;ghostOut.T=1;
 }
 // A dismissed or expired bubble settles away instead of vanishing between frames.
 void DrawGhost() {
  if(ghostOut.T<=.001f||string.IsNullOrEmpty(ghostText))return;
  var matrix=GUI.matrix;float a=A;A*=ghostOut.Out;
  GUIUtility.ScaleAroundPivot(Vector2.one*(.97f+.03f*ghostOut.Out),new Vector2(ghostTailX,ghostRect.yMax+9*S));
  Card(ghostRect,ink,20);if(ghostTail)Tail(ghostRect,ghostTailX);Tag(ghostRect,ghostSubject);
  DrawIcon(new Rect(ghostRect.xMax-48*S,ghostRect.y+2*S,44*S,44*S),4,new Color(1,1,1,.5f));
  Rect reading=new Rect(ghostRect.x+18*S,ghostRect.y+36*S,ghostRect.width-36*S,ghostHeight);
  float h=Mathf.Max(reading.height,typography.Measure(ghostText,body,reading.width-12*S,S).Height);
  GUI.BeginScrollView(reading,new Vector2(0,ghostScroll),new Rect(0,0,reading.width-12*S,h),false,false,GUIStyle.none,GUIStyle.none);
  DrawReading(reading,h,ghostScroll,ghostText,ghostVisible);GUI.EndScrollView();
  GUI.matrix=matrix;A=a;
 }
 bool MenuRow(Rect r,string label,int icon,bool enabled) {
  if(Pressed(r)&&enabled&&menu)Panel(r,new Color(1,1,1,.07f),10);
  Color fg=enabled?white:new Color(1,1,1,.33f);DrawIcon(new Rect(r.x+10*S,r.y,32*S,r.height),icon,fg);
  Label(new Rect(r.x+52*S,r.y,r.width-64*S,r.height),label,PipTypeRole.SecondaryAction,fg,TextAnchor.MiddleLeft);return Hit(r,enabled,!menu);
 }
 void DrawMenu() {
  float e=menuIn.Out,a=A;A=Mathf.Clamp01(menuIn.T*1.5f);
  GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(0,0,0,.14f*e),0,0);
  var matrix=GUI.matrix;Rect r=layout.Menu;
  string note="When looking is on, a camera frame is sent to Google Gemini through our server.";
  float noteHeight=typography.Measure(note,small,r.width-40*S,S).Height;
  // The card fits its content; taps on the trimmed space below count as outside and dismiss it.
  r.height=Mathf.Min(r.height,236*S+noteHeight+20*S);layout.Menu=r;
  // The menu grows out of the button that opened it.
  GUIUtility.ScaleAroundPivot(Vector2.one*(.92f+.08f*e),new Vector2(layout.More.center.x,layout.More.yMax));
  Card(r,new Color(.16f,.15f,.14f,.94f),20);
  Label(new Rect(r.x+20*S,r.y+8*S,r.width-40*S,24*S),"Pip",title,mint);
  if(MenuRow(new Rect(r.x+8*S,r.y+36*S,r.width-16*S,44*S),"Move Pip",5,placement.HasCharacter&&!placement.IsBusy&&!vision.Busy&&!placement.IsPlacing)){placement.Reposition();menu=false;}
  if(MenuRow(new Rect(r.x+8*S,r.y+84*S,r.width-16*S,44*S),vision.Watching?"Pause looking":"Resume looking",vision.Watching?1:2,vision.Ready&&Located&&!placement.IsPlacing)){vision.ToggleWatching();menu=false;}
  if(MenuRow(new Rect(r.x+8*S,r.y+132*S,r.width-16*S,44*S),"Say hello",6,Dialogue&&!vision.Busy)){Dialogue.Chat();menu=false;}
  if(MenuRow(new Rect(r.x+8*S,r.y+180*S,r.width-16*S,44*S),"How it works",7,true)){walkthrough.Restart();menu=false;}
  for(int i=0;i<3;i++)Panel(new Rect(r.x+52*S,Mathf.Round(r.y+(82+48*i)*S),r.width-72*S,Mathf.Max(1,Mathf.Round(.5f*S))),new Color(1,1,1,.09f),0);
  Label(new Rect(r.x+20*S,r.y+236*S,r.width-40*S,noteHeight),note,small,muted);
  GUI.matrix=matrix;A=a;
 }
 void CompleteWalkthrough() {
  walkthrough.Finish();PlayerPrefs.SetInt(PipWalkthrough.PreferenceKey,1);PlayerPrefs.Save();
  vision.InteractionPaused=false;
  if(Dialogue){Dialogue.Close();bubbleInitialized=false;}
 }
 void Dots(Vector2 origin) {
  float x=origin.x;
  for(int i=0;i<dotWidth.Length;i++){float dw=(6+12*dotWidth[i])*S;Panel(new Rect(x,origin.y-3*S,dw,6*S),Color.Lerp(new Color(1,1,1,.22f),mint,dotWidth[i]),3);x+=dw+5*S;}
 }
 void DrawWalkthrough(bool blocked) {
  float w=Mathf.Min(354*S,Safe.width-32*S),contentWidth=w-48*S;
  string notice="Looking sends a frame to Google Gemini through our server. Pause whenever you like.";
  int step=walkthrough.Step;
  float headingHeight=typography.Measure(walkthrough.Heading,PipTypeRole.Heading,contentWidth,S).Height;
  float copyHeight=typography.Measure(walkthrough.Copy,text,contentWidth,S).Height;
  float noteHeight=step==2?typography.Measure(notice,small,contentWidth,S).Height:0;
  float bodyEnd=56*S+headingHeight+10*S+copyHeight;
  float contentEnd=bodyEnd+(step==2?14*S+noteHeight:0);
  guideTarget=contentEnd+(step==1?14*S+20*S+20*S:24*S+48*S+24*S);
  if(guideHeight<=0)guideHeight=guideTarget;
  float h=guideHeight,e=guideIn.Out;
  bool top=step==1;
  guideRect=new Rect(Safe.center.x-w/2,top?Safe.y+76*S:Safe.yMax-h-20*S,w,h);
  if(GuideModal)GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(0,0,0,.12f*e),0,0);
  var matrix=GUI.matrix;float a=A;A*=Mathf.Clamp01(guideIn.T*1.6f);
  GUI.matrix=Matrix4x4.Translate(new Vector3(0,(1-e)*(top?-14:18)*S,0))*GUI.matrix;
  Card(guideRect,ink,24);
  Dots(new Vector2(guideRect.x+24*S,guideRect.y+28*S));
  var skip=new Rect(guideRect.xMax-80*S,guideRect.y+6*S,64*S,44*S);
  Label(skip,"Skip",PipTypeRole.SecondaryAction,muted);
  if(Hit(skip,true,blocked)){CompleteWalkthrough();GUI.matrix=matrix;A=a;return;}
  // Step content settles in after the card, so changing steps reads as one motion.
  // New copy waits until the card has nearly finished resizing, so it never overlaps the action below.
  float settle=Mathf.Clamp01(1-Mathf.Abs(guideHeight-guideTarget)/(18*S));
  float c=guideContent.Out*settle,ca=A;A*=c;float rise=(1-c)*6*S;
  Label(new Rect(guideRect.x+24*S,guideRect.y+56*S+rise,contentWidth,headingHeight),walkthrough.Heading,PipTypeRole.Heading,white);
  Label(new Rect(guideRect.x+24*S,guideRect.y+56*S+headingHeight+10*S+rise,contentWidth,copyHeight),walkthrough.Copy,text,white);
  if(step==2)Label(new Rect(guideRect.x+24*S,guideRect.y+bodyEnd+14*S+rise,contentWidth,noteHeight),notice,small,muted);
  if(step==1){
   bool found=placement.CanPlace;Rect status=new Rect(guideRect.x+24*S,guideRect.yMax-40*S,contentWidth,20*S);
   if(found)Panel(new Rect(status.x,status.center.y-3*S,6*S,6*S),mint,3);else Spinner(new Vector2(status.x+4*S,status.center.y),4.5f*S,mint);
   Label(new Rect(status.x+16*S,status.y,status.width-16*S,status.height),found?"Surface found. Summon Pip below.":"Looking for a surface…",small,found?mint:muted,TextAnchor.MiddleLeft);
   A=ca;
  }
  else {
   A=ca;
   string action=step==3?"Ready":step==0?"Find a spot":"Continue";
   if(Pill(new Rect(guideRect.x+24*S,guideRect.yMax-24*S-48*S,contentWidth,48*S),action,true,true,blocked)){
    if(step==3)CompleteWalkthrough();else walkthrough.Next();
   }
  }
  GUI.matrix=matrix;A=a;
 }
 void OnDestroy(){if(theme!=null)theme.Dispose();}
 void OnDisable(){if(vision)vision.InteractionPaused=false;gestureOwned=false;readingGesture=false;menu=false;}
}}
