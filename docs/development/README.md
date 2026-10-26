# Pip — iPhone 16 Pro Max prototype

Open this folder in Unity Hub using **6000.5.9f1**. This is a separate project; previous VR projects are untouched. The reference design document was read, not edited. Its embedded sketches were not visually retrieved. No matching class PDF was found in the inspected Documents folder.

**October 18 AR startup fix:** the first phone run exposed a missing ARKit compile flag in the batch export. This has been corrected, the Xcode project regenerated, and an unsigned native device build passed. Reopen Builds/iOS/Unity-iPhone.xcodeproj before pressing Run. Actual camera/plane/anchor behavior still needs a new phone test; see VALIDATION.md.

**Camera input correction:** ARKit registers a HandheldARInputDevice. The AR camera now binds to `<HandheldARInputDevice>/devicePosition` and `/deviceRotation`, not headset center-eye controls. A live camera image alone does not verify that virtual-camera pose tracking is working.

## What is implemented

The first slice uses a built-in renderer AR session, horizontal-plane detection, polygon raycasts, and an asynchronously created session anchor. Scan a textured floor/table; a teal reticle marks a valid surface. Aim the center crosshair at the surface, then press **Place Pip here** to summon one approximately 20–30 cm tall placeholder with a 15 cm body. Pip hovers 18 cm above its anchor. Tap Pip to react; use Reposition, aim the crosshair, and press **Move Pip here** to place a replacement anchor. Surface outlines reappear during repositioning. Placement stays within ARKit's currently mapped polygon; scan more of a surface to extend that polygon. Placement range defaults to 0.25–5 m and can be adjusted on PipPlacement in the Inspector. Tracking loss hides the companion until tracking recovers. Anchors last for this session only; this is not persistent room mapping.

Pip has separate movable leaves, eyes, scarf, and tail. Procedural hovering, blinking, pauses, looks, bounded local investigation, sleepy eyelids, and varied touch reactions run without user input. The mood machine has smoothed energy, threshold hysteresis, minimum dwell time, and inactivity influence. Inspector fields expose thresholds, timing, hover amplitude, and light smoothing.

ARKit ambient intensity drives gradual energy changes; nullable samples are validated and stale data falls back to neutral. Color temperature adjusts the approximate room light. This is environmental estimation, not a calibrated lux meter. Tail emission changes its surface appearance; there is no bloom or physical tail illumination.

No ARCore, Android configuration, following, depth mesh, or occlusion is enabled. Cloud object recognition is implemented as described below. Surface outlines and a reticle provide initial grounding; contact shadows remain a polish task.

## Mac setup required before an iPhone build

**October 18 setup update:** locally verified macOS **27.0.1**, **Xcode 27.1 (27A9275)** selected at `/Applications/Xcode.app/Contents/Developer`, and Unity **iOSSupport** installed. The previous missing-tool findings below describe the initial inspection. Xcode reports that its license has not yet been accepted: open Xcode and review/accept the agreement, or run `sudo xcodebuild -license` interactively in Terminal. Signing account/team and phone connection remain unverified.

Inspected locally: macOS 26.5; Unity 6000.5.9f1; installed playback engines are Mac, Android, and WebGL. **iOS Build Support is missing. Xcode.app was not found in /Applications; xcodebuild currently resolves to Command Line Tools.** Existing Android support belongs to the Editor installation and is not used by this project.

