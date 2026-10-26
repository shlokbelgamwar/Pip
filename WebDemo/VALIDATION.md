# Meet Pip validation — October 26, 2026

## Isolation
- All new application source is under Pip/WebDemo. No Unity Assets, ProjectSettings, Xcode exports, or Backend/Cloud source changed for the web demo.
- Separate Vercel project meet-pip: YOUR_VERCEL_PROJECT_ID.
- Original pip-vision project still uses dpl_FHVt9jzwm27RG4KMtPnmfow6umUb; project lookup confirmed unchanged production deployment.
- Server-only app bearer token stored as a sensitive environment variable in the new project; Gemini key not copied. Compiled client checked for the actual bearer token and none found.
- Upload allowlist: index.html, package.json, package-lock.json, vite.config.js, vercel.json, src/, api/. No Unity config, .env, photos, tests, docs, or node_modules uploaded.

## Checks
- npm test: eight tests passed (payload validation, origin restriction, secret isolation, upstream failure handling; tap/jitter/hold, swipe rejection, horizontal/vertical scratch, fast zigzag rejection).
- npm run build: passed, client app 12.56 kB plus Three.js chunk 488.64 kB before compression; dependency audit reported zero vulnerabilities at install.
- Actual in-app browser review: default desktop viewport and 390x844 mobile viewport; introduction, tap reply, keyboard petting, camera consent/cancel, character/reply layout, JPEG download. Browser error log empty. Mobile override reset afterward.
- Actual JPEG download succeeded: a-moment-with-pip-1793055894568.jpg (staged web playground, no user camera image).
- Local full recognition route: HTTP 200, synthetic drawing, 3.1 s.
- Production public homepage: HTTP 200 without login; reviewed in browser.
- Production react and learn API calls with synthetic drawing: both HTTP 200, 1.5 s and 1.4 s respectively. No phone/camera images sent during agent testing.
- Final Vercel deployment dpl_4SQKujcriuJCB5w9PD2tWfPbq24E reached READY; aliases include https://meet-pip.vercel.app and https://meet-pip-shlok-7e0b.vercel.app.

## Limits
- Physical iPhone Safari camera permission, camera rendering, finger rubbing, and download/save behavior need user device testing. No actual camera permission accepted during agent QA.
- This is screen-space Three.js, not anchored ARKit/WebXR. Camera scans are explicit button actions; no continuous automatic recognition.
- Recognition uses existing pip-vision, so provider quota and intermittent provider availability are shared with the native app. Local per-instance rate limits are not durable global abuse prevention.
- Moments are downloaded files; no cloud gallery or persistent browser memories added.
