import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import crypto from 'node:crypto';

const require = createRequire(import.meta.url);
const { createCanvas, loadImage, GlobalFonts } = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const sharp = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = path.dirname(fileURLToPath(import.meta.url));
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

const scenes=[
 {id:'01',file:'01-fill-every-tile.png',time:'0–4秒',title:'接写・ジッパースワイプ',copy:'FILL EVERY TILE.',board:'A',filled:0},
 {id:'02',file:'02-keep-going.png',time:'4–15秒',title:'引き・50×50の巨大盤面',copy:'KEEP GOING...',board:'A',filled:1625},
 {id:'03',file:'03-last-column.png',time:'15–17秒',title:'最後の列を埋める',copy:'2500 / 2500 へのカウントアップ',board:'A',filled:2475},
 {id:'04',file:'04-perfect.png',time:'17–19秒',title:'2500マス完成',copy:'2500 / 2500 → PERFECT!',board:'A',filled:2500},
 {id:'05',file:'05-2499-stop.png',time:'19–32秒',title:'新盤面・2499で停止',copy:'2499 / 2500',board:'B',filled:2499},
 {id:'06',file:'06-one-white-tile.png',time:'32–35秒',title:'中央の白い1マスへズーム',copy:'HOW DID YOU MISS THAT?',board:'B',filled:2499},
 {id:'07',file:'07-wrong-turn-again.png',time:'35–41秒',title:'引き直し・曲がる場所を間違える',copy:'HOW DID YOU MISS THAT?',board:'B',filled:2499},
 {id:'08',file:'08-play-now.png',time:'41–45秒',title:'白い穴を残してCTA',copy:'CAN YOU GET ALL 2500? / PLAY NOW',board:'B',filled:2499},
];
const previews=[];
for(const scene of scenes){
  const canvas=createCanvas(W,H),ctx=canvas.getContext('2d');background(ctx);
  if(scene.id==='01'){
    headline(ctx,['FILL EVERY TILE.'],145,111);
    // Choose a real seven-cell straight portion of the long route for the zipper shot.
    let index=0;
    for(let i=30;i<700;i++){
      const cells=routeA.slice(i-6,i+1).map(pos);
      if(cells.every(p=>p.r===cells[0].r)&&cells.at(-1).c>cells[0].c&&cells[0].c>=3&&cells.at(-1).c<44){index=i;break;}
    }
    if(!index){index=routeA.findIndex((id,i)=>i>40&&pos(id).r===pos(routeA[i-2]).r&&pos(id).c>pos(routeA[i-2]).c);}
    const target=pos(routeA[index]),view=zoomView(target.r,target.c,163,850,930);
    panel(ctx,61,355,1278,1168,52);drawBoard(ctx,boardA,index+1,view,{clip:{x:83,y:377,w:1234,h:1124,r:32}});
    const pts=routeA.slice(index-3,index+1).map(id=>point(id,view));swipe(ctx,pts,{height:465});
    bottomLabel(ctx,'50 × 50',1645);scene.filled=index+1;scene.viewport={...view,clip:{x:83,y:377,w:1234,h:1124}};scene.contactTile=target;
  }
  if(scene.id==='02'){
    headline(ctx,['KEEP GOING...'],137,118);counter(ctx,'1625 / 2500',307);
    drawBoard(ctx,boardA,1625,fullView);
    const target=point(routeA[1624],fullView),previous=point(routeA[1623],fullView);
    const d={x:target.x-previous.x,y:target.y-previous.y};
    swipe(ctx,[{x:target.x-d.x*4,y:target.y-d.y*4},target],{height:330});
    bottomLabel(ctx);scene.viewport=fullView;
  }
  if(scene.id==='03'){
    headline(ctx,['KEEP GOING...'],137,118);counter(ctx,'2475 / 2500',307);
    drawBoard(ctx,boardA,2475,fullView);
    const pts=[routeA[2468],routeA[2471],routeA[2474]].map(id=>point(id,fullView));
    // Right-edge swipes keep the palm inside the frame and away from the final column.
    swipe(ctx,pts,{height:300,angle:.52});bottomLabel(ctx);scene.viewport=fullView;
  }
  if(scene.id==='04'){
    headline(ctx,['PERFECT!'],137,151);counter(ctx,'2500 / 2500',307);
    drawBoard(ctx,boardA,2500,fullView);confetti(ctx);
    sparkle(ctx,91,497,28);sparkle(ctx,1314,1396,31);sparkle(ctx,1127,1631,35);
    bottomLabel(ctx);scene.viewport=fullView;
  }
  if(scene.id==='05'){
    headline(ctx,['2499 / 2500'],151,136);
    // The exact missing cell remains visible in the full board, without an early reveal marker.
    drawBoard(ctx,boardB,2499,fullView);bottomLabel(ctx);
    const tip={x:1209,y:1461};drawHand(ctx,tip,285,{angle:.4});scene.viewport=fullView;
  }
  if(scene.id==='06'){
    headline(ctx,['HOW DID YOU','MISS THAT?'],120,108);counter(ctx,'2499 / 2500',361,{alert:true,small:true});
    const view=zoomView(25,25,153,700,974);
    panel(ctx,61,476,1278,1066,52);drawBoard(ctx,boardB,2499,view,{clip:{x:83,y:498,w:1234,h:1022,r:32}});
    pulseHole(ctx,view,{large:true});
    // A lifted hand enters from the lower-right; it does not cover the white cell.
    drawHand(ctx,{x:1063,y:1246},310,{angle:.18});
    bottomLabel(ctx,'50 × 50',1663);scene.viewport=view;
  }
  if(scene.id==='07'){
    headline(ctx,['HOW DID YOU','MISS THAT?'],120,108);counter(ctx,'2499 / 2500',361,{alert:true,small:true});
    const view=zoomView(25,25,153,700,974);
    panel(ctx,61,476,1278,1066,52);drawBoard(ctx,boardB,2499,view,{clip:{x:83,y:498,w:1234,h:1022,r:32}});
    const pts=[[26,24],[26,25],[26,26]].map(([r,c])=>point(key(r,c),view));
    swipe(ctx,pts,{height:345,angle:.08});pulseHole(ctx,view,{large:true});
    // Three small red emphasis marks at the wrong turn; no second white cell is introduced.
    const turn=point(key(26,25),view);for(let i=0;i<3;i++){const a=-Math.PI/2+(i-1)*.42;line(ctx,[{x:turn.x+Math.cos(a)*52,y:turn.y+Math.sin(a)*52},{x:turn.x+Math.cos(a)*76,y:turn.y+Math.sin(a)*76}],'#E7374E',7);}
    bottomLabel(ctx,'50 × 50',1663);scene.viewport=view;
  }
  if(scene.id==='08'){
    headline(ctx,['CAN YOU GET','ALL 2500?'],112,118);
    const view=zoomView(25,25,130,700,883);
    panel(ctx,113,370,1174,1040,52);drawBoard(ctx,boardB,2499,view,{clip:{x:135,y:392,w:1130,h:996,r:32}});
    pulseHole(ctx,view,{large:true});
    drawHand(ctx,{x:884,y:1057},280,{angle:.16});
    rr(ctx,310,1506,780,168,63,'#0C77B5');rr(ctx,310,1488,780,168,63,'#FFCF36',NAVY,14);
    text(ctx,'PLAY NOW',700,1570,109,NAVY,false);
    // Existing official logo stays compact, below the CTA, leaving the challenge central.
    const ratio=logoImage.width/logoImage.height,lw=190,lh=lw/ratio;
    ctx.drawImage(logoImage,700-lw/2,1718-lh/2,lw,lh);scene.viewport=view;
  }
  const output=path.join(root,scene.file);await fs.writeFile(output,canvas.toBuffer('image/png'));
  const meta=await sharp(output).metadata();const data=await fs.readFile(output);
  scene.width=meta.width;scene.height=meta.height;scene.bytes=data.length;scene.sha256=crypto.createHash('sha256').update(data).digest('hex');
  scene.empty=2500-scene.filled;previews.push({scene,canvas});
}

