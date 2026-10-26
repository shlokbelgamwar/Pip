"""Pip cloud proxy. Never log request bodies, credentials or image data."""
import base64
import binascii
import os
import secrets
import threading
import time
from collections import deque
import urllib.error
from flask import Flask, jsonify, request
from werkzeug.exceptions import BadRequest, RequestEntityTooLarge
from vision import recognize

app = Flask(__name__)
app.config['MAX_CONTENT_LENGTH'] = 1500000
BURST_LOCK = threading.Lock()
BURST_REQUESTS = deque()

def allow_burst():
    # Per-instance protection only. Gemini Free tier remains the upstream quota.
    now = time.monotonic()
    with BURST_LOCK:
        while BURST_REQUESTS and BURST_REQUESTS[0] <= now - 60: BURST_REQUESTS.popleft()
        if len(BURST_REQUESTS) >= 12: return False
        BURST_REQUESTS.append(now)
        return True

@app.after_request
def no_cache(response):
    response.headers['Cache-Control'] = 'no-store'
    response.headers['X-Content-Type-Options'] = 'nosniff'
    return response

@app.get('/health')
def health():
    return jsonify(ready=bool(os.environ.get('GEMINI_API_KEY') and os.environ.get('PIP_CLIENT_TOKEN')
                              and os.environ.get('PIP_VISION_ENABLED') == 'true'))

@app.post('/inspect')
def inspect():
    token = os.environ.get('PIP_CLIENT_TOKEN', '')
    if not token or not secrets.compare_digest(request.headers.get('Authorization', ''), 'Bearer ' + token):
        return jsonify(error='unauthorized'), 401
    # Explicit enable switch; credentials alone do not activate inspections.
    if os.environ.get('PIP_VISION_ENABLED') != 'true' or not os.environ.get('GEMINI_API_KEY'):
        return jsonify(error='not_configured'), 503
    try:
        payload = request.get_json()
        if not isinstance(payload, dict): raise ValueError()
        image = payload.get('image', '')
        if not isinstance(image, str): raise ValueError()
        decoded = base64.b64decode(image, validate=True)
        if not decoded.startswith(b'\xff\xd8') or len(decoded) > 1000000: raise ValueError()
        mode=payload.get('mode','react')
        if mode not in ('react','learn'): raise ValueError()
        subject=payload.get('subject','')
        if not isinstance(subject,str): raise ValueError()
        mood = payload.get('mood', 'Content')
        if mood not in ('Curious', 'Content', 'Sleepy', 'Angry'): mood = 'Content'
    except (BadRequest, ValueError, TypeError, binascii.Error):
        return jsonify(error='invalid_request'), 400
    if not allow_burst(): return jsonify(error="cooldown"), 429
    try:
        return jsonify(recognize(image, mood, mode, subject[:60]))
    except urllib.error.HTTPError as error:
        return jsonify(error='provider_quota' if error.code == 429 else 'provider_unavailable'), 429 if error.code == 429 else 502
    except (urllib.error.URLError, TimeoutError, ValueError, IndexError, KeyError):
        return jsonify(error='recognition_unavailable'), 502

@app.errorhandler(RequestEntityTooLarge)
def too_large(error):
    return jsonify(error='image_too_large'), 413
