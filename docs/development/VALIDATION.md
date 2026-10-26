# Validation — October 17, 2026

## Completed

- Created an independent Unity project with installed Editor 6000.5.9f1.
- Unity Package Manager resolved the manifest and produced packages-lock.json.
- Compiled runtime and Editor scripts against AR Foundation / ARKit 6.3.5 and XR Management 4.5.3; no C# compile errors in the final generation or integration runs.
- Executed the scene generator successfully. Saved PipAR, PipPreview, Pip prefab, materials, ARKit loader, and XR settings. The generation log reports ARKit loader assigned True.
- Inspected saved iOS player configuration: IL2CPP/ARM64, Metal, iOS 18 minimum, portrait, camera purpose string, input handling Both, and automatic signing (no team).
- Headless Editor Play Mode integration check passed: simulated floor summon, autonomous visual movement, dim→Sleepy, bright→Curious, touch energy update, and exactly one companion after reposition.
- A second graphics-enabled Editor run passed the same checks and saved Evidence/PipPreview.png. Visually inspected the rendered placeholder: body, eyes, leaves and scarf are visible and face the preview camera. This is a synthetic scene, not a camera-feed image.
- Read the installed provider's arkit-camera.md capability table and ARKitBuildProcessor.cs Xcode guard. Ambient intensity and temperature are used; unsupported world-tracking HDR fields are not used.

Evidence/ contains compact extracts of actual Unity output. Full session logs were written to /tmp/pip-generate2.log, /tmp/pip-verify.log, and /tmp/pip-visual.log (temporary, not portable).

## Observed tooling limitations

Unity's search-index initialization logged ArgumentOutOfRangeException inside UnityEditor.Search.SearchDatabase during both preview runs. The Pip checks subsequently passed. This is an Editor search-index issue, not a clean-console claim. Unity 6.5 also emits deprecation warnings for the test harness's FindFirstObjectByType / sorted FindObjectsByType overloads; they compile and executed successfully.

Unity MCP at localhost:8080 was unavailable, so project creation, import, generation and Play Mode checks used the installed Unity executable in batch mode. Initial sandboxed creation was blocked by Unity Package Manager's local socket; authorized execution outside the sandbox succeeded.

## Not verified / remaining gates

- iOS Build Support is not installed. Full Xcode is not installed or selected. No Xcode project, signed app, IPA, device install, or iOS compilation was produced.
- User reports iOS 27 public beta; the exact beta build and Apple signing team remain unknown. Apple's currently retrieved Xcode table lists Xcode 27 with iOS 27 SDK and macOS 26.6+ requirement. This Mac is on 26.5, so the matching-SDK setup requires a macOS update. Xcode/Unity/beta-device compatibility and provisioning remain unverified; no operating-system update was performed.
- Physical ARKit session startup, camera permission flow, plane/raycast accuracy, real anchor creation, tracking-loss recovery, drift, and real ambient-light sample behavior require the phone.
- The integration test directly calls the same Tap dispatch method used by input; it does not validate physical touch input or every visual reaction.
- No frame timing, thermals, LiDAR depth/occlusion, or contact shadows have been verified. No occlusion manager is enabled.
- The character is an intentionally simple primitive placeholder. Final-model animation and full visual polish remain future work.

To repeat the Editor integration test, close the project in the GUI first and run the Unity executable with `-batchmode -projectPath <Pip folder> -executeMethod Pip.Editor.PipVerification.Run -logFile <log path>`. Do not add `-quit`: the harness exits after its asynchronous checks. Add `-nographics` for a headless run, which omits screenshot capture.

## October 18 — physical-device startup correction

The first phone run reached the app but showed “Failed to load session subsystem” and no camera feed. The original export was therefore not a working AR build. Diagnosis: the ARKit loader was assigned, but `UNITY_XR_ARKIT_LOADER_ENABLED` was missing from iOS scripting defines. ARKit's automatic define updater explicitly skips batch mode in this package version. Generated `Api.AtLeast11_0` returned constant false, and the original Xcode project did not link libUnityARKit.a or ARKit.framework.

Fixed Configure iOS to persist the required define and enabled startup initialization. Export now refuses to build from an Editor assembly lacking the iOS/ARKit compile flags, prompting configuration and recompilation first. The existing bundle identifier and selected signing team are preserved rather than replaced.

Verified after correction:

- Unity imported and compiled the corrected Editor code with no Console errors before export.
- iOS export completed with `Build Finished, Result: Success.` in Logs/Editor.log.
- Generated availability code calls native `UnityARKit_Version_AtLeast11_0` rather than returning false.
- Xcode project links libUnityARKit.a and ARKit.framework and includes UnityARKit.m.
- Unsigned ARM64 device build using Xcode 27.1 / iPhoneOS27.1 SDK completed with **BUILD SUCCEEDED**. This verifies compilation and linking; it does not verify camera/tracking on the phone.
- Restored the signing team from the user's signed project and persisted that team in Unity for future exports.

Next required check: reopen the regenerated Xcode project and Run on the physical phone. Verify camera permission, camera feed, plane scanning and anchored placement. The user-provided destination picker shows the phone running iOS 27.2; that beta runtime remains part of the device validation.

Unity-generated Objective-C deprecation warnings and the GameAssembly script-output warning are separate from the AR startup failure. The missing 1024px App Store icon remains a distribution/polish task.

## October 18 — coordinated facial expressions

Added PipFace and wired it into PipCharacter and the existing prefab. Small eyebrows, eye aperture/tilt, lip shape and a brief mouth opening now share smoothed mood/reaction state. Curious raises one brow; content keeps a soft smile; sleepy lowers eyelids/brows and flattens the smile. Sleepy touch opens the eyes unevenly. Existing head/leaf animation complements the face.

Checked compilation through Unity MCP (zero Console errors after the final change), saved prefab references, and rendered/visually inspected the three steady expressions in Evidence/PipExpressions.png (left: curious; center: content; right: sleepy). The first import briefly failed to discover the new script until a full AssetDatabase refresh; the subsequent compile succeeded. No new phone build or on-device expression-animation check has been performed. Inspector controls: PipFace.expressionSpeed and expressionStrength; context-menu preview poses are available on PipFace.

## October 18 — expression redesign after visual feedback

Replaced the similar open-eye expressions with four clear silhouettes, referencing Paimon's expressive chibi eye/mouth/pose language (official Genshin Paimon emoji sheet: https://x.com/GenshinImpact/status/1328178479414665216). Pip's geometry and design remain original.

Evidence/PipExpressions.png now shows, left to right: Curious (wide eyes / O mouth), Happy/Content (closed crescent eyes / broad smile), Angry (slanted brows / squint / frown), Sleepy (soft inner-raised brows / nearly flat closed eyes / small mouth / drooping leaves). Angry is appended to the mood enum to preserve existing serialized values and can be triggered by three taps close together; it is a temporary state with an Inspector-adjustable duration.

Final source compiled with zero Unity Console errors. Saved and rendered all four prefab expressions and visually inspected the result. New facial poses and mouth contours have not yet been exported or tested on the phone.

## October 18 — placement and repositioning check

User clarified that repositioning and farther surfaces failed. Found a hard-coded 3 m limit, center-reticle vs tap-ray disagreement, and hidden plane outlines during repositioning. Changes:

- Default range is now 0.25–5 m, adjustable on PipPlacement; both scenes explicitly save the 5 m maximum.
- Aim the center marker, then tap outside controls to place/reposition. The committed point and reticle now use the same camera-viewport-center ray. Character interaction still hit-tests the touch position.
- Plane boundaries remain provider-mapped polygons (not infinite planes). More scanning is necessary outside them. Boundaries reappear while repositioning.
- Consider all ray hits until finding an in-range tracked upward surface instead of rejecting the entire result based on only the first hit.
- Preserve existing Pip on invalid placement or anchor failure. Hide the previous instance immediately before deferred destruction. Failure feedback stays visible briefly.

Verification: final C# compilation had zero Console errors. In actual Editor Play Mode, a simulated floor check passed placement at approximately 4 m, off-center tap/center-marker agreement, repositioning to a new point, exactly one active Pip, and preservation on invalid placement. The first checks uncovered a camera/screen dimension mismatch in the Editor and transient duplicate visibility; these were corrected and the final check passed. This is not a real ARKit surface/anchor test.

