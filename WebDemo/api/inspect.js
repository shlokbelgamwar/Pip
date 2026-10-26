export const config = { maxDuration: 40 };
const buckets=new Map();
export function validPayload(body){
 if(!body||typeof body.image!=='string'||body.image.length>1100000||!['react','learn'].includes(body.mode??'react'))return false;
 if(!/^[A-Za-z0-9+/]+={0,2}$/.test(body.image))return false;
 const bytes=Buffer.from(body.image,'base64');
 return bytes.length>=4&&bytes.length<=800000&&bytes[0]===255&&bytes[1]===216&&typeof(body.subject??'')==='string'&&(body.subject??'').length<=60;
}
export default async function handler(req,res){
 res.setHeader('Cache-Control','no-store');res.setHeader('X-Content-Type-Options','nosniff');
 if(req.method!=='POST')return res.status(405).json({error:'method_not_allowed'});
 if(req.headers.origin){try{if(new URL(req.headers.origin).host!==req.headers.host)return res.status(403).json({error:'origin_not_allowed'});}catch{return res.status(403).json({error:'origin_not_allowed'});}}
 let body=req.body;try{if(typeof body==='string')body=JSON.parse(body);}catch{return res.status(400).json({error:'invalid_request'});}
 if(!validPayload(body))return res.status(400).json({error:'invalid_request'});
 if(!process.env.PIP_CLIENT_TOKEN)return res.status(503).json({error:'not_configured'});
 const now=Date.now(),ip=String(req.headers['x-forwarded-for']??'local').split(',')[0];
 for(const [key,value] of buckets)if(now-value.start>=60000)buckets.delete(key);
 const bucket=buckets.get(ip)??{start:now,count:0};if(bucket.count>=6)return res.status(429).json({error:'cooldown'});bucket.count++;buckets.set(ip,bucket);
 try{
  const upstream=await fetch('https://pip-vision.vercel.app/inspect',{method:'POST',headers:{'Content-Type':'application/json',Authorization:`Bearer ${process.env.PIP_CLIENT_TOKEN}`},body:JSON.stringify({image:body.image,mode:body.mode??'react',mood:['Content','Curious','Angry','Sleepy'].includes(body.mood)?body.mood:'Content',subject:body.subject??''}),signal:AbortSignal.timeout(32000)});
  if(!upstream.ok){const status=upstream.status===429?429:502;return res.status(status).json({error:status===429?'cooldown':'recognition_unavailable'});}
  const data=await upstream.json();if(typeof data.remark!=='string'||!data.remark.trim())return res.status(502).json({error:'invalid_response'});
  return res.status(200).json({remark:data.remark.slice(0,body.mode==='learn'?360:140),object:String(data.object??'').slice(0,60),expression:['Content','Curious','Angry','Sleepy'].includes(data.expression)?data.expression:'Curious',certain:data.certain===true});
 }catch{return res.status(504).json({error:'recognition_timeout'});}
}
