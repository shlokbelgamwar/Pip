import base64
import os
import unittest
from unittest.mock import patch
import app as proxy

class ProxyTest(unittest.TestCase):
    def setUp(self):
        self.env = patch.dict(os.environ, {'GEMINI_API_KEY': 'test', 'PIP_CLIENT_TOKEN': 'test-token', 'PIP_VISION_ENABLED': 'true'})
        self.env.start()
        proxy.BURST_REQUESTS.clear()
        self.client = proxy.app.test_client()
        self.headers = {'Authorization': 'Bearer test-token'}
        self.body = {'image': base64.b64encode(b'\xff\xd8test').decode(), 'mood': 'Curious'}
    def tearDown(self): self.env.stop()
    def test_auth(self):
        self.assertEqual(self.client.post('/inspect', json=self.body).status_code, 401)
    def test_disabled_until_enabled(self):
        with patch.dict(os.environ, {'PIP_VISION_ENABLED': 'false'}):
            self.assertEqual(self.client.post('/inspect', json=self.body, headers=self.headers).status_code, 503)
    def test_reject_bad_image(self):
        self.assertEqual(self.client.post('/inspect', json={'image':'bad'}, headers=self.headers).status_code, 400)
    def test_request_limit(self):
        self.assertEqual(self.client.post('/inspect', data='x'*1500001, content_type='application/json', headers=self.headers).status_code, 413)
    def test_routes_valid_reply_without_cache(self):
        with patch.object(proxy, 'recognize', return_value={'object':'mug','remark':'Tiny treasure!','certain':True}) as upstream:
            result = self.client.post('/inspect', json=self.body, headers=self.headers)
            self.assertEqual(result.status_code, 200)
            self.assertEqual(result.json['object'], 'mug')
            self.assertEqual(result.headers['Cache-Control'], 'no-store')
            self.assertEqual(upstream.call_args.args[1], 'Curious')
    def test_learn_mode_and_subject_reach_provider(self):
        body=dict(self.body,mode='learn',subject='earbud case')
        with patch.object(proxy,'recognize',return_value={'remark':'It recharges earbuds, like a tiny hotel.'}) as upstream:
            self.assertEqual(self.client.post('/inspect',json=body,headers=self.headers).status_code,200)
            self.assertEqual(upstream.call_args.args[2:],('learn','earbud case'))
    def test_reject_invalid_mode(self):
        self.assertEqual(self.client.post('/inspect',json=dict(self.body,mode='invalid'),headers=self.headers).status_code,400)
    def test_burst_limit(self):
        with patch.object(proxy, 'recognize', return_value={'remark':'Hello'}):
            for _ in range(12):
                self.assertEqual(self.client.post('/inspect',json=self.body,headers=self.headers).status_code,200)
            self.assertEqual(self.client.post('/inspect',json=self.body,headers=self.headers).status_code,429)
    def test_no_provider_details_leaked(self):
        with patch.object(proxy, 'recognize', side_effect=ValueError('private detail')):
            result = self.client.post('/inspect', json=self.body, headers=self.headers)
            self.assertEqual(result.status_code, 502)
            self.assertNotIn(b'private detail', result.data)
if __name__ == '__main__': unittest.main()