Re-run on the phone after the new export: scan a farther floor/table area, choose Reposition, aim the teal marker at the mapped surface and tap. Assess tracking stability and the actual detected polygon at distance. No physical-device pass is claimed for this change.

Placement update export completed successfully after running the export from Unity's actual menu (an immediate MCP request during the player-loop transition was rejected). Generated IL2CPP includes PipFace and the 5 m placement default; native ARKit linking and the existing signing team are retained. The new Xcode project is ready for a phone build. No additional native Xcode compilation was run for this placement update.

## October 18 — unclear marker / mobile placement follow-up

The phone screenshot showed the camera feed, surface outline and a valid-placement prompt, but no visible Pip; the prompt was obscured by the Dynamic Island. It does not establish whether touch input, anchor creation, or visibility failed.

Found the generated AR scene still serialized XROrigin's default 1.1176 m CameraYOffset. In the installed Core Utils source, Device/Unbounded tracking applies that offset to the camera floor-offset object. This can misalign tracked rendering relative to the camera image. Corrected both scenes and the scene generator to zero CameraYOffset and zero local camera-offset transform. A phone retest is required to confirm this explains the reported symptom.

Added an always-visible screen crosshair, explicit Place Pip here / Move Pip here buttons enabled when a surface is available, progress text during anchoring, longer failure feedback, and safe-area-aware text/button positioning with responsive sizing. The crosshair is green when placement is ready. A scanned floor/table is sufficient; no marker image or physical object is required.

Editor compilation after the final changes had zero Console errors. Inspected the updated header/crosshair visually in the Editor. Place/Move actions were checked against the simulated floor. These checks do not validate real ARKit anchor creation or the phone's final safe-area layout.

Final follow-up verification: zero camera offset, explicit Place and Move, and one active companion passed in Editor Play Mode. Corrected iOS export completed with Build Finished, Result: Success. Generated IL2CPP includes ConfirmPlacement and DrawCrosshair. No new on-device pass is claimed.

## October 18 — Pip appears at bottom of phone view

The phone screenshot confirms Pip was instantiated but appeared clipped at the bottom. Previous zero-height/UI fixes therefore did not establish correct final placement.

Changed mobile surface queries from provider screen-point conversion to the ARRaycastManager world-Ray overload, using the same camera ray as the crosshair. Installed ARKit 6.3.5 advertises supportsWorldBasedRaycast=false; the installed AR Foundation implementation therefore uses registered ARPlaneManager polygon intersections for this path. This still uses actual tracked polygons, not an infinite mathematical floor. Added a screen-projection check to reject surface hits that do not return to the intended pixel.

Changed character instantiation to zero local position under the actual returned anchor, rather than retaining a pre-await world-space pose offset. Orientation faces the user from that resulting anchor position. The empty status panel now disappears after feedback expires.

Verification went beyond the earlier mathematical Editor floor: injected a temporary synthetic bounded ARPlane into the real ARPlaneManager collection and exercised AR Foundation's managed-ray fallback through PipPlacement.TrySurface. The selected surface projected within 1 pixel of the crosshair. A synthetic refined-anchor transform deliberately offset from the requested pose verified that CreateCharacter uses local zero. Temporary test objects were removed. Final C# compilation showed zero Console errors before testing. These checks validate the coordinate path, not physical ARKit tracking or the phone's rendered result.

Latest placement-coordinate fix: iOS export succeeded; a signed Xcode Debug device build completed with BUILD SUCCEEDED. CoreDevice reported success for installation of com.shlok.pip and launch on the connected iPhone (2). The app is now running the updated build. Physical visual placement and tracking stability still require user observation; installation/launch is not a placement pass.

## October 18 — actual camera input binding error found

User reported live camera imagery but teal outlines stuck at the screen bottom. Inspection of the installed ARKit source (`Runtime/Input/InputLayoutLoader.cs`) showed that the provider registers HandheldARInputDevice, whose defined pose controls are devicePosition/deviceRotation. The saved rig had incorrectly bound position/rotation/trackingState to XRHMD center-eye controls. Camera imagery and ARKit plane detection can operate while that virtual-camera driver fails to resolve the phone's pose. Earlier raycast-only Editor checks did not exercise this binding and therefore missed the problem.

Corrected both saved scenes and the generator to `<HandheldARInputDevice>/devicePosition` and `<HandheldARInputDevice>/deviceRotation`, with explicit Vector3/Quaternion action control types. Removed the unsupported authored HMD trackingState binding; TrackedPoseDriver derives pose availability from its resolved position/rotation controls when no tracking-state binding is authored (verified in installed Input System source).

Stronger check: entered actual Editor Play Mode, added an Input System HandheldARInputDevice, queued nontrivial position and quaternion rotation state events, enabled the saved camera driver and processed Input System events. The camera followed the supplied pose within 1 mm / 0.1 degrees. Temporary device was removed and camera state restored. Source import had no Console errors.

A DEVELOPMENT_BUILD-only local JSON diagnostic now records session state, plane count, pose binding resolution/raw values and native/managed raycast counts once per second in the app's Documents folder. It transmits nothing to an external service and is intended for this debugging pass. Physical-device results are recorded below when available.

Handheld-binding update export and signed Xcode build succeeded. Installation was attempted but not performed: CoreDevice listed iPhone (2) as disconnected with developer services unavailable. The newly signed handheld-binding build is therefore NOT yet on the phone. User reconnection requested; live diagnostic verification is pending.

Subsequent direct installation to the paired USB phone succeeded, followed by successful launch. The earlier helper incorrectly required an already-connected CoreDevice tunnel; an idle/disconnected tunnel did not prove USB disconnection. Initial on-device JSON confirms both pose actions resolve /ARKit/deviceposition and /ARKit/devicerotation, both enabled, and camera rotation matches raw device rotation. Plane/raycast subsystems are running. Initial session was SessionInitializing with zero planes; user instructed to hold upright and scan floor. Floor scanning and placement remain pending physical verification.

## Upright character correction
User confirmed face/body tilted sideways after placement/reposition. Character root now starts with world-upright rotation even with an anchor parent, and LateUpdate flattens its forward direction against gravity up after anchor updates. Anchor position parenting is retained; visual mood animation still applies its small local tilt. Editor regression rotated a real prefab parent by 90 degrees and invoked LateUpdate: root up dot world up = 1. No Console errors. Export/device build and physical visual confirmation pending. This check verifies the upright guard, not the exact on-device cause of the reported roll.

Upright correction: Unity export succeeded, signed Xcode Debug device build succeeded (`/tmp/pip-upright-build.log`), and corrected app installed and launched successfully on paired iPhone (2). Physical visual confirmation of upright placement remains pending.

User supplied IMG_6084.PNG after upright update: creature visibly remains sideways. Thus prior anchor-upright regression is insufficient; visual roll issue remains unresolved. Added local device diagnostics for root up, visual up/local rotation, projected gravity up, projection matrix, and all XR quaternion features to distinguish model/anchor rotation from camera/view alignment. No camera compensation applied without device measurements.

On-device diagnostic captured SessionTracking, one plane, successful native/managed hits, root and visual up (0,1,0), visual local rotation zero, but projected world up (-243,34) pixels. Camera raw rotation (41.8,359.4,275.4) matches ARKit CameraRotation; its gravity in camera coordinates is (-.74,.07,-.67). Added PipARCameraPoseDriver, a TrackedPoseDriver subclass, applying local +90 degree Z correction on iOS Portrait only. It transforms measured gravity to (.07,.74,-.67), matching an upright portrait view. Both scene components and scene generator updated; pose bindings preserved. No generic ARKit compatibility claim: correction is for this measured iOS/package combination and portrait prototype. Console clear. Device build/verification pending.

Portrait camera correction exported, signed build succeeded (`/tmp/pip-portrait-pose-build.log`), installed and launched on iPhone (2). Live placed-device verification: SessionTracking, one plane, raw rotation (28.7,18.9,271.4), camera rotation (28.7,18.9,1.4); character and visual up (0,1,0). Projected world up is now (5.8,229.0) pixels, compared with (-243,34) before. This confirms the intended portrait camera-basis correction executes on the physical device and restores upward projection. User visual confirmation still welcome.

