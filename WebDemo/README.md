# Meet Pip — isolated browser companion

A separate Vite / Three.js demo of Pip. This directory is independent of Unity's Assets, ProjectSettings, exported Xcode project, and Backend/Cloud deployment.

## Experience
- Original Pip geometry recreated from the Unity model proportions, with cream body, leaves, teal scarf, brows, lips, expressions, bobbing and twirling.
- Tap versus deliberate back-and-forth petting. Enter/Space provides keyboard pet interaction.
- Optional rear camera, explicit one-frame scans, Tell me more reuses that scan. No continuous uploads and no automatic retries.
- Download a camera/character/quote moment as JPEG. No server-side memory gallery.
- Responsive warm charcoal UI; reduced-motion preference supported.
- This is a screen-space browser companion, **not ARKit room tracking or anchored placement**.

## Run
`npm install`, then `npm run dev` (http://127.0.0.1:5178). `npm test`; `npm run build`.
The dev server reads the existing Unity app bearer token into server memory only, or accepts PIP_CLIENT_TOKEN in the server environment. Never use a VITE_ variable for a token or copy the Unity config into public assets.

## Vercel
Separate project: meet-pip / YOUR_VERCEL_PROJECT_ID. Server-only PIP_CLIENT_TOKEN is configured as a sensitive environment variable. api/inspect.js proxies to the existing pip-vision production API; the Gemini key is never copied. Existing backend source, settings and deployment remain unchanged.

The demo therefore shares the existing provider quota with the phone. A per-instance, per-IP six-request/minute limit supplements the existing backend limit; this is not a durable global quota or production-grade anti-abuse control. For broad public use, provision separate credentials/quotas and durable access controls first. No paid services configured.

Only index.html, src/, api/, package.json, package-lock.json, vite.config.js and vercel.json are uploaded. Do not deploy the Pip parent directory, Unity config, .env, node_modules, test artifacts or phone images.

Camera frames are processed by the existing Vercel/Gemini backend after explicit user action; Google's free-tier processing may use submitted content to improve products. Closing/hiding the page stops camera tracks. No microphone requested.
