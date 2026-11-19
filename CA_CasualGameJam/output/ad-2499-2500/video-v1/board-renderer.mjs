import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import crypto from 'node:crypto';

const require = createRequire(import.meta.url);
const { createCanvas, loadImage, GlobalFonts } = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const sharp = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../storyboard-v1');
const project = path.resolve(root, '../../..');
const W = 1400, H = 1800;
const NAVY = '#081D4F', CREAM = '#FFF7DF';
const palette = ['#FF455B', '#FF942B', '#FFD43B', '#24C8A0', '#1488FF', '#9851F4'];
GlobalFonts.registerFromPath(path.join(project, 'Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf'), 'Lilita');
GlobalFonts.registerFromPath(path.join(project, 'Assets/Fonts/Inter/Inter-Regular.ttf'), 'Inter');
GlobalFonts.registerFromPath('C:/Windows/Fonts/meiryo.ttc', 'Meiryo');
const handPath = path.join(root, 'assets/swipe-hand.png');
const handImage = await loadImage(handPath);
const logoImage = await loadImage(path.join(project, 'Assets/UI/ToonBold/BlendTrailsLogo.png'));
// Alpha bounding box is measured only; the generated sprite is preserved verbatim.
const rawHand = await sharp(handPath).ensureAlpha().raw().toBuffer({resolveWithObject:true});
let hb = {x: rawHand.info.width, y: rawHand.info.height, right:0, bottom:0};
for (let y=0;y<rawHand.info.height;y++) for (let x=0;x<rawHand.info.width;x++) {
  if (rawHand.data[(y*rawHand.info.width+x)*4+3] > 24) {
    hb.x=Math.min(hb.x,x);hb.y=Math.min(hb.y,y);hb.right=Math.max(hb.right,x);hb.bottom=Math.max(hb.bottom,y);
  }
}
hb.w=hb.right-hb.x+1;hb.h=hb.bottom-hb.y+1;
// The contact point is the middle of the visible index tip, slightly inside its outline.
const handTip = {x: Math.round(hb.x+hb.w*0.10), y:Math.round(hb.y+hb.h*0.035)};

function rng(seed) {return ()=>{seed|=0;seed=seed+0x6D2B79F5|0;let t=Math.imul(seed^seed>>>15,1|seed);t=t+Math.imul(t^t>>>7,61|t)^t;return ((t^t>>>14)>>>0)/4294967296;};}
const key=(r,c)=>r*50+c;
const pos=id=>({r:Math.floor(id/50),c:id%50});

function makeBoardA() {
  // Merge 2x2 cycles along a seeded spanning tree: an exact Hamiltonian cycle
  // of 50 rows x 48 columns. Its open end continues into the two last columns.
  const adj=Array.from({length:2500},()=>new Set());
  const add=(a,b)=>{adj[a].add(b);adj[b].add(a);};
  const del=(a,b)=>{adj[a].delete(b);adj[b].delete(a);};
  for(let mr=0;mr<25;mr++)for(let mc=0;mc<24;mc++){
    const a=key(mr*2,mc*2), b=a+1, d=a+50, c=d+1;
    add(a,b);add(b,c);add(c,d);add(d,a);
  }
  const random=rng(24992500), visited=new Set(['0,0']), stack=[[0,0]];
  while(stack.length){
    const [r,c]=stack.at(-1);
    const choices=[[r-1,c],[r+1,c],[r,c-1],[r,c+1]].filter(([y,x])=>y>=0&&y<25&&x>=0&&x<24&&!visited.has(`${y},${x}`));
    if(!choices.length){stack.pop();continue;}
    const [nr,nc]=choices[Math.floor(random()*choices.length)];
    visited.add(`${nr},${nc}`);stack.push([nr,nc]);
    if(nr===r){
      const left=Math.min(c,nc)*2, rr=r*2;
      const a=key(rr,left+1),b=key(rr+1,left+1),d=key(rr,left+2),e=key(rr+1,left+2);
      del(a,b);del(d,e);add(a,d);add(b,e);
    }else{
      const top=Math.min(r,nr)*2, cc=c*2;
      const a=key(top+1,cc),b=key(top+1,cc+1),d=key(top+2,cc),e=key(top+2,cc+1);
      del(a,b);del(d,e);add(a,d);add(b,e);
    }
  }
  const end=key(0,47), start=key(1,47);
  if(!adj[end].has(start))throw new Error('Expected outer boundary edge');
  del(end,start);
  const route=[start];let previous=-1,current=start;
  while(current!==end){const next=[...adj[current]].find(id=>id!==previous);route.push(next);previous=current;current=next;}
  for(let r=0;r<50;r++)route.push(key(r,48));
  for(let r=49;r>=0;r--)route.push(key(r,49));
  return route;
}

