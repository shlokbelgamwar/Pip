using System;
using System.Collections.Generic;
using UnityEngine;
namespace Pip {
// Local motion-compensated change detection, not semantic object identification.
public class PipLookGate {
 public float Progress { get; private set; }
 bool initialized,started,retry;
 float since,lastNovel;
 Vector3 origin;
 Quaternion rotation;
 byte[] baseline;
 sealed class View {public byte[] Image;public Vector3 Position;public Quaternion Rotation;}
 readonly List<View> seen=new List<View>();
 struct Change {public float Mean,Coverage,Background;}
 public void Pause(){started=false;Progress=0;}
 public void Reset(){Pause();initialized=false;retry=false;baseline=null;seen.Clear();}
 public void RetryCurrent(){Pause();initialized=true;retry=true;}
 public bool Sample(Vector3 position,Quaternion pose,byte[] image,float now){
  if(image==null||image.Length==0){Pause();return false;}
  if(!initialized){initialized=true;Remember(image,position,pose);Pause();return false;}
  if(!retry&&!NewContent(position,pose,image)){
   // A single noisy frame must not erase a deliberate hold. Never capture on a
   // non-novel sample; sustained absence still cancels the candidate.
   if(!started||now-lastNovel>.5f)Pause();
   return false;
  }
  lastNovel=now;
  // Compare with the start of the hold, not the previous sample, so a pan
  // cannot accumulate unlimited drift. Registered background tolerates tremor
  // and small object motion without requiring a perfectly frozen photograph.
  var change=Compare(baseline,image);
  if(!started||Quaternion.Angle(rotation,pose)>8||Vector3.Distance(origin,position)>.08f||change.Background>.04f||change.Mean>.10f){
   started=true;since=now;origin=position;rotation=pose;baseline=(byte[])image.Clone();Progress=0;return false;
  }
  Progress=Mathf.Clamp01((now-since)/2f);if(Progress<1)return false;
  Remember(image,position,pose);retry=false;Pause();return true;
 }
 public void AcceptCurrent(Vector3 position,Quaternion pose,byte[] image){initialized=true;Remember(image,position,pose);retry=false;Pause();}
 bool NewContent(Vector3 position,Quaternion pose,byte[] image){
  View closest=null;Change reference=default;float nearest=float.PositiveInfinity;
  foreach(var view in seen){
   var difference=Compare(view.Image,image);
   // Registered recently seen views stay quiet despite exposure or small framing changes.
   if(difference.Mean<.03f||difference.Coverage<.10f)return false;
   float score=Quaternion.Angle(view.Rotation,pose)/35f+Vector3.Distance(view.Position,position)/.6f;
   if(score<nearest){nearest=score;closest=view;reference=difference;}
  }
  if(closest==null)return false;
  float angle=Quaternion.Angle(closest.Rotation,pose),travel=Vector3.Distance(closest.Position,position);
  // Showing an object changes a limited region while most of the background still matches.
  bool objectAdded=angle<=10&&travel<=.12f&&reference.Coverage>=.10f&&reference.Coverage<=.45f&&reference.Background<.025f;
  // A room/scene request needs substantial travel or a deliberate large turn AND broad visual change.
  bool newPlace=(angle>=35||travel>=.6f)&&reference.Coverage>=.40f&&reference.Mean>=.08f;
  return objectAdded||newPlace;
 }
 void Remember(byte[] image,Vector3 position,Quaternion pose){if(seen.Count>=8)seen.RemoveAt(1);seen.Add(new View{Image=(byte[])image.Clone(),Position=position,Rotation=pose});}
 static Change Compare(byte[] a,byte[] b){
  if(a==null||b==null||a.Length!=b.Length||a.Length==0)return new Change{Mean=1,Coverage=1,Background=1};
  int width=a.Length==768?32:a.Length,height=a.Length/width;
  int radiusX=height>1?3:0,radiusY=height>1?2:0;
  var offsets=new int[511];var residuals=new int[256];
  Change best=new Change{Mean=1,Coverage=1,Background=1};float bestScore=float.PositiveInfinity;
  for(int dy=-radiusY;dy<=radiusY;dy++)for(int dx=-radiusX;dx<=radiusX;dx++){
   Array.Clear(offsets,0,offsets.Length);Array.Clear(residuals,0,residuals.Length);
   int x0=Mathf.Max(0,-dx),x1=Mathf.Min(width,width-dx),y0=Mathf.Max(0,-dy),y1=Mathf.Min(height,height-dy),count=(x1-x0)*(y1-y0);
   for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)offsets[b[(y+dy)*width+x+dx]-a[y*width+x]+255]++;
   int cumulative=0,offset=0;
   for(int i=0;i<offsets.Length;i++){cumulative+=offsets[i];if(cumulative>=count/2){offset=i-255;break;}}
   int changed=0;float sum=0;
   for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){
    int d=Mathf.Min(255,Mathf.Abs(b[(y+dy)*width+x+dx]-a[y*width+x]-offset));
    residuals[d]++;sum+=d;if(d>25)changed++;
   }
   // Score alignment against the best matching 60% so an introduced object does
   // not drag the background registration toward itself.
   int remaining=Mathf.Max(1,Mathf.RoundToInt(count*.6f)),used=remaining;float background=0;
   for(int i=0;i<residuals.Length&&remaining>0;i++){int take=Mathf.Min(remaining,residuals[i]);background+=take*i;remaining-=take;}
   var change=new Change{Mean=sum/(count*255f),Coverage=changed/(float)count,Background=background/(used*255f)};
   float score=change.Background+change.Mean*.05f+(Mathf.Abs(dx)+Mathf.Abs(dy))*.0001f;
   if(score<bestScore){bestScore=score;best=change;}
  }
  return best;
 }
}}