## First dialogue prototype
User confirmed upright physical placement and requested text. Added runtime PipDialogue to each summoned creature: summon greeting, 32 characters/sec reveal, tap-to-reveal/tap-to-close, seven seconds reading time after typing, per-mood original lines, no immediate line repeats, tap cooldown, anger interruption, and idle comments at 40–65 second intervals. Text box lives above reposition button inside safe area; its hit region consumes screen input before character raycasting. Chat with Pip button appears when text is closed. Dialogue is hidden while repositioning/tracking disables the creature. No object-recognition claims or external model calls. Play Mode checks passed for placed-dialogue wiring, reveal, close, chat, cooldown; Console clear. Phone build pending.

Dialogue prototype: Unity export and signed Xcode build succeeded (`/tmp/pip-dialogue-build.log`); app installed and launched on paired iPhone (2). Physical text layout and readability await user review.

## Tap-to-inspect vision prototype
Gemini key verified without printing it. Old gemini-2.5-flash-lite endpoint rejected this new account; changed private/public example configuration to gemini-3.5-flash-lite after official pricing and live API check. Python uses Mac system CA trust after default Python CA configuration failed. TLS verification remains enabled.
Implemented standard-library token-authenticated local proxy, in-memory images, bounded JPEG body, five-second cooldown, hard 60-request process-session cap, no automatic retries/paid fallback. Mock-provider HTTP checks passed for invalid token, malformed request, valid reply routing, cooldown and session cap. Added Unity asynchronous CPU-camera conversion/reduced JPEG, explicit aim/Inspect controls, local config without provider key, safe errors and local-network Info.plist configuration. Console clear.
Automatic approval initially blocked launching the proxy pending explicit camera-image destination consent. User subsequently said "enable uploads"; then proxy started on Mac Wi-Fi interface. Live end-to-end generated mug-image check returned HTTP 200, object mug and a playful remark. No personal images used in that test. Real phone camera capture and LAN permission remain pending physical test.

Vision build exported, signed Xcode build succeeded (`/tmp/pip-vision-build.log`), installed and launched on iPhone (2). Export Info.plist has local network usage description and NSAllowsLocalNetworking; provider key absent from client config. Mac .local backend health returns ready. Backend remains running in development process; phone must share trusted Wi-Fi with Mac. Actual phone Inspect action awaits user verification.

## Camera capture Pending-state fix
User reported "My eyes blinked". Inspection of the installed XRCpuImage AsyncConversionStatus enum showed Pending and Processing are both unfinished; previous loop only waited for Processing, then disposed a queued Pending request before it could finish. Replaced loop with !status.IsDone() and a ten-second deadline. Added development-only metadata diagnostics (conversion status, JPEG byte count, upload response code; no images/keys) in Documents/PipVisionDiagnostics.txt. Console clear. Physical capture verification pending after install.

Capture Pending-state fix exported, signed Xcode build succeeded (`/tmp/pip-capture-pending-build.log`), and app installed/launched successfully on iPhone (2). Backend health ready. On-device Inspect result pending user attempt; do not claim end-to-end camera success until observed.

## October 19 — cloud backend migration
Created Vercel project pip-vision and Flask deployment at https://pip-vision.vercel.app. Copied backend credentials to encrypted production environment variables without displaying values; source upload excluded .env. Initial framework-null deployment failed due app.py function matching, corrected project to Flask and deployed successfully. Production alias reachable; preview/generated deployment URLs retain Vercel protection while app production route uses bearer authentication.
Seven cloud tests passed: auth, explicit enable gate, invalid input, payload size, reply mapping/no-cache, per-instance burst ceiling, and safe provider errors. Live HTTPS health returned ready:true; missing-auth request returned401; generated mug image returned200 with label mug and playful remark. No personal camera image used for that test.
Firewall API repeatedly returned config404; dashboard allowed log-only rule but enforced429 opened Pro upgrade prompt despite Hobby allowance in documentation. No paid upgrade accepted. Free prototype uses token, client cooldown and per-instance12/min burst guard; this is NOT a durable global/daily limit. Gemini free-tier quota remains upstream limit. No additional paid service activated.
Unity client endpoint changed to HTTPS cloud alias, local-Mac error copy replaced with internet check, insecure HTTP disabled and local-network ATS exception/usage description removed from export. Console clear. Phone export/install pending.

Cloud iOS export and signed Xcode build succeeded (`/tmp/pip-cloud-client-build.log`). Exported Data/Raw config confirmed HTTPS production endpoint; no local-network usage description or NSAllowsLocalNetworking exception remain. Installation was attempted but CoreDevice returned4016 (required connectivity/device-services/power states unavailable). User asked to connect/unlock. New build is NOT confirmed installed; previous phone build still depends on local server, which is deliberately retained until update.

Cloud client installation completed after explicit user approval to install and launch now. Device install and process launch both succeeded using the signed cloud-connected build. Physical Inspect over Wi-Fi/cellular awaits user verification.

## Cloud fetch report investigation
User reports API fetch failure after cloud client installation. Current production health returned200 ready:true; fresh authenticated synthetic mug request returned200 in1.7seconds. Runtime logs show successful test request. Physical-device metadata shows conversion Ready and JPEG byte counts, then upload0 on recent failures; this indicates no HTTP response rather than an HTTP auth/quota status. Exported and signed app config both point to HTTPS cloud production URL. Added request hostname, UnityWebRequest result/error and elapsed time to development-only metadata, without images or credentials. Exact phone network cause remains unverified pending new diagnostic attempt; user asked for displayed message and Safari health reachability.

Network diagnostic export and signed build succeeded (`/tmp/pip-network-diagnostic-build.log`). Installation succeeded, but iOS refused process launch because the device is locked (CoreDevice10002/FBS Locked). User must unlock/open Pip and trigger one Inspect attempt before the exact connection error can be read. API fetch issue remains unresolved; no speculative network/TLS workaround applied.

## October 21 — personality/context prompt
Added shared Cloud/personality.py with original companion identity, four distinct mood voices, visual grounding, whole-scene/unfamiliar/unclear-image handling, brief140-character speech, no fabricated memory, and image-text instruction isolation. Local and cloud payloads now use Gemini systemInstruction with mood context in the user turn. Removed unconditional replacement of uncertain responses so honest in-character reactions survive. Nine tests passed including false-confidence reply preservation, payload context and output length.
Production deployment was rejected by automatic approval review for lack of explicit authorization to upload the six backend source files to Vercel. Verified project/account/domain via get_project and matching prior deployment; second review still required exact payload/destination authorization. No deployment was executed. Explicit publish question pending. Existing production prompt remains active until approved.

Live direct-Gemini tests of the new system prompt succeeded on three synthetic frames (mug/Curious, unfamiliar colored shape/Sleepy, blank/Content), producing distinct grounded reactions. Sleepy sample emitted a leading *yawn* despite output instructions; added shared spoken_remark normalization to remove leading stage directions. Ten tests now pass including that regression. Cloud production deployment remains unexecuted pending approval.

Personality update explicitly approved and published to existing production project. Deployment dpl_FFv1WcVttiyM4C1e9NYsLbcxJSCo READY at https://pip-vision.vercel.app. Post-deployment generated abstract-image Sleepy test returned200 and a grounded sleepy-moon comparison, with no stage direction. Current iPhone client signed build succeeded (`/tmp/pip-personality-client-build.log`). No device installation attempted in this step. The phone connection issue still requires a fresh diagnostic Inspect; personality changes do not establish phone-side connectivity.

## October 21 — automatic reaction and optional explanation
Implemented local two-second view stability gate with camera pose and 16x16 center-luminance sample. Actual Unity gate regression passed dwell timing, motion reset, same-view suppression and visual-change rearming. This is a view-change heuristic, not semantic object identity tracking. Added Pause looking, progress hint, Know more and a scrollable longer dialogue panel. Editor reflection checks passed cached explanation reuse without a request and non-overlapping dialogue/Know more/reposition rectangles. Screenshot capture did not produce a file, so visual phone-layout verification remains pending.

13 backend tests passed for modes, validation, provider context and output normalization. Production deployment dpl_96wNLEG4Vi6sw6Asx2MJYPxyY8L5 reached READY. Authenticated live synthetic mug requests returned HTTP200 for both modes: react gave a brief swimming-pool/potion joke; learn described a ceramic vessel for warm liquids and added a swimming-pool analogy. No personal image used.

