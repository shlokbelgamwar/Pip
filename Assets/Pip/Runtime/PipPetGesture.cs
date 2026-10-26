using UnityEngine;
namespace Pip {
// Coordinates are logical screen points. Require deliberate rubbing, not a tap or one swipe.
public sealed class PipPetGesture {
 public bool Recognized { get; private set; }
 bool active;
 Vector2 last,direction;
 float since,distance;
 int reversals;
 public void Begin(Vector2 point,float now){active=true;Recognized=false;last=point;direction=Vector2.zero;since=now;distance=0;reversals=0;}
 public void Cancel(){active=false;Recognized=false;}
 // True only once, when the gesture first qualifies. Recognized persists until release.
 public bool Move(Vector2 point,float now,bool overCharacter){
  if(!active)return false;
  if(!overCharacter){last=point;direction=Vector2.zero;distance=0;reversals=0;since=now;return false;}
  Vector2 delta=point-last;if(delta.magnitude<6)return false;last=point;
  if(Recognized)return false;
  var next=delta.normalized;
  if(direction.sqrMagnitude>.1f&&Vector2.Dot(direction,next)<-.35f)reversals++;
  direction=next;distance+=delta.magnitude;
  if(now-since<.45f||distance<60||reversals<2)return false;
  Recognized=true;return true;
 }
}}
