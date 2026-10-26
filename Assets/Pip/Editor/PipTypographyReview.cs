using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;
namespace Pip.Editor {
public static class PipTypographyReview {
 public static string Run(){
  var theme=new PipUITheme();var type=new PipTypography(theme);int count=0;
  try {
   foreach(float scale in new[]{.82f,1f,2f})foreach(float width in new[]{160f,200f,306f})foreach(PipTypeRole role in System.Enum.GetValues(typeof(PipTypeRole)))foreach(string sample in new[]{"A tiny hotel for your earbuds. Do they get room service?",new string('W',360),"One line.\n\nAnother line with an emoji \U0001F343 and a longwordwithoutspaces.","Looking sends a frame to Google Gemini through our server. Pause whenever you like."}) {
    var l=type.Measure(sample,role,width*scale,scale);var output=new StringBuilder();
    foreach(var run in l.Lines) {
     string line=sample.Substring(run.Start,run.Length);output.Append(line);
     if(l.Style.CalcSize(new GUIContent(line)).x>width*scale+.1f)throw new System.Exception("Wrapped line exceeds width");
     if(line.Length>0&&(char.IsLowSurrogate(line[0])||char.IsHighSurrogate(line[line.Length-1])))throw new System.Exception("Broken emoji at line boundary");
    }
    if(Regex.Replace(output.ToString(),@"\s","")!=Regex.Replace(sample,@"\s",""))throw new System.Exception("Wrapping lost text");
    if(l.Leading+.01f<l.Style.lineHeight || Mathf.Abs(l.Height-l.Leading*l.Lines.Length)>.01f)throw new System.Exception("Line boxes collide: "+role+" scale="+scale+" leading="+l.Leading+" fontHeight="+l.Style.lineHeight+" height="+l.Height);
    count++;
   }
   foreach(float scale in new[]{.82f,1f,2f})foreach(string label in new[]{"Tell me more","Back to Pip","Continue","Ready","Find a spot"}){
    var l=type.Measure(label,PipTypeRole.Action,306*scale,scale);if(l.Lines.Length!=1)throw new System.Exception("Action label wraps");
   }
   return "PASS "+count+" font/wrap combinations, text preservation, emoji boundaries, leading and single-line actions";
  }finally{theme.Dispose();}
 }
}
}
