import {createServer} from 'vite';
import {readFileSync} from 'node:fs';
import handler from './api/inspect.js';
// Local development only: load the existing token in server memory, never in Vite's client environment.
if(!process.env.PIP_CLIENT_TOKEN){try{process.env.PIP_CLIENT_TOKEN=JSON.parse(readFileSync(new URL('../Assets/StreamingAssets/pip-vision.json',import.meta.url))).token;}catch{}}
const server=await createServer({server:{host:'127.0.0.1',port:5178,strictPort:true},plugins:[{name:'pip-local-api',configureServer(server){server.middlewares.use('/api/inspect',async(req,res)=>{let data='';try{for await(const chunk of req){data+=chunk;if(data.length>1200000){res.statusCode=413;res.end();return;}}req.body=data;res.status=n=>{res.statusCode=n;return res;};res.json=value=>res.end(JSON.stringify(value));await handler(req,res);}catch{res.statusCode=500;res.end('{"error":"request_failed"}');}});}}]});await server.listen();server.printUrls();
