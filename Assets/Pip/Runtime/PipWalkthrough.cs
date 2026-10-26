namespace Pip {
// Explicit steps keep onboarding separate from AR/network state and make replay safe.
// Only step 0 is a card. Placement (1), looking (2) and Tell me more (3) are taught in context as they happen.
public class PipWalkthrough {
 public const string PreferenceKey="Pip.Walkthrough.v1";
 public const string CoachLine="Show me something new and hold still. If I miss it, give your phone a little shake, then hold steady!";
 public const string Notice="Looking sends a camera frame to Google Gemini through our server. You can pause it anytime.";
 public int Step { get; private set; }
 public bool Active => Step<4;
 public PipWalkthrough(bool completed) { Step=completed?4:0; }
 public void Restart() { Step=0; }
 public void Next() { if(Step!=1 && Active)Step++; }
 public void ObservePlacement(bool placed) { if(Step==1 && placed)Step=2; }
 public void Finish() { Step=4; }
 public string Heading => "Hi, I'm Pip.";
 public string Copy => "I'm a little companion who loves looking at things with you. Let's find me a spot to float.";
}
}