Unity export succeeded and generated IL2CPP contains PipLookGate. Signed Xcode Debug iPhone build succeeded (`/tmp/pip-automatic-look-build.log`); product `/tmp/Pip-iOS-VerifiedBuild/Build/Products/Debug-iphoneos/Pip.app`. Product config confirmed HTTPS cloud endpoint and no provider key or local ATS exception. No current-flow installation attempted. Earlier phone HTTP0 fetch failure remains unverified by this update.

## October 22 — minimal Unity UI
Replaced full-width runtime panels with rounded status/menu controls, compact summon action, mint aiming brackets and dwell progress, small pause control, and a screen-space speech bubble positioned from Pip's projected upper body. Tell me more retains the existing cached-frame explanation flow. Move Pip and Say hello are in the overflow menu. First-placement and menu copy explain frame uploads.
Unity compilation has no Console errors. Editor portrait preview (780x1688) used actual runtime UI and character with simple staged Unity desk props and a canned reaction; no camera image or recognition upload was made. Cached Tell me more and non-overlapping bubble/learn/pause bounds passed. Actual captures: Evidence/PipMinimalUI-Reaction.png and PipMinimalUI-Explanation.png. PipUIPreview stages only Play Mode in the explicit simulation scene. This step does not export/install a new phone build; safe-area/touch behavior still needs device review.


## October 25 — UI refinement and regression pass
Reviewed placement, reposition/cancel, short dialogue, explanations, scrolling, menus, paused/error states, offscreen Pip and anchor recovery. Runtime overlay remains minimal and portrait/safe-area aware.

Changes:
- Separate PipUILayout geometry; content-sized bubble with bounded height, smoothed movement, bottom docking when space is limited, 44-point close/menu/action targets and readable contrast.
- Gesture ownership from touch beginning; menu dismissal consumes the tap, world drags cannot place Pip, and explanation swipes scroll without closing or touching the world. Menu blocks underlying actions and pauses local dwell detection.
- Explicit dialogue kinds. Close always dismisses, reveal preserves appropriate reading time, explanations/errors remain until dismissed, and Back to Pip restores the actual last visual reaction. Rendered text does not interpret rich-text markup from responses.
- Placement/recovery/retry hints and disabled actions now reflect their actual availability, including anchor loss while the AR session still tracks. Reposition clears stale placement feedback, provides Cancel, and preserves captured context on cancellation; new confirmed companions clear the old context.
- Resume clears pinned errors; Pause aborts an active network request. Same-view suppression survives reading/menu holds. Long explanation reading suspends automatic observation.

Verification:
- PipUIReview.Layouts passed 480 combinations of five portrait safe areas, content lengths, docking/visibility and action/cancel states. Bounds, separation and minimum touch-target checks passed.
- PipUIReview.Interactions passed pinned expiry, immediate close, cached explanation/back, outside-menu dismissal, world drag routing, reposition cancel/context retention and dwell pause/same-view suppression.
- Additional Play Mode checks passed overflow touch scrolling without closing, retry releasing the pinned error, and anchor-loss recovery status taking precedence over session tracking.
- Unity Console has no errors after final changes. Actual Unity captures are in Evidence/UIReview; Refined-UI-Final.png is the final overlay capture. Preview props and dialogue are staged Editor simulation, not live recognition output.
- Unity iOS export and signed Xcode Debug build passed, log /tmp/pip-ui-refinement-final-build.log. Product: /tmp/Pip-iOS-VerifiedBuild/Build/Products/Debug-iphoneos/Pip.app. No installation performed for this pass.

Physical iPhone touch, notch, tracking and network behavior remain to be checked. The earlier phone HTTP0 connectivity report is not established as fixed by UI work. No backend deployment or personal-image upload was performed in this pass.

## October 25 — floating speech and first-use walkthrough
Reactions and explanations now remain above Pip's projected head, with horizontal/safe-area clamping, restrained following, and scrolling when there is less vertical space. Removed the normal status pill and permanent instruction/footer text. Contextual recovery/loading/error and offscreen feedback remains when needed.

Added PipWalkthrough: welcome, guided surface placement (advances only after Pip exists), two-second looking with camera-upload disclosure, and optional Tell me more/menu controls. Completion/Skip saves Pip.Walkthrough.v1 in PlayerPrefs; How to play replays the guide. Walkthroughs block accidental world taps and suspend automatic recognition; the placement step allows normal summon controls. Preview staging finishes only the in-memory walkthrough and does not change the saved preference.

480 portrait layout combinations passed, including above-Pip positioning where space permits. Walkthrough progression, returning-user state and replay checks passed. Existing close/reading/menu/cancel/dwell regressions passed. Additional Play Mode check passed modal world-tap consumption and recognition suspension. Unity Console clear. Actual screenshots: Evidence/Walkthrough/Floating-Speech.png, Welcome.png and Looking-Guide.png (staged Unity simulation). Final signed iPhone build succeeded: /tmp/pip-walkthrough-build.log. No installation or new physical-device validation performed.

## October 25 — visual polish against concept reference
Used the original generated concept as a style reference, then implemented the styling in the actual Unity overlay. Speech width reduced from 286 to 248 logical points, with content-sized warm charcoal surfaces, subtle edges, an antialiased speech pointer and restrained mint accents. Replaced the default skin font with runtime SF Pro Text / Helvetica Neue / Arial fallback; no system font files were copied. Controls now use drawn icons and lighter circular surfaces. Tell me more no longer uses sparkle decoration. The menu is a divided list, not nested pills; underlying dialogue is hidden while it is open. Long replies use a thin custom reading indicator instead of the default desktop scrollbar. Walkthrough uses a cream bottom sheet, sage action, small step dots and simpler copy. Existing first-use persistence and replay behavior are retained; replay is labeled How it works.

480 geometry/walkthrough combinations and focused dialogue/menu/gesture/cancel/dwell checks passed after the style changes. Final hidden-native-scrollbar touch scrolling check passed. Unity Console clear. Actual final captures: Evidence/VisualPolish/Polished-Unity.png, Menu-Final.png, Walkthrough-Final.png and Reading-Final.png (the latter deliberately contains a 360-character W stress string). These are Editor simulation, not a live phone camera or fresh recognition output. Native iPhone signed Debug build succeeded, /tmp/pip-visual-polish-build.log. No installation, backend deployment or image upload performed. Physical device appearance/performance remains to be reviewed.

## Full-width bottom action
Moved Tell me more (and its Back to Pip state) into a centered 48-point bottom action with 16-point safe-area side insets. Pause remains above it, speech remains above Pip, and contextual feedback is positioned above Pause when this action is present. 480 layout checks passed, including new full-width centering and non-overlap assertions. Unity compilation is clear. Evidence/VisualPolish/Bottom-Action.png is the actual simulated Unity capture. This layout change has not yet been exported to a new phone build.

Walkthrough palette follow-up: replaced the cream surface with the same warm translucent charcoal as speech, off-white body text, muted supporting copy, mint accents and a matching translucent action. Verified actual Unity render at Evidence/VisualPolish/Dark-Walkthrough.png; Console clear. Not yet exported to the phone build.

## Typography coherence pass
Introduced shared PipTypography roles with explicit sizes, line boxes, normal/bold weights and alignment rules; documented in TYPOGRAPHY.md. Removed temporary per-heading style mutation. Primary and secondary controls have distinct weights. Body copy, speech, captions and labels now have deliberate, consistent leading. Walkthrough height is based on measured text, and menu disclosure has sufficient height for the larger line boxes. Full reply wrapping is measured once; the typewriter reveals a prefix of those stable runs. Emoji surrogate pairs are retained across line boundaries.

An initial separate SF Pro bold face was listed by the OS but failed to load in Unity. Corrected to one Helvetica Neue family (Arial fallback) with normal/bold styles; actual rendered headings/actions verified and subsequent Console had no UI/font errors. No OS font assets copied.

PipTypographyReview passed 252 font/width/scale/sample combinations for width limits, character preservation, emoji boundaries and line leading, plus single-line action checks. PipUIReview passed 480 layout/walkthrough combinations and existing interaction regressions. Touch scrolling with explicit-leading long text passed. Actual final previews: Evidence/Typography/Final-Speech.png and Final-Walkthrough.png. Long-Text.png uses a 360-character stress string. All previews are staged Unity simulation, not phone or model output. iPhone native build outcome recorded below when complete; no installation performed.

Final typography iPhone export and signed Debug build succeeded (/tmp/pip-typography-build.log). This build also includes the full-width bottom action and charcoal walkthrough follow-ups. Product remains /tmp/Pip-iOS-VerifiedBuild/Build/Products/Debug-iphoneos/Pip.app. Physical font appearance, touch and network verification remain pending; no installation performed.