// Contact sheet: every panel is an exact, uncropped scaled copy of a delivered keyframe.
const overview=createCanvas(1960,1548),oc=overview.getContext('2d');oc.fillStyle='#EDF7FC';oc.fillRect(0,0,1960,1548);
text(oc,'2499 / 2500 — 画面イメージ',45,53,34,NAVY,false,'left','Meiryo');
text(oc,'1400 × 1800  /  45秒構成  /  スワイプする手つき  /  動画制作前の確認用',45,99,20,NAVY,false,'left','Meiryo');
for(let i=0;i<previews.length;i++){
  const {scene,canvas}=previews[i],x=40+(i%4)*482,y=142+Math.floor(i/4)*693;
  rr(oc,x,y,454,670,16,'#FFFFFF');oc.drawImage(canvas,x+10,y+10,434,558);
  text(oc,`${scene.id}  ${scene.time}`,x+18,y+592,24,NAVY,false,'left','Meiryo');
  text(oc,scene.title,x+18,y+631,17,NAVY,false,'left','Meiryo');
}
await fs.writeFile(path.join(root,'overview.png'),overview.toBuffer('image/png'));

const specification={
  version:1,date:'2026-10-25',timezone:'Asia/Tokyo',status:'静止画確認用・動画未生成',
  video:{width:1400,height:1800,durationSeconds:45,proposedFps:30,allowedDurationSeconds:[30,59]},
  imageGeneration:{tool:'built-in image_gen',asset:'assets/swipe-hand.png',promptFile:'hand-prompt.txt',source:'C:/Users/robor/.codex/generated_images/01a170d1-e31c-7700-83fb-483caf54c98a/exec-5a417f70-54a9-49d2-af9e-00a28ddf72bb.png'},
  renderer:'Native code-authored 2D board, typography, panels, arrows and rings, rendered with @napi-rs/canvas. Generated hand composited consistently across keyframes.',
  grid:{rows:50,columns:50,total:2500,hole:{row:26,column:26,coordinates:'1-based'}},
  invariants:{routesValidated:true,routeAVisits:2500,routeBVisits:2500,noDiagonalConnections:true,last50CellsAreLastColumnA:true,finalBoardBWhiteCells:1},
  scenes,
  videoDirections:[
    '0–4秒: 接写で指先と着色先端を同期。接触リングは指先に固定し、細い残像と矢印でスワイプ方向を示す。',
    '4–15秒: 50×50盤面へ連続ズームアウト。複数色の長い連続経路を順番に高速着色。',
    '15–19秒: 最後の列を塗り切り、2500 / 2500 を約0.4秒保持してから PERFECT!。完成時は手を退場。',
    '19–32秒: 別の巨大盤面へ切り替え、高速着色後に2499 / 2500で約1秒停止。白い穴は中央付近の1マスだけ。',
    '32–35秒: 全体の2499カウンターを維持し、r26c26の白い穴へ連続ズーム。',
    '35–41秒: 赤色の最後の9マスを一度巻き戻して再スワイプ。r27c26で上へ曲がるべきところを右へ進み、外周8マスだけが再着色される。静止画07は引き直し軌跡の見せ方を合成したキーフレームで、再着色の途中フレームそのものではない。最後に2499へ戻る。',
    '41–45秒: 白い穴を残した拡大盤面とCTAを保持。PLAY NOWを軽く脈動させる。'
  ]
};
await fs.writeFile(path.join(root,'manifest.json'),JSON.stringify(specification,null,2));
await fs.writeFile(path.join(root,'board-data.json'),JSON.stringify({rows:50,columns:50,coordinateSystem:'0-based cell ID = row*50 + column',routeA,routeB,rangesA,rangesB,colorsA,colorsB,hole,correctFinalNineB:[[26,24],[25,24],[24,24],[24,25],[25,25],[26,25],[26,26],[25,26],[24,26]].map(([r,c])=>key(r,c))},null,2));

