using UnityEngine;
namespace Pip {
// Lightweight portrait AR overlay. All pointer gestures are owned from their beginning.
// The screen belongs to the world: speech is as light as its content allows, hints appear only for problems,
// and controls show up when they apply.
public partial class PipInput : MonoBehaviour {
 public PipPlacement placement;
 PipVision vision;
 PipUITheme theme;
 bool pointerDown;
 int activeTouch=-1;
 PipCharacter petTarget;
 readonly PipPetGesture petGesture=new PipPetGesture();
 Vector2 pointerPosition;
 PipWalkthrough walkthrough;
 Rect guideRect;
 // Only the welcome card takes over the screen. The rest of onboarding happens in context.
 bool GuideModal => walkthrough!=null && walkthrough.Active && walkthrough.Step==0;
 bool menu,gestureOwned,readingGesture,dragged,bubbleInitialized;
 Vector2 pointerStart,bubblePosition,bubbleVelocity,bubbleTarget;
 float pointerTime;
 string shownText;
 Rect previousSafe;
 PipUILayout layout;
 PipTypography typography;
 const PipTypeRole text=PipTypeRole.Body,small=PipTypeRole.Caption,title=PipTypeRole.Label,button=PipTypeRole.Action,body=PipTypeRole.Speech;
 readonly Color mint=new Color(.71f,.91f,.80f),ink=new Color(.15f,.14f,.13f,.84f),white=new Color(.98f,.97f,.94f),muted=new Color(.80f,.81f,.77f);
 // Shared surface tokens: one accent fill for chosen/primary states, one faint well for grouped controls.
 readonly Color accent=new Color(.20f,.33f,.27f,.96f),well=new Color(1,1,1,.05f),hairline=new Color(1,1,1,.09f),backdrop=new Color(.11f,.105f,.10f,1);
 // Reactions and chatter are light barks above Pip. Explanations get a reading sheet. Errors keep a dismissible card.
 enum Surface { Bark, Card, Sheet }
 Surface speech;
 Rect speechRect,speechReading,speechClose;
 Rect companionBounds;
 bool hasSpeechSpace=true;
 struct Tween {
  public float T;
  public void Step(bool on,float inTime,float outTime,float dt){T=Mathf.MoveTowards(T,on?1:0,dt/Mathf.Max(.001f,on?inTime:outTime));}
  public float Out => 1-(1-T)*(1-T)*(1-T);
 }
 Tween bubbleIn,menuIn,guideIn,footerIn,hintIn,ghostOut,actionIn,thinkIn,arrowIn;
 float A=1; // opacity of the layer currently being drawn
 float shownProgress,pulse,footerWidth,footerTarget,reactionSeen=-100,noSurfaceSince,coachAt=-1;
 int lastStep=-2;
 bool coachSaid,sawAction,learnRequested,wasBusy,wasPlaced;
 string lastHint="",lastAction="Tell me more";
 Surface ghostSurface;
 Rect ghostRect,ghostReading,ghostClose,footerRect;
 string ghostText,ghostSubject;
 int ghostVisible;
 float ghostTailX;
 bool ghostTail;
 Vector2 arrowPoint,arrowDir=Vector2.right;
 bool arrowPlaced;
 float awaySince=-1;
 bool FollowPrompt => awaySince>=0&&Time.unscaledTime-awaySince>=2&&!placement.IsBusy&&!placement.IsPlacing&&!vision.Busy&&!HasBubble&&!GuideModal&&!menu&&!memoriesOpen&&!capturingMoment;