1. In Unity Hub → Installs → 6000.5.9f1 → gear/menu → Add modules, install **iOS Build Support**.
2. Your phone is running **iOS 27 public beta** (user reported). For a matching iOS 27 SDK, Apple's [Xcode requirements table](https://developer.apple.com/xcode/system-requirements) currently lists **Xcode 27** and requires **macOS 26.6 or later**. This Mac was inspected as macOS 26.5, so that Xcode toolchain requires a macOS update first. Check the exact phone beta version/build in Settings → General → About → iOS Version and select a corresponding Xcode release/beta from Apple Developer Downloads; do not assume that an arbitrary latest beta matches. This is the recommended matching-SDK path, not a claim that all older Xcode versions categorically cannot connect. Unity 6000.5.9f1 / ARKit 6.3.5 with this Xcode and iOS beta combination remains untested; keep this project version fixed until an actual export/build identifies a compatibility issue. macOS updates and Xcode installation have not been performed.
3. Open Xcode once, accept its license, and install the offered iOS platform components. In Xcode Settings → Locations select that Xcode's Command Line Tools. Check `xcodebuild -version` and `xcode-select -p`; the latter should point inside Xcode.app rather than /Library/Developer/CommandLineTools. If needed, select it with `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer`.
4. Xcode → Settings → Accounts: sign in with your Apple Account. Connect and unlock your iPhone via USB, trust the Mac, and enable Settings → Privacy & Security → Developer Mode when prompted (restart and confirm). See [Apple device distribution guidance](https://developer.apple.com/documentation/xcode/distributing-your-app-to-registered-devices).
5. Open Pip in Unity. Wait for package import. File → Build Profiles → iOS → Switch Platform. Project Settings → XR Plug-in Management → iOS should have **ARKit** checked and initialization on startup enabled. Run XR Plug-in Management → Project Validation and resolve any reported iOS requirements. Do not add Android providers.
6. Choose **Pip → Configure iOS** if settings need reapplication. Set a unique bundle ID and your signing Team ID under Player Settings; the starter ID is `com.shlok.pip`. An Apple signing team is intentionally not guessed.
7. **Pip → Export Xcode project** writes Builds/iOS. Open its Unity-iPhone.xcodeproj. Select Unity-iPhone target → Signing & Capabilities → Automatically manage signing → your team. Select the connected iPhone as destination and Run. Allow camera access on the phone.

A free Personal Team can test this prototype on your own phone; profiles expire after seven days and need rebuilding. Distribution/TestFlight needs an appropriate paid team. See [Apple account overview](https://developer.apple.com/help/account/basics/about-your-developer-account).

Configured: iOS minimum 18.0 (chosen for the target device), ARM64, IL2CPP, Metal, portrait, camera permission text, automatic signing without a team, ARKit loader, and only PipAR in the build scene list. The installed ARKit build processor has a historical Xcode minimum of 11.0; this does **not** establish compatibility with a modern phone OS. Use the current supported Xcode/device combination above.

The mobile camera rig uses **zero additional camera-height offset**; mobile AR must not inherit the 1.1176 m XR Origin default. Placement controls respect Screen.safeArea so instructions clear the Dynamic Island. A green crosshair plus enabled Place/Move button means a surface is available. No marker object needs to be put on the floor.

## Editor preview

Open **Assets/Pip/Generated/PipPreview.unity**, press Play, aim at the floor with the center marker, and click to summon. A slider at the top left simulates dim/bright light. Click Pip to react. Reposition replaces it. Give the default mood settings tens of seconds to settle. This uses a fixed camera, mathematical floor, and simulated brightness; it does not verify ARKit, real surfaces, anchors, light sensors, drift, or performance.

The device scene is **Assets/Pip/Generated/PipAR.unity**. Do not put PipPreview into the iOS build. Open the prefab to adjust PipMood and PipCharacter. Adjust PipLightSensor on “Pip systems” in either scene. The generated files are real saved Unity scenes, materials, XR settings, and a prefab, not instructions requiring manual wiring.

## Architecture

- PipPlacement: raycasts, anchor lifetime, tracking gates, replacement.
- PipInput: touch/mouse dispatch, minimal placement guidance and reposition UI.
- PipPlaneGuide: provider boundary outlines before placement.
- PipMood: environment/inactivity/touch → energy → Curious/Content/Sleepy.
- PipLightSensor: nullable provider estimates, smoothing, stale fallback, Editor-only simulation.
- PipCharacter: procedural visual movement and reactions beneath a stationary anchor.
- Editor/PipProjectSetup: reproducible asset generation, iOS settings, export.
- Editor/PipVerification: batch integration check of the simulated scene.

The character prefab's Visual child is replaceable. Preserve animation references or provide an adapter for the final model. The anchor and character root are separate from animated local offsets.

## Actual package capability check

Pinned and resolved: **AR Foundation 6.3.5**, **Apple ARKit XR Plugin 6.3.5**, **XR Plug-in Management 4.5.3**. Transitive dependencies, including Input System/Core Utils, are captured in Packages/packages-lock.json. Unity's official [AR Foundation samples](https://github.com/Unity-Technologies/arfoundation-samples) document the 6.3 line as compatible with Unity 6000.0 and newer; local compilation additionally checks the actual 6000.5.9f1 setup.

The installed package's `Documentation~/arkit-camera.md` explicitly lists **world-tracking ambient intensity and color temperature**, with **no world-tracking main-light direction, main-light intensity, or spherical harmonics**. It also does not advertise average brightness or color correction. Accordingly, the implementation consumes `averageIntensityInLumens` and `averageColorTemperature`, requests AmbientIntensity/AmbientColor, and tolerates missing values. It does not infer HDR support from the phone's LiDAR hardware.

LiDAR depth/occlusion can be evaluated after placement passes on device. Check the running occlusion subsystem descriptor and actual requested/current environment-depth modes, then compare frame timing and edge artifacts before enabling it. No claim of occlusion support has been device-tested here.

## Physical-device acceptance checklist

1. Camera permission allowed; denied permission gives recoverable guidance after restoring access in iOS Settings.
2. Scan a textured, well-lit surface; outlines/reticle appear and placement stays inside the polygon.
3. Walk around Pip for 1–2 minutes and return to the same viewpoint; assess drift, scale, and facing.
4. Cover the camera / interrupt the app, then return: Pip hides during lost tracking and returns without duplicating.
5. Reposition repeatedly: exactly one companion, no old visible anchor contents.
6. Move between bright and dim rooms: observe slow mood changes; tune intensity thresholds on real values rather than assuming laboratory accuracy.
7. Observe idle for several minutes and tap each mood: varied reactions with pauses, no frozen idle.
8. Profile on the phone: aim for smooth 60 fps, check sustained thermals; lower target to 30 if necessary. Neither frame rate nor thermal behavior is measured yet.

See VALIDATION.md for checks actually completed and remaining build gates.

### Pip dialogue
Summoning Pip starts a greeting. Tap Pip for a mood-dependent remark; tap the text box to reveal the full sentence, then tap again to close it. Chat with Pip requests another line when the box is closed. Idle remarks appear about once per minute. Text is currently authored locally; pointing at objects does not recognize them yet.

### Cloud recognition (October 19)
Pip targets https://pip-vision.vercel.app/inspect. The cloud client was installed in an earlier step, but the reported phone HTTP0 connection failure still awaits a fresh device diagnostic. Mac-to-cloud checks pass. See Backend/Cloud/README.md for free-tier limits and configuration.

## October 21 — automatic looking and Know more
After placement, hold a view steady for about two seconds: Pip sends one reduced camera frame and makes a short playful reaction. Tap **Know more** for a factual explanation plus a playful analogy of that same captured subject. Reopening the same explanation uses the in-memory cached response. **Pause looking** stops automatic requests. Network failures pause looking until resumed.

The trigger compares camera pose and a small local luminance sample; it is view stability detection, not semantic object tracking. The same view is suppressed until movement or sufficient visual change, with an eight-second minimum between automatic requests. Confirming a new placement clears the captured subject; canceling a move keeps the previous context. No images are saved by the client or backend; selected frames transit Vercel and Google Gemini. Idle comments pause while automatic looking is enabled.

Backend tests and live synthetic-image reaction/explanation checks pass. Physical phone behavior and the previously reported connection failure still require testing.

## October 25 — refined interface
The compact menu contains Move Pip, Pause/Resume looking and Say hello. Close dismisses dialogue immediately; tapping typing text reveals it. Tell me more opens an explanation that stays until closed, with touch scrolling for longer replies and Back to Pip for the original reaction. Reposition offers Cancel. Menus consume taps and temporarily suspend dwell detection. Offscreen and tracking/retry cues guide recovery.

Final Unity UI preview: Evidence/UIReview/Refined-UI-Final.png. Layout and focused interaction regressions pass; the signed iPhone build is ready. This UI pass was verified in the Editor and native build, not installed/validated on the phone. See VALIDATION.md for details.

## Floating speech and onboarding
Pip's responses float above its head instead of docking at the bottom. Normal persistent status/instruction banners are removed. A four-step first-use guide introduces Pip, surface placement, holding a view for two seconds, and Tell me more. Replay it from the overflow menu's How to play action. Completion is remembered on the device. Looking is suspended during the guide; contextual error/recovery feedback still appears when necessary.

## Concept-aligned visual polish
Latest UI uses compact warm translucent speech cards, a native font fallback, simple circular controls, a list menu and a cream walkthrough sheet. Static top-of-screen instruction banners remain removed. Replay onboarding under Menu → How it works. Actual Unity preview: Evidence/VisualPolish/Polished-Unity.png. The updated signed iPhone build is ready; device visual review remains pending.

## Typography system
One Helvetica Neue family (Arial fallback), defined normal/bold hierarchy and explicit line boxes now cover every UI surface. Walkthroughs size to their copy, and speech reveals without changing its final line breaks. See TYPOGRAPHY.md for the role scale and Evidence/Typography/Final-Walkthrough.png for the actual Unity preview. The signed typography build includes the full-width bottom action and dark walkthrough; phone review remains pending.