## October 25 — feel pass (motion, depth, haptics)
Kept the approved charcoal direction, layout roles and typography system. Changes are in PipInput (drawing and motion), PipTypography (edge fading), PipUILayout (one placement rule) and a new PipHaptics bridge.

Motion: speech bubbles rise and scale in from Pip's pointer. Dismissed or expired bubbles fade out instead of vanishing. Replacing text (reaction to explanation) re-settles the bubble. The menu grows from its button while everything beneath fades back and stays blocked. Walkthrough cards slide in, step content settles after the card resizes, and the progress dots stretch to the active step. Feedback is now a content-sized pill that fades in, cross-fades its message, keeps its last words while fading out, and shows a small spinner while busy. Buttons scale down on press. The reticle tightens as the two-second hold completes and a mint ring pulses when a capture fires.

Depth and detail: soft layered shadows under cards, pills and circles. The speech pointer shares the card rim and overlaps it, removing the seam. Explanations show the subject beside Pip's name (word-boundary trimming). Long replies fade at the scroll edges, replacing the "Swipe for more" line, and the bubble's bottom inset now matches its sides. Menu card fits its content and dividers snap to whole pixels. Walkthrough uses progress dots in place of the "Getting started" label and a primary action. Line icons now rotate in local space, fixing strokes that skewed while a surface was animating. Near the top of the screen, bubbles shift left only enough to clear the menu button so the pointer still reaches Pip.

Haptics: Assets/Plugins/iOS/PipHaptics.mm (UIKit feedback generators) via PipHaptics.cs. Light tap on controls and new reactions, soft impact on capture, medium on placement, warning on errors. No-op in the Editor.

Verification: PipUIReview.Layouts passed 480 combinations. PipTypographyReview passed 252. PipUIReview.Interactions passed in the staged preview. Unity Console clear of errors. iOS plugin importer confirmed iOS-only. Native Pip → Export Xcode project succeeded and includes Libraries/Plugins/iOS/PipHaptics.mm. Captures in Evidence/FeelPass (staged Unity simulation; animation states were settled before capture because the background Editor was throttled to a few frames). Not yet done: xcodebuild of this export, installation, on-device review of motion timing and haptics. Previous versions of the edited files are in Backups/UIPass-2026-10-25.

## October 25 — AR guideline pass (lighter speech, fewer overlays)
Based on Apple's AR HIG and Google's ARCore UI guidance: keep the screen for the world, minimize text, prefer visual and in-context cues, and teach tasks when they occur.

Speech now uses three surfaces. Reactions and chatter are light barks above Pip that hug their text, with no name tag or close button; tapping one finishes it, then dismisses it. Explanations open in a full-width reading sheet above the bottom action (Pip name, subject, close), so a typical explanation shows about 9 to 10 lines before scrolling; Pause steps aside while it is open. Errors keep the dismissible card above Pip.

Hint pills are now for problems only: tracking loss, placement errors, no surface after 4 seconds, looking not connected, and a closed error card. First-use placement also shows one coaching line. Removed the hold, paused, waiting and explanation hints. While Pip is looking, a small thought bubble with animated dots appears above it. When Pip is out of view, an edge arrow points toward it instead of text. Tell me more appears while a reaction or explanation is up and for 6 seconds after, then hides; the "Last look" state is gone and a pending explanation shows dots inside the button.

Onboarding is one welcome card (who Pip is, the camera disclosure, Find a spot, Skip). Placement is coached by the hint line and a mint ring lying on the detected surface (PipSurfaceRing, built at runtime from the existing marker material); the flat screen corners hide once a surface is found. After placement Pip says the looking instruction itself, and the first reaction gives Tell me more a one-time glow. Completion is saved as before (Pip.Walkthrough.v1); How it works replays.

Haptics trimmed to placement (medium) and capture (soft). Button taps, reactions and errors no longer vibrate, since ARCore guidance notes vibration can disturb tracking.

Fixes found during review: the hint pill re-wrapped its text while resizing (text now lays out at final width); the surface ring rendered dark from cancelled normals and showed outside placement; the edge arrow could draw before it had a position.

Verification: PipUIReview.Layouts passed 480 combinations including new explanation-sheet bounds. PipTypographyReview passed 252. PipUIReview.Interactions passed. Console clear of errors. Native export succeeded. Captures in Evidence/ARPass (staged Unity simulation with animation states settled for capture). Not yet done: xcodebuild, installation, on-device review. Previous files in Backups/ARPass-2026-10-25.

## October 25 — AR pass native Debug build
Confirmed Xcode 27.1 (27A9275), selected developer directory /Applications/Xcode.app/Contents/Developer, and today's 16:13 EDT export. xcodebuild completed with BUILD SUCCEEDED using the existing automatic signing configuration. PipHaptics.mm compiled successfully into UnityFramework; no haptics compile or link errors. Product: /tmp/Pip-iOS-VerifiedBuild/Build/Products/Debug-iphoneos/Pip.app. Log: /tmp/pip-arpass-build.log.

codesign -dv confirmed com.shlok.pip and the existing team. Deep strict signature verification passed with normal certificate trust-store access (valid on disk; satisfies its Designated Requirement). The initial sandboxed verification returned CSSMERR_TP_NOT_TRUSTED; repeating outside the sandbox passed without changing signing settings.

Non-blocking warnings include Unity deprecated APIs, libc++ selected-platform support warnings in generated compilation, the GameAssembly script's unspecified outputs, and the missing 1024x1024 App Store icon. No application code, team, bundle ID or provisioning changes made. Installation and physical device review are not done. The earlier phone HTTP 0 connectivity issue remains open and unverified.

## October 25 — complete replies without scrolling
Expanded reaction/error bubble maximum width from 248 to 300 logical points. Removed the height cap tied to Pip's projected head position: bubbles now grow to their measured content and shift within the safe area below the overflow control. Removed live and fade-out scroll views, scroll indicators and scroll offsets. Explanations retain the content-sized full-width bottom sheet. Normal replies retain the established typography; exceptional content exceeding the available safe-area height scales to fit rather than hiding lines. Drags over text remain owned by the UI and cannot place or tap Pip through it.

480 portrait layout checks and dialogue/gesture interaction checks passed; Unity Console had no errors. Staged visual checks of reaction and explanation show all text without scrolling (Evidence/NoScroll/screenshot-20261025-191313.png and screenshot-20261025-191336.png). These are simulated Unity previews. Native build and device installation result follows.
Native signed Debug build succeeded (/tmp/pip-no-scroll-build.log), deep strict codesign verification passed, and the updated app installed and launched successfully on the paired physical iPhone. Device visual review of the new text behavior remains pending. The earlier network issue is not verified by these checks.

## October 25 — dialogue-driven character acting
PipDialogue now stores an explicit expression and gives visible dialogue priority over ambient light mood. PipCharacter uses that acting mood for face and body/leaf animation, returning to environmental mood on dismissal/expiry. Authored mood lines preserve their intended expression; explanations/errors default to Curious. Ambient changes no longer interrupt an already visible line. Cloud responses now carry a validated Content/Curious/Sleepy/Angry expression selected for the actual spoken tone; cached reaction and explanation expressions survive Back to Pip and cached reopening. Legacy responses fall back safely without requiring a model field.

15 backend tests pass, including schema and malformed-expression fallback. Unity compilation is clear; focused expression priority, reveal, ambient reset, enum validation and cached reply checks pass. Angry dialogue visually verified with the corresponding brows/eyes/lips in the staged Unity preview (Evidence/DialogueExpressions/screenshot-20261025-191933.png).

User approved production publication. Vercel deployment dpl_7UCFc2aVjmGNyWr3QAHMbR33qx3w is READY and assigned to pip-vision.vercel.app. Only six backend source/config files uploaded; keys, phone images and Unity assets excluded. Authenticated production smoke test used a synthetic white-case drawing and returned a playful remark with expression Content. System Python lacked its local CA bundle; the same test with curl and normal certificate verification succeeded. This is Mac-to-backend validation, not a claim about the earlier phone HTTP 0 issue.

Native signed Debug build succeeded (/tmp/pip-expressions-build.log), deep strict signature verification passed, and the app installed on the paired iPhone (/tmp/pip-expressions-install.log). Live physical review of expression/dialogue matching remains to be done.

