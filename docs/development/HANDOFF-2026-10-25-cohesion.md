# Pip handoff, October 25 2026 (after the cohesion pass)

You are continuing Pip, an iPhone AR companion built in Unity. The latest change is a UI cohesion pass that makes Memories and capture match the Live screen. It is implemented and verified in the Unity Editor, and a fresh Xcode project was exported. **It has not been built with xcodebuild, installed, or reviewed on the phone.**

Your next task: build the exported project, then install and review on the phone when the user asks.

## Project and toolchain
- Root: `/Users/shlokbelgamwar/Documents/Playground/Pip`
- Unity 6000.5.9f1, AR Foundation and ARKit XR Plugin 6.3.5, Built-in pipeline, IMGUI overlay UI
- Device: iPhone 16 Pro Max, paired as `iPhone (2)`, iOS 27.2 beta
- Xcode 27.1 at `/Applications/Xcode.app/Contents/Developer`
- Bundle ID `com.shlok.pip`, automatic signing. Do not change team or provisioning without asking.
- Backend: `https://pip-vision.vercel.app`
- Docs: `README.md`, `VALIDATION.md` (newest at bottom), `TYPOGRAPHY.md`

## Rules to keep
- Never print, log, commit or upload credentials: `Backend/.env`, `Assets/StreamingAssets/pip-vision.json`. The client config holds an app bearer token, not the Gemini key.
- **Do not claim the phone HTTP 0 connectivity issue is fixed.** UI and build work do not establish that.
- Pip is inspired by Paimon: cute, sassy, playful, informative, an original fantastical creature. Do not frame the project as helping people notice their surroundings.
- Keep the warm charcoal, mint and off-white direction. The user rejected white walkthrough cards.
- No voice/chat, other platforms, paid services or backend migration without asking.
- Do not call it fully on-device recognition. Frames go to the backend.
- Do not label staged Unity images as phone screenshots.

## Latest change: cohesion pass
Files changed:
- `Assets/Pip/Runtime/PipInput.cs`: shared color tokens (`accent`, `well`, `hairline`, `backdrop`), new icons (9 album photo, 10 back chevron, 11 forward chevron), copy fixes, hooks `StepMemoryMotion` (in `Animate`) and `DrawSavedFlight` (after memory controls in `OnGUI`), `ClearFlight` in `OnDestroy`.
- `Assets/Pip/Runtime/PipMemoriesUI.cs`: rewritten gallery and viewer, style selector, capture flight animation, memory image styling.
- `Assets/Pip/Runtime/PipPlaneGuide.cs`: plane outlines use a mint copy of the line material.
- Backups of the previous versions: `Backups/CohesionPass-2026-10-25/`

What changed:
1. **Memories mirrors Live.** The top-right circle (same rect as the Live menu button, `layout.More`) closes back to Live. The Live/Memories tab bar is gone. The viewer has a back circle on the left, the date centered, close on the right.
2. **Gallery:** 3 by 2 grid of uncropped portrait photos (6 per page), rounded corners and the shared soft shadow. Paging uses chevron circles and an "X of Y" caption.
3. **Viewer:** photo frame fits the image aspect (no letterbox panel). Clean / Pip's take / Keepsake selector is a pill group with a sliding accent pill (`styleSlide`). Save to Photos is the primary pill.
4. **Capture feedback:** brief shutter flash, then the saved photo shrinks into the album button (0.7 s, `flyT`), then the album button pulses once (`albumPulse`). The "Saved to Memories" text was removed. Errors still use the hint pill. Capture is ignored while the flight runs.
5. **Memory images:** "Pip" and "with Pip" use the speech label role. Keepsake date is measured and flush right on the signature line. Keepsake quote card is inset from the mint frame. Changes apply to new captures only. Older memories are unchanged.
6. **Copy:** shake hint is now "Hold still. I'll take a look." The first-placement hint says "Tap Move Pip here when the ring looks right" when moving Pip.
7. **Placement:** plane outlines are mint to match the surface ring.

Verification done in the Editor:
- PipUIReview: Layouts (480), SpeechAvoidance (1,890), PetGestures, PetResponses, ShakeScanning, MotionSuppression, NewViews, Expressions, Interactions, MemoryVariants, MemoriesStorage all pass.
- PipTypographyReview: 252 pass.
- Console clear of errors.
- Staged captures (Unity previews, not phone): `Evidence/CohesionPass/` (Live, Saved-Flight, Memories-Grid, Viewer-Pips-take, Viewer-Clean, Keepsake-Capture). `Viewer-Keepsake.png` predates the final Keepsake date and inset fix, so use `Keepsake-Capture.jpg` instead.
- **Pip → Export Xcode project** re-run after the pass (about 21:47 local). The export contains the new code and still includes `PipPhotos.mm`, `PipHaptics.mm` and `NSPhotoLibraryAddUsageDescription`.
- Results are appended to `VALIDATION.md`.

Not done:
- xcodebuild, signing check, install, launch.
- On the phone: flight animation timing, album pulse, gallery and viewer layout with notch insets, Photos permission and saving for all three styles, mint plane outlines, and the Keepsake look on a real camera image.
- The HTTP 0 connectivity issue.

## Your task: build
From the project root:
```sh
xcodebuild -project Builds/iOS/Unity-iPhone.xcodeproj \
  -scheme Unity-iPhone -configuration Debug \
  -destination 'generic/platform=iOS' \
  -derivedDataPath /tmp/Pip-iOS-VerifiedBuild \
  -allowProvisioningUpdates build \
  > /tmp/pip-cohesion-build.log 2>&1
```
Expected app: `/tmp/Pip-iOS-VerifiedBuild/Build/Products/Debug-iphoneos/Pip.app`. Verify with `codesign -dv --verify --deep --strict`.

Known nonblocking warnings: Unity API deprecations, GameAssembly script output warnings, missing App Store icon. If signing fails, report the exact error and stop.

Install only when the user asks and the phone is connected and unlocked. Then review the items under "Not done", append results to `VALIDATION.md`, and say plainly what was and was not verified.

## Unity automation notes
- The Unity MCP bridge used this session is on port 6400. The helper `/tmp/pip_socket.py` and the skill at `/Users/shlokbelgamwar/.codex/skills/unity-mcp-skill/SKILL.md` still apply.
- Staging: enter Play Mode in PipPreview, call `Pip.Editor.PipUIPreview.Stage()`, pause, then `EditorApplication.Step()` across separate calls. `ScreenCapture.CaptureScreenshot` finishes on the next step.
- The Game view has a custom 780x1688 size (iOS group, index 36) for portrait captures.
- Interactions, Expressions and PetResponses need the staged preview in Play Mode. The rest run in Edit Mode.
- Editor test captures were saved to the Editor's own Memories store (`~/Library/Application Support/Shlok/Pip/PipMemories`), not the phone.
- Export through the Unity menu. Do not call BuildPipeline from inside an MCP player-loop callback.

## Presentation screenshots still pending
The user wants real phone screenshots for the deck: mint placement ring (one retake exists at `Evidence/Phone-Final-2026-10-25/01-placement-ring-retake-2.png`), thinking dots with the earbud case, reaction bubble with the case, explanation sheet, offscreen arrow, and optionally the welcome card. Capture with:
```sh
xcrun devicectl device capture screenshot --device 'iPhone (2)' --destination /absolute/path.png
```
The user aims the phone. Capture can be remote.
