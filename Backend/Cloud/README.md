# Pip cloud backend

Production: https://pip-vision.vercel.app
Routes: GET /health, POST /inspect (Bearer token).

Hosted in the user's Vercel project pip-vision, Flask / Python runtime, iad1, 35-second timeout.
Production environment: GEMINI_API_KEY and PIP_CLIENT_TOKEN encrypted; GEMINI_MODEL=gemini-3.5-flash-lite; PIP_VISION_ENABLED=true.
The project source contains no provider key or client token. Camera images are processed in memory, not stored by this application. Images transit Vercel and Google Gemini; free-tier Google data terms still apply.

Free prototype safeguards: authentication, 1.5 MB HTTP limit, JPEG validation, client eight-second automatic-reaction cooldown and per-instance 12 requests/minute burst protection. Instance counters are not a durable global or daily cap. Google's project quotas provide the upstream free-tier limit. No automatic retries or paid model fallback.

Vercel's API returned 404 for firewall config operations. Dashboard observation rule was created, but selecting enforced 429 rate limiting opened a Pro upgrade prompt. No upgrade was accepted. The remaining firewall rule is log-only; do not describe it as enforced protection.
For a public/multi-user release, provision an atomic shared limiter (such as Redis), rotate/per-device credentials and add proper user authentication.

Tests: .venv/bin/python -m unittest -v test_app.py test_vision.py
Deploy only app.py, vision.py, personality.py, requirements.txt, pyproject.toml and vercel.json from this directory, with the Flask framework preset. Never include ../.env or Unity files.

## Character context
`personality.py` defines Pip’s system instruction and four mood contexts. Both local and cloud recognition use it. Replies cover objects, scenes, unfamiliar shapes and unclear frames. `certain=false` preserves the generated honest remark rather than replacing it with a fixed sentence. This does not add persistent conversation memory or guarantee exact recognition.

## Response modes
POST /inspect accepts mode `react` (default) for a playful remark up to 140 characters, or `learn` for 2–3 short sentences up to 360 characters including information and a playful analogy. Optional `subject` supplies the previous recognition label; the captured image remains the grounding source. Know more reuses the previous frame rather than photographing a changed view.