const esc=s=>s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('"','&quot;');
const cards=scenes.map(s=>`<article><button class="shot" data-file="${s.file}" aria-label="${esc(s.title)}を拡大"><img src="${s.file}" alt="${esc(s.title)}"></button><h2>${s.id} <span>${s.time}</span></h2><p>${esc(s.title)}</p><p class="copy">${esc(s.copy)}</p><a href="${s.file}" download>PNGを保存 · 1400×1800</a></article>`).join('');
await fs.writeFile(path.join(root,'index.html'),`<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>2499 / 2500 — 画面イメージ</title><style>*{box-sizing:border-box}body{margin:0;background:#edf7fc;color:#081d4f;font:16px/1.65 system-ui,"Meiryo",sans-serif}main{max-width:1550px;margin:auto;padding:35px}h1{font-size:34px;margin:0 0 8px}.lead{max-width:950px;margin-bottom:28px}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:22px}article{padding:12px 12px 20px;background:white;border-radius:18px;box-shadow:0 4px 14px #081d4f12}.shot{width:100%;padding:0;border:0;background:none;cursor:zoom-in}.shot img{width:100%;display:block;border-radius:11px}h2{font-size:21px;margin:10px 2px 0}h2 span{font-weight:normal;font-size:16px;margin-left:10px}p{margin:6px 2px}.copy{font-size:13px;min-height:43px;color:#506482}a{color:#0755ba}article a{font-size:14px;margin-left:2px}.links{margin:30px 0}dialog{background:#071931;border:0;padding:0;max-width:96vw;max-height:96vh;border-radius:16px}dialog::backdrop{background:#03112bd9}dialog img{display:block;max-width:94vw;max-height:94vh}dialog button{position:fixed;right:25px;top:20px;border:0;border-radius:50%;font-size:28px;width:48px;height:48px;background:white;cursor:pointer}@media(max-width:1100px){.grid{grid-template-columns:repeat(2,1fr)}}@media(max-width:600px){main{padding:20px}.grid{grid-template-columns:1fr}h1{font-size:28px}}</style><main><h1>2499 / 2500 — 巨大盤面のたった1マス</h1><p class="lead">45秒広告の画面イメージ8枚。すべて1400×1800。50×50＝2500マスを実際に描画し、後半は中央付近に白い1マスだけを残しています。手・接触リング・矢印・残像でスワイプを示します。動画制作前の静止画確認用です。</p><section class="grid">${cards}</section><p class="links"><a href="overview.png">8枚の一覧PNG</a> ／ <a href="storyboard-2499-2500.zip" download>素材一式ZIP</a> ／ <a href="README.md">演出メモ</a> ／ <a href="manifest.json">寸法・マス数・制作記録</a></p></main><dialog id="viewer"><img id="large" alt="拡大画像"><button aria-label="閉じる" id="close">×</button></dialog><script>const v=document.querySelector('#viewer');document.querySelectorAll('[data-file]').forEach(b=>b.addEventListener('click',()=>{document.querySelector('#large').src=b.dataset.file;v.showModal()}));document.querySelector('#close').addEventListener('click',()=>v.close());v.addEventListener('click',e=>{if(e.target===v)v.close()});</script></html>`);

console.log(JSON.stringify({output:root,frames:scenes.map(s=>({file:s.file,width:s.width,height:s.height,filled:s.filled,empty:s.empty})),routeValidation:'passed',generatedSprite:{width:rawHand.info.width,height:rawHand.info.height,bounds:hb,tip:handTip}},null,2));
