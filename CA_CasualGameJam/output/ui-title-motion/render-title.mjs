import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {spawn, spawnSync} from 'node:child_process';
import {once} from 'node:events';
import {createCanvas, loadImage, GlobalFonts} from 'file:///C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas/index.js';

const root = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(root, '../..');
const ffmpeg = path.join(repo, 'output/ui-clear-motion/_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const W=720, H=1280, DW=941, DH=1672, FPS=60, DURATION=12, TAU=Math.PI*2;
const C={ink:'#071b52',cream:'#fff6d8',red:'#ff2438',yellow:'#ffdb08',orange:'#ff8b05',skyTop:'#2db5f3',skyBottom:'#2ab9f0'};
const canvas=createCanvas(W,H), ctx=canvas.getContext('2d');
GlobalFonts.registerFromPath(path.join(repo,'Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf'),'Lilita');
GlobalFonts.registerFromPath('C:/Windows/Fonts/YuGothM.ttc','Yu Gothic');

const assetPaths={logo:'Assets/UI/ToonBold/BlendTrailsLogo.png',mix:'Assets/UI/ToonBold/BlendTrailsMix.png',button:'Assets/UI/ToonBold/PrimaryButton.png',play:'Assets/UI/ToonBold/PlayIcon.png'};
const assets=Object.fromEntries(await Promise.all(Object.entries(assetPaths).map(async([k,p])=>[k,await loadImage(path.join(repo,p))])));
await fs.mkdir(path.join(root,'frames'),{recursive:true});
await fs.mkdir(path.join(root,'qa'),{recursive:true});

const tracked=['Assets/Scenes/Title.unity','Assets/Scripts/ToonTitleLayout.cs','Assets/Scripts/ToonSkyGraphic.cs','Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf',...Object.values(assetPaths)];
async function sourceHashes(){return Object.fromEntries(await Promise.all(tracked.map(async p=>[p,crypto.createHash('sha256').update(await fs.readFile(path.join(repo,p))).digest('hex')])));}
const hashesBefore=await sourceHashes();
await fs.writeFile(path.join(root,'source-hashes.json'),JSON.stringify(hashesBefore,null,2));

const clamp=(p,a=0,b=1)=>Math.min(b,Math.max(a,p));
const lerp=(a,b,p)=>a+(b-a)*p;
const progress=(t,a,b)=>clamp((t-a)/(b-a));
const out=p=>1-Math.pow(1-clamp(p),3);
const smooth=p=>{p=clamp(p);return p*p*(3-2*p);};
const back=p=>{p=clamp(p);return 1+2.4*Math.pow(p-1,3)+1.4*Math.pow(p-1,2);};
const mod=(v,n)=>((v%n)+n)%n;
function group(x,y,sx,sy,rotation,alpha,draw){
  if(alpha<=0||sx<=0||sy<=0)return;
  ctx.save();ctx.translate(x,y);ctx.rotate(rotation);ctx.scale(sx,sy);ctx.globalAlpha*=clamp(alpha);draw();ctx.restore();
}
function circle(x,y,r,fill){ctx.beginPath();ctx.arc(x,y,r,0,TAU);ctx.fillStyle=fill;ctx.fill();}
function line(points,color,width){
  if(points.length<2)return;
  ctx.beginPath();ctx.moveTo(...points[0]);points.slice(1).forEach(p=>ctx.lineTo(...p));
  ctx.lineWidth=width;ctx.strokeStyle=color;ctx.lineCap=ctx.lineJoin='round';ctx.stroke();
}
function star(x,y,r,alpha,rotation=0){group(x,y,r,r,rotation,alpha,()=>{
  ctx.beginPath();ctx.moveTo(0,-1);ctx.quadraticCurveTo(.17,-.17,1,0);ctx.quadraticCurveTo(.17,.17,0,1);
  ctx.quadraticCurveTo(-.17,.17,-1,0);ctx.quadraticCurveTo(-.17,-.17,0,-1);ctx.fillStyle=C.cream;ctx.fill();
});}
function glow(x,y,r,color,alpha){
  ctx.save();ctx.globalAlpha*=alpha;const g=ctx.createRadialGradient(x,y,0,x,y,r);
  g.addColorStop(0,color);g.addColorStop(1,color+'00');ctx.fillStyle=g;ctx.fillRect(x-r,y-r,2*r,2*r);ctx.restore();
}
function sky(t){
  const g=ctx.createLinearGradient(0,0,0,DH);g.addColorStop(0,C.skyTop);g.addColorStop(1,C.skyBottom);ctx.fillStyle=g;ctx.fillRect(0,0,DW,DH);
  const clouds=[[-.025,.09,.20],[.99,.19,.18],[.02,.21,.12],[1.02,.71,.20],[-.035,.77,.18],[.98,.95,.19]];
  for(const [i,[x,y,width]] of clouds.entries()){
    const drift=Math.sin(t*TAU/6+i*.72)*11, bob=Math.sin(t*TAU/6+i*.47)*3;
    const ww=width*DW;ctx.save();ctx.translate(x*DW+drift,y*DH+bob);ctx.fillStyle='#aee5fa';
    for(const [dx,dy,rx,ry]of[[-.29,0,.38,.22],[-.13,-.24,.33,.37],[.20,-.10,.32,.28]]){
      ctx.beginPath();ctx.ellipse(dx*ww,dy*ww,rx*ww,ry*ww,0,0,TAU);ctx.fill();
    }ctx.restore();
  }
}
function buttonPanel(x,y,w,h){
  // Reproduce the existing ToonBold rounded panel as continuous paths so its
  // antialiased PNG slice edges cannot leave seams in the encoded animation.
  const rr=(xx,yy,ww,hh,r,fill)=>{ctx.beginPath();ctx.roundRect(xx,yy,ww,hh,r);ctx.fillStyle=fill;ctx.fill();};
  const body=h-12;
  rr(x,y+12,w,body,92,'#0089dd');rr(x,y,w,body,92,C.ink);
  rr(x+12,y+12,w-24,body-24,80,C.cream);rr(x+25,y+25,w-50,body-50,67,'#008bff');
}
const logoLayer=createCanvas(941,700),lctx=logoLayer.getContext('2d');
lctx.drawImage(assets.logo,(941-771)/2,(700-514)/2,771,514);
const logoShine=createCanvas(941,700),shctx=logoShine.getContext('2d');
function logo(t){
  const p=progress(t,.08,.86);if(p<=0)return;
  const q=back(p),settle=progress(t,.9,2.6),float=smooth(settle)*Math.sin((t-2.6)*TAU/6)*5;
  const scale=lerp(.70,1,q),y=lerp(353,420,q)+float;
  const rotation=lerp(-.065,0,out(p))+smooth(settle)*Math.sin((t-2.6)*TAU/6)*.0035;
  group(470.5,y,scale,scale,rotation,out(progress(t,.08,.32)),()=>{
    ctx.drawImage(logoLayer,-470.5,-350);
    // Rare, alpha-masked sheen follows the original transparent logo silhouette.
    const shine=mod(t-3.8,6)/.85;
    if(shine<1){
      shctx.clearRect(0,0,941,700);shctx.globalCompositeOperation='source-over';shctx.drawImage(logoLayer,0,0);
      shctx.globalCompositeOperation='source-in';const x=lerp(-180,1120,shine);
      const g=shctx.createLinearGradient(x-95,0,x+95,0);g.addColorStop(0,'#ffffff00');g.addColorStop(.5,'#ffffff66');g.addColorStop(1,'#ffffff00');
      shctx.fillStyle=g;shctx.fillRect(0,0,941,700);shctx.globalCompositeOperation='source-over';ctx.drawImage(logoShine,-470.5,-350);
    }
  });
  // Two brief sparkles frame the entrance, then sparse accents repeat every six seconds.
  for(const [at,x,yy,r]of[[.55,166,249,18],[.77,804,526,21],[3.85,196,208,14],[4.22,792,565,16]]){
    const dt=at<1?t-at:mod(t-at,6);const d=at<1?.65:.75;
    if(dt>0&&dt<d){const a=Math.sin(dt/d*Math.PI);star(x,yy,r*a,a*.9,dt*.45);}
  }
}
function cubic(a,b,c,d,p){const q=1-p;return [q*q*q*a[0]+3*q*q*p*b[0]+3*q*p*p*c[0]+p*p*p*d[0],q*q*q*a[1]+3*q*q*p*b[1]+3*q*p*p*c[1]+p*p*p*d[1]];}
function route(segments){
  const points=[];for(const seg of segments)for(let i=0;i<48;i++)points.push(cubic(...seg,i/47));
  const lengths=[0];for(let i=1;i<points.length;i++)lengths.push(lengths[i-1]+Math.hypot(points[i][0]-points[i-1][0],points[i][1]-points[i-1][1]));
  const total=lengths.at(-1);
  return p=>{const d=clamp(p)*total;let i=1;while(i<lengths.length-1&&lengths[i]<d)i++;
    const f=(d-lengths[i-1])/(lengths[i]-lengths[i-1]||1);return [lerp(points[i-1][0],points[i][0],f),lerp(points[i-1][1],points[i][1],f)];};
}
const red=route([[[311,297],[360,297],[425,297],[470,297]],[[470,297],[520,297],[537,320],[537,370]],[[537,370],[537,415],[537,486],[537,527]],[[537,527],[537,588],[586,603],[690,603]]]);
const yellow=route([[[1095,297],[1040,297],[991,297],[947,297]],[[947,297],[884,297],[854,320],[854,376]],[[854,376],[854,430],[854,483],[854,526]],[[854,526],[854,585],[806,603],[711,603]]]);
const orange=route([[[700,603],[700,660],[700,725],[700,838]]]);
function movingLight(route,p,color){
  const points=[];for(let k=0;k<=12;k++)points.push(route(clamp(p-.14+k*.14/12)));
  line(points,color,42);line(points,'#fff9d9b0',21);
  const [x,y]=route(p);glow(x,y,60,color,.6);circle(x,y,18,'#fffbe5');
}
function mix(t){
  const p=progress(t,.48,1.22);if(p<=0)return;
  const arrival=back(p),cycle=mod(t-1.25,6);
  const float=smooth(progress(t,1.6,2.6))*Math.sin((t-2.6)*TAU/3)*4;
  const hit=cycle>=.84&&cycle<1.36?Math.sin(progress(cycle,.84,1.36)*Math.PI):0;
  const sx=lerp(.83,1,arrival)*(1+hit*.014),sy=lerp(.83,1,arrival)*(1-hit*.012);
  group(470.5,lerp(1053,977,out(p))+float,sx,sy,0,out(progress(t,.48,.80)),()=>{
    const scale=534/1145;
    ctx.save();ctx.scale(scale,scale);ctx.translate(-1374/2,-1145/2);
    // Small backlight has the same timing as the two original colored sources.
    if(cycle<.82&&t>=1.25){const a=Math.sin(cycle/.82*Math.PI);glow(311,297,160,'#ffca91',a*.26);glow(1095,297,160,'#fff4a9',a*.26);}
    ctx.drawImage(assets.mix,0,0);
    if(t>=1.25&&cycle<.86){const q=smooth(progress(cycle,0,.86));movingLight(red,q,'#ffb6b0');movingLight(yellow,q,'#fff5a7');}
    if(t>=1.25&&cycle>=.86&&cycle<1.31){const q=smooth(progress(cycle,.86,1.31));movingLight(orange,q,'#ffd295');}
    if(t>=1.25&&cycle>=1.13&&cycle<1.95){
      const q=progress(cycle,1.13,1.95),a=Math.sin(q*Math.PI);
      glow(700,838,190,'#ffe794',a*.60);
      for(const [dx,dy,r]of[[-210,-38,34],[210,-38,34],[-167,116,22],[167,116,22]])star(700+dx,838+dy,r*a,a,q*.45);
    }
    ctx.restore();
  });
}
function button(t){
  const p=progress(t,1.98,2.58);if(p<=0)return;
  const q=back(p),idle=smooth(progress(t,2.58,3.1));
  const scale=lerp(.94,1,q)*(1+Math.sin((t-2.6)*TAU/3)*.004*idle);
  group(470.5,lerp(1490,1402.5,out(p)),scale,scale,0,out(progress(t,1.98,2.22)),()=>{
    buttonPanel(-442.5,-150.5,885,301);
    const arrowX=-280.5+Math.sin((t-2.6)*TAU/3)*4*idle;
    ctx.drawImage(assets.play,arrowX-68.44,-89.5,136.88,154);
    ctx.save();ctx.translate(100,-17.5);ctx.font='144px Lilita';ctx.textAlign='center';ctx.textBaseline='middle';ctx.lineJoin='round';
    const value='STAGE 1',fit=Math.min(1,575/ctx.measureText(value).width);ctx.scale(fit,1);
    ctx.lineWidth=18;ctx.strokeStyle=C.ink;ctx.strokeText(value,0,0);ctx.fillStyle='#ffffff';ctx.fillText(value,0,0);ctx.restore();
    const s=mod(t-2.9,6)/.8;
    if(s<1){ctx.save();ctx.beginPath();ctx.roundRect(-417.5,-125.5,835,239,66);ctx.clip();ctx.transform(1,0,-.32,1,0,0);
      const x=lerp(-650,650,s),g=ctx.createLinearGradient(x-58,0,x+58,0);g.addColorStop(0,'#ffffff00');g.addColorStop(.5,'#ffffff42');g.addColorStop(1,'#ffffff00');
      ctx.fillStyle=g;ctx.fillRect(x-58,-160,116,320);ctx.restore();}
  });
}
function render(t){ctx.resetTransform();ctx.clearRect(0,0,W,H);ctx.scale(W/DW,H/DH);sky(t);logo(t);mix(t);button(t);return canvas;}

const checkpoints=[.30,.65,1.0,1.65,2.4,2.85,4.15,8.65];
for(const t of checkpoints)await fs.writeFile(path.join(root,'frames',`title-${t.toFixed(2)}.png`),render(t).toBuffer('image/png'));
await fs.writeFile(path.join(root,'poster.png'),render(2.85).toBuffer('image/png'));
const sheet=createCanvas(1280,1240),sc=sheet.getContext('2d');sc.fillStyle='#071631';sc.fillRect(0,0,sheet.width,sheet.height);
const captions=['ロゴが弾んで登場','イラストが浮かび上がる','構図が整う','赤と黄の光が合流','ボタンが登場','オレンジがきらめく','ゆったりと待機','混色の演出を繰り返す'];
for(let i=0;i<checkpoints.length;i++){
  const x=18+(i%4)*316,y=18+Math.floor(i/4)*614;
  const im=await loadImage(path.join(root,'frames',`title-${checkpoints[i].toFixed(2)}.png`));sc.drawImage(im,x,y,296,526.22);
  sc.font='24px Lilita';sc.fillStyle='#fff6d8';sc.fillText(`${checkpoints[i].toFixed(2)} s`,x+4,y+560);
  sc.font='15px "Yu Gothic"';sc.fillStyle='#b8cee9';sc.fillText(captions[i],x+4,y+588);
}
await fs.writeFile(path.join(root,'storyboard.png'),sheet.toBuffer('image/png'));

const spec={title:'Blend Trails — タイトル画面アニメーション案',type:'既存素材を使った実装前の動画モックアップ',date:'2026-10-30',format:{width:W,height:H,fps:FPS,seconds:DURATION,codec:'H.264',audio:false},source:{scene:'Assets/Scenes/Title.unity',designResolution:[941,1672],assets:assetPaths,stageLabel:'STAGE 1（保存シーンの初期表示）'},timeline:[{seconds:[.08,.86],motion:'ロゴを70%から軽くオーバーシュートして100%へ。少し傾けた状態から水平へ着地'},{seconds:[.48,1.22],motion:'混色イラストが下から浮かび上がる'},{seconds:[1.25,2.56],motion:'赤と黄の既存の経路に光を流し、合流後にオレンジへ送る'},{seconds:[1.98,2.58],motion:'ステージ開始ボタンが下から弾んで登場。約2.6秒で操作可能にする想定'},{seconds:[2.58,12],motion:'ロゴ上下±5px、イラスト上下±4px、ボタン±0.4%のゆるやかな呼吸。6秒ごとに混色の光と控えめなツヤ'}],loop:{file:'blend-trails-title-idle-loop.mp4',seconds:6,seamless:true,sourceSeconds:[6,12],allTracksPeriodSeconds:[3,6]},implementationNotes:['待機中の動きを小さくし、文字とステージボタンの読みやすさを保つ','初回の登場と待機ループを別シーケンスにする','ボタンの登場演出完了から入力可能にする想定。動画では操作を実装していない','経路の光は既存イラストへ重ねるエフェクトで、元のPNGは変更しない','シーン、スクリプト、プレイヤーの進行情報には変更を加えていない']};
await fs.writeFile(path.join(root,'motion-spec.json'),JSON.stringify(spec,null,2));
if(process.argv.includes('--stills-only')){console.log('Title proposal stills rendered.');process.exit(0);}

async function encode(file,seconds,start){
  const child=spawn(ffmpeg,['-hide_banner','-loglevel','warning','-y','-f','rawvideo','-pixel_format','rgba','-video_size',`${W}x${H}`,'-framerate',String(FPS),'-i','pipe:0','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart','-metadata','title=Blend Trails - Title Motion Concept',path.join(root,file)],{stdio:['pipe','ignore','pipe'],windowsHide:true});
  child.stderr.on('data',v=>process.stderr.write(v));const done=once(child,'close');
  for(let i=0;i<seconds*FPS;i++){
    const frame=render(start+i/FPS).data();if(!child.stdin.write(frame))await once(child.stdin,'drain');
    if(i%120===0)console.log(`${file}: ${i/FPS}/${seconds}s`);
  }
  child.stdin.end();const[status]=await done;if(status!==0)throw new Error(`FFmpeg exited ${status}`);
}
await encode('blend-trails-title-proposal.mp4',DURATION,0);
await encode('blend-trails-title-idle-loop.mp4',6,6);

const validations=[];
for(const[file,seconds]of[['blend-trails-title-proposal.mp4',12],['blend-trails-title-idle-loop.mp4',6]]){
  const result=spawnSync(ffmpeg,['-hide_banner','-i',path.join(root,file),'-map','0:v:0','-nostats','-progress','pipe:1','-f','null','NUL'],{encoding:'utf8',windowsHide:true,maxBuffer:8*1024*1024});
  if(result.status!==0)throw new Error(result.stderr);
  const count=Number([...result.stdout.matchAll(/^frame=(\d+)/gm)].at(-1)?.[1]);
  if(count!==seconds*FPS||!result.stderr.includes('720x1280')||!result.stderr.includes('60 fps')||!result.stderr.includes('Video: h264')||!result.stderr.includes('yuv420p'))throw new Error(`Unexpected video metadata: ${result.stderr}`);
  const data=await fs.readFile(path.join(root,file)),atoms=[];let offset=0;
  while(offset+8<=data.length){let size=data.readUInt32BE(offset);if(size===1)size=Number(data.readBigUInt64BE(offset+8));if(!size)size=data.length-offset;
    atoms.push(data.toString('ascii',offset+4,offset+8));if(size<8)throw new Error('Invalid MP4 atom');offset+=size;}
  if(atoms.indexOf('moov')>atoms.indexOf('mdat'))throw new Error('Missing faststart');
  validations.push({file,seconds,width:W,height:H,fps:FPS,decodedFrames:count,decodeErrors:0,codec:'H.264',pixelFormat:'yuv420p',faststart:true,audio:false,bytes:data.length,sha256:crypto.createHash('sha256').update(data).digest('hex')});
  await fs.writeFile(path.join(root,'qa',file.replace('.mp4','-decode.log')),result.stderr+'\n'+result.stdout);
}

// Decode the delivered MP4, so the contact sheet also verifies the encoded result.
const sampleFrames=checkpoints.map(t=>Math.round(t*FPS));
const selector=sampleFrames.map(n=>`eq(n\\,${n})`).join('+');
const contact=spawnSync(ffmpeg,['-hide_banner','-loglevel','error','-y','-i',path.join(root,'blend-trails-title-proposal.mp4'),'-vf',`select=${selector},scale=240:-2,tile=4x2:padding=12:margin=12:color=0x071631`,'-frames:v','1','-fps_mode','vfr',path.join(root,'qa/encoded-storyboard.png')],{encoding:'utf8',windowsHide:true});
if(contact.status!==0)throw new Error(contact.stderr);
const loopStart=Buffer.from(render(6).data()),loopEnd=Buffer.from(render(12).data());
let maxDifference=0,totalDifference=0;
for(let i=0;i<loopStart.length;i++){const d=Math.abs(loopStart[i]-loopEnd[i]);maxDifference=Math.max(maxDifference,d);totalDifference+=d;}
const loopDifference=totalDifference/loopStart.length;
if(loopDifference>.03)throw new Error(`Loop boundary mismatch: ${loopDifference}`);
const hashesAfter=await sourceHashes();
if(JSON.stringify(hashesBefore)!==JSON.stringify(hashesAfter))throw new Error('Project source changed during export');
await fs.writeFile(path.join(root,'video-validation.json'),JSON.stringify({status:'passed',videos:validations,idleLoopBoundary:{meanChannelDifference:loopDifference,maxChannelDifference:maxDifference,periodSeconds:6},projectSourceUnchanged:true,visualReview:'qa/encoded-storyboard.png'},null,2));
console.log(JSON.stringify({status:'passed',videos:validations.map(({sha256,...v})=>v),loopDifference,projectSourceUnchanged:true},null,2));
