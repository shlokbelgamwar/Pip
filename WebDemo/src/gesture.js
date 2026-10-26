// Gesture geometry is independent of rendering: a tap, swipe and rub are distinct.
export function beginScratch(id,x,y,now){return{id,start:now,x,y,lastX:x,lastY:y,distance:0,reversals:0,axis:null,sign:0,stroked:0,pet:false};}
export function moveScratch(g,x,y,now){
 const dx=x-g.lastX,dy=y-g.lastY;g.distance+=Math.hypot(dx,dy);
 if(!g.axis&&Math.hypot(x-g.x,y-g.y)>8)g.axis=Math.abs(x-g.x)>Math.abs(y-g.y)?'x':'y';
 const delta=g.axis==='x'?dx:dy;
 if(Math.abs(delta)>1){const sign=Math.sign(delta);if(g.sign&&sign!==g.sign&&g.stroked>8){g.reversals++;g.stroked=0;}g.sign=sign;g.stroked+=Math.abs(delta);}
 g.lastX=x;g.lastY=y;
 if(now-g.start>450&&g.distance>60&&g.reversals>=2)g.pet=true;
 return g.pet;
}
export function isTap(g,now){return !g.pet&&g.distance<16&&now-g.start<500;}