function makeBoardB() {
  const spiral=[];let t=0,b=49,l=0,r=49;
  while(t<=b&&l<=r){
    for(let c=l;c<=r;c++)spiral.push(key(t,c));t++;
    for(let y=t;y<=b;y++)spiral.push(key(y,r));r--;
    for(let c=r;c>=l;c--)spiral.push(key(b,c));b--;
    for(let y=b;y>=t;y--)spiral.push(key(y,l));l++;
  }
  const rotated=spiral.map(id=>{const p=pos(id);return key(p.c,49-p.r);});
  const tail=[[23,26],[23,25],[23,24],[23,23],[24,23],[25,23],[26,23],
    [26,24],[26,25],[26,26],[25,26],[24,26],[24,25],[24,24],[25,24],[25,25]].map(([r,c])=>key(r,c));
  return [...rotated.slice(0,2484),...tail];
}

const routeA=makeBoardA(),routeB=makeBoardB();
function validateRoute(route,name){
  if(route.length!==2500||new Set(route).size!==2500)throw new Error(`${name}: cell coverage`);
  for(let i=1;i<route.length;i++){const a=pos(route[i-1]),b=pos(route[i]);if(Math.abs(a.r-b.r)+Math.abs(a.c-b.c)!==1)throw new Error(`${name}: discontinuity at ${i}`);}
}
validateRoute(routeA,'A');validateRoute(routeB,'B');
const rangesA=[330,390,420,430,420,510],rangesB=[442,371,398,404,410,375,100];
const colorsA=palette,colorsB=[...palette,palette[0]];
function boardModel(route,ranges,colors){
  const index=new Int32Array(2500),color=new Array(2500);let offset=0;const segments=[];
  ranges.forEach((len,i)=>{const ids=route.slice(offset,offset+len);segments.push({ids,offset,color:colors[i]});ids.forEach((id,n)=>{index[id]=offset+n;color[id]=colors[i];});offset+=len;});
  if(offset!==2500)throw new Error('Color range total');return {route,index,color,segments};
}
const boardA=boardModel(routeA,rangesA,colorsA),boardB=boardModel(routeB,rangesB,colorsB);
const hole=key(25,25);
if(routeB[2499]!==hole)throw new Error('Expected center hole');

