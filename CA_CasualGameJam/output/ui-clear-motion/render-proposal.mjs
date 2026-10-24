import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn, spawnSync} from 'node:child_process';
import {once} from 'node:events';
import {createCanvas, loadImage, GlobalFonts} from 'file:///C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas/index.js';

const ROOT=path.dirname(fileURLToPath(import.meta.url));
const REPO=path.resolve(ROOT,'../..');
const W=720,H=1280,DW=941,DH=1672,FPS=60,DURATION=8;
const C={ink:'#071b52',cream:'#fff6d8',blue:'#0068ff',red:'#ff3e43',purple:'#8a25f5',cyan:'#0089dd',sky:'#2db5f3'};
GlobalFonts.registerFromPath(path.join(REPO,'Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf'),'Lilita');
const assets={};
for(const name of ['CreamPanel','HeaderPanel','PrimaryButton','NeutralButton','ResetBlue','ResetRed','ResetArrowGenerated','HomeButton','HomeIcon','NextIcon'])
  assets[name]=await loadImage(path.join(REPO,'Assets/UI/ToonBold',name+'.png'));
const canvas=createCanvas(W,H), ctx=canvas.getContext('2d');
const clamp=(v,a=0,b=1)=>Math.min(b,Math.max(a,v));
const lerp=(a,b,p)=>a+(b-a)*p;
const progress=(t,a,b)=>clamp((t-a)/(b-a));
const cubic=p=>p<.5?4*p*p*p:1-Math.pow(-2*p+2,3)/2;
const out=p=>1-Math.pow(1-clamp(p),3);
const back=p=>{p=clamp(p);const c=1.28;return 1+(c+1)*Math.pow(p-1,3)+c*Math.pow(p-1,2);};
const TAU=Math.PI*2;
function group(x,y,s=1,rotation=0,alpha=1,draw){
  if(alpha<=0 || s<=0)return;
  ctx.save();ctx.globalAlpha*=clamp(alpha);ctx.translate(x,y);ctx.rotate(rotation);ctx.scale(s,s);draw();ctx.restore();
}
function round(x,y,w,h,r,fill,stroke=null,lw=1){ctx.beginPath();ctx.roundRect(x,y,w,h,r);if(fill){ctx.fillStyle=fill;ctx.fill();}if(stroke){ctx.strokeStyle=stroke;ctx.lineWidth=lw;ctx.stroke();}}
function circle(x,y,r,fill){ctx.beginPath();ctx.arc(x,y,r,0,TAU);ctx.fillStyle=fill;ctx.fill();}
function line(points,color,width,reveal=1){
  if(reveal<=0)return;
  const lengths=points.slice(1).map((p,i)=>Math.hypot(p[0]-points[i][0],p[1]-points[i][1]));
  let remain=lengths.reduce((a,b)=>a+b,0)*clamp(reveal);
  ctx.beginPath();ctx.moveTo(...points[0]);
  for(let i=0;i<lengths.length;i++){
    let p=clamp(remain/lengths[i]);ctx.lineTo(lerp(points[i][0],points[i+1][0],p),lerp(points[i][1],points[i+1][1],p));
    remain-=lengths[i];if(remain<=0)break;
  }
  ctx.lineJoin=ctx.lineCap='round';ctx.lineWidth=width;ctx.strokeStyle=color;ctx.stroke();
}
function paintPanel(target,name,x,y,w,h){
  // Reproduce ToonSkinBuilder's rounded panels as paths, avoiding raster slice seams in the movie.
  const definitions={CreamPanel:[C.cream,false,54],HeaderPanel:[C.cream,false,112],PrimaryButton:['#008bff',true,92],NeutralButton:['#444851',false,46],HomeButton:['#0096ff',false,49],ResetRed:[C.red,false,46],ResetBlue:[C.blue,false,46]};
  const [fill,rim,radius]=definitions[name],fit=Math.min(1,w/192,h/204),s=12*fit,body=h-s;
  const shape=(xx,yy,ww,hh,rr,color)=>{target.beginPath();target.roundRect(xx,yy,ww,hh,Math.max(0,Math.min(rr,ww/2,hh/2)));target.fillStyle=color;target.fill();};
  shape(x,y+s,w,body,radius*fit,C.cyan);shape(x,y,w,body,radius*fit,C.ink);
  if(rim)shape(x+s,y+s,w-2*s,body-2*s,(radius-12)*fit,C.cream);
  const inset=(rim?25:12)*fit;
  shape(x+inset,y+inset,w-2*inset,body-2*inset,(radius-(rim?25:12))*fit,fill);
}
function panel(name,x,y,w,h){paintPanel(ctx,name,x,y,w,h);}
function text(value,x,y,size,{outline=0,fill='#fff',shadow=false,max=900}={}){
  ctx.font=`${size}px Lilita`;const width=ctx.measureText(value).width;
  ctx.save();ctx.translate(x,y);ctx.scale(Math.min(1,max/width),1);ctx.textAlign='center';ctx.textBaseline='middle';ctx.lineJoin='round';
  if(shadow){ctx.strokeStyle=C.cyan;ctx.lineWidth=outline+3;ctx.strokeText(value,0,12);ctx.fillStyle=C.cyan;ctx.fillText(value,0,12);}
  if(outline){ctx.strokeStyle=C.ink;ctx.lineWidth=outline;ctx.strokeText(value,0,0);}
  ctx.fillStyle=fill;ctx.fillText(value,0,0);ctx.restore();
}
function sky(t){
  const g=ctx.createLinearGradient(0,0,0,DH);g.addColorStop(0,'#2db5f3');g.addColorStop(1,'#2ab9f0');ctx.fillStyle=g;ctx.fillRect(0,0,DW,DH);
  for(const [x,y,w]of[[-.025,.09,.20],[.99,.19,.18],[.02,.21,.12],[1.02,.71,.20],[-.035,.77,.18],[.98,.95,.19]]){
    ctx.save();ctx.translate(x*DW+Math.sin(t*.22+y*9)*5,y*DH);ctx.fillStyle='#aee5fa';const ww=w*DW;
    for(const[dx,dy,rx,ry]of[[-.29,0,.38,.22],[-.13,-.24,.33,.37],[.2,-.1,.32,.28]]){
      ctx.beginPath();ctx.ellipse(dx*ww,dy*ww,rx*ww,ry*ww,0,0,TAU);ctx.fill();
    }ctx.restore();
  }
}
const BOARD_W=885,BOARD_H=760.7895,UNIT=BOARD_W/5.7;
const pos=(col,row)=>[(col-2)*UNIT,(row-1.5)*UNIT-7];
const redRoute=[[4,0],[4,3],[3,3],[3,0],[2,0],[2,2]].map(p=>pos(...p));
const blueRoute=[[0,3],[0,0],[1,0],[1,3],[2,3],[2,1]].map(p=>pos(...p));
function boardLayer(){
  const layer=createCanvas(941,820);
  // Draw once at native design resolution; cache avoids re-rasterizing twenty tiles on every frame.
  const bctx=layer.getContext('2d');
  // Board has its own context so the main renderer remains stateless.
  bctx.save();bctx.translate(470.5,400);
  paintPanel(bctx,'CreamPanel',-BOARD_W/2,-BOARD_H/2,BOARD_W,BOARD_H);
  const bround=(x,y,w,h,r,fill,stroke,lw)=>{bctx.beginPath();bctx.roundRect(x,y,w,h,r);bctx.fillStyle=fill;bctx.fill();if(stroke){bctx.strokeStyle=stroke;bctx.lineWidth=lw;bctx.stroke();}};
  for(let r=0;r<4;r++)for(let c=0;c<5;c++){
    const [px,py]=pos(c,r),mixed=c===2&&(r===1||r===2),blue=c<2||(c===2&&r===3),fill=mixed?'#d9a8fc':blue?'#9bc5ff':'#ff9fa2';
    bround(px-UNIT*.46,py-UNIT*.46,UNIT*.92,UNIT*.92,14,fill,C.ink,4.2);
    if(mixed||(c===2&&(r===0||r===3)))bround(px-UNIT*.46,py-UNIT*.46,UNIT*.92,UNIT*.92,14,fill,mixed?C.purple:blue?C.blue:C.red,10);
  }
  const bl=(pts,color,width)=>{bctx.beginPath();bctx.moveTo(...pts[0]);pts.slice(1).forEach(p=>bctx.lineTo(...p));bctx.strokeStyle=color;bctx.lineWidth=width;bctx.lineCap=bctx.lineJoin='round';bctx.stroke();};
  bl(redRoute,C.red,UNIT*.24);bl(blueRoute,C.blue,UNIT*.24);
  bl([pos(2,1),pos(2,2)],C.purple,UNIT*.24);
  for(const [p,color]of[[redRoute[0],C.red],[blueRoute[0],C.blue]]){bctx.beginPath();bctx.arc(...p,UNIT*.27,0,TAU);bctx.fillStyle=color;bctx.fill();}
  bctx.restore();return layer;
}
const board=boardLayer();
await fs.writeFile(path.join(ROOT,'board-source.png'),board.toBuffer('image/png'));
function drawBoard(t,x,y,scale,alpha=1){
  group(x,y,scale,0,alpha,()=>{
    ctx.drawImage(board,-470.5,-400);
    const q=progress(t,.18,.68);
    if(q>0&&q<1){
      ctx.save();ctx.beginPath();ctx.roundRect(-BOARD_W/2+12,-BOARD_H/2+12,BOARD_W-24,BOARD_H-24,60);ctx.clip();
      const beam=lerp(-700,800,q),g=ctx.createLinearGradient(beam-120,0,beam+120,0);g.addColorStop(0,'#ffffff00');g.addColorStop(.5,'#ffffff88');g.addColorStop(1,'#ffffff00');ctx.fillStyle=g;ctx.transform(1,0,-.35,1,0,0);ctx.fillRect(beam-120,-BOARD_H/2,240,BOARD_H);ctx.restore();
    }
  });
}
function gameplayChrome(alpha){
  group(0,0,1,0,alpha,()=>{
    panel('HeaderPanel',164,74,613,195);text('STAGE 3',470.5,169,129,{outline:18,max:570});text('TRACE & MIX',470.5,325,59,{outline:11,shadow:true,max:610});
    panel('HomeButton',778,20,140,150);ctx.drawImage(assets.HomeIcon,804,45,88,94);
    panel('HeaderPanel',214,1156,513,140);circle(304,1225,42,C.red);text('+',384,1227,82,{fill:C.ink});circle(472,1225,42,C.blue);text('=',552,1227,82,{fill:C.ink});circle(639,1225,42,C.purple);
    panel('CreamPanel',24,1316,893,315);text('RESET',470.5,1356,47,{fill:C.ink});
    for(const[x,name,all]of[[84,'NeutralButton',true],[351,'ResetBlue',false],[618,'ResetRed',false]]){
      panel(name,x,1392,240,212);ctx.drawImage(assets.ResetArrowGenerated,x+52,all?1404:1426,136,136);if(all)text('ALL',x+120,1558,44,{outline:5});
    }
  });
}
function badge(t,scale=1){
  const ring=progress(t,.60,1.10);
  if(ring<1&&ring>0){ctx.save();ctx.globalAlpha*=1-ring;ctx.strokeStyle=C.cream;ctx.lineWidth=8;ctx.beginPath();ctx.arc(0,0,125+ring*70,0,TAU);ctx.stroke();ctx.restore();}
  circle(0,11,125,C.cyan);circle(0,0,123,C.ink);circle(0,0,113,C.cream);
  const merge=out(progress(t,.68,1.02)),check=out(progress(t,.91,1.20));
  if(merge<1){
    const a=1-progress(t,.93,1.05);group(0,0,1,0,a,()=>{circle(lerp(-73,-20,merge),lerp(-12,38,merge),22,C.red);circle(lerp(73,-20,merge),lerp(-12,38,merge),22,C.blue);});
  }
  const left=[[-62,-6],[-20,38]],right=[[-20,38],[59,-46]];
  line(left,C.ink,66,clamp(check*2));line(right,C.ink,66,clamp(check*2-1));
  line(left,C.red,44,clamp(check*2));line(right,C.blue,44,clamp(check*2-1));
  if(check>.5)circle(-20,38,22*back(progress(check,.5,.76)),C.purple);
}
const random=(()=>{let n=4073;return()=>{n=(n*1664525+1013904223)>>>0;return n/4294967296;};})();
const confetti=[];
for(let burst=0;burst<2;burst++)for(let i=0;i<(burst===0?92:42);i++){
  const side=i%2===0?-1:1;
  confetti.push({start:burst===0?1.10:2.45,x:burst===0?DW/2:side<0?40:DW-40,y:burst===0?700:700,vx:burst===0?(random()-.5)*1040:-side*(150+random()*400),vy:-290-random()*630,g:600+random()*170,size:7+random()*9,spin:(random()-.5)*11,rot:random()*TAU,color:[C.red,C.blue,C.purple,C.cream,'#ffd928'][i%5],life:2.8+random()*.9});
}
function papers(t,front=false){
  for(const[p,i]of confetti.map((p,i)=>[p,i])){
    if((i%3===0)!==front)continue;const dt=t-p.start;if(dt<0||dt>p.life)continue;
    const x=p.x+p.vx*dt*.66,y=p.y+p.vy*dt+p.g*dt*dt*.5;
    // Keep the settled board and the next-stage text unobstructed.
    if(front&&t>2.1&&x>75&&x<870&&y>235&&y<1470)continue;
    group(x,y,1,p.rot+p.spin*dt,Math.min(1,(p.life-dt)/.6),()=>round(-p.size/2,-p.size,p.size*Math.max(.25,Math.abs(Math.cos(dt*5))),p.size*2,2,p.color));
  }
}
function star(x,y,size,alpha,angle=0){group(x,y,size,angle,alpha,()=>{
  ctx.beginPath();ctx.moveTo(0,-1);ctx.quadraticCurveTo(.20,-.20,1,0);ctx.quadraticCurveTo(.20,.20,0,1);ctx.quadraticCurveTo(-.20,.20,-1,0);ctx.quadraticCurveTo(-.20,-.20,0,-1);ctx.fillStyle=C.cream;ctx.fill();
});}
function sparkle(t,start,x,y,size){const p=progress(t,start,start+.8);if(p<=0||p>=1)return;star(x,y,size*Math.sin(Math.PI*p),Math.sin(Math.PI*p),p*.7);}
function clearLetters(t,x,y,scale){
  const value='STAGE CLEAR!';ctx.font='112px Lilita';const widths=[...value].map(c=>ctx.measureText(c).width),total=widths.reduce((a,b)=>a+b,0),fit=Math.min(1,750/total);
  group(x,y,scale*fit,0,1,()=>{
    let offset=-total/2;
    for(let i=0;i<value.length;i++){
      const p=progress(t,1.07+i*.022,1.45+i*.022),v=back(p),s=lerp(.55,1,v);
      if(p>0)group(offset+widths[i]/2,lerp(65,0,v),s,lerp((i%2?1:-1)*.10,0,p),out(p),()=>text(value[i],0,0,112,{outline:20,shadow:true}));offset+=widths[i];
    }
  });
}
function sunburst(t,x,y,scale,alpha){
  group(x,y,scale,t*.08,alpha,()=>{
    const glow=ctx.createRadialGradient(0,0,60,0,0,330);glow.addColorStop(0,'#fff6d866');glow.addColorStop(1,'#fff6d800');ctx.fillStyle=glow;ctx.fillRect(-360,-360,720,720);
    for(let i=0;i<12;i++){ctx.save();ctx.rotate(i*TAU/12);ctx.fillStyle=i%2?'#fff6d81c':'#ffffff12';ctx.beginPath();ctx.moveTo(0,0);ctx.lineTo(-35,-320);ctx.lineTo(35,-320);ctx.closePath();ctx.fill();ctx.restore();}
  });
}
function nextButton(t){
  const p=progress(t,2.82,3.18);if(p<=0)return;
  const rest=1+Math.sin(Math.max(0,t-3.18)*TAU/2.8)*.008;
  const s=lerp(.80,1,back(p))*rest,y=lerp(1365,1306,out(p));
  group(470.5,y,s,0,out(p),()=>{
    panel('PrimaryButton',-355,-92,710,184);
    text('STAGE 4',-51,-1,78,{max:540});
    const arrow=assets.NextIcon,ox=t>3.2?Math.sin((t-3.2)*TAU/1.6)*5:0;ctx.drawImage(arrow,239+ox,-46,82,82);
    const shine=(start)=>{const q=progress(t,start,start+.65);if(q<=0||q>=1)return;
      ctx.save();ctx.beginPath();ctx.roundRect(-329,-69,658,129,47);ctx.clip();ctx.transform(1,0,-.4,1,0,0);const sx=lerp(-540,500,q),g=ctx.createLinearGradient(sx-45,0,sx+45,0);g.addColorStop(0,'#ffffff00');g.addColorStop(.5,'#ffffff66');g.addColorStop(1,'#ffffff00');ctx.fillStyle=g;ctx.fillRect(sx-45,-120,90,240);ctx.restore();};
    shine(3.42);shine(6.62);
  });
}
function render(t){
  ctx.resetTransform();ctx.clearRect(0,0,W,H);ctx.scale(W/DW,H/DH);
  sky(t);
  const enter=out(progress(t,.48,.82)),transition=cubic(progress(t,1.98,2.78));
  const scrim=enter*lerp(.76,.37,transition);
  const chromeAlpha=1-out(progress(t,.40,.70));gameplayChrome(chromeAlpha);
  // The solved board stays in the same space while the celebration overlays it.
  if(transition===0)drawBoard(t,470.5,747.8,1+Math.sin(progress(t,.05,.52)*Math.PI)*.018,1-out(progress(t,1.65,1.95)));
  ctx.fillStyle=`rgba(0,18,49,${scrim})`;ctx.fillRect(0,0,DW,DH);
  papers(t,false);
  if(transition>0){
    const card=out(progress(t,2.0,2.66));
    group(470.5,871+lerp(85,0,card),lerp(.88,1,back(card)),0,card,()=>panel('CreamPanel',-407.5,-580,815,1160));
    const settle=1+Math.sin(progress(t,2.72,3.12)*Math.PI)*.024;
    drawBoard(t,470.5,lerp(747.8,916,transition),lerp(1,.6,transition)*settle,out(progress(t,2.30,2.65)));
  }
  const badgeIn=progress(t,.60,.98),badgeScale=lerp(0,1.46,back(badgeIn));
  const float=t>2.78?Math.sin((t-2.78)*TAU/2.6)*3:0;
  const by=lerp(660,285,transition)+float,bScale=lerp(badgeScale,1,transition);
  const badgeRot=(1-transition)*Math.sin(badgeIn*TAU)*.075;
  sunburst(t,470.5,by,bScale,enter*(1-transition)*.8);
  group(470.5,by,bScale,badgeRot,enter,()=>badge(t));
  const pulse=t>2.78?1+Math.sin((t-2.78)*TAU/1.3)*.035:1;
  clearLetters(t,470.5,lerp(940,442,transition),lerp(1.04,1,transition)*pulse);
  const detail=out(progress(t,2.48,2.88));
  group(0,lerp(28,0,detail),1,0,detail,()=>{
    text('STAGE 3',470.5,551,68,{outline:11,max:570});
    text('Every color in its place.',470.5,628,38,{fill:C.ink,max:740});
  });
  nextButton(t);
  papers(t,true);
  sparkle(t,1.02,257,555,28);sparkle(t,1.12,695,640,32);sparkle(t,1.30,230,810,18);
  sparkle(t,2.72,208,745,22);sparkle(t,2.87,731,1042,26);
  sparkle(t,4.45,354,199,17);sparkle(t,4.82,614,301,21);sparkle(t,6.05,270,257,16);sparkle(t,6.35,659,191,18);
  return canvas;
}
await fs.mkdir(path.join(ROOT,'frames'),{recursive:true});
const checkpoints=[.25,.85,1.40,2.20,2.65,3.25,4.10,6.10];
for(const t of checkpoints)await fs.writeFile(path.join(ROOT,'frames',`proposal-${t.toFixed(2)}.png`),render(t).toBuffer('image/png'));
const sheet=createCanvas(1280,1216),sctx=sheet.getContext('2d');sctx.fillStyle=C.ink;sctx.fillRect(0,0,1280,1216);
for(let i=0;i<checkpoints.length;i++){
  const x=16+(i%4)*316,y=16+Math.floor(i/4)*600,im=await loadImage(path.join(ROOT,'frames',`proposal-${checkpoints[i].toFixed(2)}.png`));
  sctx.drawImage(im,x,y,300,533.333);sctx.font='26px Lilita';sctx.fillStyle=C.cream;sctx.fillText(`${checkpoints[i].toFixed(2)} s`,x+8,y+572);
}
await fs.writeFile(path.join(ROOT,'storyboard.png'),sheet.toBuffer('image/png'));
await fs.writeFile(path.join(ROOT,'poster.png'),render(3.65).toBuffer('image/png'));
if(process.argv.includes('--stills-only')){console.log('Stills rendered');process.exit(0);}
const py='C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';
const locate=spawnSync(py,['-c','import sys;sys.path.insert(0,sys.argv[1]);import imageio_ffmpeg;print(imageio_ffmpeg.get_ffmpeg_exe())',path.join(ROOT,'_tools')],{encoding:'utf8'});
if(locate.status!==0)throw new Error(locate.stderr);
const ffmpeg=locate.stdout.trim();
const video=spawn(ffmpeg,['-hide_banner','-loglevel','warning','-y','-f','rawvideo','-pixel_format','rgba','-video_size',`${W}x${H}`,'-framerate',String(FPS),'-i','pipe:0','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart','-metadata','title=Blend Trails - Stage Clear Motion Proposal',path.join(ROOT,'blend-trails-clear-proposal.mp4')],{stdio:['pipe','ignore','pipe']});
video.stderr.on('data',v=>process.stderr.write(v));
let videoError=null;video.on('error',e=>videoError=e);
const done=once(video,'close');
for(let i=0;i<FPS*DURATION;i++){
  const frame=render(i/FPS).data();
  if(!video.stdin.write(frame))await once(video.stdin,'drain');
  if(i%60===0)console.log(`Rendering ${i/FPS}/${DURATION}s`);
}
video.stdin.end();const [status]=await done;if(status!==0||videoError)throw videoError??new Error(`FFmpeg exited ${status}`);
const spec={title:'Blend Trails — クリア演出案',format:{width:W,height:H,fps:FPS,seconds:DURATION,codec:'H.264',audio:false},reference:{file:'Screenrecorder-2026-10-24-17-28-21-912.mp4',analyzedSeconds:[18,23],observations:['クリアバッジの弾む登場','背景を暗くして祝福を強調','紙吹雪を残して結果画面へ遷移','結果画面の要素と次ステージボタンが順に登場']},timeline:[{seconds:[0,.60],motion:'完成盤面の小さな弾みと光のスイープ'},{seconds:[.60,1.10],motion:'赤と青が集まり、混色チェックがバウンスして登場'},{seconds:[1.07,1.72],motion:'STAGE CLEAR!を一文字ずつ弾ませ、紙吹雪を放射'},{seconds:[1.98,2.78],motion:'バッジと見出しを上部へ移動。クリーム色カードを表示し、完成盤面を60%へ縮小'},{seconds:[2.48,2.88],motion:'現在ステージとメッセージがフェードイン'},{seconds:[2.82,3.18],motion:'次ステージのSTAGE 4ボタンが弾んで登場。3.2秒で操作可能にする想定'},{seconds:[3.18,8],motion:'見出しの拡大縮小、バッジの微小な上下動、星のきらめき。ボタンを一度光らせ、矢印を軽く動かす'}],constraints:{gameCodeChanged:false,progressChanged:false,homeButton:false,solvedBoard:true,nextStageNumber:4,foreignMascotOrScoreAdded:false},assets:'既存のToonBold PNG・Lilita Oneフォント・Stage3の正解盤面を使った動画モックアップ。Unity実装前。'};
await fs.writeFile(path.join(ROOT,'motion-spec.json'),JSON.stringify(spec,null,2));
console.log('Video rendered:',path.join(ROOT,'blend-trails-clear-proposal.mp4'));
