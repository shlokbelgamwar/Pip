using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace Pip {
public class PipLightSensor : MonoBehaviour {
 public ARCameraManager source;
 public Light roomLight;
 public float smoothingSeconds = 4, staleSeconds = 5, dimLumens = 150, brightLumens = 1200;
 [Range(0,1)] public float fallbackBrightness = .55f, simulatedBrightness = .55f;
 public bool simulateInEditor = true;
 public float Brightness { get; private set; } = .55f;
 public bool HasEstimate => Time.unscaledTime-lastSample < staleSeconds;
 public string EstimateSource { get; private set; } = "Neutral fallback";
 float raw = .55f, lastSample = float.NegativeInfinity;
 void OnEnable() { if(source) { source.requestedLightEstimation = LightEstimation.AmbientIntensity | LightEstimation.AmbientColor; source.frameReceived += OnFrame; } }
 void OnDisable() { if(source) source.frameReceived -= OnFrame; }
 void OnFrame(ARCameraFrameEventArgs args) {
  var estimate = args.lightEstimation;
  // World tracking does not imply main-light direction or spherical harmonics support.
  if(estimate.averageIntensityInLumens.HasValue) {
   float value = estimate.averageIntensityInLumens.Value;
   if(!float.IsNaN(value) && !float.IsInfinity(value) && value>=0) {
    raw = Mathf.InverseLerp(dimLumens, Mathf.Max(dimLumens+1,brightLumens),value);
    lastSample = Time.unscaledTime; EstimateSource = "AR camera ambient intensity";
   }
  }
  if(roomLight && estimate.averageColorTemperature.HasValue) roomLight.color = Color.Lerp(roomLight.color,Mathf.CorrelatedColorTemperatureToRGB(estimate.averageColorTemperature.Value),.08f);
 }
 void Update() {
  float target = HasEstimate ? raw : fallbackBrightness;
#if UNITY_EDITOR
  if(simulateInEditor) { target = simulatedBrightness; EstimateSource = "SIMULATED light"; }
  else
#endif
  if(!HasEstimate) EstimateSource = "Neutral fallback — no recent light sample";
  Brightness = Mathf.Lerp(Brightness,target,1-Mathf.Exp(-Time.unscaledDeltaTime/Mathf.Max(.1f,smoothingSeconds)));
  if(roomLight) roomLight.intensity = Mathf.Lerp(.35f,1.1f,Brightness);
 }
}}
