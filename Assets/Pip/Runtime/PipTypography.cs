using System.Collections.Generic;
using UnityEngine;
namespace Pip {
public enum PipTypeRole { Heading, Speech, Body, Action, SecondaryAction, Label, Caption }
// One family, two weights, explicit line boxes. Wrap against the entire reply before revealing it.
public sealed class PipTypography {
 public struct Run { public int Start,Length; public Run(int start,int length){Start=start;Length=length;} }
 public sealed class Layout { public string Text;public Run[] Lines;public float Height,Leading;public GUIStyle Style; }
 readonly PipUITheme theme;
 readonly Dictionary<PipTypeRole,GUIStyle> styles=new Dictionary<PipTypeRole,GUIStyle>();
 readonly Dictionary<string,Layout> cache=new Dictionary<string,Layout>();
 public PipTypography(PipUITheme theme){this.theme=theme;}
 public static float Size(PipTypeRole role)=>role==PipTypeRole.Heading?24:role==PipTypeRole.Speech?17:role==PipTypeRole.Body||role==PipTypeRole.Action||role==PipTypeRole.SecondaryAction?16:role==PipTypeRole.Label?13:12;
 public static float Leading(PipTypeRole role)=>role==PipTypeRole.Heading?30:role==PipTypeRole.Speech?24:role==PipTypeRole.Body?23:role==PipTypeRole.Action||role==PipTypeRole.SecondaryAction?20:role==PipTypeRole.Label?18:17;
 GUIStyle Style(PipTypeRole role,float scale) {
  if(!styles.TryGetValue(role,out var s)) {
   bool strong=role==PipTypeRole.Heading||role==PipTypeRole.Action||role==PipTypeRole.Label;
   s=new GUIStyle {font=theme.Font,fontStyle=strong?FontStyle.Bold:FontStyle.Normal,alignment=TextAnchor.UpperLeft,richText=false,wordWrap=false,padding=new RectOffset(),margin=new RectOffset(),clipping=TextClipping.Clip};s.normal.textColor=Color.white;styles.Add(role,s);
  }
  s.fontSize=Mathf.Max(1,Mathf.RoundToInt(Size(role)*scale));return s;
 }
 static int Next(string value,int index)=>index+((char.IsHighSurrogate(value[index])&&index+1<value.Length&&char.IsLowSurrogate(value[index+1]))?2:1);
 public Layout Measure(string value,PipTypeRole role,float width,float scale) {
  value=value??"";width=Mathf.Max(1,width);var s=Style(role,scale);
  string key=((int)role)+"/"+s.fontSize+"/"+Mathf.RoundToInt(width*10)+"/"+Mathf.RoundToInt(scale*1000)+"/"+value;
  if(cache.TryGetValue(key,out var found))return found;
  var lines=new List<Run>();int start=0;
  while(start<value.Length) {
   int scan=start,lastSpace=-1,fit=start;
   if(value[start]=='\n'){lines.Add(new Run(start,0));start++;continue;}
   while(scan<value.Length&&value[scan]!='\n') {
    int next=Next(value,scan);
    if(s.CalcSize(new GUIContent(value.Substring(start,next-start))).x>width)break;
    if(char.IsWhiteSpace(value[scan]))lastSpace=scan;
    fit=next;scan=next;
   }
   int end=scan,nextStart;
   if(scan>=value.Length||value[scan]=='\n'){end=scan;nextStart=scan<value.Length?scan+1:scan;}
   else if(lastSpace>=start){end=lastSpace;nextStart=lastSpace+1;while(nextStart<value.Length&&value[nextStart]!='\n'&&char.IsWhiteSpace(value[nextStart]))nextStart++;}
   else {end=fit>start?fit:Next(value,start);nextStart=end;}
   while(end>start&&char.IsWhiteSpace(value[end-1]))end--;
   lines.Add(new Run(start,end-start));start=nextStart;
  }
  if(lines.Count==0)lines.Add(new Run(0,0));
  float leading=Mathf.Max(Leading(role)*scale,s.lineHeight);
  var result=new Layout {Text=value,Lines=lines.ToArray(),Height=leading*lines.Count,Leading=leading,Style=s};
  if(cache.Count>=64)cache.Clear();cache[key]=result;return result;
 }
 public float Widest(Layout l){float m=0;foreach(var r in l.Lines)m=Mathf.Max(m,l.Style.CalcSize(new GUIContent(l.Text.Substring(r.Start,r.Length))).x);return m;}
 // fadeTop/fadeBottom are edges in rect coordinates; lines within fadeLength of an edge fade out (scrolling text).
 public void Draw(Rect rect,string value,PipTypeRole role,Color color,float scale,TextAnchor alignment=TextAnchor.UpperLeft,int visible=int.MaxValue,float fadeTop=float.NegativeInfinity,float fadeBottom=float.PositiveInfinity,float fadeLength=0) {
  var l=Measure(value,role,rect.width,scale);var style=Style(role,scale);
  bool centerX=alignment==TextAnchor.MiddleCenter||alignment==TextAnchor.UpperCenter;
  bool centerY=alignment==TextAnchor.MiddleCenter||alignment==TextAnchor.MiddleLeft;
  float y=rect.y+(centerY?Mathf.Max(0,(rect.height-l.Height)*.5f):0);
  var old=GUI.color;GUI.color=color;
  foreach(var run in l.Lines) {
   int end=Mathf.Min(run.Start+run.Length,visible);if(end>run.Start&&char.IsHighSurrogate(l.Text[end-1]))end--;
   float lineAlpha=1;
   // A line touching an edge keeps a hint of itself; any part hidden past the edge pulls it further down.
   if(fadeLength>0)lineAlpha=Mathf.Clamp01(.35f+Mathf.Min((y-fadeTop)/fadeLength,(fadeBottom-(y+l.Leading))/fadeLength));
   GUI.color=new Color(color.r,color.g,color.b,color.a*lineAlpha);
   if(end>run.Start&&lineAlpha>.01f){
    string fragment=l.Text.Substring(run.Start,end-run.Start);
    float lineWidth=style.CalcSize(new GUIContent(l.Text.Substring(run.Start,run.Length))).x;
    float x=rect.x+(centerX?Mathf.Max(0,(rect.width-lineWidth)*.5f):0);
    GUI.Label(new Rect(x,y,rect.width,l.Leading),new GUIContent(fragment),style);
   }
   y+=l.Leading;
  }
  GUI.color=old;
 }
}
}
