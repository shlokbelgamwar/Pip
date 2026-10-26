import './style.css';
import {createPip} from './character.js';
import {beginScratch,moveScratch,isTap} from './gesture.js';
const $=id=>document.getElementById(id),stage=$('stage'),canvas=$('pip'),video=$('camera');
let pip;try{pip=createPip(canvas,stage);}catch{$('notice').textContent='Pip needs WebGL to float here. Try a browser with graphics enabled.';}
let stream=null,busy=false,lastFrame=null,lastSubject='',lastReply='',lastExpression='Content',controller=null,feedbackTimer,petAt=0,pokes=0;
function say(words,expression='Content'){if(!pip)return;pip.setMood(expression);$('learn').hidden=true;$('words').textContent=words;$('speech').hidden=false;}
function notice(message){clearTimeout(feedbackTimer);$('notice').textContent=message;if(message)feedbackTimer=setTimeout(()=>{$('notice').textContent='';},9000);}
function setBusy(value){busy=value;$('thinking').hidden=!value;$('inspect').disabled=value;$('learn').disabled=value;$('camera-toggle').disabled=value;if(value)$('speech').hidden=true;$('inspect').querySelector('span').textContent=value?'Looking…':'Take a look';}
function syncCamera(on){document.body.classList.toggle('camera-on',on);pip?.setCamera(on);$('inspect').hidden=!on;$('camera-toggle').querySelector('span').textContent=on?'Close camera':'Show my world';$('mode-label').textContent=on?'Camera on · scans only when you ask':'Just hanging out';video.hidden=!on;}
function stopCamera(){controller?.abort();stream?.getTracks().forEach(t=>t.stop());stream=null;video.srcObject=null;lastFrame=null;lastSubject='';$('learn').hidden=true;syncCamera(false);setBusy(false);}
async function openCamera(){
 $('camera-consent').close();if(!navigator.mediaDevices?.getUserMedia){notice('Camera access needs a supported browser over HTTPS. You can still hang out with Pip.');return;}
 try{stream=await navigator.mediaDevices.getUserMedia({video:{facingMode:{ideal:'environment'},width:{ideal:1280},height:{ideal:720}},audio:false});video.srcObject=stream;await video.play();syncCamera(true);say('Oh, a window into your world. Show me a tiny treasure?','Curious');notice('Point at something, then tap Take a look.');}
 catch(e){stopCamera();notice(e.name==='NotAllowedError'?'Camera permission wasn’t granted. You can enable it in browser settings, or keep playing here.':'That camera isn’t available. Close other camera apps and try again.');}
}
function captureFrame(){
 if(!stream||!video.videoWidth)throw new Error('camera_not_ready');
 const c=document.createElement('canvas'),ratio=Math.min(1,800/video.videoWidth);c.width=Math.round(video.videoWidth*ratio);c.height=Math.round(video.videoHeight*ratio);c.getContext('2d').drawImage(video,0,0,c.width,c.height);return c.toDataURL('image/jpeg',.72).split(',')[1];
}
async function inspect(mode='react'){
 if(busy)return;let image;try{image=mode==='learn'?lastFrame:captureFrame();if(!image)return;}catch{notice('Give the camera a moment to wake up, then try again.');return;}
 setBusy(true);controller=new AbortController();const timeout=setTimeout(()=>controller.abort(),45000);
 try{
  const r=await fetch('/api/inspect',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({image,mode,subject:mode==='learn'?lastSubject:'',mood:lastExpression}),signal:controller.signal});
  let data;try{data=await r.json();}catch{throw new Error('unavailable');}
  if(!r.ok)throw new Error(data.error??'unavailable');
  if(!data.remark)throw new Error('invalid_response');
  if(mode==='react'){lastFrame=image;lastSubject=data.object??'';lastReply=data.remark;}
  lastExpression=data.expression;say(data.remark,data.expression);$('learn').hidden=false;notice('');
 }catch(e){
  const messages={cooldown:'My looking glasses need a little break. Try again in a minute.',recognition_timeout:'That look took too long. Give me another try?',recognition_unavailable:'My looking glasses are having a moment. Try again shortly.',not_configured:'My looking glasses aren’t connected yet. You can still play with me.'};
  say(messages[e.message]??(e.name==='AbortError'?'That look took too long. Let’s try once more.':'I couldn’t connect that time. Check your connection and try again.'),'Curious');
  notice('No automatic retries. Tap Take a look when you’re ready.');
 }finally{clearTimeout(timeout);controller=null;setBusy(false);}
}
$('about').onclick=()=>$('welcome').showModal();$('hello').onclick=()=>{$('welcome').close();try{localStorage.setItem('pip-web-welcomed','1');}catch{}pip?.twirl();say('There you are. I was just supervising this very important patch of nothing.');};
$('welcome').querySelector('.dialog-close').onclick=()=>$('welcome').close();
$('camera-toggle').onclick=()=>{if(stream){stopCamera();say('Back to our little corner of the universe.');}else $('camera-consent').showModal();};
$('allow-camera').onclick=openCamera;$('cancel-camera').onclick=()=>$('camera-consent').close();
$('inspect').onclick=()=>inspect();$('learn').onclick=()=>inspect('learn');$('close-speech').onclick=()=>{$('speech').hidden=true;};
const petLines=['Okay, okay… you found my favorite spot.','Excellent human. Very professional scratching.','I was going to be sassy, but this is much better.','A little to the left… yes. Very important companion maintenance.'];
let gesture=null;
canvas.addEventListener('pointerdown',e=>{if(gesture||!pip?.hit(e.clientX,e.clientY)||busy)return;canvas.setPointerCapture(e.pointerId);gesture=beginScratch(e.pointerId,e.clientX,e.clientY,performance.now());});
canvas.addEventListener('pointermove',e=>{const g=gesture;if(!g||g.id!==e.pointerId)return;if(moveScratch(g,e.clientX,e.clientY,performance.now())){g.pet=true;pip.pet();if(performance.now()-petAt>3500){petAt=performance.now();say(petLines[Math.floor(Math.random()*petLines.length)]);}}});
canvas.addEventListener('pointerup',e=>{const g=gesture;if(!g||g.id!==e.pointerId)return;gesture=null;if(isTap(g,performance.now())){pokes++;if(pokes%4===0)say('I am a companion, not a doorbell. A little scratch would be lovely.','Angry');else{pip.twirl();say(['Oh! Hello again. Still adorable? Thought so.','Present! Tiny, but very present.','You rang? I charge in snacks. Imaginary ones.'][pokes%3],'Content');}}});
canvas.addEventListener('pointercancel',()=>{gesture=null;});canvas.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();pip?.pet();say(petLines[0]);}});
$('save').onclick=async()=>{
 if(busy||!pip){notice('Let me finish this look first.');return;}
 try{
 const rect=stage.getBoundingClientRect(),c=document.createElement('canvas'),scale=Math.min(devicePixelRatio,2);c.width=rect.width*scale;c.height=rect.height*scale;const ctx=c.getContext('2d');ctx.scale(scale,scale);ctx.fillStyle='#28352a';ctx.fillRect(0,0,rect.width,rect.height);
 if(stream&&video.videoWidth){const ratio=Math.max(rect.width/video.videoWidth,rect.height/video.videoHeight),w=video.videoWidth*ratio,h=video.videoHeight*ratio;ctx.drawImage(video,(rect.width-w)/2,(rect.height-h)/2,w,h);}ctx.drawImage(canvas,0,0,rect.width,rect.height);
 const quote=$('speech').hidden?'':$('words').textContent;const pad=24,maxW=rect.width-2*pad,font=rect.width<500?14:18;ctx.font=`${font}px sans-serif`;const lines=[];let line='';for(const word of quote.split(' ')){const next=line?line+' '+word:word;if(ctx.measureText(next).width>maxW-32&&line){lines.push(line);line=word;}else line=next;}if(line)lines.push(line);const boxH=quote?lines.length*(font*1.5)+58:48;ctx.fillStyle='rgba(24,31,25,.92)';ctx.beginPath();ctx.roundRect(pad,rect.height-boxH-pad,maxW,boxH,16);ctx.fill();ctx.fillStyle='#c5e1b4';ctx.font='12px sans-serif';ctx.fillText('with Pip',pad+16,rect.height-boxH-pad+24);ctx.fillStyle='#f0f1e8';ctx.font=`${font}px sans-serif`;lines.forEach((text,i)=>ctx.fillText(text,pad+16,rect.height-boxH-pad+50+i*font*1.5));
 const blob=await new Promise(resolve=>c.toBlob(resolve,'image/jpeg',.92));const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=`a-moment-with-pip-${Date.now()}.jpg`;a.click();setTimeout(()=>URL.revokeObjectURL(url),10000);notice('Your moment is ready. Check your downloads.');
 }catch{notice('Couldn’t save that moment. You can take a screenshot instead.');}
};
document.addEventListener('visibilitychange',()=>{if(document.hidden&&stream)stopCamera();});window.addEventListener('pagehide',()=>{stopCamera();pip?.dispose();});
say('Oh, hello. I’m Pip. Small creature. Exceptionally large opinions.');
try{if(!localStorage.getItem('pip-web-welcomed'))$('welcome').showModal();}catch{$('welcome').showModal();}