 Rect Safe => new Rect(Screen.safeArea.x,Screen.height-Screen.safeArea.yMax,Screen.safeArea.width,Screen.safeArea.height);
 float S => Mathf.Min(Safe.width/390f,Safe.height/680f);
 PipDialogue Dialogue => placement.Dialogue;
 bool Located => placement.Tracking && (!placement.HasCharacter || placement.IsPlacing || Dialogue);
 bool HasBubble => Dialogue && Dialogue.IsVisible;
 bool Explaining => HasBubble && Dialogue.Kind==PipDialogueKind.Explanation;
 bool Learning => learnRequested && vision.Busy;
 // Tell me more is offered while a reaction or explanation is up and briefly after, then gets out of the way.
 bool ActionWindow => HasBubble ? Dialogue.Kind==PipDialogueKind.Reaction||Dialogue.Kind==PipDialogueKind.Explanation : Time.unscaledTime-reactionSeen<6;
 bool HasAction => Dialogue && ActionWindow && (vision.CanKnowMore || Learning);
 bool Thinking => (vision.Busy||vision.ShakePending) && !learnRequested && Located && !placement.IsPlacing && !HasBubble && !GuideModal;
 bool ArrowVisible => placement.HasCharacter && !placement.IsPlacing && Located && layout.Offscreen && !GuideModal && !menu;
 static Surface SurfaceOf(PipDialogueKind kind)=>kind==PipDialogueKind.Explanation?Surface.Sheet:kind==PipDialogueKind.Error?Surface.Card:Surface.Bark;
 void Awake() {
  vision=GetComponent<PipVision>();if(!vision)vision=gameObject.AddComponent<PipVision>();vision.placement=placement;
  walkthrough=new PipWalkthrough(PlayerPrefs.GetInt(PipWalkthrough.PreferenceKey,0)==1);
  Application.targetFrameRate=60;Screen.sleepTimeout=SleepTimeout.NeverSleep;
  if(placement&&placement.reticle){var ring=new GameObject("Surface ring").AddComponent<PipSurfaceRing>();ring.target=placement.reticle;ring.placement=placement;}
 }
 // Text hints are for problems and first-time placement only. Everything else is shown, not told.
 string Problem {
  get {
   if(MemoryNotice!=null)return MemoryNotice;
   if(vision.ShakePending)return "Hold still. I'll take a look.";
   if(HasBubble&&!hasSpeechSpace)return "Step back a little to give my words some room";
   if(!Located)return placement.Status=="AR is unavailable on this device"?"AR is unavailable on this device":"Move slowly toward the scanned area";
   if(placement.IsPlacing) {
    if(!string.IsNullOrEmpty(placement.Feedback))return placement.Feedback;
    if(walkthrough.Step==1&&!placement.IsBusy)return placement.CanPlace?(placement.HasCharacter?"Tap Move Pip here when the ring looks right":"Tap Summon Pip to place me here"):"Slowly scan a table or floor";
    if(!placement.CanPlace&&Time.unscaledTime-noSurfaceSince>4)return "Try more light, or aim at a textured table or floor";
    return null;
   }
   if(!vision.Ready)return "Looking isn't connected. You can still tap Pip.";
   if(vision.LastError!=null&&!(HasBubble&&Dialogue.Kind==PipDialogueKind.Error))return "Tap retry when you're ready to look again";
   return null;
  }
 }
 bool ShowFeedback => !GuideModal && Problem!=null;
 bool OverControl(Vector2 p) => memoriesOpen || capturingMoment || MemoriesButton.Contains(p) || CaptureButton.Contains(p) || GuideModal || (ShowFeedback&&footerRect.Contains(p)) || layout.More.Contains(p) || (placement.IsPlacing ? layout.Summon.Contains(p)||(placement.CanCancelReposition&&layout.Cancel.Contains(p)) : layout.Pause.Contains(p)||(HasBubble&&speechRect.Contains(p))||((HasAction||FollowPrompt)&&layout.Learn.Contains(p)));
 void BeginPointer(Vector2 screenPoint) {
  var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);pointerDown=true;pointerPosition=p;pointerStart=p;pointerTime=Time.unscaledTime;dragged=false;
  petGesture.Cancel();petTarget=null;
  gestureOwned=menu||OverControl(p);readingGesture=!memoriesOpen&&!capturingMoment&&!GuideModal&&!menu&&HasBubble&&(speech==Surface.Bark?speechRect:speechReading).Contains(p);
  if(!gestureOwned){petTarget=placement.CharacterAt(screenPoint);if(petTarget){gestureOwned=true;petGesture.Begin(screenPoint/S,Time.unscaledTime);}}
 }
 void MovePointer(Vector2 screenPoint,float screenDeltaY) {
  var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);pointerPosition=p;dragged|=Vector2.Distance(pointerStart,p)>10*S;
  if(readingGesture&&dragged&&Dialogue)Dialogue.HoldReading();
  if(petTarget){
   bool over=!menu&&!GuideModal&&!OverControl(p)&&placement.CharacterAt(screenPoint)==petTarget;
   if(petGesture.Move(screenPoint/S,Time.unscaledTime,over)){vision.CompanionPetted();petTarget.Pet();}
   if(over&&petGesture.Recognized)petTarget.ContinuePet();
  }
 }
 void EndPointer(Vector2 screenPoint) {
  var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);
  bool tap=!dragged&&Vector2.Distance(pointerStart,p)<10*S&&Time.unscaledTime-pointerTime<.65f;
  if(tap) {
   if(petTarget){if(!petGesture.Recognized&&!OverControl(p)&&placement.CharacterAt(screenPoint)==petTarget)petTarget.React();}
   else if(menu&&!layout.Menu.Contains(p)&&!layout.More.Contains(p))menu=false;
   // A tap on a bark finishes it, then dismisses it. Longer surfaces only reveal on tap and close with their button.
   else if(readingGesture&&Dialogue){if(speech==Surface.Bark)Dialogue.Advance();else Dialogue.Reveal();}
   else if(!gestureOwned&&!menu&&!OverControl(p))placement.Tap(screenPoint);
  }
  CancelPointer();
 }
 void CancelPointer(){
  pointerDown=false;gestureOwned=false;readingGesture=false;petTarget=null;petGesture.Cancel();activeTouch=-1;
 }
 void OnApplicationFocus(bool focused){if(!focused)CancelPointer();}
 void Update() {
  if(!placement)return;
  if(petTarget&&(!placement.Tracking||placement.IsPlacing))CancelPointer();
  walkthrough.ObservePlacement(placement.HasCharacter&&!placement.IsPlacing);
  vision.InteractionPaused=memoriesOpen||capturingMoment||GuideModal||menu||readingGesture||petTarget;
  if((memoriesOpen||capturingMoment||menu||GuideModal)&&Dialogue)Dialogue.HoldReading();
  if(bubbleInitialized)bubblePosition=Vector2.SmoothDamp(bubblePosition,bubbleTarget,ref bubbleVelocity,.16f,Mathf.Infinity,Time.unscaledDeltaTime);
  if(HasBubble&&(Dialogue.Kind==PipDialogueKind.Reaction||Dialogue.Kind==PipDialogueKind.Explanation))reactionSeen=Time.unscaledTime;
  if(!(placement.IsPlacing&&!placement.CanPlace))noSurfaceSince=Time.unscaledTime;
  if(!vision.Busy)learnRequested=false;
  if(activeTouch<0&&Input.touchCount>0){var t=Input.GetTouch(0);if(t.phase==TouchPhase.Began){activeTouch=t.fingerId;BeginPointer(t.position);}}
  else if(activeTouch>=0){
   bool found=false;
   for(int i=0;i<Input.touchCount;i++){
    var t=Input.GetTouch(i);if(t.fingerId!=activeTouch)continue;found=true;
    if(t.phase==TouchPhase.Moved)MovePointer(t.position,t.deltaPosition.y);
    else if(t.phase==TouchPhase.Ended)EndPointer(t.position);
    else if(t.phase==TouchPhase.Canceled)CancelPointer();
    break;
   }
   if(!found)CancelPointer();
  }
#if UNITY_EDITOR
  if(Input.GetMouseButtonDown(0))BeginPointer(Input.mousePosition);
  if(Input.GetMouseButton(0))MovePointer(Input.mousePosition,Input.GetAxis("Mouse Y")*12);
  if(Input.GetMouseButtonUp(0))EndPointer(Input.mousePosition);
#endif
  if(!memoriesOpen&&!capturingMoment)Coach();
  var point=placement.CompanionScreenPoint;var viewport=placement.arCamera.pixelRect;
  var view=new Vector3((point.x-viewport.x)/viewport.width,(point.y-viewport.y)/viewport.height,point.z);
  bool away=placement.HasCharacter&&!placement.IsPlacing&&!placement.IsBusy&&placement.Tracking&&(view.z<=0||view.x<0||view.x>1||view.y<0||view.y>1);
  if(!away||memoriesOpen||GuideModal||menu)awaySince=-1;else if(awaySince<0)awaySince=Time.unscaledTime;
  Animate(Time.unscaledDeltaTime);
  Feel();
 }
 // After placement Pip explains looking in its own voice, then the first reaction highlights Tell me more once.
 void Coach() {
  int step=walkthrough.Step;
  if(step==2) {
   if(coachAt<0)coachAt=Time.unscaledTime+4.5f;
   if(!coachSaid&&Time.unscaledTime>=coachAt&&Dialogue&&!vision.Busy&&!(HasBubble&&Dialogue.Pinned)){Dialogue.Say(PipWalkthrough.CoachLine);coachSaid=true;}
   if(HasBubble&&Dialogue.Kind==PipDialogueKind.Reaction){walkthrough.Next();sawAction=false;}
  }
  else if(step==3) {
   if(HasAction)sawAction=true;
   else if(sawAction&&!vision.Busy)Complete(false);
  }
 }
 void Animate(float dt) {
  if(walkthrough.Step!=lastStep) {
   if(walkthrough.Step==0)guideIn.T=0;
   if(walkthrough.Step==2){coachAt=-1;coachSaid=false;}
   lastStep=walkthrough.Step;
  }
  bubbleIn.Step(HasBubble,.22f,.14f,dt);
  menuIn.Step(menu,.2f,.14f,dt);
  guideIn.Step(GuideModal,.3f,.16f,dt);
  footerIn.Step(ShowFeedback,.2f,.24f,dt);
  hintIn.Step(true,.2f,.2f,dt);
  ghostOut.Step(false,1,.16f,dt);
  actionIn.Step(HasAction,.22f,.24f,dt);
  thinkIn.Step(Thinking,.18f,.14f,dt);
  arrowIn.Step(ArrowVisible,.25f,.15f,dt);
  footerWidth=Mathf.Lerp(footerWidth,footerTarget,1-Mathf.Exp(-dt*16));
  shownProgress=Mathf.Lerp(shownProgress,vision.Progress,1-Mathf.Exp(-dt*18));
  pulse=Mathf.MoveTowards(pulse,0,dt/.55f);
  StepMemoryMotion(dt);
 }
 // Haptics only confirm physical moments: Pip touching down and Pip taking a look.
 // Taps stay silent so device vibration rarely coincides with tracking.
 void Feel() {
  bool busy=vision.Busy;
  if(busy&&!wasBusy&&!learnRequested&&shownProgress>.5f){pulse=1;PipHaptics.Soft();}
  wasBusy=busy;
  bool placed=placement.HasCharacter&&!placement.IsPlacing;
  if(placed&&!wasPlaced)PipHaptics.Medium();
  wasPlaced=placed;
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
 void Card(Rect r,Color fill,float radius=20,float shadow=1) {
  Shadow(r,radius,shadow);
  Panel(r,new Color(1,1,1,.12f),radius);
  Panel(new Rect(r.x+.6f*S,r.y+.6f*S,r.width-1.2f*S,r.height-1.2f*S),fill,radius-.6f);
 }
 static Rect Scale(Rect r,float s)=>new Rect(r.center.x-r.width*s*.5f,r.center.y-r.height*s*.5f,r.width*s,r.height*s);
 bool Hit(Rect r,bool enabled=true,bool blocked=false) {
  bool old=GUI.enabled;GUI.enabled=old&&enabled&&!blocked;bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=old;return hit;
 }
 bool Pressed(Rect r)=>pointerDown&&r.Contains(pointerStart)&&r.Contains(pointerPosition)&&!dragged;
 bool Pill(Rect r,string value,bool primary=false,bool enabled=true,bool blocked=false) {
  Color bg=primary?accent:theme.Glass,fg=white;
  bool down=enabled&&!blocked&&Pressed(r);
  if(!enabled){bg=new Color(.2f,.2f,.18f,.38f);fg=new Color(1,1,1,.43f);}
  else if(down)bg=Color.Lerp(bg,Color.white,.1f);
  Rect v=down?Scale(r,.97f):r;
  if(enabled)Shadow(v,22,primary?1:.7f);
  if(primary&&enabled){Panel(v,new Color(mint.r,mint.g,mint.b,.32f),22);Panel(new Rect(v.x+.8f*S,v.y+.8f*S,v.width-1.6f*S,v.height-1.6f*S),bg,22);}
  else Panel(v,bg,22);
  if(value!=null)Label(v,value,button,fg);
  return Hit(r,enabled,blocked);
 }
 bool Circle(Rect r,int icon,bool enabled,bool blocked) {
  bool down=Pressed(r)&&enabled&&!blocked;
  Rect visual=new Rect(r.center.x-21*S,r.center.y-21*S,42*S,42*S);if(down)visual=Scale(visual,.92f);
  var fill=theme.Control;if(down)fill.a=.66f;
  Shadow(visual,21,.6f);Panel(visual,fill,21);DrawIcon(r,icon,enabled?white:new Color(1,1,1,.35f));return Hit(r,enabled,blocked);
 }
 // 0 more, 1 pause, 2 play, 3 retry, 4 close, 5 move, 6 hello, 7 help, 8 camera, 9 memories, 10 back, 11 forward.
 void DrawIcon(Rect r,int icon,Color color) {
  Vector2 c=r.center;
  if(icon==0){for(int i=-1;i<=1;i++)Panel(new Rect(c.x+i*6*S-1.6f*S,c.y-1.6f*S,3.2f*S,3.2f*S),color,2);}
  else if(icon==1){Panel(new Rect(c.x-5*S,c.y-7*S,3*S,14*S),color,1.5f);Panel(new Rect(c.x+2*S,c.y-7*S,3*S,14*S),color,1.5f);}
  else if(icon==2){GUI.color=C(color);GUI.DrawTexture(new Rect(c.x-4*S,c.y-7*S,11*S,14*S),theme.Play);GUI.color=Color.white;}
  else if(icon==8){
   Line(c+new Vector2(-10,-6)*S,c+new Vector2(10,-6)*S,1.4f*S,color);Line(c+new Vector2(-10,7)*S,c+new Vector2(10,7)*S,1.4f*S,color);
   Line(c+new Vector2(-10,-6)*S,c+new Vector2(-10,7)*S,1.4f*S,color);Line(c+new Vector2(10,-6)*S,c+new Vector2(10,7)*S,1.4f*S,color);
   Line(c+new Vector2(-4,-9)*S,c+new Vector2(4,-9)*S,1.4f*S,color);Arc(c,4*S,0,Mathf.PI*2,1.3f*S,color,24);
  }
  else if(icon==9){
   // A small photo (hill and sun) with a second print peeking behind it, drawn in the camera icon's stroke weight.
   float w=1.4f*S;var a=c+new Vector2(-9,-5)*S;var b=c+new Vector2(7,8)*S;
   Line(a,new Vector2(b.x,a.y),w,color);Line(new Vector2(b.x,a.y),b,w,color);Line(b,new Vector2(a.x,b.y),w,color);Line(new Vector2(a.x,b.y),a,w,color);
   Line(c+new Vector2(-6,-8)*S,c+new Vector2(10,-8)*S,w,color);Line(c+new Vector2(10,-8)*S,c+new Vector2(10,5)*S,w,color);
   Line(c+new Vector2(-9,6)*S,c+new Vector2(-3,1)*S,w,color);Line(c+new Vector2(-3,1)*S,c+new Vector2(1,4)*S,w,color);Line(c+new Vector2(1,4)*S,c+new Vector2(4,2)*S,w,color);Line(c+new Vector2(4,2)*S,c+new Vector2(7,5)*S,w,color);
   Panel(new Rect(c.x+1.5f*S,c.y-2.5f*S,3*S,3*S),color,2);
  }
  else if(icon==10||icon==11){float d=icon==10?1:-1;Line(c+new Vector2(-3*d,0)*S,c+new Vector2(3*d,-6)*S,1.6f*S,color);Line(c+new Vector2(-3*d,0)*S,c+new Vector2(3*d,6)*S,1.6f*S,color);}
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
 void Dots(Vector2 c,Color color,float spacing) {
  for(int i=0;i<3;i++){float b=Mathf.Max(0,Mathf.Sin(Time.unscaledTime*6-i*.75f));Panel(new Rect(c.x+(i-1)*spacing*S-3*S,c.y-3*S-b*3*S,6*S,6*S),new Color(color.r,color.g,color.b,color.a*(.5f+.5f*b)),3);}
 }
 void OnGUI() {
  if(!placement||!vision)return;Prepare();
  speech=HasBubble?SurfaceOf(Dialogue.Kind):Surface.Bark;
  float w=Mathf.Min(300*S,Safe.width-32*S),content=40*S;
  // Calculate adds a 64-point frame; barks only need 28, cards 54.
  if(HasBubble&&speech==Surface.Bark)content=typography.Measure(Dialogue.Text,body,w-32*S,S).Height-36*S;
  else if(HasBubble&&speech==Surface.Card)content=typography.Measure(Dialogue.Text,body,w-48*S,S).Height-10*S;
  bool action=HasAction||FollowPrompt||actionIn.T>.001f;
  layout=PipUILayout.Calculate(Safe,placement.CompanionScreenPoint,Screen.height,content,HasBubble&&Dialogue.Pinned,placement.CanCancelReposition,action);
  bubbleTarget=layout.Bubble.position;
  if(!bubbleInitialized || previousSafe!=Safe || shownText!=(Dialogue?Dialogue.Text:null)) {bubblePosition=bubbleTarget;bubbleVelocity=Vector2.zero;bubbleInitialized=true;previousSafe=Safe;}
  layout.SetBubblePosition(bubblePosition,action);
  Geometry();
  A=1;
  if(memoriesOpen){DrawMemories();return;}
  // Capture keeps the AR scene and spoken words, without navigation or shutter controls.
  if(capturingMoment){if(momentOverlay)DrawMomentOverlay();return;}
  if(GuideModal){DrawWalkthrough();return;}
  // A modal menu owns all input: everything underneath fades back and cannot activate through it.
  bool blocked=menu;
  A=1-menuIn.Out;
  Vector2 aim=placement.ScreenAimPoint;aim.y=Screen.height-aim.y;
  if(placement.IsPlacing) {
   // Once a surface is found, the ring lying on it takes over from the flat screen marker.
   if(!placement.CanPlace)Reticle(aim,0,false);
   if(Pill(layout.Summon,placement.IsBusy?(placement.HasCharacter?"Floating over…":"Summoning…"):placement.HasCharacter?"Move Pip here":"Summon Pip",true,placement.CanPlace,blocked))placement.ConfirmPlacement();
   if(placement.CanCancelReposition&&Pill(layout.Cancel,"Cancel",false,true,blocked))placement.CancelReposition();
  } else {
   if(Located&&vision.Ready&&vision.Watching&&!vision.Busy&&!(HasBubble&&Dialogue.Pinned))Reticle(aim,shownProgress,true);
   Pulse(aim);
   DrawThinking();
   DrawArrow();
   if(!HasBubble)DrawGhost();
   if(HasBubble&&hasSpeechSpace)DrawSpeech(blocked);
   else if(HasBubble)Dialogue.HoldReading();
   if(FollowPrompt){if(Pill(layout.Learn,"Bring Pip here",true,true,blocked)){placement.Reposition();awaySince=-1;}}
   else DrawAction(blocked);
   {
    bool canPause=vision.Ready&&Located;
    int pauseIcon=vision.LastError!=null?3:!vision.Watching?2:1;
    if(Circle(layout.Pause,pauseIcon,canPause,blocked)){vision.ToggleWatching();if(Dialogue&&Dialogue.Kind==PipDialogueKind.Error)Dialogue.Close();}
   }
  }
  DrawFeedback();
  A=1;
  if(!menu&&menuIn.T<.001f)DrawMemoryControls();
  DrawSavedFlight();
  if(menuIn.T>.001f)DrawMenu();
  if(Circle(layout.More,menu?4:0,true,false)){menu=!menu;vision.InteractionPaused=memoriesOpen||capturingMoment||GuideModal||menu;}
 }
 void Geometry() {
  speechRect=layout.Bubble;speechReading=layout.Reading;speechClose=layout.Close;
  hasSpeechSpace=true;companionBounds=placement.CompanionScreenBounds;
  if(!HasBubble)return;
  string value=Dialogue.Text;
  // Re-measure at candidate widths so wrapping and available space agree.
  hasSpeechSpace=false;
  float maxWidth=speech==Surface.Sheet?Safe.width-32*S:Mathf.Min(300*S,Safe.width-32*S);
  foreach(float logicalWidth in new[]{maxWidth/S,(Safe.width-32*S)/S,280f,260f,240f,220f,200f,180f,160f,140f}){
   float width=Mathf.Min(Safe.width-32*S,logicalWidth*S);
   float inset=(speech==Surface.Bark?32: speech==Surface.Sheet?52:48)*S;
   if(speech==Surface.Bark)width=Mathf.Min(width,typography.Widest(typography.Measure(value,body,width-inset,S))+inset+1);
   float height=typography.Measure(value,body,width-inset,S).Height+(speech==Surface.Bark?28: speech==Surface.Sheet?64:54)*S;
   Rect box;
   if(!layout.TrySpeechPosition(companionBounds,width,height,speech==Surface.Sheet,out box))continue;
   speechRect=box;
   speechReading=speech==Surface.Bark?new Rect(box.x+16*S,box.y+14*S,box.width-32*S,box.height-28*S):speech==Surface.Sheet?new Rect(box.x+20*S,box.y+44*S,box.width-40*S,box.height-60*S):new Rect(box.x+18*S,box.y+36*S,box.width-36*S,box.height-50*S);
   speechClose=new Rect(box.xMax-50*S,box.y+4*S,44*S,44*S);hasSpeechSpace=true;break;
  }
  if(!hasSpeechSpace){speechRect=new Rect();speechReading=new Rect();speechClose=new Rect();}
 }
 float TextWidth(Surface s,Rect reading)=>s==Surface.Bark?reading.width:reading.width-12*S;
 void DrawFeedback() {
  if(ShowFeedback){string hint=Problem;if(hint!=lastHint){if(footerIn.T>.3f)hintIn.T=0;lastHint=hint;}}
  if(footerIn.T<=.001f){footerRect=new Rect();return;}
  // The hint keeps its last words while fading out, so it never flickers to an unrelated message.
  float textWidth=Mathf.Min(Width(lastHint,small),layout.Footer.width-36*S);
  footerTarget=Mathf.Min(layout.Footer.width,textWidth+36*S);
  if(footerIn.T<.2f||footerWidth<=0)footerWidth=footerTarget;
  float fw=footerWidth,e=footerIn.Out;
  footerRect=new Rect(layout.Footer.center.x-fw*.5f,layout.Footer.y+(1-e)*6*S,fw,layout.Footer.height);
  float a=A;A*=e;
  Shadow(footerRect,19,.7f);Panel(footerRect,new Color(.15f,.14f,.13f,.80f),19);
  // Text is laid out at its final width so it never re-wraps while the pill resizes underneath it.
  A*=hintIn.Out;Label(new Rect(footerRect.center.x-textWidth*.5f-1,footerRect.y,textWidth+2,footerRect.height),lastHint,small,white,TextAnchor.MiddleCenter);
  A=a;
 }
 // While Pip is looking, a tiny thought bubble says "wait" in character instead of a status message.
 void DrawThinking() {
  if(thinkIn.T<=.001f||layout.Offscreen)return;
  Rect r;if(!layout.TrySpeechPosition(companionBounds,56*S,30*S,false,out r))return;
  float x=r.center.x;
  var m=GUI.matrix;float a=A;A*=thinkIn.Out;
  GUIUtility.ScaleAroundPivot(Vector2.one*(.8f+.2f*thinkIn.Out),new Vector2(x,r.yMax+9*S));
  Shadow(r,15,.4f);Panel(r,ink,15);if(r.yMax+12*S<=companionBounds.yMin)Tail(r,x);Dots(r.center,white,11);
  GUI.matrix=m;A=a;
 }
 // An edge arrow points toward Pip when it is out of view, as a cue rather than a sentence.
 void DrawArrow() {
  if(arrowIn.T<=.001f)return;
  if(ArrowVisible) {
   Vector3 p=placement.CompanionScreenPoint;Vector2 c=Safe.center,sp=new Vector2(p.x,Screen.height-p.y);
   if(p.z<0)sp=c-(sp-c);
   Vector2 d=sp-c;if(d.sqrMagnitude<1)d=Vector2.right;
   float hx=Safe.width*.5f-34*S,top=c.y-(Safe.y+96*S),bottom=(Safe.yMax-150*S)-c.y;
   float t=Mathf.Min(Mathf.Abs(d.x)>.01f?hx/Mathf.Abs(d.x):float.MaxValue,d.y<0?top/-d.y:d.y>0?bottom/d.y:float.MaxValue);
   arrowPoint=c+d*Mathf.Min(t,1);arrowDir=d.normalized;arrowPlaced=true;
  }
  if(!arrowPlaced)return;
  float a=A;A*=arrowIn.Out;
  Vector2 at=arrowPoint+arrowDir*Mathf.Sin(Time.unscaledTime*4)*2.5f*S,n=new Vector2(-arrowDir.y,arrowDir.x);
  Rect r=new Rect(at.x-20*S,at.y-20*S,40*S,40*S);
  Shadow(r,20,.6f);Panel(r,theme.Control,20);
  Vector2 tip=at+arrowDir*4.5f*S;
  Line(tip,tip-arrowDir*7*S+n*6*S,2*S,white);Line(tip,tip-arrowDir*7*S-n*6*S,2*S,white);
  A=a;
 }
 void DrawAction(bool blocked) {
  if(actionIn.T<=.001f)return;
  bool live=HasAction;
  if(live)lastAction=vision.ShowingExplanation?"Back to Pip":"Tell me more";
  float a=A;A*=actionIn.Out;
  Rect r=layout.Learn;r.y+=(1-actionIn.Out)*10*S;
  // First use only: the button glows once so its purpose is found by looking, not by reading.
  if(live&&walkthrough.Step==3){float g=.5f+.5f*Mathf.Sin(Time.unscaledTime*3);Panel(new Rect(r.x-3*S,r.y-3*S,r.width+6*S,r.height+6*S),new Color(mint.r,mint.g,mint.b,.16f+.22f*g),26);}
  bool busy=Learning;
  if(Pill(r,busy?null:lastAction,false,true,blocked||busy||!live)) {
   if(vision.ShowingExplanation)vision.ShowReaction();else{learnRequested=true;vision.KnowMore();}
   if(walkthrough.Step==3)Complete(false);
  }
  if(busy)Dots(r.center,white,12);
  A=a;
 }
 string SubjectName() {
  string s=vision.Subject;if(string.IsNullOrWhiteSpace(s))return null;s=s.Trim();
  return char.ToUpperInvariant(s[0])+s.Substring(1);
 }
 // Name tag for cards and sheets. Sheets carry the subject beside Pip's name so the reader keeps context.
 void Tag(Vector2 at,float width,string subject) {
  Rect tag=new Rect(at.x,at.y,width,24*S);
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
 void Frame(Surface s,Rect box,float tailX,bool tail,string subject) {
  if(s==Surface.Bark){Shadow(box,18,.45f);Panel(box,new Color(1,1,1,.10f),18);Panel(new Rect(box.x+.6f*S,box.y+.6f*S,box.width-1.2f*S,box.height-1.2f*S),ink,17.4f);if(tail)Tail(box,tailX);}
  else if(s==Surface.Card){Card(box,ink,20);if(tail)Tail(box,tailX);Tag(new Vector2(box.x+18*S,box.y+10*S),box.width-68*S,null);}
  else {Card(box,ink,22);Tag(new Vector2(box.x+20*S,box.y+12*S),box.width-74*S,subject);}
 }
 // Barks and cards grow from Pip's pointer; the sheet rises from the bottom where it lives.
 void Enter(Surface s,float e,float tailX,Rect box,float scaleFrom,float rise) {
  if(s==Surface.Sheet){GUI.matrix=Matrix4x4.Translate(new Vector3(0,(1-e)*rise*S,0))*GUI.matrix;return;}
  GUIUtility.ScaleAroundPivot(Vector2.one*(scaleFrom+(1-scaleFrom)*e),new Vector2(tailX,box.yMax+9*S));
  GUI.matrix=Matrix4x4.Translate(new Vector3(0,(1-e)*rise*S,0))*GUI.matrix;
 }
 void DrawSpeech(bool blocked) {
  Rect box=speechRect;
  float tailX=Mathf.Clamp(placement.CompanionScreenPoint.x,box.x+18*S,box.xMax-18*S);bool tail=speech!=Surface.Sheet&&!layout.Offscreen&&box.yMax+12*S<=companionBounds.yMin&&placement.CompanionScreenPoint.x>=box.xMin&&placement.CompanionScreenPoint.x<=box.xMax;
  if(shownText!=Dialogue.Text){shownText=Dialogue.Text;bubbleIn.T=Mathf.Min(bubbleIn.T,.45f);}
  string subject=speech==Surface.Sheet?SubjectName():null;
  var matrix=GUI.matrix;float a=A;
  A*=Mathf.Clamp01(bubbleIn.T*1.8f);
  Enter(speech,bubbleIn.Out,tailX,box,speech==Surface.Bark?.96f:.94f,speech==Surface.Sheet?18:speech==Surface.Bark?4:8);
  Frame(speech,box,tailX,tail,subject);
  bool old=GUI.enabled;
  if(speech!=Surface.Bark){
   DrawIcon(speechClose,4,new Color(1,1,1,.5f));
   GUI.enabled=old&&!blocked;bool closed=GUI.Button(speechClose,GUIContent.none,GUIStyle.none);GUI.enabled=old;
   if(closed){Remember(box,tailX,tail,subject);Dialogue.Close();bubbleInitialized=false;GUI.matrix=matrix;A=a;return;}
  }
  DrawCompleteReading(speechReading,speech,Dialogue.Text,Dialogue.VisibleText.Length);
  GUI.matrix=matrix;A=a;
  Remember(box,tailX,tail,subject);
 }
 // Normal replies retain the established size. Exceptional text fits the available
 // safe area as a complete block rather than hiding lines behind a scroll gesture.
 void DrawCompleteReading(Rect reading,Surface surface,string value,int visible) {
  float scale=S,tw=TextWidth(surface,reading);
  for(int i=0;i<40&&typography.Measure(value,body,tw,scale).Height>reading.height+.1f;i++)scale*=.97f;
  typography.Draw(new Rect(reading.x,reading.y,tw,reading.height),value,body,C(white),scale,TextAnchor.UpperLeft,visible);
 }
 void Remember(Rect box,float tailX,bool tail,string subject) {
  ghostSurface=speech;ghostRect=box;ghostReading=speechReading;ghostClose=speechClose;ghostTailX=tailX;ghostTail=tail;ghostSubject=subject;
  ghostText=Dialogue.Text;ghostVisible=Dialogue.VisibleText.Length;ghostOut.T=1;
 }
 // A dismissed or expired surface settles away instead of vanishing between frames.
 void DrawGhost() {
  if(ghostOut.T<=.001f||string.IsNullOrEmpty(ghostText))return;
  var avoid=companionBounds;avoid.xMin-=24*S;avoid.xMax+=24*S;avoid.yMin-=24*S;avoid.yMax+=24*S;
  if(ghostRect.Overlaps(avoid))return;
  var matrix=GUI.matrix;float a=A,e=ghostOut.Out;A*=e;
  Enter(ghostSurface,e,ghostTailX,ghostRect,.97f,ghostSurface==Surface.Sheet?10:0);
  Frame(ghostSurface,ghostRect,ghostTailX,ghostTail,ghostSubject);
  if(ghostSurface!=Surface.Bark)DrawIcon(ghostClose,4,new Color(1,1,1,.5f));
  DrawCompleteReading(ghostReading,ghostSurface,ghostText,ghostVisible);
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
 void Complete(bool closeDialogue) {
  walkthrough.Finish();PlayerPrefs.SetInt(PipWalkthrough.PreferenceKey,1);PlayerPrefs.Save();
  vision.InteractionPaused=false;
  if(closeDialogue&&Dialogue){Dialogue.Close();bubbleInitialized=false;}
 }
 // The one onboarding card: who Pip is, what looking shares, and the first action.
 void DrawWalkthrough() {
  float w=Mathf.Min(354*S,Safe.width-32*S),cw=w-48*S;
  float hh=typography.Measure(walkthrough.Heading,PipTypeRole.Heading,cw-72*S,S).Height;
  float ch=typography.Measure(walkthrough.Copy,text,cw,S).Height;
  float nh=typography.Measure(PipWalkthrough.Notice,small,cw,S).Height;
  float h=24*S+hh+10*S+ch+14*S+nh+24*S+48*S+24*S,e=guideIn.Out;
  guideRect=new Rect(Safe.center.x-w/2,Safe.yMax-h-20*S,w,h);
  GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(0,0,0,.12f*e),0,0);
  var matrix=GUI.matrix;float a=A;A*=Mathf.Clamp01(guideIn.T*1.6f);
  GUI.matrix=Matrix4x4.Translate(new Vector3(0,(1-e)*18*S,0))*GUI.matrix;
  Card(guideRect,ink,24);
  float y=guideRect.y+24*S;
  Label(new Rect(guideRect.x+24*S,y,cw-72*S,hh),walkthrough.Heading,PipTypeRole.Heading,white);
  var skip=new Rect(guideRect.xMax-80*S,guideRect.y+16*S,64*S,44*S);
  Label(skip,"Skip",PipTypeRole.SecondaryAction,muted);
  if(Hit(skip)){Complete(false);GUI.matrix=matrix;A=a;return;}
  y+=hh+10*S;Label(new Rect(guideRect.x+24*S,y,cw,ch),walkthrough.Copy,text,white);
  y+=ch+14*S;Label(new Rect(guideRect.x+24*S,y,cw,nh),PipWalkthrough.Notice,small,muted);
  if(Pill(new Rect(guideRect.x+24*S,guideRect.yMax-72*S,cw,48*S),"Find a spot",true))walkthrough.Next();
  GUI.matrix=matrix;A=a;
 }
 void OnDestroy(){ClearMomentCapture();ClearMemoryTextures();ClearFlight();if(theme!=null)theme.Dispose();}
 void OnDisable(){CancelMomentCapture();if(vision)vision.InteractionPaused=false;CancelPointer();menu=false;memoriesOpen=false;ClearMemoryTextures();}
}}
