using UnityEngine;
namespace Pip {
// Three alternating acceleration peaks distinguish a deliberate shake from a bump.
// Capture is released only after quiet motion and normal tracking return.
public sealed class PipShakeGesture {
 public bool Pending { get; private set; }
 public float Progress { get; private set; }
 bool initialized,settling;
 Vector3 gravity,lastPeak,origin;
 Quaternion rotation;
 int peaks;
 float lastSample,lastPeakAt=-100,nextAllowed,deadline,quietSince;
 public void Cancel(){Pending=false;settling=false;peaks=0;Progress=0;}
 public bool Sample(Vector3 acceleration,Vector3 position,Quaternion pose,float now,bool enabled,bool tracking){
  if(!initialized){initialized=true;gravity=acceleration;lastSample=now;return false;}
  float dt=Mathf.Clamp(now-lastSample,0,.1f);lastSample=now;
  Vector3 motion=acceleration-gravity;
  gravity=Vector3.Lerp(gravity,acceleration,1-Mathf.Exp(-dt*2));
  if(!enabled){Cancel();return false;}
  if(Pending){
   if(now>deadline){Cancel();return false;}
   if(!tracking||motion.magnitude>.25f){settling=false;Progress=0;return false;}
   if(!settling||Vector3.Distance(origin,position)>.015f||Quaternion.Angle(rotation,pose)>1.5f){settling=true;quietSince=now;origin=position;rotation=pose;Progress=0;return false;}
   Progress=Mathf.Clamp01((now-quietSince)/.7f);
   if(Progress<1)return false;
   Cancel();return true;
  }
  if(now<nextAllowed)return false;
  if(now-lastPeakAt>.7f)peaks=0;
  if(motion.magnitude<1.1f||now-lastPeakAt<.08f)return false;
  if(peaks>0&&Vector3.Dot(lastPeak.normalized,motion.normalized)>-.35f)return false;
  lastPeak=motion;lastPeakAt=now;peaks++;
  if(peaks>=3){Pending=true;settling=false;peaks=0;deadline=now+8;nextAllowed=now+8;}
  return false;
 }
}}