## October 25 — new-view automatic looking
Automatic looking now treats the initial camera view after placement/reposition as a silent baseline. A view must differ visually from that baseline and recently inspected views before starting the existing two-second steady hold. Camera pose changes alone cannot arm a request. Eight local view signatures are retained, preserving the placement baseline when older inspected views are evicted. Uniform exposure/brightness shifts are subtracted before comparisons. A coarse 32x24 luminance grid samples the central 80% of the camera frame with nine-pixel averaging per cell; this widens coverage beyond the old central patch and reduces fine texture noise. Only these tiny signatures are retained by the novelty gate; this adds no cloud calls. Pause/resume and temporary CPU-image unavailability preserve seen views; explicit error retry can deliberately retry the current view.

Focused checks passed: no initial floor inspection, pose-only and exposure-only changes ignored, new object/scene hold, pause resets dwell, same/recent view suppression, return to placement baseline, explicit retry and reposition reset. Existing dialogue/menu/drag/cancel interactions pass; Unity compilation has no errors. Coaching now asks to show something new.

Limit: visual novelty is an image-based heuristic, not semantic object tracking. A substantially different viewpoint of the same floor/object can still count as new, and small or visually similar objects may not trigger. Physical-room tuning remains pending. This is not a claim of perfect object identity or floor segmentation.
Signed Debug build succeeded (/tmp/pip-new-views-build.log); deep strict signature verification passed. Updated app installed successfully on the physical iPhone (/tmp/pip-new-views-install.log). Real-camera behavior remains to be tested by the user.

## October 25 — higher floating position
Raised PipCharacter hoverHeight and the serialized Pip prefab from 0.18 m to 0.50 m above the placement anchor (visual pivot), preserving bobbing and anchored horizontal behavior. Speech/edge-arrow projection already follows the visual transform. Staged preview now uses the configured hoverHeight rather than a hard-coded low position. Imported prefab verified at 0.5; Unity Console clear. Device appearance remains to be checked.
Signed Debug build succeeded (/tmp/pip-higher-hover-build.log), signature verification passed, and updated app installed on the paired iPhone (/tmp/pip-higher-hover-install.log). Not launched automatically; the user can reopen Pip to review the new floating height.

## October 25 — distinct rubbing/petting interaction
Added PipPetGesture: a contact must start on Pip, stay on his hit area for qualifying strokes, move at least 60 logical points with two direction reversals, and last at least 0.45 seconds. Tiny jitter, resting a finger, a tap, or a single swipe does not qualify. Horizontal and vertical rubbing both work. The gesture emits once per contact, so release never adds a poke. UI-started and world-started drags cannot turn into petting. Touch ownership follows the original finger ID; cancellation, focus loss and placement/tracking loss clear it.

Petting gives a Content face, a subtle lean and leaf wiggles while rubbing, plus one of five authored affectionate/sassy responses. A four-second response cooldown prevents spam, and pinned explanations/errors are preserved. Petting resets the poke burst and soothes annoyance; repeated taps retain the previous poke behavior. Recognition pauses during companion contact, and a qualifying pet cancels an outstanding camera response so it cannot overwrite the pet response. All petting responses are local; no backend changes or extra image uploads.

Focused checks passed for taps/jitter, swipe, minimum duration, horizontal/vertical rubbing, one event per contact, cancellation and off-character rejection. Character checks passed for pokes causing annoyance, pets soothing it, happy response, cooldown and pinned reading preservation. Existing menu/gesture/reposition/new-view regressions pass. Unity Console clear after compilation. Physical touch feel and animation timing remain pending device review.
Signed Debug build succeeded (/tmp/pip-petting-build.log), deep strict signature verification passed, and the updated app installed on the paired iPhone (/tmp/pip-petting-install.log). Not automatically launched. Reopen Pip to test physical rubbing versus taps.

## October 25 — slightly smaller companion
Reduced Pip's visual scale to 0.85 (15% smaller) via an explicit modelScale setting in PipCharacter and the serialized prefab. Summon animation retains this final scale. Kept the visual pivot's 0.50 m hover height unchanged. Above-head speech/arrow projection scales its head offset to match; the collider remains on the scaled visual. Staged preview respects the same scale. Imported prefab verified at modelScale 0.85 and hoverHeight 0.5; Unity Console clear. Updated phone build prepared; installation pending reconnection.
Signed Debug build succeeded (/tmp/pip-smaller-build.log), and deep strict signature verification passed. Product: /tmp/Pip-iOS-VerifiedBuild/Build/Products/Debug-iphoneos/Pip.app. This size update has not been installed on the phone.

## October 25 — speech avoids Pip and stays in view
Added conservative screen-space bounds projected from the full character renderer bounds, including leaves, body and tail. Renderer references cache per character. Dialogue geometry now measures complete text at available wrapping widths and tries free bands above, beside or below those bounds with 24-point clearance. Reactions prefer above; explanations prefer the lower sheet area but relocate when that would cover Pip. Controls and notch-safe margins remain reserved. Thinking dots use the same free-space solver. Pointer tails draw only for an above-character bubble where they can point correctly. Fading ghost surfaces are suppressed if Pip moves beneath them; clearance includes entrance animation motion.

If Pip fills the usable viewport and no complete reply fits, the speech surface waits, its reading time is held, and a contextual step-back hint appears instead of covering Pip or clipping text. Text stays non-scrolling. This fallback is intentional: an arbitrarily close character and a full readable reply cannot both fit in every viewport.

1,890 geometry collision cases passed (1,827 fit; the remainder safely report no room), covering multiple phone safe areas, edge positions, bubble sizes and explanation surfaces. Existing interaction checks passed; Unity Console clear. Actual staged Unity visual checks: Evidence/SpeechAvoidance/screenshot-20261025-194946.png (reaction below a high-in-frame Pip) and screenshot-20261025-195014.png (explanation). Physical camera review remains pending.
Signed Debug build succeeded (/tmp/pip-speech-avoidance-build.log), deep strict signature verification passed, and the updated app installed on the physical iPhone (/tmp/pip-speech-avoidance-install.log).

## October 25 — hover reduced by two-thirds
At user request, reduced hoverHeight from 0.50 m to 0.1667 m (one-third of the previous height). Runtime default and serialized Pip prefab match. Model scale remains 0.85. Imported prefab verified at hover 0.1667 and scale 0.85; Unity Console clear. Other animation and adaptive bubble behavior unchanged.
Signed Debug build succeeded (/tmp/pip-lower-hover-build.log), signature verification passed, and the updated app installed on the physical iPhone (/tmp/pip-lower-hover-install.log).

## October 25 — separate introduced objects from room changes
Replaced broad frame-difference arming with two deliberate paths. An introduced-object candidate requires a limited changed region (10–45%), a largely matching background, and camera pose near a stored view (within 10 degrees/12 cm). A new-room/scene candidate requires broad visual change plus substantial travel (60 cm) or a large deliberate turn (35 degrees), followed by the existing two-second steady hold. A small pose change with broad image difference is rejected instead of being treated as a new room.

Coarse luminance signatures are registered over small translations before comparison. A median exposure offset handles global light shifts, and the best-matching background fraction scores registration independently of an introduced foreground object. Recently seen registered views remain suppressed. Placement baseline, explicit error retry and eight-view memory are retained. This logic is local and adds no image uploads or backend changes.

Focused synthetic regressions pass: translated textured floor with slight camera movement remains quiet; broad changes with small movement remain quiet; inserted foreground object triggers after hold; substantial move/turn into a different scene triggers after settling. Existing baseline, repeat, exposure, pause, retry and reposition checks pass. Unity Console clear. This remains a heuristic, not semantic object segmentation: very small/low-contrast objects may not trigger, and significant local background changes can resemble an introduced object. Physical-room validation remains pending.
Signed Debug build succeeded (/tmp/pip-motion-gate-build.log), deep strict signature verification passed, and the updated app installed on the physical iPhone (/tmp/pip-motion-gate-install.log). Physical camera behavior still needs verification.

## October 25 — deliberate shake-to-scan fallback
Added local accelerometer shake recognition: three alternating high-pass acceleration peaks within a short window queue one scan, while single bumps, cooldown activity and disabled contexts are ignored. After the shake, capture waits for 0.7 seconds of quiet motion with stable camera pose and normal AR tracking. Pending gestures time out after 8 seconds; repeated shakes have an 8-second cooldown and existing request spacing still applies. Shake bypasses visual novelty for the current view; the captured view is remembered so automatic looking cannot immediately repeat it.

