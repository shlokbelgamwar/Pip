using UnityEngine;
namespace Pip {
public enum PipDialogueKind { Chatter, Reaction, Explanation, Error }
public class PipDialogue : MonoBehaviour {
 public PipDialogueKind Kind { get; private set; }
 public Mood Expression { get; private set; } = Mood.Content;
 public Mood ActingMood => IsVisible ? Expression : character && character.mood ? character.mood.Current : Mood.Content;
 public bool Pinned => Kind==PipDialogueKind.Explanation || Kind==PipDialogueKind.Error;
 public bool AllowIdle=true,ExternalActivity;
 public string Text { get; private set; }
 public string LastSpokenText { get; private set; }
 public string VisibleText => string.IsNullOrEmpty(Text)?"":Text.Substring(0,Mathf.Min(Text.Length,Mathf.FloorToInt(letters)));
 public bool IsVisible => !string.IsNullOrEmpty(Text);
 public bool IsTyping => IsVisible && letters<Text.Length;
 public string MoodLabel => character.mood.Current.ToString();
 PipCharacter character;
 float letters,expires,nextIdle,nextTouch;
 Mood previousMood;
 int lastLine=-1;
 static readonly string[][] lines={
  new[]{"Adventure check: you, me, and absolutely no plan. Perfect!","Think this room has a secret treasure? I vote snacks.","I'm tiny. That makes every little adventure a BIG adventure."},
  new[]{"This is a good spot. Excellent company, too.","Official report: I like hanging out with you.","If anyone asks, we're doing very important floating."},
  new[]{"Five more minutes... floating is surprisingly tiring.","Can we schedule the adventure after my tiny nap?","I'm awake. My eyes are just saving energy."},
  new[]{"Hey! I'm a companion, not a doorbell!","One more poke and I'm filing a tiny complaint.","My cheeks are not buttons. Very cute, yes. Buttons, no."}
 };
 void Awake() { character=GetComponent<PipCharacter>(); }
 void Start() {
  previousMood=character.mood.Current;
  Say("Hi! I'm Pip. You bring the adventure; I'll bring the commentary.");
 }
 public void Say(string text,PipDialogueKind kind=PipDialogueKind.Chatter) {
  SayWithExpression(text,kind,kind==PipDialogueKind.Explanation||kind==PipDialogueKind.Error?Mood.Curious:character&&character.mood?character.mood.Current:Mood.Content);
 }
 public void SayWithExpression(string text,PipDialogueKind kind,Mood expression) {
  Text=text;Kind=kind;Expression=expression;letters=0;
  if(!string.IsNullOrWhiteSpace(text))LastSpokenText=text;
  expires=Time.unscaledTime+(text==null?0:text.Length/32f+ReadingTime);
  nextIdle=Time.unscaledTime+Random.Range(40f,65f);
 }
 float ReadingTime => Mathf.Max(9,(Text==null?0:Text.Length)/14f);
 public void HoldReading() { if(IsVisible) expires=Mathf.Max(expires,Time.unscaledTime+ReadingTime); }
 public void Reveal() { if(!IsVisible)return;letters=Text.Length;expires=Time.unscaledTime+ReadingTime; }
 public void Close() { Text=null; }
 public void Advance() { if(IsTyping) Reveal();else Close(); }
 public void Chat() {
  if(Time.unscaledTime<nextTouch) return;
  nextTouch=Time.unscaledTime+1.2f;
  var choices=lines[(int)character.mood.Current];
  int pick=Random.Range(0,choices.Length-1);if(pick>=lastLine) pick++;
  lastLine=pick;SayWithExpression(choices[pick],PipDialogueKind.Chatter,character.mood.Current);
 }
 void Update() {
  if(!character || !character.mood) return;
  letters+=Time.unscaledDeltaTime*32;
  if(IsVisible && !Pinned && Time.unscaledTime>=expires) Text=null;
  var mood=character.mood.Current;
  if(ExternalActivity || (IsVisible && Pinned)) return;
  if(mood!=previousMood) {
   previousMood=mood;
   if(!IsVisible && mood==Mood.Angry) Say("Hey! Gentle taps, please. I'm little!");
   else if(!IsVisible && AllowIdle) Chat();
  }
  if(AllowIdle && !IsVisible && Time.unscaledTime>=nextIdle) Chat();
 }
}}
