# Pip vision development backend

The Gemini key stays in Backend/.env. The iPhone contains only a local client token.
Uses Python 3 standard library, with the Mac system certificate bundle for verified TLS.

After authorizing Google Gemini image processing, start from this directory:

```sh
PIP_BIND=YOUR_MAC_WIFI_IP python3 server.py
```

The Mac and phone must share Wi-Fi. Keep this terminal running. Allow the iPhone's local network prompt.
The development app uses the Mac .local hostname on port 8765. This LAN link uses HTTP and is intended for trusted home Wi-Fi only; a deployed service needs HTTPS.
Tap Pip, look!, center the object, then Inspect to send one reduced camera frame to Google. No continuous capture and no server-side image storage.
The free Gemini tier may use submitted content to improve products. Current model: gemini-3.5-flash-lite (older 2.5 Flash-Lite rejected this new account).
Proxy accepts JPEG only, caps request size, requires the client token, has a five-second cooldown and a 60-request process-session cap. No automatic retries or paid fallback.

Do not commit .env or Assets/StreamingAssets/pip-vision.json. Rotate the local client token if distributing the development app beyond this phone.
