"""Pip's local development vision proxy. Python standard library only."""
import base64
import json
import os
from pathlib import Path
import secrets
import ssl
import threading
import time
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from Cloud.personality import PERSONALITY, inspection_context, spoken_remark

ROOT = Path(__file__).resolve().parent

def settings():
    result = {}
    for line in (ROOT / '.env').read_text().splitlines():
        if '=' in line and not line.lstrip().startswith('#'):
            key, value = line.split('=', 1)
            result[key.strip()] = value.strip().strip('\"\'')
    return result

ENV = settings()
KEY = ENV.get('GEMINI_API_KEY', '')
MODEL = ENV.get('GEMINI_MODEL', 'gemini-3.5-flash-lite')
TOKEN = ENV.get('PIP_CLIENT_TOKEN', '')
GUARD = threading.Lock()
LAST_REQUEST = 0.0
REQUESTS = 0
LIMIT = 60  # Hard cap per process session; no automatic retries or paid-model fallback.
CONTEXT = ssl.create_default_context(cafile='/etc/ssl/cert.pem') if Path('/etc/ssl/cert.pem').exists() else ssl.create_default_context()

def recognize(image, mood, mode="react", subject=""):
    payload = {'systemInstruction': {'parts': [{'text': PERSONALITY}]},
               'contents': [{'parts': [{'text': inspection_context(mood, mode, subject)}, {'inlineData': {'mimeType': 'image/jpeg', 'data': image}}]}],
               'generationConfig': {'maxOutputTokens': 512, 'responseMimeType': 'application/json',
                  'responseJsonSchema': {'type': 'object', 'properties': {
                     'object': {'type': 'string'}, 'remark': {'type': 'string'}, 'certain': {'type': 'boolean'}},
                     'required': ['object', 'remark', 'certain']}}}
    req = urllib.request.Request('https://generativelanguage.googleapis.com/v1beta/models/' + MODEL + ':generateContent',
          json.dumps(payload).encode(), {'Content-Type': 'application/json', 'x-goog-api-key': KEY})
    with urllib.request.urlopen(req, timeout=25, context=CONTEXT) as response:
        result = json.load(response)
    parts = result.get('candidates', [{}])[0].get('content', {}).get('parts', [])
    output = json.loads(''.join(p.get('text', '') for p in parts if not p.get('thought')))
    if not isinstance(output.get('remark'), str) or not output['remark'].strip():
        raise ValueError('Missing remark')
    certain = output.get('certain') is True
    return {'object': str(output.get('object', 'unknown'))[:60], 'certain': certain,
            'remark': spoken_remark(output['remark'],360 if mode=='learn' else 140)}

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass  # Never log images, auth headers or provider response bodies.
    def reply(self, status, value):
        body = json.dumps(value).encode()
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)
    def do_GET(self):
        self.reply(200 if self.path == '/health' else 404, {'ready': bool(KEY and TOKEN)} if self.path == '/health' else {'error': 'not_found'})
    def do_POST(self):
        global LAST_REQUEST, REQUESTS
        self.connection.settimeout(30)
        if self.path != '/inspect': return self.reply(404, {'error': 'not_found'})
        if not TOKEN or not secrets.compare_digest(self.headers.get('Authorization', ''), 'Bearer ' + TOKEN):
            return self.reply(401, {'error': 'unauthorized'})
        try:
            length = int(self.headers.get('Content-Length', '0'))
            if not 0 < length <= 1500000: return self.reply(413, {'error': 'image_too_large'})
            payload = json.loads(self.rfile.read(length))
            if not isinstance(payload, dict): return self.reply(400, {'error': 'invalid_request'})
            image = payload.get('image', '')
            data = base64.b64decode(image, validate=True)
            if not data.startswith(b'\xff\xd8') or len(data) > 1000000:
                return self.reply(400, {'error': 'invalid_jpeg'})
            mode=payload.get('mode','react')
            if mode not in ('react','learn'): return self.reply(400, {'error':'invalid_mode'})
            subject=payload.get('subject','')
            if not isinstance(subject,str): return self.reply(400, {'error':'invalid_request'})
            mood = payload.get('mood', 'Content')
            if mood not in ('Curious', 'Content', 'Sleepy', 'Angry'): mood = 'Content'
        except (ValueError, TypeError, KeyError):
            return self.reply(400, {'error': 'invalid_request'})
        with GUARD:
            if REQUESTS >= LIMIT or time.monotonic() - LAST_REQUEST < 5:
                return self.reply(429, {'error': 'cooldown_or_session_limit'})
            LAST_REQUEST = time.monotonic()
            REQUESTS += 1
        try:
            self.reply(200, recognize(image, mood, mode, subject[:60]))
        except urllib.error.HTTPError as error:
            self.reply(429 if error.code == 429 else 502, {'error': 'provider_quota' if error.code == 429 else 'provider_unavailable'})
        except (urllib.error.URLError, TimeoutError, ValueError, IndexError, KeyError):
            self.reply(502, {'error': 'recognition_unavailable'})

if __name__ == '__main__':
    if not KEY or not TOKEN: raise SystemExit('Add GEMINI_API_KEY and PIP_CLIENT_TOKEN to Backend/.env first.')
    host = os.environ.get('PIP_BIND', '127.0.0.1')
    print('Pip vision proxy ready on port 8765. Images are processed in memory only.', flush=True)
    ThreadingHTTPServer((host, 8765), Handler).serve_forever()