Enabled only after placement, when looking is enabled (or an error can be retried), backend config is ready, no request is busy, no menu/guide/petting interaction owns input, and no explanation is pinned. A deliberate shake can retry a visible error. Manual pause remains respected. Temporary hold-still feedback appears while settling; first-use coaching mentions the fallback. Component disable and activity cancellation clear pending state. No backend changes or new upload destination.

Synthetic checks passed for single bump rejection, three alternating peaks, tracking and settle requirements, movement resetting settling, one capture, cooldown, disabled state, pending timeout and automatic duplicate suppression. Unity Console clear. Actual accelerometer sensitivity and handheld camera flow still need physical-device testing.
Signed Debug build succeeded (/tmp/pip-shake-build.log), deep strict signature verification passed, and the updated app installed on the physical iPhone (/tmp/pip-shake-install.log). Physical shake sensitivity remains to be reviewed.

## October 25 — capture moments and local Memories
Added a camera button beside the overflow menu and a Memories entry on the Live view. Capture saves the actual rendered viewport with Pip and visible speech, hiding app controls, reticle overlays and feedback for the captured frame. Full-resolution JPEG, a downsampled thumbnail and timestamp/subject/dialogue metadata are stored in the app's private persistent PipMemories directory. No Photos-library permission, backend call, cloud sync or API key is involved in saving memories.

The charcoal Memories tab provides newest-first thumbnails (four per page), a larger uncropped photo viewer with timestamp and dialogue caption, and a Live tab to return to AR. Automatic looking/shake/petting/world placement is paused or blocked while browsing; tracking continues behind the gallery. Only visible-page thumbnails and one full image are loaded, and textures are released when paging/leaving. Incomplete/corrupt records are skipped; file IDs are validated and records committed after image files. No deletion/export/sync UI in this first version.

Storage regressions pass for persistence/reload, order, Unicode captions, corrupt/incomplete records and path validation. An actual Unity end-of-frame capture was saved and visually checked: Pip, scene and dialogue remain, controls are absent. Editor background throttling stalled WaitForEndOfFrame until a Game View frame was stepped; capture then completed successfully. Isolated preview storage was used, not phone memories. Gallery grid/viewer and pause/resume verified visually; existing menu/gesture/placement interactions pass. Unity Console clear. Evidence/Memories/screenshot-20261025-200909.png (Live controls), screenshot-20261025-201144.png (grid), screenshot-20261025-201210.png (viewer) are staged Unity previews, not phone captures. Physical capture and iOS gallery persistence still require device review.
Signed Debug build succeeded (/tmp/pip-memories-build.log), deep strict signature verification passed, and the updated app installed on the physical iPhone (/tmp/pip-memories-install.log).

## October 25 — bottom camera/pause pair and album
Moved camera capture and Pause into a centered 108-point group (48-point targets, 12-point gap). Album is an icon button at the bottom-left safe-area inset on the same row. Removed the previous top-bar Memories/camera controls. During placement, camera/album move one row above Summon to avoid collision; after placement the requested centered pair appears. Pause now remains available while reading an explanation. Speech and explanation geometry reserve the bottom control row and feedback space, so the sheet cannot cover capture or album. Full-width Tell me more remains below the controls.

480 portrait layout checks pass with new pair-centering and button-separation assertions; 1,890 speech collision cases pass, including all bottom buttons. Unity Console clear. Actual staged Unity preview: Evidence/MemoriesBottomControls/screenshot-20261025-201758.png. Physical layout review remains pending.
Signed Debug build succeeded (/tmp/pip-bottom-controls-build.log), deep strict signature verification passed, and the updated app installed on the physical iPhone (/tmp/pip-bottom-controls-install.log).

## October 25 — memory overlay with Pip and his captured line
New memory captures bake a charcoal overlay into the image: a frozen portrait of Pip on the left and the exact dialogue on the right. The first frame captures the clean AR scene; a temporary isolated camera renders the current Pip model/expression with transparent background, restoring all original layers afterward. The second frame composites the frozen scene, portrait and saved words, then saves the resulting image and thumbnail locally. Live controls and duplicate world speech are omitted. LastSpokenText preserves the latest actual utterance if its bubble has just closed; a quiet moment has a non-quote label rather than invented dialogue.

New records carry withPipOverlay, so the viewer does not repeat a caption below an image that already contains it. New thumbnails show the entire composition. Existing saved images remain unchanged and compatible. Interrupted/disabled captures clear temporary textures and frozen state. No backend changes or uploads.

An actual end-of-frame overlay capture was saved and visually checked in Unity: Evidence/Memories/Pip-Quote-Overlay.jpg (staged simulated scene, not a phone photo). Portrait and original quote persisted after current dialogue was changed; record reload retained the overlay flag and exact original words. Storage regressions pass; Unity Console clear. Physical iOS portrait capture/compositing remains to be reviewed.

## October 25 — save memories to Photos and toggle their overlay
New captures now retain both the plain captured AR scene and the portrait/quote composition, each with a thumbnail. The memory viewer adds a per-memory Pip overlay switch; its choice persists across reloads and controls the displayed image, gallery thumbnail and exported file. The switch removes the portrait/quote panel, retaining the captured AR scene. Existing one-version memories remain viewable and exportable; their already-flattened images are not altered.

Added Save to Photos through a native iOS Photos bridge (PipPhotos.mm / PipPhotos.cs). It requests PHAccessLevelAddOnly after a user tap, saves the currently selected version, reports success/denial/failure via a preserved Unity callback, and blocks duplicate requests while saving. Export includes NSPhotoLibraryAddUsageDescription and links Photos.framework to UnityFramework. No read access to the user's photo library, backend changes or image uploads. Native APIs verified in installed Xcode SDK headers.

MemoryVariants checks pass for clean/overlay file and thumbnail routing, persistent choice, original quote retention and legacy compatibility; existing storage tests pass. Actual capture saved both variants. Viewer visually verified on and off in staged Unity previews: Evidence/MemoryExport/screenshot-20261025-210234.png and screenshot-20261025-210256.png. Unity Console clear. Editor disables the Photos button because native photo saving is device-only. iPhone permission prompt and successful Photos-library write remain unverified until the user taps Save to Photos on device.
Signed Debug build succeeded (/tmp/pip-photo-export-build.log), including native PipPhotos.mm compilation/linking. Signature verification passed and the final app contains the add-only Photos usage description. Updated app installed on the paired iPhone (/tmp/pip-photo-export-install.log). No Photos-library write was triggered by the agent; first-save permission and native result still require user testing.

## 2026-10-25 — Memory photo styles
- Added Clean, Pip’s take, and Keepsake selection in the memory viewer. The selected image and thumbnail route to the same persisted style; Save to Photos uses that displayed version.
- New captures save clean, compact portrait/quote, and mint-framed keepsake JPEGs plus thumbnails. Keepsake includes a small signature and capture date. Quote and portrait remain frozen at capture time; no new network uploads.
- Existing two-version memories retain Clean/Pip’s take; legacy single-version photos remain viewable and exportable.
- Unity compiled without Console errors. MemoryVariants (including three-style persistence), MemoriesStorage, and Layouts checks passed. Actual three-frame staged capture completed with all variants in an isolated temporary store. Visually reviewed the Keepsake image; final small caption alignment refinement compiled afterward.
- Staged evidence: Evidence/MemoryStyles/{Clean,Pips-take,Keepsake}.jpg (Unity preview, not phone photos).
- No Xcode build, install, or device review for this update. Photos permission/save behavior and prior phone HTTP 0 issue remain unverified.

