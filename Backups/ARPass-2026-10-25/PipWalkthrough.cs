namespace Pip {
// Explicit steps keep onboarding separate from AR/network state and make replay safe.
public class PipWalkthrough {
 public const string PreferenceKey="Pip.Walkthrough.v1";
 public int Step { get; private set; }
 public bool Active => Step<4;
 public PipWalkthrough(bool completed) { Step=completed?4:0; }
 public void Restart() { Step=0; }
 public void Next() { if(Step!=1 && Active)Step++; }
 public void ObservePlacement(bool placed) { if(Step==1 && placed)Step=2; }
 public void Finish() { Step=4; }
 public string Heading => Step==0?"Hi, I'm Pip.":Step==1?"Find me a spot.":Step==2?"Show me something.":"There's more to it.";
 public string Copy => Step==0?"I'll keep you company. First, let's find me somewhere to float.":Step==1?"Move slowly over a floor or table. Aim the marker at a surface, then tap Summon Pip.":Step==2?"Point at something and hold steady for 2 seconds. I'll tell you what I think.":"Tap Tell me more to hear about what we're looking at. You can also tap me to say hello. Moving me and this guide live in the menu.";
}
}
