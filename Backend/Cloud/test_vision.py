import json
import unittest
from unittest.mock import patch
import vision

class VisionTest(unittest.TestCase):
    def call_with(self, result, mode="react"):
        response = unittest.mock.MagicMock()
        response.__enter__.return_value.read.return_value = json.dumps({'candidates':[{'content':{'parts':[{'text':json.dumps(result)}]}}]}).encode()
        with patch.object(vision.urllib.request, 'urlopen', return_value=response) as fetch:
            actual=vision.recognize('test-image', 'Sleepy',mode,'mug')
            payload=json.loads(fetch.call_args.args[0].data)
        return actual,payload
    def test_uncertainty_keeps_character_response(self):
        result,payload=self.call_with({'object':'unknown','certain':False,'remark':'A tiny mystery! Does it come with a nap button?'})
        self.assertFalse(result['certain'])
        self.assertEqual(result['remark'],'A tiny mystery! Does it come with a nap button?')
        self.assertIn('systemInstruction',payload)
        self.assertIn('sleepy',payload['contents'][0]['parts'][0]['text'])
    def test_learn_keeps_explanation_beyond_reaction_length(self):
        text='This mug holds drinks. '+('It keeps your drink together. '*8)+'Like a tiny swimming pool for tea.'
        result,payload=self.call_with({'object':'mug','certain':True,'remark':text},'learn')
        self.assertGreater(len(result['remark']),140)
        self.assertLessEqual(len(result['remark']),360)
        context=payload['contents'][0]['parts'][0]['text']
        self.assertIn('Know more',context)
        self.assertIn('mug',context)
    def test_stage_direction_is_not_spoken(self):
        result,_=self.call_with({'object':'shape','certain':False,'remark':'*yawn* That looks cozy.'})
        self.assertEqual(result['remark'],'That looks cozy.')
    def test_cap_output_for_phone_layout(self):
        result,_=self.call_with({'object':'scene','certain':True,'remark':'x'*200})
        self.assertEqual(len(result['remark']),140)
    def test_expression_matches_reply_and_is_required(self):
        for expression in ('Content','Curious','Sleepy','Angry'):
            result,payload=self.call_with({'object':'case','certain':True,'remark':'A tiny hotel!','expression':expression})
            self.assertEqual(result['expression'],expression)
            self.assertIn('expression',payload['generationConfig']['responseJsonSchema']['required'])
    def test_invalid_expression_falls_back(self):
        for expression in (None,'Furious',{},42):
            result,_=self.call_with({'object':'case','certain':True,'remark':'A tiny hotel!','expression':expression})
            self.assertEqual(result['expression'],'Sleepy')
            result,_=self.call_with({'object':'case','certain':True,'remark':'It stores earbuds.','expression':expression},'learn')
            self.assertEqual(result['expression'],'Curious')
if __name__=='__main__':unittest.main()
