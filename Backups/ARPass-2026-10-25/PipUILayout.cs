using UnityEngine;
namespace Pip {
// Geometry is separate from drawing so notch insets and smaller phones can be checked.
public struct PipUILayout {
 public Rect Safe,Status,More,Menu,Pause,Footer,Notice,Summon,Cancel,Bubble,Learn,Reading,Close;
 public float Scale,MaxBubbleY;
 public bool Offscreen;
 public static PipUILayout Calculate(Rect safe,Vector3 projected,float screenHeight,float bodyHeight,bool detail,bool canCancel,bool hasAction) {
  var l=new PipUILayout { Safe=safe,Scale=Mathf.Min(safe.width/390f,safe.height/680f) };
  float s=l.Scale,pad=16*s;
  l.Status=new Rect(safe.x+pad,safe.y+12*s,Mathf.Min(226*s,safe.width-88*s),44*s);
  l.More=new Rect(safe.xMax-pad-44*s,safe.y+12*s,44*s,44*s);
  l.Menu=new Rect(safe.xMax-pad-Mathf.Min(246*s,safe.width-32*s),l.More.yMax+8*s,Mathf.Min(246*s,safe.width-32*s),312*s);
  l.Pause=new Rect(safe.center.x-24*s,safe.yMax-118*s,48*s,48*s);
  l.Footer=new Rect(safe.x+pad,safe.yMax-62*s,safe.width-2*pad,38*s);
  l.Notice=new Rect(safe.x+pad,safe.yMax-23*s,safe.width-2*pad,22*s);
  l.Summon=new Rect(safe.center.x-(canCancel?48:94)*s,l.Pause.y,188*s,48*s);
  l.Cancel=new Rect(l.Summon.x-100*s,l.Pause.y,88*s,48*s);
  float w=Mathf.Min(248*s,safe.width-32*s);
  l.Offscreen=projected.z<=0 || projected.x<safe.x+12*s || projected.x>safe.xMax-12*s || screenHeight-projected.y<safe.y || screenHeight-projected.y>safe.yMax;
  float headY=screenHeight-projected.y,gap=16*s;
  float bubbleBottom=l.Pause.y-64*s;
  if(hasAction)l.Footer.y=l.Pause.y-50*s;
  float maxH=Mathf.Min(306*s,bubbleBottom-safe.y-24*s-gap);
  // Both short reactions and longer explanations remain above Pip; long text scrolls.
  if(!l.Offscreen)maxH=Mathf.Min(maxH,Mathf.Max(90*s,headY-safe.y-12*s-gap));
  float h=Mathf.Clamp(bodyHeight+64*s,90*s,Mathf.Max(90*s,maxH));
  float x=projected.x-w*.5f,y=headY-gap-h;
  if(l.Offscreen){x=safe.x+pad;y=safe.y+72*s;}
  l.Bubble=new Rect(Mathf.Clamp(x,safe.x+pad,safe.xMax-pad-w),Mathf.Clamp(y,safe.y+12*s,bubbleBottom-h-gap),w,h);
  l.MaxBubbleY=l.Offscreen?bubbleBottom-h-gap:Mathf.Max(safe.y+12*s,Mathf.Min(bubbleBottom-h-gap,headY-gap-h));
  l.SetBubblePosition(l.Bubble.position,hasAction);
  return l;
 }
 public void SetBubblePosition(Vector2 p,bool hasAction) {
  float s=Scale;
  Bubble.position=new Vector2(Mathf.Clamp(p.x,Safe.x+16*s,Safe.xMax-16*s-Bubble.width),Mathf.Clamp(p.y,Safe.y+12*s,MaxBubbleY));
  // Near the top, slide left only as far as needed to clear the menu button, so the pointer can still reach Pip.
  if(Bubble.y<More.yMax+8*s)Bubble.x=Mathf.Max(Safe.x+16*s,Mathf.Min(Bubble.x,More.x-8*s-Bubble.width));
  Learn=new Rect(Safe.x+16*s,Safe.yMax-64*s,Safe.width-32*s,48*s);
  Close=new Rect(Bubble.xMax-48*s,Bubble.y+2*s,44*s,44*s);
  Reading=new Rect(Bubble.x+18*s,Bubble.y+36*s,Bubble.width-36*s,Bubble.height-50*s);
 }
}
}
