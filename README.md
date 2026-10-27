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
- **Backend:** local development server and Vercel/Gemini recognition proxy; shared personality and response rules.
- **WebDemo:** separate Vite / Three.js browser companion with camera-based reactions, petting, Tell me more, and downloadable moments. It uses screen-space rendering rather than ARKit anchors.
- **Evidence:** staged Unity previews, character/UI iterations, and the presentation QR. These are not phone captures unless explicitly labeled.
- **Backups:** earlier source iterations.
- **docs/development:** original project notes, implementation history, typography notes, and handoffs. Historical entries can describe superseded behavior; read newest validation entries for current status.

## Run the Unity prototype

Open this folder with Unity **6000.5.9f1** and iOS Build Support. Allow package restore. Open `Assets/Pip/Generated/PipAR.unity` for AR or `PipPreview.unity` for the Editor preview. Configure your own Apple signing team and bundle identifier. Use **Pip → Export Xcode project**, then build with Xcode for a supported physical iPhone.

Copy `Assets/StreamingAssets/pip-vision.example.json` to `pip-vision.json` and supply your own backend endpoint and app bearer token. The real file is ignored. The example does not grant access to the hosted API.

## Run the web demo

```sh
cd WebDemo
npm install
npm test
npm run dev
```

Use `npm run build` for production. Configure `PIP_CLIENT_TOKEN` only on the server. Never put credentials in a VITE_ variable or public asset. See [WebDemo/README.md](WebDemo/README.md) for setup and limitations.

## Recognition backend

See [Backend/Cloud/README.md](Backend/Cloud/README.md). Configure your own Gemini key and app token on the server, then enable inspection. No credentials are included here. Camera inspection sends a frame to the backend and Google Gemini; this is not fully on-device recognition. Users explicitly opt into camera uploads in the browser demo.

## Sharing and limitations

The web demo is available by link. Native iOS sharing requires appropriate signing; free Personal Team provisioning has device and expiry restrictions. Live recognition depends on provider availability and quota. The web demo shares the hosted API's quota. Room placement and camera behavior still require physical-device validation after changes.

This repository is a public source snapshot. Publishing it does not alter the existing Unity checkout or trigger changes to the deployed services. Generated builds are provided separately as the sanitized review downloads linked above. Dependencies, API credentials, personal signing assets, logs, user settings, and personal phone screenshots are excluded from the source snapshot.
