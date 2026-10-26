using UnityEngine;
namespace Pip {
// Strong expression silhouettes inspired by chibi acting; original geometry for Pip.
public class PipFace : MonoBehaviour {
 public Transform leftEye, rightEye, leftBrow, rightBrow, lowerLip, mouthInterior;
 public LineRenderer lips, leftLid, rightLid;
 public float expressionSpeed=4;
 [Range(0,1)] public float expressionStrength=1;
 float curiosity, sleepiness, anger, excitement;
 public void Animate(Mood mood,float reaction,bool blinking,float deltaTime) {
  float blend=1-Mathf.Exp(-deltaTime*expressionSpeed);
  curiosity=Mathf.Lerp(curiosity,mood==Mood.Curious?1:0,blend);
  sleepiness=Mathf.Lerp(sleepiness,mood==Mood.Sleepy?1:0,blend);
  anger=Mathf.Lerp(anger,mood==Mood.Angry?1:0,blend);
  excitement=Mathf.Lerp(excitement,reaction,1-Mathf.Exp(-deltaTime*10));
  float c=curiosity*expressionStrength,s=sleepiness*expressionStrength,a=anger*expressionStrength;
  float happy=Mathf.Clamp01(1-c-s-a);
  float wake=excitement*s;
  float eyeVisibility=Mathf.Clamp01(c+a+wake);
  float eyeHeight=(.048f*c+.013f*a+.026f*wake)*(blinking?.08f:1);
  float eyeWidth=.032f*c+.028f*a+.024f*wake;
  leftEye.localScale=new Vector3(Mathf.Max(.001f,eyeWidth),Mathf.Max(.001f,eyeHeight),.016f);
  rightEye.localScale=new Vector3(Mathf.Max(.001f,eyeWidth),Mathf.Max(.001f,eyeHeight-wake*.009f),.016f);
  leftEye.gameObject.SetActive(eyeVisibility>.025f); rightEye.gameObject.SetActive(eyeVisibility>.025f);
  leftEye.localRotation=Quaternion.Euler(0,0,-23*a);
  rightEye.localRotation=Quaternion.Euler(0,0,23*a);
  DrawLid(leftLid,happy,s,blinking,-1);
  DrawLid(rightLid,happy,s,blinking,1);
  // Angry inner corners point down. Sleepy inner corners rise, with soft horizontal eyelids.
  float browY=.045f+c*.014f+happy*.004f-s*.003f-a*.01f+excitement*.002f;
  leftBrow.localPosition=new Vector3(-.032f,browY+c*.003f,.057f);
  rightBrow.localPosition=new Vector3(.032f,browY,.057f);
  leftBrow.localRotation=Quaternion.Euler(0,0,-28*a+12*s+6*c+8*happy);
  rightBrow.localRotation=Quaternion.Euler(0,0,28*a-12*s-6*c-8*happy);
  leftBrow.localScale=rightBrow.localScale=new Vector3(.023f,.0035f+a*.0015f,.004f);
  const int count=33; lips.positionCount=count;
  float width=.0065f*c+.013f*happy+.009f*a+.006f*s;
  float open=.007f*c+.0025f*happy+.0003f*a+.0002f*s+excitement*.002f;
  float curvature=.004f*happy-.004f*a;
  for(int i=0;i<count;i++) {
   float angle=i/(float)(count-1)*Mathf.PI*2, across=Mathf.Sin(angle);
   lips.SetPosition(i,new Vector3(across*width,curvature*across*across-.0015f+open*Mathf.Cos(angle),0));
  }
  lowerLip.localPosition=new Vector3(0,-.0015f-open,.0003f);
  lowerLip.localScale=new Vector3(.009f+c*.002f+happy*.005f,.0022f,.0024f);
  mouthInterior.gameObject.SetActive(open>.001f);
  // Follows the lip contour: an O for curiosity and a broad smile for happiness.
  mouthInterior.localPosition=new Vector3(0,-.0015f+curvature*.45f,-.0006f);
  mouthInterior.localScale=new Vector3(width*1.7f,open*1.6f,.001f);
 }
 void DrawLid(LineRenderer lid,float happy,float sleepy,bool blink,float side) {
  if(!lid) return;
  float visibility=Mathf.Clamp01(happy+sleepy);
  lid.enabled=visibility>.025f;
  lid.widthMultiplier=.0028f*visibility;
  lid.positionCount=17;
  for(int i=0;i<17;i++) {
   float x=(i/16f-.5f)*.028f;
   float arch=(1-Mathf.Pow(x/.014f,2))*(happy*.006f-sleepy*.001f);
   lid.SetPosition(i,new Vector3(x,blink?0:arch,0));
  }
 }
 [ContextMenu("Preview Curious")]
 public void PreviewCurious() { Animate(Mood.Curious,0,false,10); }
 [ContextMenu("Preview Happy")]
 public void PreviewContent() { Animate(Mood.Content,0,false,10); }
 [ContextMenu("Preview Angry")]
 public void PreviewAngry() { Animate(Mood.Angry,0,false,10); }
 [ContextMenu("Preview Sleepy")]
 public void PreviewSleepy() { Animate(Mood.Sleepy,0,false,10); }
}}