## October 25 — cohesion pass (Memories joins the Live system)
Goal: make Memories, capture and the new controls read as one app with Live.
- Memories now mirrors Live. The top-right circle sits where the menu button is and closes back to Live (the Live/Memories tab bar is gone). The viewer has a back circle on the left, the date centered, and close on the right.
- Gallery is a 3-by-2 grid of uncropped portrait photos with rounded corners and the shared soft shadow. Paging uses chevron circles instead of text pills.
- Photos in the viewer hug their own aspect ratio (no letterbox panel). The Clean / Pip’s take / Keepsake selector is a pill group whose accent pill slides between choices, matching the primary button style.
- Shared tokens (accent, well, hairline, backdrop) replace the separate greens and greys used by Memories.
- Capture success is shown, not announced: a brief shutter flash, then the saved photo shrinks into the album button, which pulses once. Errors still use the hint pill. Capture is ignored while a save is animating.
- New album icon (photo with a print behind it) in the same stroke weight as the camera. New back/forward chevrons.
- Memory images: “Pip” name and “with Pip” signature use the same label role as speech. The Keepsake date sits flush right on the signature line and the quote card is inset from the mint frame.
- Copy: shake hint drops its dash (“Hold still. I’ll take a look.”). The first-placement hint names the right button when moving Pip.
- Placement plane outlines use a mint material copy to match the surface ring.
Verification: Layouts 480, SpeechAvoidance 1,890, PetGestures, PetResponses, ShakeScanning, MotionSuppression, NewViews, Expressions, Interactions, MemoryVariants, MemoriesStorage and typography 252 all pass. Console clear of errors. Staged Unity captures in Evidence/CohesionPass (not phone screenshots). Pip → Export Xcode project re-run after this pass. Not yet done: xcodebuild, install, phone review of the flight animation, Photos saving, plane outline color on device. The phone HTTP 0 issue is still open. Previous files in Backups/CohesionPass-2026-10-25.

## 2026-10-25 — Cohesion build for phone screenshot session
- Exported cohesion project built successfully: /tmp/pip-cohesion-build.log.
- Signed app passed codesign --verify --deep --strict and installed on paired iPhone (2): /tmp/pip-cohesion-install.log.
- Screenshot session starting. Installation does not verify Photos saving, recognition connectivity, or all device UI states.

## 2026-10-25 — Speech available-space fix during phone screenshots
- User reported Step back despite visible empty space. Replaced projection of one combined world-axis character box with projection of each renderer's local bounds, reducing invented empty corners around close-up/rotated Pip.
- Reply fitting now tries the full available viewport width as well as intermediate and narrower wrapping widths before falling back. Collision avoidance, full-text display and normal typography remain in place.
- Unity Console had no errors; Layouts (480) and SpeechAvoidance (1,890) passed. Visually checked a close-up staged reply fitting above Pip: Evidence/BubbleSpaceFix/screenshot-20261025-220528.png (Unity preview).
- Export succeeded, Xcode BUILD SUCCEEDED (/tmp/pip-bubble-space-build.log), strict signing verified, installed (/tmp/pip-bubble-space-install.log).
- Exact reported handheld case awaits user retest. This does not verify recognition connectivity.

## 2026-10-26 — Gemini question handling and response scope
- Updated Backend/Cloud/personality.py, shared by local and cloud providers, with explicit rules for relevant answers, uncertainty, supported Tell me more follow-ups, no invented chat history, and treating image text/previous labels as data rather than instructions.
- Removed encouragement to append conversational questions. Added no filler/offers/unrelated advice, minimal clarification, calm treatment of distressing scenes, and no unsupported exact identities or image-based safety judgments. Preserved playful character, expression metadata and existing response limits.
- Existing 15 backend tests passed (mocked provider); no live Gemini behavioral evaluation or deployment performed. Production prompt remains unchanged until published.

## 2026-10-26 — Published question-handling prompt
- User explicitly requested publish. Uploaded only the six allowlisted backend source/config files to the existing pip-vision production project using the Vercel deployment skill and connector.
- Deployment dpl_FHVt9jzwm27RG4KMtPnmfow6umUb reached READY and is assigned pip-vision.vercel.app. No credentials, images, tests or Unity project files uploaded.
- Prior 15 mocked backend tests passed. Production health check performed separately; live Gemini behavioral quality and phone connectivity are not established by deployment readiness.

## 2026-10-26 — Tolerate hand tremor during automatic inspection
- User reported new objects were ignored with shaky hands. PipLookGate previously reset a two-second dwell above 2 degrees or 2.5 cm from its starting pose. Expanded that anchored envelope to 8 degrees / 8 cm, retaining a fixed origin so sustained pans do not accumulate unlimited drift.
- Registered image stability now checks background residual and a wider mean-change tolerance. One transient non-novel sample receives 0.5 s grace; capture still requires a currently novel sample, and sustained disappearance cancels the candidate.
- Added ShakyHands regression: oscillating pose and registered image shifts trigger a new object once, continuous pans remain rejected, short noise survives, sustained removal resets. ShakyHands, NewViews, MotionSuppression, ShakeScanning all passed; Console had no errors.
- Unity export and Xcode build succeeded (/tmp/pip-tremor-build.log); strict signing verified. App: /tmp/Pip-iOS-VerifiedBuild/Build/Products/Debug-iphoneos/Pip.app.
- Device list reports iPhone (2) unavailable; no install or handheld validation performed. Automatic detection remains an image-change heuristic, not semantic tracking.

## 2026-10-26 — Offer relocation after panning away; float between anchors
- After Pip is offscreen for two seconds, Live offers Bring Pip here in the bottom action position. Existing edge arrow remains. Prompt is suppressed during visible dialogue, requests, menus, walkthrough, Memories and captures. It invites user-controlled surface placement rather than automatically following camera motion.
- Reposition creates the destination anchor first, retains the original character/mood, detaches its root and animates an eased world-space arc. At arrival it reparents to the new anchor and removes the old one. Failed anchor creation leaves original placement intact. Flight pauses during tracking loss; root animation has a per-frame progress cap to avoid jumping on a long frame. No obstacle-aware path planning.
- RepositionFlight geometry, Layouts (480), SpeechAvoidance (1,890), ShakyHands regressions passed. Preview relocation preserved one named original character at the exact destination. Final frame-progress cap compiled with no errors. Offscreen prompt visually reviewed: Evidence/RepositionFlight/screenshot-20261026-170110.png (staged Unity image).
- Unity export and Xcode BUILD SUCCEEDED (/tmp/pip-float-reposition-build.log); strict signing passed. Signed app includes this feature plus the hand-tremor scan fix.
- iPhone (2) remains unavailable. No install or on-device anchor/flight test performed.

## 2026-10-26 — Face travel direction during relocation
- Pip turns toward the destination before departure, faces its horizontal travel direction along the arc, stays gravity-upright, then turns to the camera after landing. Idle visual yaw/roll and wandering are suppressed during travel and restored afterward.
- Added heading regression checks for four directions, gravity up, and vertical-only fallback. RepositionFlight passed; Unity Console clear of errors.
- Unity export succeeded; Xcode BUILD SUCCEEDED (/tmp/pip-flight-facing-build.log); strict signing passed. Latest app includes facing, relocation and shaky-hand detection changes. Not installed or reviewed on device.

## 2026-10-26 — Connected phone installation
- Latest signed app installed successfully on iPhone (2): /tmp/pip-flight-facing-install.log.
- Launch rejected with CoreDevice 10002 / FBS security: invalid signature, inadequate entitlements, or profile not explicitly trusted. Local strict signature verification passed; device trust needs user review. No successful launch/device behavior verification claimed.

## 2026-10-26 — App verification resolved launch
- After user reported app verification and reconnected USB, devicectl successfully launched com.shlok.pip. Prior trust/signature launch rejection no longer reproduced.
- Installed build includes tremor tolerance, Bring Pip here, relocation arc, and travel-facing orientation. Their handheld behavior still requires user testing.
- Wireless connectivity remains unresolved; do not attribute the launch recovery to a confirmed campus-network cause.

## 2026-10-26 — Investigated looking-glasses error on phone
- Production /health returned ready:true. Metadata-only phone diagnostics: 22:09:27Z request timed out at 35 seconds with HTTP 0; matching Vercel request returned 502 at 22:09:53Z. This establishes an upstream recognition failure for that attempt, with client timeout obscuring the server response; does not establish why the response was not received before timeout.
- Subsequent actual phone requests succeeded: 22:10:52Z -> HTTP 200 in 17.2 s, 22:11:11Z -> HTTP 200 in 4.5 s. Vercel logs corroborate both.
- Authenticated end-to-end synthetic drawing request from Mac returned HTTP 200 with remark/expression in 1.7 s. No phone image uploaded for this test; credentials not printed.
- Current client error text conflates non-429 backend errors and transport failures. Health readiness alone is not a Gemini availability test. No permanent credential/endpoint outage demonstrated; intermittent provider/transport failure remains possible. No code or deployment changed during diagnosis.
