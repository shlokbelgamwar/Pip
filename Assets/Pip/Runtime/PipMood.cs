using UnityEngine;
namespace Pip {
public enum Mood { Curious, Content, Sleepy, Angry }
public class PipMood : MonoBehaviour {
 public Mood Current { get; private set; } = Mood.Content;
 [Range(0,1)] public float energy = .55f;
 public float responseSeconds = 18, minimumMoodSeconds = 12, inactivitySeconds = 90;
 public float brightThreshold = .68f, dimThreshold = .3f;
 public PipLightSensor environment;
 public float annoyedDuration = 5;
 float sinceTouch, held, annoyedRemaining;
 int pokeBurst;
 public void Touch() {
  pokeBurst=sinceTouch<3?pokeBurst+1:1;
  sinceTouch=0;
  energy=Mathf.Clamp01(energy+(Current==Mood.Sleepy?.035f:.12f));
  if(pokeBurst>=3) { Current=Mood.Angry; annoyedRemaining=annoyedDuration; held=0; pokeBurst=0; }
 }
 public void Pet() {
  sinceTouch=0;pokeBurst=0;annoyedRemaining=0;held=0;
  energy=Mathf.Clamp01(energy+.025f);Current=Mood.Content;
 }
 void Update() {
  sinceTouch += Time.deltaTime; held += Time.deltaTime;
  if(annoyedRemaining>0) { annoyedRemaining-=Time.deltaTime; return; }
  if(Current==Mood.Angry) { Current=Mood.Content; held=0; }
  float target = environment ? environment.Brightness : .55f;
  if (sinceTouch > inactivitySeconds) target -= .18f;
  energy = Mathf.Lerp(energy, Mathf.Clamp01(target), 1-Mathf.Exp(-Time.deltaTime/Mathf.Max(1,responseSeconds)));
  Mood next = energy < dimThreshold ? Mood.Sleepy : energy > brightThreshold ? Mood.Curious : Mood.Content;
  // Hysteresis around each threshold prevents repeated transitions.
  if (Current == Mood.Sleepy && energy < dimThreshold+.08f) next = Mood.Sleepy;
  if (Current == Mood.Curious && energy > brightThreshold-.08f) next = Mood.Curious;
  if (held >= minimumMoodSeconds && next != Current) { Current = next; held = 0; }
 }
}}
