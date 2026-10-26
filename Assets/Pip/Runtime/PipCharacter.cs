using UnityEngine;
namespace Pip {
public class PipCharacter : MonoBehaviour {
 public PipMood mood;
 public PipFace face;
 public Transform visual, leftLeaf, rightLeaf, scarf, tail, leftEye, rightEye;
 public Renderer tailRenderer;
 [Range(.5f,1.5f)] public float modelScale = .85f;
 public float hoverHeight = .1667f, bobAmount = .012f, animationSmoothing = 5;
 float clock, nextAction, nextBlink, blinkUntil, reaction, reactionStrength, yaw, tilt, sleep;
 float petUntil,nextPetLine;
 int lastPetLine=-1;
 static readonly string[] petLines={"Okay, okay... you found my favorite spot.","You may continue. This is very important companion maintenance.","A little to the left... yes! Excellent human.","Fine. You're officially my favorite human today.","I was going to be sassy, but this is much better."};
 Vector3 wander, destination;
 Material glow;
 public PipDialogue Dialogue { get; private set; }
 void Awake() { Dialogue=GetComponent<PipDialogue>(); if(!Dialogue) Dialogue=gameObject.AddComponent<PipDialogue>(); }
 void Start() { nextAction=Random.Range(2,5); nextBlink=Random.Range(2,6); if(tailRenderer) glow=tailRenderer.material; }
 public bool Traveling { get; private set; }
 public void SetTraveling(bool value){Traveling=value;wander=destination=Vector3.zero;if(!value)nextAction=clock+2;}
 public void Notice(Vector3 target) { var look=transform.InverseTransformPoint(target);yaw=Mathf.Clamp(Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg,-65,65);tilt=-5;nextAction=clock+4; }
 public void React() {
  if(reaction>0) return;
  mood.Touch(); Dialogue.Chat(); reaction = 1; reactionStrength = Random.Range(.7f,1.1f);
  if(mood.Current == Mood.Sleepy) { blinkUntil=0; nextBlink=clock+1.2f; }
 }
 public void Pet() {
  mood.Pet();ContinuePet();reaction=0;nextAction=clock+3;
  if(Time.unscaledTime<nextPetLine||Dialogue.Pinned)return;
  nextPetLine=Time.unscaledTime+4;
  int index=Random.Range(0,petLines.Length-1);if(index>=lastPetLine)index++;lastPetLine=index;
  Dialogue.SayWithExpression(petLines[index],PipDialogueKind.Chatter,Mood.Content);
 }
 public void ContinuePet(){petUntil=Time.unscaledTime+.65f;nextAction=clock+2;}
 void Update() {
  float dt=Time.deltaTime; clock+=dt;
  float pet=Mathf.Clamp01((petUntil-Time.unscaledTime)/.4f);
  Mood acting=pet>0?Mood.Content:Dialogue?Dialogue.ActingMood:mood.Current;
  bool sleepy=acting==Mood.Sleepy, curious=acting==Mood.Curious, angry=acting==Mood.Angry;
  sleep=Mathf.Lerp(sleep,sleepy?1:0,1-Mathf.Exp(-dt*2));
  if(clock>nextAction&&!Traveling) {
   nextAction=clock+Random.Range(sleepy?8:3,sleepy?14:7);
   yaw=Random.Range(-45f,45f); tilt=Random.Range(-9f,9f);
   // Local, bounded investigation; the session anchor itself never moves.
   Vector2 offset=Random.insideUnitCircle*(curious?.035f:.008f);
   destination=new Vector3(offset.x,0,offset.y);
   if(Random.value<.4f && Camera.main) {
    Vector3 look=transform.InverseTransformPoint(Camera.main.transform.position);
    yaw=Mathf.Clamp(Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg,-65,65);
   }
  }
  wander=Vector3.Lerp(wander,destination,1-Mathf.Exp(-dt*.7f));
  reaction=Mathf.MoveTowards(reaction,0,dt/(sleepy?1.8f:.85f));
  float bounce=Mathf.Sin((1-reaction)*Mathf.PI)*(reaction>0?reactionStrength:0)*(sleepy?.008f:.045f);
  visual.localPosition=wander+Vector3.up*(hoverHeight+Mathf.Sin(clock*(curious?2:1.2f))*bobAmount*(1-.7f*sleep)+bounce);
  visual.localRotation=Quaternion.Slerp(visual.localRotation,Quaternion.Euler(Traveling?-5:sleep*12+(angry?-7:0)-pet*5,Traveling?0:yaw*(1-pet*.7f),Traveling?0:angry?Mathf.Sin(clock*7)*3:tilt*(1-sleep)+pet*Mathf.Sin(clock*7)*7),1-Mathf.Exp(-dt*animationSmoothing));
  visual.localScale=Vector3.one*modelScale*Mathf.SmoothStep(0,1,Mathf.Clamp01(clock/1.2f));
  leftLeaf.localRotation=Quaternion.Euler(0,0,-25-sleep*40+(angry?17:0)+Mathf.Sin(clock*1.7f)*4+pet*Mathf.Sin(clock*9)*5);
  rightLeaf.localRotation=Quaternion.Euler(0,0,25+sleep*40-(angry?17:0)-Mathf.Sin(clock*1.4f)*4-pet*Mathf.Sin(clock*9)*5);
  scarf.localRotation=Quaternion.Euler(Mathf.Sin(clock*1.6f)*5,0,Mathf.Sin(clock)*3);
  tail.localRotation=Quaternion.Euler(Mathf.Sin(clock)*8,Mathf.Sin(clock*.8f)*10,0);
  if(clock>nextBlink) { blinkUntil=clock+.14f; nextBlink=clock+Random.Range(2.5f,6); }
  float eye=(clock<blinkUntil ? .08f : Mathf.Lerp(1,.13f,sleep));
  if(sleepy && reaction>.3f) eye=.55f;
  if(face) face.Animate(acting,reaction,clock<blinkUntil,dt);
  else {
   leftEye.localScale=new Vector3(.026f,.038f*eye,.016f);
   rightEye.localScale=new Vector3(.026f,.038f*eye,.016f);
  }
  if(glow) glow.SetColor("_EmissionColor",new Color(.35f,1,.65f)*Mathf.Lerp(.6f,.12f,sleep));
 }
 // Anchors may refine their orientation after creation. Keep the creature gravity-upright
 // while retaining the anchor as its position parent and preserving its heading.
 void LateUpdate() {
  Vector3 heading=Vector3.ProjectOnPlane(transform.forward,Vector3.up);
  if(heading.sqrMagnitude<.0001f) heading=Vector3.ProjectOnPlane(transform.right,Vector3.up);
  if(heading.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(heading,Vector3.up);
 }
 void OnDestroy() { if(glow) Destroy(glow); }
}}