function mix(hex,t=0.25){const n=parseInt(hex.slice(1),16);return `rgb(${[n>>16,(n>>8)&255,n&255].map(v=>Math.round(v+(255-v)*t)).join(',')})`;}
function rr(ctx,x,y,w,h,r,fill,stroke=null,lw=0){ctx.beginPath();ctx.roundRect(x,y,w,h,r);if(fill){ctx.fillStyle=fill;ctx.fill();}if(stroke){ctx.strokeStyle=stroke;ctx.lineWidth=lw;ctx.stroke();}}
function line(ctx,points,color,width,dash=[]){ctx.beginPath();points.forEach((p,i)=>i?ctx.lineTo(p.x,p.y):ctx.moveTo(p.x,p.y));ctx.strokeStyle=color;ctx.lineWidth=width;ctx.lineJoin='round';ctx.lineCap='round';ctx.setLineDash(dash);ctx.stroke();ctx.setLineDash([]);}
function text(ctx,value,x,y,size=80,fill='white',stroke=true,align='center',font='Lilita'){
  ctx.save();ctx.font=`${size}px ${font}`;ctx.textAlign=align;ctx.textBaseline='middle';ctx.lineJoin='round';
  if(stroke){ctx.strokeStyle=NAVY;ctx.lineWidth=Math.max(5,size*0.15);ctx.strokeText(value,x,y+5);ctx.strokeText(value,x,y);}
  ctx.fillStyle=fill;ctx.fillText(value,x,y);ctx.restore();
}
function panel(ctx,x,y,w,h,r=48){rr(ctx,x,y+13,w,h,r,'#0876BD');rr(ctx,x,y,w,h,r,CREAM,NAVY,13);}
function cloud(ctx,x,y,s){ctx.save();ctx.translate(x,y);ctx.scale(s,s);ctx.fillStyle='#D0F4FF';ctx.beginPath();ctx.ellipse(0,8,125,43,0,0,Math.PI*2);ctx.ellipse(-55,-18,55,55,0,0,Math.PI*2);ctx.ellipse(15,-45,75,73,0,0,Math.PI*2);ctx.ellipse(86,-9,51,51,0,0,Math.PI*2);ctx.fill();ctx.restore();}
function background(ctx){
  const g=ctx.createLinearGradient(0,0,1400,1800);g.addColorStop(0,'#31C7FF');g.addColorStop(1,'#12A7EE');ctx.fillStyle=g;ctx.fillRect(0,0,W,H);
  ctx.save();ctx.translate(700,830);for(let i=0;i<18;i++){ctx.rotate(Math.PI/9);ctx.beginPath();ctx.moveTo(0,0);ctx.lineTo(-150,-2000);ctx.lineTo(150,-2000);ctx.closePath();ctx.fillStyle='rgba(255,255,255,0.035)';ctx.fill();}ctx.restore();
  cloud(ctx,-28,250,.95);cloud(ctx,1380,410,1.0);cloud(ctx,-80,1350,1.2);cloud(ctx,1395,1680,1.05);
}
function headline(ctx,lines,top=137,size=100){lines.forEach((v,i)=>text(ctx,v,700,top+i*(size*.98),size));}
function counter(ctx,value,y=310,{alert=false,small=false}={}){
  const w=small?610:760,h=small?100:118;panel(ctx,700-w/2,y-h/2,w,h,45);
  text(ctx,value,700,y+2,small?65:81,alert?'#E83C52':NAVY,false);
}
function bottomLabel(ctx,value='50 × 50',y=1648){panel(ctx,498,y-43,404,86,42);text(ctx,value,700,y+2,49,NAVY,false);}
const fullView={x:150,y:435,pitch:22};
function point(id,view){const p=pos(id);return{x:view.x+(p.c+.5)*view.pitch,y:view.y+(p.r+.5)*view.pitch};}
function zoomView(centerR,centerC,pitch,centerX=700,centerY=945){return{x:centerX-(centerC+.5)*pitch,y:centerY-(centerR+.5)*pitch,pitch};}
function drawBoard(ctx,model,count,view,{clip=null,frame=true}={}){
  const p=view.pitch,boardSize=p*50;
  if(frame&&!clip)panel(ctx,view.x-22,view.y-22,boardSize+44,boardSize+44,35);
  ctx.save();if(clip){ctx.beginPath();ctx.roundRect(clip.x,clip.y,clip.w,clip.h,clip.r??35);ctx.clip();ctx.fillStyle=CREAM;ctx.fillRect(clip.x,clip.y,clip.w,clip.h);}
  const gap=p<30?1.45:p*.055,bw=p<30?0.6:p*.021;
  for(let r=0;r<50;r++)for(let c=0;c<50;c++){
    const id=key(r,c),x=view.x+c*p+gap/2,y=view.y+r*p+gap/2;
    if(clip&&(x+p<clip.x||y+p<clip.y||x>clip.x+clip.w||y>clip.y+clip.h))continue;
    const filled=model.index[id]<count;rr(ctx,x,y,p-gap,p-gap,p*.14,filled?mix(model.color[id],.30):'#FFFFFF',NAVY,bw);
    if(filled&&p>50){rr(ctx,x+p*.10,y+p*.07,p*.70,p*.09,p*.04,'rgba(255,255,255,0.25)');}
  }
  for(const segment of model.segments){
    const active=segment.ids.slice(0,Math.max(0,count-segment.offset));if(!active.length)continue;
    const points=active.map(id=>point(id,view));
    line(ctx,points,segment.color,p*.53);
    line(ctx,points,'rgba(255,255,255,0.22)',Math.max(1,p*.075));
    if(view.pitch>50){const start=points[0];ctx.fillStyle=segment.color;ctx.beginPath();ctx.arc(start.x,start.y,p*.31,0,Math.PI*2);ctx.fill();}
  }
  ctx.restore();
}
function arrow(ctx,points,color='#FFFFFF',width=11){
  line(ctx,points,NAVY,width+8);line(ctx,points,color,width);
  const a=points.at(-2),b=points.at(-1),angle=Math.atan2(b.y-a.y,b.x-a.x),len=width*2.1;
  const head=[{x:b.x-len*Math.cos(angle-.65),y:b.y-len*Math.sin(angle-.65)},b,{x:b.x-len*Math.cos(angle+.65),y:b.y-len*Math.sin(angle+.65)}];
  line(ctx,head,NAVY,width+8);line(ctx,head,color,width);
}
function ring(ctx,x,y,r=28,color='#FFFFFF'){ctx.beginPath();ctx.arc(x,y,r,0,Math.PI*2);ctx.strokeStyle=NAVY;ctx.lineWidth=11;ctx.stroke();ctx.strokeStyle=color;ctx.lineWidth=6;ctx.stroke();}
function drawHand(ctx,tip,height=365,{ghosts=[],angle=0,opacity=1}={}){
  const scale=height/hb.h;
  const paint=(p,a)=>{ctx.save();ctx.globalAlpha=a;ctx.translate(p.x,p.y);ctx.rotate(angle);ctx.drawImage(handImage,hb.x,hb.y,hb.w,hb.h,-(handTip.x-hb.x)*scale,-(handTip.y-hb.y)*scale,hb.w*scale,hb.h*scale);ctx.restore();};
  ghosts.forEach((p,i)=>paint(p,opacity*(.09+i*.06)));paint(tip,opacity);
}
function swipe(ctx,points,{height=365,angle=0}={}){
  const tip=points.at(-1),before=points.at(-2),length=Math.hypot(tip.x-before.x,tip.y-before.y)||1;
  const pullback=Math.min(58,length*.64),guide=points.map(p=>({...p}));
  guide[guide.length-1]={x:tip.x-(tip.x-before.x)/length*pullback,y:tip.y-(tip.y-before.y)/length*pullback};
  arrow(ctx,guide,'#FFFFFF',points.length>3?10:9);ring(ctx,tip.x,tip.y,25);
  const a=points.at(-2),dx=tip.x-a.x,dy=tip.y-a.y;
  const d=Math.hypot(dx,dy)||1;
  drawHand(ctx,tip,height,{angle,ghosts:[{x:tip.x-dx/d*82,y:tip.y-dy/d*82},{x:tip.x-dx/d*43,y:tip.y-dy/d*43}]});
}
function confetti(ctx){const random=rng(2500);for(let i=0;i<80;i++){
  const x=80+random()*1240,y=370+random()*1170;
  if(x>165&&x<1235&&y>445&&y<1520)continue;
  ctx.save();ctx.translate(x,y);ctx.rotate(random()*5);rr(ctx,-7,-15,14,30,4,palette[i%6]);ctx.restore();
}}
function sparkle(ctx,x,y,s=26){ctx.beginPath();ctx.moveTo(x,y-s);ctx.quadraticCurveTo(x+5,y-5,x+s,y);ctx.quadraticCurveTo(x+5,y+5,x,y+s);ctx.quadraticCurveTo(x-5,y+5,x-s,y);ctx.quadraticCurveTo(x-5,y-5,x,y-s);ctx.fillStyle='#FFF6A2';ctx.fill();ctx.strokeStyle=NAVY;ctx.lineWidth=3;ctx.stroke();}
function pulseHole(ctx,view,{large=false}={}){
  const p=point(hole,view),radius=view.pitch*(large?.72:.9);
  ctx.save();ctx.strokeStyle='#FF4A5E';ctx.lineWidth=large?8:5;ctx.setLineDash([large?17:9,large?11:7]);ctx.beginPath();ctx.arc(p.x,p.y,radius,0,Math.PI*2);ctx.stroke();ctx.restore();
}


export {createCanvas, loadImage, sharp, W, H, NAVY, CREAM, palette, boardA, boardB, routeA, routeB, hole, key, pos, point, fullView, zoomView, rr, line, text, panel, headline, counter, bottomLabel, background, drawBoard, drawHand, ring, arrow, sparkle, pulseHole, logoImage, rng};
