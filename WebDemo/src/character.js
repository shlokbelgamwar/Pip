import * as T from 'three';
export function createPip(canvas,stage){
 const renderer=new T.WebGLRenderer({canvas,alpha:true,antialias:true,preserveDrawingBuffer:true});renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.setClearColor(0,0);renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.ACESFilmicToneMapping;renderer.toneMappingExposure=1.25;
 const scene=new T.Scene(),camera=new T.PerspectiveCamera(32,1,.01,50);camera.position.set(0,0,5);camera.lookAt(0,0,0);
 scene.add(new T.HemisphereLight(0xf4f5db,0x405044,2.5));const light=new T.DirectionalLight(0xffefd0,4);light.position.set(-3,5,5);scene.add(light);const rim=new T.DirectionalLight(0xaedec2,2);rim.position.set(4,1,-2);scene.add(rim);
 const root=new T.Group();scene.add(root);const visual=new T.Group();root.add(visual);
 const mat=(color,roughness=.6)=>new T.MeshStandardMaterial({color,roughness});const cream=mat(0xffefd0),teal=mat(0x147f7c,.45),green=mat(0x81b64e,.5),ink=mat(0x283532,.45),browMat=mat(0x64574a),lipMat=new T.MeshBasicMaterial({color:0xcf8c85}),innerMat=mat(0x75554a);
 const ball=(parent,material,x,y,z,sx,sy,sz)=>{const mesh=new T.Mesh(new T.SphereGeometry(1,40,28),material);mesh.position.set(x,y,z);mesh.scale.set(sx,sy,sz);parent.add(mesh);return mesh;};
 ball(visual,cream,0,0,0,.62,.70,.54);
 const leftLeaf=ball(visual,green,-.27,.86,0,.14,.46,.075),rightLeaf=ball(visual,green,.27,.86,0,.14,.46,.075);leftLeaf.rotation.z=-.32;rightLeaf.rotation.z=.32;
 const scarf=ball(visual,teal,0,-.43,.02,.70,.13,.59);const end=ball(visual,teal,-.45,-.76,-.02,.13,.32,.09);end.rotation.z=-.2;
 const tail=ball(visual,new T.MeshStandardMaterial({color:0xa4e6b0,emissive:0x4da879,emissiveIntensity:.2,roughness:.5}),.50,-.10,-.42,.16,.16,.42);tail.rotation.y=.6;
 const eyeL=ball(visual,ink,-.25,.11,.49,.115,.18,.055),eyeR=ball(visual,ink,.25,.11,.49,.115,.18,.055);
 const browL=ball(visual,browMat,-.25,.39,.46,.10,.014,.013),browR=ball(visual,browMat,.25,.39,.46,.10,.014,.013);
 const line=(color,points,radius=.009)=>{const curve=new T.CatmullRomCurve3(points);return new T.Mesh(new T.TubeGeometry(curve,30,radius,6,false),color);};
 const lids=new T.Group();visual.add(lids);const mouth=new T.Group();mouth.position.set(0,-.24,.52);visual.add(mouth);let mood='Content',petUntil=0,pokeUntil=0,twirlUntil=0,lastMood='',lastBlink=-1;
 function face(expression,blink=false){
  const key=expression+blink;if(key===lastMood)return;lastMood=key;
  while(lids.children.length){const child=lids.children[0];lids.remove(child);child.geometry.dispose();}
  while(mouth.children.length){const child=mouth.children[0];mouth.remove(child);child.geometry.dispose();}
  const happy=expression==='Content',sleep=expression==='Sleepy',angry=expression==='Angry';
  eyeL.visible=eyeR.visible=!happy&&!sleep&&!blink;
  eyeL.scale.set(.115,angry?.055:.18,.055);eyeR.scale.copy(eyeL.scale);eyeL.rotation.z=angry?-.35:0;eyeR.rotation.z=angry?.35:0;
  for(const side of [-1,1]){const points=[];for(let i=0;i<=16;i++){const x=(i/16-.5)*.24;points.push(new T.Vector3(side*.25+x,.11+(happy?Math.sin(i/16*Math.PI)*.055:0),.54));}if(happy||sleep||blink)lids.add(line(ink,points,.009));}
  browL.position.y=browR.position.y=angry?.34:sleep?.35:.4;browL.rotation.z=angry?-.42:sleep?.2:.12;browR.rotation.z=-browL.rotation.z;
  leftLeaf.rotation.z=sleep?-1.05:angry?-.13:-.32;rightLeaf.rotation.z=-leftLeaf.rotation.z;
  const points=[];
  for(let i=0;i<=32;i++){const a=i/32*Math.PI*2,x=Math.sin(a)*(happy?.115:angry?.075:.055),y=(happy?.045:angry?-.035:0)*Math.sin(a)**2+Math.cos(a)*(expression==='Curious'?.055:happy?.022:.004);points.push(new T.Vector3(x,y,0));}
  mouth.add(line(lipMat,points,.008));if(happy||expression==='Curious')ball(mouth,innerMat,0,happy?.02:0,-.005,happy?.095:.045,happy?.022:.045,.005);
 }
 let cameraMode=false,width=0,height=0,frame,stopped=false;const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches;const raycaster=new T.Raycaster(),pointer=new T.Vector2();
 function resize(){width=stage.clientWidth;height=stage.clientHeight;renderer.setSize(width,height,false);camera.aspect=width/height;camera.updateProjectionMatrix();}
 const observer=new ResizeObserver(resize);observer.observe(stage);resize();
 function update(ms){if(stopped)return;const t=ms/1000;const mobile=window.innerWidth<=800;
  const nx=cameraMode?(mobile?.40:.42):(mobile?0:.37),ny=cameraMode?-.32:(mobile?-.55:-.30);
  const z=0,dist=camera.position.z-z,visibleH=2*Math.tan(T.MathUtils.degToRad(camera.fov/2))*dist;
  root.position.x=nx*visibleH*camera.aspect/2;
  const speech=document.getElementById('speech');
  const stageRect=stage.getBoundingClientRect();
  const upper=speech&&!speech.hidden?speech.getBoundingClientRect().bottom-stageRect.top+22:(cameraMode?35:mobile?height*.32:height*.28);
  const lower=height-(cameraMode?28:72),space=Math.max(90,lower-upper);
  const nominal=cameraMode?(mobile?.48:.60):(mobile?.55:.65);
  const scale=Math.min(nominal,space/height*visibleH/2.5);
  const center=Math.max(upper,lower-space/2)+scale*.11/visibleH*height;
  root.position.y=(.5-center/height)*visibleH;
  root.scale.setScalar(scale);
  visual.position.y=reduced?0:Math.sin(t*1.7)*.04;
  const pet=t<petUntil;visual.rotation.z=pet?Math.sin(t*9)*.08:reduced?0:Math.sin(t*.7)*.025;
  visual.rotation.y=t<twirlUntil?(twirlUntil-t)*Math.PI*3:pet?0:reduced?0:Math.sin(t*.6)*.12;
  visual.rotation.x=pet?-.06:0;
  const blinking=t%5.2<.12;face(pet?'Content':mood,blinking);
  const acting=pet?"Content":mood;const leafBase=acting==="Sleepy"?-1.05:acting==="Angry"?-.13:-.32;leftLeaf.rotation.z=leafBase+(reduced?0:Math.sin(t*2.5)*.025);rightLeaf.rotation.z=-leafBase-(reduced?0:Math.sin(t*2.2)*.025);if(!reduced)tail.rotation.x=Math.sin(t)*.15;
  renderer.render(scene,camera);frame=requestAnimationFrame(update);
 }
 frame=requestAnimationFrame(update);
 function hit(x,y){const r=canvas.getBoundingClientRect();pointer.set((x-r.left)/r.width*2-1,-((y-r.top)/r.height*2-1));raycaster.setFromCamera(pointer,camera);return raycaster.intersectObject(visual,true).length>0;}
 return{hit,setMood(value){mood=value;},pet(){petUntil=performance.now()/1000+1.8;},twirl(){if(!reduced)twirlUntil=performance.now()/1000+1.3;},setCamera(on){cameraMode=on;},canvas,dispose(){stopped=true;cancelAnimationFrame(frame);observer.disconnect();renderer.dispose();}};
}
