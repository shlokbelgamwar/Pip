import json
import os
import ssl
import urllib.request
from personality import PERSONALITY, inspection_context, spoken_remark

KEY = os.environ.get("GEMINI_API_KEY", "")
MODEL = os.environ.get("GEMINI_MODEL", "gemini-3.5-flash-lite")
CONTEXT = ssl.create_default_context()

def recognize(image, mood, mode="react", subject=""):
    payload = {'systemInstruction': {'parts': [{'text': PERSONALITY}]},
               'contents': [{'parts': [{'text': inspection_context(mood, mode, subject)}, {'inlineData': {'mimeType': 'image/jpeg', 'data': image}}]}],
               'generationConfig': {'maxOutputTokens': 512, 'responseMimeType': 'application/json',
                  'responseJsonSchema': {'type': 'object', 'properties': {
                     'expression': {'type': 'string', 'enum': ['Content', 'Curious', 'Sleepy', 'Angry']},
                     'object': {'type': 'string'}, 'remark': {'type': 'string'}, 'certain': {'type': 'boolean'}},
                     'required': ['object', 'remark', 'certain', 'expression']}}}
    req = urllib.request.Request('https://generativelanguage.googleapis.com/v1beta/models/' + MODEL + ':generateContent',
          json.dumps(payload).encode(), {'Content-Type': 'application/json', 'x-goog-api-key': KEY})
    with urllib.request.urlopen(req, timeout=25, context=CONTEXT) as response:
        result = json.load(response)
    parts = result.get('candidates', [{}])[0].get('content', {}).get('parts', [])
    output = json.loads(''.join(p.get('text', '') for p in parts if not p.get('thought')))
    if not isinstance(output.get('remark'), str) or not output['remark'].strip():
        raise ValueError('Missing remark')
    expression = output.get('expression')
    if expression not in ('Content', 'Curious', 'Sleepy', 'Angry'):
        expression = 'Curious' if mode == 'learn' else mood if mood in ('Content', 'Curious', 'Sleepy', 'Angry') else 'Content'
    certain = output.get('certain') is True
    return {'expression': expression, 'object': str(output.get('object', 'unknown'))[:60], 'certain': certain,
            'remark': spoken_remark(output['remark'],360 if mode=='learn' else 140)}

