# Pip — a playful AR companion

An original little companion inspired by the charm of fantastical game sidekicks: cute, sassy, curious, and informative.

**[Try Meet Pip in your browser](https://meet-pip.vercel.app/)**

## iPhone app & Xcode build — review downloads

**Completed Unity-to-Xcode Debug build for iPhone · October 26, 2026**

| Download | What it contains |
| --- | --- |
| [Built iPhone app (.app ZIP)](https://github.com/shlokbelgamwar/Pip/releases/download/v0.1.0-build-review/Pip-iPhone-Built-App-Review.zip) | Compiled arm64 Pip.app with Unity frameworks, assets, and a build-result receipt. |
| [Xcode project (ZIP)](https://github.com/shlokbelgamwar/Pip/releases/download/v0.1.0-build-review/Pip-Xcode-Project-Review.zip) | Generated Xcode export. Unzip and open `iOS/Unity-iPhone.xcodeproj` to inspect the native project. |

[View the build release and notes](https://github.com/shlokbelgamwar/Pip/releases/tag/v0.1.0-build-review).

These are public review copies with backend credentials and personal provisioning data removed. The app is ad-hoc signed for inspection and cannot be installed directly on an iPhone. Building the Xcode project for a device requires your own signing team and backend configuration.

## What is here

- **Assets / Packages / ProjectSettings:** Unity 6 iPhone AR prototype, using AR Foundation and Apple ARKit 6.3.5. Surface placement, anchored floating companion, expressions, petting, object reactions, information, and local photo memories.
- **WebDemo:** separate Vite / Three.js browser companion with camera-based reactions, petting, Tell me more, and downloadable moments. It uses screen-space rendering rather than ARKit anchors.
- **Evidence:** staged Unity previews, character/UI iterations, and the presentation QR. These are not phone captures unless explicitly labeled.
- **docs/development:** original project notes, implementation history, typography notes, and handoffs. Historical entries can describe superseded behavior; read newest validation entries for current status.

## Try the hosted web experience

No developer tools or installation are needed.

1. Open **[Meet Pip](https://meet-pip.vercel.app/)** on your phone or computer and dismiss the introduction.
2. Tap or click Pip to say hello. Rub back and forth over Pip to pet it.
3. Select **Show my world**, read the camera notice, and allow camera access.
4. Hold an object in view and select **Take a look**. One selected frame is sent to the backend and Google Gemini for a playful reaction.
5. Select **Tell me more** for information and an analogy about the same captured frame.
6. Select **Keep a moment** to download a picture with Pip and its remark. Use the camera control to stop the camera.

The browser version renders Pip on screen. Floor detection, room anchoring, and native iPhone features are demonstrated by the Unity app.

## Inspect the compiled iPhone app

1. Download [Pip-iPhone-Built-App-Review.zip](https://github.com/shlokbelgamwar/Pip/releases/download/v0.1.0-build-review/Pip-iPhone-Built-App-Review.zip) and unzip it.
2. Read the included `README.md` and `BUILD-RESULT.txt`.
3. On a Mac, right-click `Pip.app` in Finder and choose **Show Package Contents** to inspect its executable, Unity frameworks, and packaged assets.

This is an **iPhone app**, so double-clicking it on a Mac does not launch the experience. The public copy has blank backend credentials, no device provisioning profile, and an ad-hoc signature. It is a build artifact for review, not a directly installable iPhone app. Use the web link to try Pip immediately, or follow the Xcode instructions to build your own device version.

## Open and build the Xcode project

**Requires:** a Mac with Xcode and an iOS SDK compatible with the connected iPhone. The original build used Xcode 27.1. Simulator execution does not demonstrate the ARKit room experience.

1. Download [Pip-Xcode-Project-Review.zip](https://github.com/shlokbelgamwar/Pip/releases/download/v0.1.0-build-review/Pip-Xcode-Project-Review.zip) and unzip it.
2. Open `iOS/Unity-iPhone.xcodeproj` in Xcode.
3. Choose the **Unity-iPhone** scheme. You can inspect the targets, native code, frameworks, assets, and generated IL2CPP output without installing the app.
4. To build for a phone, connect and unlock the iPhone, trust the Mac, and enable Developer Mode if requested.
5. In the **Unity-iPhone** target → **Signing & Capabilities**, enable automatic signing and select your own development team. Use a bundle identifier available to your account. Personal signing details were cleared from this export.
6. For object recognition, edit `iOS/Data/Raw/pip-vision.json` with your own backend's HTTPS `/inspect` endpoint and matching app bearer token. See **Set up recognition** below. Leave the Gemini key on the backend.
7. Select your physical iPhone as the run destination. Choose **Product → Build** to compile, or **Product → Run** to build, install, and launch. Resolve any account agreements or device provisioning prompts through Xcode.
8. Allow camera access on the phone, scan a textured floor or table slowly, and tap when the placement ring appears.

A free Personal Team can build for eligible personal devices, with provisioning restrictions and expiry. The public download contains no working hosted-API token.

## Open and build the Unity source

**Requires:** Unity **6000.5.9f1** with **iOS Build Support**, plus Xcode for device builds. The project uses AR Foundation and Apple ARKit **6.3.5**.

1. Clone or download this repository:

   ```sh
   git clone https://github.com/shlokbelgamwar/Pip.git
   cd Pip
   ```

2. In Unity Hub, add the cloned repository folder as a project and open it with the matching Editor. Allow packages to restore.
3. Open `Assets/Pip/Generated/PipPreview.unity` for the Editor preview, or `Assets/Pip/Generated/PipAR.unity` for the native AR scene. Editor preview is not a substitute for device tracking.
4. Copy `Assets/StreamingAssets/pip-vision.example.json` to `Assets/StreamingAssets/pip-vision.json`. Enter your own backend endpoint and app bearer token after setting up recognition.
5. In the iOS build profile and Player Settings, configure your own bundle identifier, camera usage description, and Apple signing team. Confirm ARKit is enabled in XR Plug-in Management for iOS.
6. Use **Pip → Export Xcode project**, then open the exported `Unity-iPhone.xcodeproj` and follow the Xcode signing and device steps above.

Keep the real `pip-vision.json` private. The included example is a placeholder and does not grant hosted API access.

## Set up recognition

Backend implementation and earlier source backups are not included in the current public snapshot. To enable recognition in your own build, supply your own compatible HTTPS `/inspect` service and configure its endpoint and app bearer token privately. Keep provider API keys on your server.

## Run or deploy your own web demo

**Requires:** a Node.js version supported by Vite 7 and supporting `--use-system-ca` (for example, Node.js 24), plus npm.

1. Set up your own cloud backend first if you want camera recognition.
2. In `WebDemo/api/inspect.js`, replace the existing hosted upstream URL with your own backend's HTTPS `/inspect` URL. A cloned copy does not include permission or credentials for the original hosted API.
3. For local development, the server reads the token from the private Unity `Assets/StreamingAssets/pip-vision.json` if present, or from the server's `PIP_CLIENT_TOKEN` environment variable. Configure it privately before starting.
4. Start the demo:

   ```sh
   cd WebDemo
   npm install
   npm run dev
   ```

5. Open `http://127.0.0.1:5178/`. Camera access works on localhost; a phone accessing a remotely hosted site needs HTTPS.
6. Use `npm run build` to generate the frontend production files. For Vercel deployment, create a separate project with **`WebDemo` as its root**, use Vite, and configure the matching `PIP_CLIENT_TOKEN` as a server-only environment variable. Deploy the source including `api/inspect.js`; the static frontend alone does not provide the recognition proxy.

Never put credentials in a `VITE_` variable or public asset. Without backend configuration, the character interactions can be explored, but recognition will be unavailable. See [WebDemo/README.md](WebDemo/README.md) for privacy, camera, and hosting details.

## Troubleshooting

| What happens | What to check |
| --- | --- |
| Camera does not open | Camera permission in the browser or iPhone Settings; use HTTPS or localhost for the web demo. |
| No floor placement ring in the native app | Use a supported physical iPhone, brighter lighting, a textured surface, and slow camera movement while tracking initializes. |
| “Looking glasses” or recognition error | Backend configuration, matching token, image-processing enablement, provider quota, and network reachability. A successful `/health` response alone does not prove Gemini is reachable. Intermittent request timeouts remain a known prototype issue. |
| Xcode cannot sign or install | Your own team, bundle identifier, device trust, Developer Mode, compatible Xcode/iOS versions, and valid provisioning. |
| Downloaded `Pip.app` does not run | It is an iPhone review artifact with credentials and device provisioning removed. Use the web demo or build through Xcode with your own configuration. |

## Optional checks for developers

These commands exercise source checks; they do not replace testing AR tracking, touch, camera behavior, or haptics on an iPhone.

```sh
# From the repository root
cd WebDemo
npm test
```

## Sharing and limitations

The web demo is available by link. Native iOS sharing requires appropriate signing; free Personal Team provisioning has device and expiry restrictions. Live recognition depends on provider availability and quota. The web demo shares the hosted API's quota. Room placement and camera behavior still require physical-device validation after changes.

This repository is a public source snapshot with `Backend` and `Backups` omitted from the current branch; earlier commits can still contain them. Publishing it does not alter the existing Unity checkout or trigger changes to the deployed services. Generated builds are provided separately as the sanitized review downloads linked above. Dependencies, API credentials, personal signing assets, logs, user settings, and personal phone screenshots are excluded from the source snapshot.
