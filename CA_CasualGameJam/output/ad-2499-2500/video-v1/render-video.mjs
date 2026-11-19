import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {once} from 'node:events';
import {
  createCanvas,W,H,NAVY,CREAM,palette,boardA,boardB,routeA,routeB,hole,key,pos,point,fullView,
  rr,line,text,panel,headline,counter,bottomLabel,background,drawBoard,drawHand,ring,arrow,sparkle,pulseHole,logoImage,rng
} from './board-renderer.mjs';

const root=path.dirname(fileURLToPath(import.meta.url));
const project=path.resolve(root,'../../..');
const ffmpeg=path.join(project,'output/ui-clear-motion/_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const FPS=30,DURATION=45,FRAMES=FPS*DURATION;
const clamp=(v,a=0,b=1)=>Math.max(a,Math.min(b,v));
const lerp=(a,b,p)=>a+(b-a)*p;
const ease=p=>{p=clamp(p);return p*p*(3-2*p);};
const cubic=p=>1-Math.pow(1-clamp(p),3);
const back=p=>{p=clamp(p);return 1+2.70158*Math.pow(p-1,3)+1.70158*Math.pow(p-1,2);};
const mixView=(a,b,p)=>({pitch:Math.exp(lerp(Math.log(a.pitch),Math.log(b.pitch),p)),x:0,y:0});
const closeCenter={x:700,y:974},ctaCenter={x:700,y:883};
const clipFull={x:128,y:413,w:1144,h:1144,r:35};
const clipClose={x:61,y:476,w:1278,h:1066,r:52};
const clipCta={x:113,y:370,w:1174,h:1040,r:52};
const nearView={x:700-25.5*153,y:974-25.5*153,pitch:153};
const ctaView={x:700-25.5*130,y:883-25.5*130,pitch:130};
const interpolateBox=(a,b,p)=>Object.fromEntries(Object.keys(a).map(k=>[k,lerp(a[k],b[k],p)]));
const frameCanvas=createCanvas(W,H),ctx=frameCanvas.getContext('2d');
const sky=createCanvas(W,H);background(sky.getContext('2d'));
const checkpoints=[0,1.3,3.7,5.1,10,15.5,17.25,18.2,19.4,24,30.9,32.9,34.5,35.7,37.5,38,39.9,41.7,44.5];
const sampleFrameSet=new Map(checkpoints.map(t=>[Math.round(t*FPS),t]));
const random=rng(20261025);
const particles=Array.from({length:90},(_,i)=>({x:80+random()*1240,y:-220-random()*450,vy:245+random()*240,vx:(random()-.5)*120,size:10+random()*13,spin:random()*5,phase:random()*6.28,color:palette[i%6]}));
const stateLog=[];

function routePosition(model,count,view){
  const f=clamp(count-1,0,2499),i=Math.floor(f),frac=f-i;
  const a=point(model.route[i],view),b=point(model.route[Math.min(i+1,2499)],view);
  return {x:lerp(a.x,b.x,frac),y:lerp(a.y,b.y,frac)};
}

// Draw the authored grid plus a continuously growing leading segment between cells.
function board(ctx,model,count,view,box=null){
  const integer=Math.floor(count),fraction=count-integer;
  if(box){
    panel(ctx,box.x,box.y,box.w,box.h,box.r);
    drawBoard(ctx,model,integer,view,{clip:{x:box.x+22,y:box.y+22,w:box.w-44,h:box.h-44,r:Math.max(18,box.r-20)}});
  }else drawBoard(ctx,model,integer,view);
  if(fraction>.001&&integer>0&&integer<2500){
    const previous=model.route[integer-1],next=model.route[integer];
    if(model.color[previous]===model.color[next]){
      ctx.save();
      const clip=box?{x:box.x+22,y:box.y+22,w:box.w-44,h:box.h-44,r:box.r-20}:clipFull;
      ctx.beginPath();ctx.roundRect(clip.x,clip.y,clip.w,clip.h,clip.r);ctx.clip();
      const a=point(previous,view),b=point(next,view),end={x:lerp(a.x,b.x,fraction),y:lerp(a.y,b.y,fraction)};
      line(ctx,[a,end],model.color[next],view.pitch*.53);
      line(ctx,[a,end],'rgba(255,255,255,0.22)',Math.max(1,view.pitch*.075));ctx.restore();
    }
  }
}

function handAngle(tip,isClose){
  if(isClose){if(tip.x<650&&tip.y<1100)return .95;if(tip.x<765&&tip.y<935)return -.85;return .08;}
  return tip.x>1070?.68:tip.x<250?-.14:0;
}
function movingHand(ctx,model,count,view,t,{height=300,close=false,opacity=1,reverse=false,guide=true}={}){
  if(opacity<=0)return;
  const tip=routePosition(model,count,view),past=routePosition(model,clamp(count+(reverse?.42:-.42),1,2500),view);
  const dx=tip.x-past.x,dy=tip.y-past.y,d=Math.hypot(dx,dy)||1;
  ctx.save();ctx.globalAlpha=opacity;
  if(guide&&view.pitch>60){
    const start={x:tip.x-dx/d*145,y:tip.y-dy/d*145},end={x:tip.x-dx/d*56,y:tip.y-dy/d*56};
    arrow(ctx,[start,end],'#FFFFFF',8);
  }
  ring(ctx,tip.x,tip.y,view.pitch>60?25:18);
  const angle=handAngle(tip,close);
  drawHand(ctx,tip,height,{angle,opacity,ghosts:[{x:tip.x-dx/d*50,y:tip.y-dy/d*50},{x:tip.x-dx/d*25,y:tip.y-dy/d*25}]});
  ctx.restore();
  return tip;
}

function countA(t){
  if(t<4)return lerp(480,484,t/4);
  if(t<6)return 484;
  if(t<15)return lerp(484,2450,(t-6)/9);
  if(t<17)return lerp(2450,2500,(t-15)/2);
  return 2500;
}
function countB(t){
  if(t<19.25)return 0;
  if(t<30.4)return lerp(0,2491,(t-19.25)/11.15);
  if(t<30.85)return lerp(2491,2499,(t-30.4)/.45);
  if(t<35)return 2499;
  if(t<35.85)return lerp(2499,2491,(t-35)/.85);
  if(t<36.2)return 2491;
  if(t<36.65)return lerp(2491,2492,(t-36.2)/.45);
  if(t<37.15)return lerp(2492,2493,(t-36.65)/.5);
  if(t<37.5)return 2493;
  if(t<38.0)return lerp(2493,2494,(t-37.5)/.5);
  if(t<39.8)return lerp(2494,2499,(t-38)/1.8);
  return 2499;
}
function holeRing(ctx,view,t,strength=1){
  const p=point(hole,view),r=view.pitch*(.72+.045*Math.sin(t*5));
  ctx.save();ctx.globalAlpha=strength*(.75+.25*Math.sin(t*4)**2);
  ctx.strokeStyle='#FF364F';ctx.lineWidth=7;ctx.setLineDash([16,11]);ctx.lineDashOffset=-t*28;
  ctx.beginPath();ctx.arc(p.x,p.y,r,0,Math.PI*2);ctx.stroke();ctx.restore();
}
function animatedConfetti(ctx,t){
  const age=t-17.4;if(age<0)return;
  for(const p of particles){
    const x=p.x+p.vx*age+Math.sin(p.phase+age*4)*24,y=p.y+p.vy*age+age*age*62;
    ctx.save();ctx.translate(x,y);ctx.rotate(p.spin*age+p.phase);ctx.globalAlpha=clamp((19.1-t)*2);
    rr(ctx,-p.size*.4,-p.size*.8,p.size*.8,p.size*1.6,3,p.color);ctx.restore();
  }
}
function countHud(ctx,count,y=307,alert=false,small=false,pop=0){
  ctx.save();ctx.translate(700,y);ctx.scale(1+pop,1+pop);ctx.translate(-700,-y);counter(ctx,`${Math.floor(count)} / 2500`,y,{alert,small});ctx.restore();
}
function renderA(t){
  const count=countA(t);let view,box=null;
  if(t<4){
    const pitch=163,tip=pos(routeA[483]);view={x:850-(tip.c+.5)*pitch,y:930-(tip.r+.5)*pitch,pitch};
    box={x:61,y:355,w:1278,h:1168,r:52};headline(ctx,['FILL EVERY TILE.'],145,111);
  }else if(t<6){
    const p=ease((t-4)/2),pitch=Math.exp(lerp(Math.log(163),Math.log(22),p)),tip=pos(routeA[483]);
    const startAnchor={x:850,y:930},endAnchor=point(routeA[483],fullView);
    view={pitch,x:lerp(startAnchor.x,endAnchor.x,p)-(tip.c+.5)*pitch,y:lerp(startAnchor.y,endAnchor.y,p)-(tip.r+.5)*pitch};
    box=interpolateBox({x:61,y:355,w:1278,h:1168,r:52},clipFull,p);
    if(t<4.2){ctx.save();ctx.globalAlpha=1-(t-4)/.2;headline(ctx,['FILL EVERY TILE.'],145,111);ctx.restore();}
    if(t>=4.2){ctx.save();ctx.globalAlpha=clamp((t-4.2)/.2);headline(ctx,['KEEP GOING...'],137,118);ctx.restore();}
    ctx.save();ctx.globalAlpha=clamp((t-4.45)/.3);countHud(ctx,count);ctx.restore();
  }else{
    view=fullView;
    if(t<17.4)headline(ctx,['KEEP GOING...'],137,118);
    else{
      const pop=back((t-17.4)/.42);ctx.save();ctx.translate(700,137);ctx.scale(Math.max(.01,pop),Math.max(.01,pop));text(ctx,'PERFECT!',0,0,151);ctx.restore();
    }
    countHud(ctx,count,307,false,false,t>=17&&t<17.4?Math.sin((t-17)/.4*Math.PI)*.035:0);
  }
  const pulse=t>=17&&t<17.4?Math.sin((t-17)/.4*Math.PI)*.014:0;
  ctx.save();if(pulse){ctx.translate(700,985);ctx.scale(1+pulse,1+pulse);ctx.translate(-700,-985);}
  board(ctx,boardA,count,view,box);ctx.restore();
  const opacity=t<17?1:clamp(1-(t-17)/.26);
  movingHand(ctx,boardA,count,view,t,{height:lerp(440,280,clamp((t-4)/2)),opacity});
  bottomLabel(ctx,'50 × 50',1648);
  if(t>=17.4){animatedConfetti(ctx,t);const k=.8+.2*Math.sin(t*8);sparkle(ctx,91,497,28*k);sparkle(ctx,1314,1396,31*k);sparkle(ctx,1127,1631,35*k);}
  return {board:'A',count,view,phase:t<4?'close-swipe':t<6?'zoom-out':t<17?'fill':'perfect'};
}
function renderB(t){
  const count=countB(t);let view=fullView,box=null,phase='fill-new-board';
  if(t<32){
    if(t<30.85){headline(ctx,['KEEP GOING...'],137,118);countHud(ctx,count);}
    else{const shake=t<31.3?Math.sin((t-30.85)*45)*3:0;ctx.save();ctx.translate(shake,0);headline(ctx,['2499 / 2500'],151,136);ctx.restore();phase='2499-stop';}
    board(ctx,boardB,count,view);
    const active=t>=19.25&&t<30.85;const opacity=active?clamp((t-19.25)*5):t>=30.85?clamp(1-(t-30.85)/.5):0;
    movingHand(ctx,boardB,Math.max(1,count),view,t,{height:280,opacity});bottomLabel(ctx);
  }else if(t<41){
    const p=ease((t-32)/1.7),pitch=Math.exp(lerp(Math.log(22),Math.log(153),p));
    const before=point(hole,fullView),cx=lerp(before.x,700,p),cy=lerp(before.y,974,p);
    view={pitch,x:cx-25.5*pitch,y:cy-25.5*pitch};box=interpolateBox(clipFull,clipClose,p);
    headline(ctx,['HOW DID YOU','MISS THAT?'],120,108);
    countHud(ctx,count,lerp(307,361,p),true,p>.25,t>=39.8?Math.sin(t*9)*.012:0);
    board(ctx,boardB,count,view,box);
    if(t>33)holeRing(ctx,view,t,clamp((t-33)/.7));
    if(t>=34.2&&t<35){
      const enter=cubic((t-34.2)/.55);drawHand(ctx,{x:lerp(1430,1063,enter),y:lerp(1630,1246,enter)},310,{angle:.18});
    }else if(t>=35&&t<39.8){
      movingHand(ctx,boardB,Math.max(2491,count),view,t,{height:290,close:true,reverse:t<35.85});
    }else if(t>=39.8){
      const exit=clamp((t-40.1)/.45);movingHand(ctx,boardB,2499,view,t,{height:290,close:true,opacity:1-exit,guide:false});
    }
    if(t>=37.5&&t<38.3){
      const turn=point(key(26,25),view);ctx.save();ctx.globalAlpha=clamp((38.3-t)*3);for(let i=0;i<3;i++){const a=-Math.PI/2+(i-1)*.42;line(ctx,[{x:turn.x+Math.cos(a)*52,y:turn.y+Math.sin(a)*52},{x:turn.x+Math.cos(a)*76,y:turn.y+Math.sin(a)*76}],'#E7374E',7);}ctx.restore();
    }
    bottomLabel(ctx,'50 × 50',1663);phase=t<33.7?'zoom-to-hole':t<35?'reveal-hole':t<35.85?'rewind':t<39.8?'wrong-turn':'missed-again';
  }else{
    const p=ease((t-41)/.6),pitch=lerp(153,130,p),cy=lerp(974,883,p);
    view={pitch,x:700-25.5*pitch,y:cy-25.5*pitch};box=interpolateBox(clipClose,clipCta,p);
    ctx.save();ctx.globalAlpha=1-p;headline(ctx,['HOW DID YOU','MISS THAT?'],120,108);ctx.restore();
    ctx.save();ctx.globalAlpha=p;headline(ctx,['CAN YOU GET','ALL 2500?'],112,118);ctx.restore();
    board(ctx,boardB,2499,view,box);holeRing(ctx,view,t);
    const enter=cubic((t-41.3)/.6);drawHand(ctx,{x:lerp(1230,884,enter),y:lerp(1340,1057,enter)+Math.sin(t*3)*5},280,{angle:.16,opacity:enter});
    const arrival=back((t-41.15)/.65),buttonY=lerp(1840,1488,arrival),pulse=1+.018*Math.sin((t-42)*5.2);
    ctx.save();ctx.translate(700,buttonY+84);ctx.scale(pulse,pulse);ctx.translate(-700,-(buttonY+84));
    rr(ctx,310,buttonY+18,780,168,63,'#0C77B5');rr(ctx,310,buttonY,780,168,63,'#FFCF36',NAVY,14);text(ctx,'PLAY NOW',700,buttonY+82,109,NAVY,false);
    // Periodic clipped sheen gives the CTA a soft sweep.
    if(t>42){ctx.save();ctx.beginPath();ctx.roundRect(319,buttonY+9,762,150,54);ctx.clip();const sx=210+((t-42)%2)*600;ctx.fillStyle='rgba(255,255,255,0.22)';ctx.beginPath();ctx.moveTo(sx,buttonY);ctx.lineTo(sx+90,buttonY);ctx.lineTo(sx+10,buttonY+168);ctx.lineTo(sx-80,buttonY+168);ctx.fill();ctx.restore();}
    ctx.restore();
    const lw=190,lh=lw/(logoImage.width/logoImage.height);ctx.save();ctx.globalAlpha=p;ctx.drawImage(logoImage,700-lw/2,1718-lh/2,lw,lh);ctx.restore();phase='cta';
  }
  return {board:'B',count,view,phase};
}
function render(t){
  ctx.clearRect(0,0,W,H);ctx.drawImage(sky,0,0);
  const state=t<19?renderA(t):renderB(t);
  // Short cream flash makes the new-board cut readable, then immediately clears.
  if(t>=19&&t<19.18){ctx.fillStyle=`rgba(255,247,223,${(1-(t-19)/.18)*.55})`;ctx.fillRect(0,0,W,H);}
  return state;
}

async function makeAudio(){
  const rate=48000,n=rate*DURATION,left=new Float32Array(n),right=new Float32Array(n),noise=rng(5025);
  function event(start,duration,synth,volume=1,pan=0){
    const offset=Math.round(start*rate),length=Math.round(duration*rate),lg=Math.sqrt((1-pan)*.5),rg=Math.sqrt((1+pan)*.5);
    for(let j=0;j<length&&offset+j<n;j++){if(offset+j<0)continue;const t=j/rate,v=synth(t,j)*volume;left[offset+j]+=v*lg;right[offset+j]+=v*rg;}
  }
  const midi=m=>440*Math.pow(2,(m-69)/12);
  const chords=[[62,66,69,74],[59,62,66,71],[55,59,62,67],[57,61,64,69]],beat=.5;
  for(let step=0;step<176;step++){
    const start=step*.25,bar=Math.floor(start/2)%4,chord=chords[bar],note=chord[[0,2,1,3,2,1,3,2][step%8]],f=midi(note);
    const duck=start>=31&&start<41?.38:1;
    event(start,.36,t=>(Math.sin(2*Math.PI*f*t)+.23*Math.sin(4*Math.PI*f*t))*Math.exp(-t*13)*Math.min(1,t*300),.043*duck,(step%4-1.5)*.20);
    if(step%4===0){const bf=midi(chord[0]-24);event(start,.55,t=>Math.sin(2*Math.PI*bf*t)*Math.exp(-t*7)*Math.min(1,t*150),.085*duck);}
    if(step%2===0)event(start,.17,t=>Math.sin(2*Math.PI*(58*t+65*(1-Math.exp(-t*30))/30))*Math.exp(-t*29),.11*duck);
    if(step%4===2)event(start,.1,t=>(noise()*2-1)*Math.exp(-t*50),.033*duck,.12);
    event(start+.125,.04,t=>(noise()*2-1)*Math.exp(-t*120),.012*duck,-.25);
  }
  // Gentle zipper ticks follow the active drawing passages, at a readable audio rate.
  for(const [from,to,interval] of [[.15,4,.19],[4,17,.105],[19.3,30.85,.105],[35,35.85,.11],[36.2,37.15,.17],[37.5,39.8,.20]]){
    let i=0;for(let at=from;at<to;at+=interval,i++){
      const freq=680+(i%8)*48;event(at,.036,t=>Math.sin(2*Math.PI*freq*t)*Math.exp(-t*135),.035,(i%5-2)*.15);
    }
  }
  for(const [i,note] of [74,78,81,86].entries())event(17.4+i*.1,.64,t=>Math.sin(2*Math.PI*midi(note)*t)*Math.exp(-t*5.5)*Math.min(1,t*500),.105,(i-1.5)*.2);
  for(const at of [31.0,39.95])event(at,.22,t=>(Math.sin(2*Math.PI*155*t)+.3*Math.sin(2*Math.PI*164*t))*Math.exp(-t*13)*Math.min(1,t*200),.075);
  event(37.72,.12,t=>Math.sin(2*Math.PI*(410*t-650*t*t))*Math.exp(-t*21),.042,.25);
  for(const [i,note] of [74,81,86].entries())event(41.5+i*.08,.48,t=>Math.sin(2*Math.PI*midi(note)*t)*Math.exp(-t*8),.06,(i-1)*.2);
  let peak=0;for(let i=0;i<n;i++){const fade=Math.min(1,i/(rate*.04),(n-i)/(rate*.8));left[i]*=fade;right[i]*=fade;peak=Math.max(peak,Math.abs(left[i]),Math.abs(right[i]));}
  const gain=.75/Math.max(.75,peak),wav=Buffer.alloc(44+n*4);wav.write('RIFF',0);wav.writeUInt32LE(36+n*4,4);wav.write('WAVEfmt ',8);wav.writeUInt32LE(16,16);wav.writeUInt16LE(1,20);wav.writeUInt16LE(2,22);wav.writeUInt32LE(rate,24);wav.writeUInt32LE(rate*4,28);wav.writeUInt16LE(4,32);wav.writeUInt16LE(16,34);wav.write('data',36);wav.writeUInt32LE(n*4,40);
  for(let i=0;i<n;i++){wav.writeInt16LE(Math.round(clamp(left[i]*gain,-1,1)*32767),44+i*4);wav.writeInt16LE(Math.round(clamp(right[i]*gain,-1,1)*32767),46+i*4);}
  await fs.writeFile(path.join(root,'original-audio.wav'),wav);
}

await fs.mkdir(path.join(root,'qa'),{recursive:true});
if(process.argv.includes('--preview')){
  for(const t of checkpoints){const state=render(t),name=`preview-${t.toFixed(2).replace('.','-')}.png`;await fs.writeFile(path.join(root,'qa',name),frameCanvas.toBuffer('image/png'));stateLog.push({time:t,...state});}
  await fs.writeFile(path.join(root,'qa/preview-states.json'),JSON.stringify(stateLog,null,2));
  const sheet=createCanvas(1750,Math.ceil(checkpoints.length/5)*502+70),sc=sheet.getContext('2d');sc.fillStyle='#EDF7FC';sc.fillRect(0,0,sheet.width,sheet.height);text(sc,'2499 / 2500 — 動画チェック',24,35,26,NAVY,false,'left','Meiryo');
  for(let i=0;i<checkpoints.length;i++){render(checkpoints[i]);const x=22+(i%5)*347,y=70+Math.floor(i/5)*502;sc.drawImage(frameCanvas,x,y,326,419);text(sc,`${checkpoints[i].toFixed(2)}s  ${stateLog[i].phase}`,x,y+444,15,NAVY,false,'left','Inter');}
  await fs.writeFile(path.join(root,'qa/animation-overview.png'),sheet.toBuffer('image/png'));console.log('Preview images complete.');
}else{
  await makeAudio();console.log('Original music and sound effects ready.');
  const output=path.join(root,'blend-trails-2499-2500-1400x1800-45s.mp4');
  const args=['-hide_banner','-loglevel','warning','-y','-f','rawvideo','-pixel_format','rgba','-video_size',`${W}x${H}`,'-framerate',String(FPS),'-i','pipe:0','-i',path.join(root,'original-audio.wav'),'-map','0:v:0','-map','1:a:0','-c:v','libx264','-preset','fast','-crf','18','-profile:v','high','-level:v','5.0','-pix_fmt','yuv420p','-r',String(FPS),'-af','loudnorm=I=-16:TP=-1.5:LRA=9','-c:a','aac','-b:a','192k','-ar','48000','-t',String(DURATION),'-movflags','+faststart','-color_primaries','bt709','-color_trc','bt709','-colorspace','bt709',output];
  const child=spawn(ffmpeg,args,{stdio:['pipe','ignore','pipe'],windowsHide:true});let log='';child.stderr.on('data',d=>{log+=d.toString();});child.stdin.on('error',()=>{});
  const finished=new Promise((resolve,reject)=>{child.once('error',reject);child.once('close',code=>code===0?resolve():reject(new Error(`FFmpeg exit ${code}: ${log}`)));});
  const started=Date.now();
  for(let frame=0;frame<FRAMES;frame++){
    const t=frame/FPS,state=render(t);
    if(sampleFrameSet.has(frame)){const name=`render-${t.toFixed(2).replace('.','-')}.png`;await fs.writeFile(path.join(root,'qa',name),frameCanvas.toBuffer('image/png'));stateLog.push({frame,time:t,...state});}
    const raw=frameCanvas.data();
    if(!child.stdin.write(raw))await once(child.stdin,'drain');
    if(frame%90===0)console.log(JSON.stringify({frame,total:FRAMES,seconds:t,progress:Math.round(frame/FRAMES*100),elapsedSeconds:Math.round((Date.now()-started)/1000),phase:state.phase}));
  }
  child.stdin.end();await finished;
  await fs.writeFile(path.join(root,'render.log'),log);
  await fs.writeFile(path.join(root,'qa/render-states.json'),JSON.stringify(stateLog,null,2));
  console.log(JSON.stringify({status:'rendered',output,width:W,height:H,fps:FPS,durationSeconds:DURATION,frames:FRAMES,elapsedSeconds:Math.round((Date.now()-started)/1000)}));
}
