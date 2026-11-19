import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {spawn} from 'node:child_process';
const require=createRequire(import.meta.url);
const deps='C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/';
const {createCanvas,loadImage,GlobalFonts}=require(deps+'@napi-rs/canvas');
const sharp=require(deps+'sharp');
const sourceRoot=path.dirname(fileURLToPath(import.meta.url)),project=path.resolve(sourceRoot,'../..');
const boardOnly=process.argv.includes('--board-only');
const root=boardOnly?path.join(sourceRoot,'02-board-only-v2'):sourceRoot;
const ffmpeg=path.join(project,'output/ui-clear-motion/_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const N='#081D4F',C='#FFF7DF',R='#FF354B',Y='#FFD326',O='#FF931B',B='#0879FF',P='#943DFA';
const FPS=20,S=512;
GlobalFonts.registerFromPath(path.join(project,'Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf'),'Lilita');
const assets={};
for(const [k,file] of [['logo','BlendTrailsLogo.png'],['motif','BlendTrailsMix.png']]){
  const png=await sharp(path.join(project,'Assets/UI/ToonBold',file)).trim({threshold:12}).png().toBuffer();assets[k]=await loadImage(png);
}
for(const [k,file] of [['startRed','StartRed.png'],['startBlue','StartBlue.png']]){
 assets[k]=await loadImage(path.join(project,'Assets/UI/ToonBold',file));
}
// Palette and dimensions from StagePaletteVisuals.UiColor and ToonSkinBuilder.
const gameLines={red:'#FF3E43',blue:'#0068FF',mixed:'#8A25F5',width:88*.24/.95,markerSize:88*.57/.95};
const cv=createCanvas(S,S),ctx=cv.getContext('2d');
const clamp=x=>Math.max(0,Math.min(1,x));
const ease=x=>{x=clamp(x);return x*x*(3-2*x);};
const mix=(a,b,t)=>{const n=h=>[1,3,5].map(i=>parseInt(h.slice(i,i+2),16));return '#'+n(a).map((v,i)=>Math.round(v+(n(b)[i]-v)*clamp(t)).toString(16).padStart(2,'0')).join('');};
function rr(x,y,w,h,r,fill,stroke=null,lw=3){ctx.beginPath();ctx.roundRect(x,y,w,h,r);if(fill){ctx.fillStyle=fill;ctx.fill();}if(stroke){ctx.strokeStyle=stroke;ctx.lineWidth=lw;ctx.stroke();}}
function line(points,color,width){if(points.length<2)return;ctx.beginPath();points.forEach(([x,y],i)=>i?ctx.lineTo(x,y):ctx.moveTo(x,y));ctx.strokeStyle=color;ctx.lineWidth=width;ctx.lineCap='round';ctx.lineJoin='round';ctx.stroke();}
function circle(x,y,r,fill){ctx.beginPath();ctx.arc(x,y,r,0,Math.PI*2);ctx.fillStyle=fill;ctx.fill();}
function star(x,y,r,color=C,rot=0){ctx.save();ctx.translate(x,y);ctx.rotate(rot);ctx.beginPath();for(let i=0;i<8;i++){let a=i*Math.PI/4,rad=i%2?r*.3:r;ctx.lineTo(Math.cos(a)*rad,Math.sin(a)*rad);}ctx.closePath();ctx.fillStyle=color;ctx.fill();ctx.restore();}
function cloud(x,y,s){ctx.save();ctx.translate(x,y);ctx.scale(s,s);circle(-23,0,22,'#C7F2FF');circle(7,-12,30,'#C7F2FF');circle(37,4,22,'#C7F2FF');rr(-36,0,90,26,13,'#C7F2FF');ctx.restore();}
function bg(){ctx.fillStyle='#28B9F0';ctx.fillRect(0,0,512,512);cloud(-12,62,1.15);cloud(488,153,.9);cloud(-23,414,1.1);cloud(507,465,1.25);star(456,52,7);star(57,172,6);}
function imageFit(img,x,y,w,h){const s=Math.min(w/img.width,h/img.height);ctx.drawImage(img,x+(w-img.width*s)/2,y+(h-img.height*s)/2,img.width*s,img.height*s);}
function logo(big=false){imageFit(assets.logo,big?30:112,big?30:6,big?452:288,big?260:155);}
function label(value,y=479,width=262){rr(256-width/2,y-18+4,width,36,18,'#0789CB');rr(256-width/2,y-18,width,36,18,C,N,3);ctx.font='23px Lilita';ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillStyle=N;ctx.fillText(value,256,y+1);}
function partial(points,p){p=clamp(p);const lengths=points.slice(1).map((q,i)=>Math.hypot(q[0]-points[i][0],q[1]-points[i][1]));let remain=lengths.reduce((a,b)=>a+b,0)*p;const out=[points[0]];for(let i=0;i<lengths.length;i++){let v=Math.min(1,remain/lengths[i]);out.push([points[i][0]+(points[i+1][0]-points[i][0])*v,points[i][1]+(points[i+1][1]-points[i][1])*v]);remain-=lengths[i];if(v<1)break;}return out;}
function trail(points,p,color,w=26){if(p<=0)return;const out=partial(points,p);line(out,color,w);line(out.map(([x,y])=>[x,y-3]),'#FFFFFF55',w*.15);return out.at(-1);}
function tile(x,y,size,fill='white',frame=null,highlight=true){rr(x,y+6,size,size,17,'#0586CE',N,4);rr(x,y,size,size,17,fill,N,4);if(highlight)rr(x+12,y+8,size-24,5,2,'#FFFFFF65');if(frame)rr(x+4,y+4,size-8,size-8,13,null,frame,4);}
function rays(x,y,t,alpha=1){ctx.save();ctx.globalAlpha=alpha;for(let i=0;i<6;i++){const a=i*Math.PI/3+t*.2,r=65+Math.sin(t*5)*2;star(x+Math.cos(a)*r,y+Math.sin(a)*r,7,C,a);}ctx.restore();}
function cursor(p,alpha=1){if(!p)return;ctx.save();ctx.globalAlpha=alpha;circle(...p,12,N);circle(...p,9,'white');circle(...p,5,'#FFFFFF00');ctx.restore();}
function concept1(t){bg();logo();const reset=1-ease((t-2.85)/.65);const a=ease((t-.2)/.85)*reset,b=ease((t-1)/.85)*reset;const success=ease((t-1.6)/.3)*(1-ease((t-2.75)/.25));
 const lx=67,rx=325,ty=192,sz=120,cx=196,cy=335;
 tile(lx,ty,sz,mix('#FFFFFF',R,a*.32));tile(rx,ty,sz,mix('#FFFFFF',Y,b*.36));
 ctx.save();const pop=1+.055*Math.sin(clamp((t-1.7)/.5)*Math.PI);ctx.translate(256,395);ctx.scale(pop,pop);ctx.translate(-256,-395);tile(cx,cy,sz,mix('#FFFFFF',b>.8?O:R,Math.max(a,b)*.36));ctx.restore();
 const pa=[[127,252],[170,252],[170,395],[256,395]],pb=[[385,252],[342,252],[342,395],[256,395]];
 const tipA=trail(pa,a,R,30),tipB=trail(pb,b,Y,30);
 circle(127,252,28,R);circle(385,252,28,Y);circle(127,245,7,'#FFFFFF85');circle(385,245,7,'#FFFFFF85');
 if(a>.88)circle(256,395,25,R);if(b>.88){circle(256,395,27,O);line([[256,395],[286,395]],O,30);circle(249,387,6,'#FFFFFF85');}
 if(a>0&&a<.98&&t<1.2)cursor(tipA);if(b>0&&b<.98&&t<2)cursor(tipB);
 if(success)rays(256,395,t,success);label('MIX COLORS',487,230);
}
// A valid 3x3 demonstration: two orthogonal paths cover all nine cells.
// Only the two marked purple cells are shared; start cells never overlap.
const red=[[0,0],[1,0],[1,1],[0,1],[0,2]],blue=[[2,2],[2,1],[2,0],[1,0],[1,1],[1,2]];
const key=([r,c])=>r*3+c;
const redIds=new Set(red.map(key)),blueIds=new Set(blue.map(key));
if(new Set([...redIds,...blueIds]).size!==9)throw Error('Incomplete demonstration');
for(const route of [red,blue])for(let i=1;i<route.length;i++)if(Math.abs(route[i][0]-route[i-1][0])+Math.abs(route[i][1]-route[i-1][1])!==1)throw Error('Non-orthogonal step');
function cellProgress(route,p,id){const i=route.findIndex(c=>key(c)===id);return i<0?0:clamp(p*(route.length-1)-i+1);}
const point=([r,c])=>[160+c*96,222+r*96];
function gameBoardLines(a,b){
 const width=gameLines.width,radius=width/2,nodes=new Map();
 const stages=[{route:red,progress:a,mask:1,color:gameLines.red},{route:blue,progress:b,mask:2,color:gameLines.blue}];
 const edges=[];
 function disc(p,color){ctx.beginPath();for(let i=0;i<=12;i++){const angle=i*Math.PI/6;const x=p[0]+Math.cos(angle)*radius,y=p[1]+Math.sin(angle)*radius;i?ctx.lineTo(x,y):ctx.moveTo(x,y);}ctx.closePath();ctx.fillStyle=color;ctx.fill();}
 function strip(points,color){ctx.beginPath();ctx.moveTo(...points[0]);ctx.lineTo(...points[1]);ctx.lineWidth=width;ctx.lineCap='butt';ctx.strokeStyle=color;ctx.stroke();}
 for(const {route,progress,mask,color} of stages){
  const count=clamp(progress)*(route.length-1);
  for(let i=0;i<route.length;i++)if(i<=count){const id=key(route[i]);nodes.set(id,(nodes.get(id)||0)|mask);}
  for(let i=0;i<route.length-1;i++){
   const p=clamp(count-i);if(p<=0)continue;
   const start=point(route[i]),finish=point(route[i+1]);
   const tip=[start[0]+(finish[0]-start[0])*p,start[1]+(finish[1]-start[1])*p];
   strip([start,tip],color);
   edges.push({from:key(route[i]),to:key(route[i+1]),start,finish,p,mask});
   if(p<1)disc(tip,color);
  }
 }
 // Both authored paths traverse the shared edge in the same direction.
 // Composite its common portion once, matching BoardLineRenderer's union.
 for(const first of edges.filter(e=>e.mask===1))for(const second of edges.filter(e=>e.mask===2)){
  if(first.from!==second.from||first.to!==second.to)continue;
  const p=Math.min(first.p,second.p),tip=[first.start[0]+(first.finish[0]-first.start[0])*p,first.start[1]+(first.finish[1]-first.start[1])*p];
  strip([first.start,tip],gameLines.mixed);
  if(p<1)rr(tip[0]-radius,tip[1]-radius,width,width,0,gameLines.mixed);
 }
 for(const [id,mask] of nodes){const p=point([Math.floor(id/3),id%3]);
  if(mask===3)rr(p[0]-radius,p[1]-radius,width,width,0,gameLines.mixed);
  else disc(p,mask===1?gameLines.red:gameLines.blue);
 }
 const size=gameLines.markerSize;
 for(const [cell,img] of [[red[0],assets.startRed],[blue[0],assets.startBlue]]){const p=point(cell);ctx.drawImage(img,p[0]-size/2,p[1]-size/2,size,size);}
}
function board(a,b,t,celebrate=0,lastPulse=false,backing=C,gameStyle=false){
 ctx.save();const pop=1+.025*Math.sin(clamp(celebrate*2)*Math.PI);ctx.translate(256,318);ctx.scale(pop,pop);ctx.translate(-256,-318);
 rr(94,161,324,310,27,'#0586CE');rr(94,155,324,310,27,backing,N,6);
 const frames={2:R,3:P,4:P,5:B};
 for(let r=0;r<3;r++)for(let c=0;c<3;c++){
 const id=r*3+c,ra=cellProgress(red,a,id),ba=cellProgress(blue,b,id),color=ra>0&&ba>0?mix(R,P,ba):ra>0?R:B,coverage=Math.max(ra,ba);
 tile(116+c*96,178+r*96,88,coverage?mix('#FFFFFF',color,.42*coverage):'#FFFFFF',frames[id],!gameStyle);
 if(id===5&&lastPulse&&ba===0){ctx.save();ctx.globalAlpha=.4+.3*Math.sin(t*5);rr(120+c*96,182+r*96,80,80,13,null,B,5);ctx.restore();}
 }
 if(gameStyle)gameBoardLines(a,b);
 else {
 trail(red.map(point),a,R,25);trail(blue.map(point),b,B,25);
 // Repaint the shared segment purple as the second ingredient traverses it.
 const sharedStart=3/5,sharedEnd=4/5;
 if(b>sharedStart&&a>0){const q=clamp((b-sharedStart)/(sharedEnd-sharedStart));trail([point([1,0]),point([1,1])],q,P,25);circle(...point([1,0]),12,P);}
 circle(...point(red[0]),22,R);circle(...point(blue[0]),22,B);
 circle(point(red[0])[0]-5,point(red[0])[1]-7,5,'#FFFFFF90');circle(point(blue[0])[0]-5,point(blue[0])[1]-7,5,'#FFFFFF90');
 if(a>0&&a<1)cursor(partial(red.map(point),a).at(-1));if(b>0&&b<1)cursor(partial(blue.map(point),b).at(-1));
 }
 ctx.restore();
 if(celebrate>0){ctx.save();ctx.globalAlpha=Math.min(1,celebrate*4);for(let i=0;i<10;i++){const a=i*2.4,age=clamp(celebrate),x=256+Math.cos(a)*(170+age*48),y=300+Math.sin(a)*(118+age*60);star(x,y,5+(i%3),[R,Y,P,C][i%4],t+i);}ctx.restore();}
}
function concept2(t){bg();logo();let a=ease((t-.25)/1.3),b=ease((t-1.6)/1.65);if(t>4.05){const q=1-ease((t-4.05)/.65);a*=q;b*=q;}
 const done=t>=3.25&&t<4.05;board(a,b,t,done?clamp((t-3.25)/.5):0);label(done?'PERFECT!':'TRACE  •  MIX  •  CLEAR',488,done?205:323);}
function concept2BoardOnly(t){
 let a=ease((t-.25)/1.3),b=ease((t-1.6)/1.65);
 if(t>4.05){const q=1-ease((t-4.05)/.65);a*=q;b*=q;}
 ctx.fillStyle='#28B9F0';ctx.fillRect(0,0,S,S);
 // Enlarge only the nine tiles. The source board rim and decorations lie
 // outside this square, so the full animation has the same tight crop.
 ctx.save();ctx.scale(S/296,S/296);ctx.translate(-108,-172);
 board(a,b,t,0,false,'#28B9F0',true);
 ctx.restore();
}
function concept3(t){bg();logo();let b=.8+.2*ease((t-.85)/.8);if(t>2.9)b=1-.2*ease((t-2.9)/.6);const done=t>=1.65&&t<2.9;board(1,b,t,done?clamp((t-1.65)/.5):0,!done);label(done?'PERFECT!':'ONE TILE LEFT!',488,250);}
function concept4(t){bg();logo(true);const bob=Math.sin(t/3.6*Math.PI*2)*2;const x=80,y=284+bob,w=352,h=203;
 ctx.save();ctx.translate(256,y+h/2);const scale=1+.022*Math.sin(t/3.6*Math.PI*2);ctx.scale(scale,scale);ctx.translate(-256,-y-h/2);imageFit(assets.motif,x,y,w,h);
 // Move a subtle highlight over the existing approved sprite.
 const layer=createCanvas(512,512),l=layer.getContext('2d');const s=Math.min(w/assets.motif.width,h/assets.motif.height),dx=x+(w-assets.motif.width*s)/2,dy=y+(h-assets.motif.height*s)/2;l.drawImage(assets.motif,dx,dy,assets.motif.width*s,assets.motif.height*s);l.globalCompositeOperation='source-atop';const sx=40+(t/3.6)*500;l.fillStyle='#FFFFFF55';l.beginPath();l.moveTo(sx-35,270);l.lineTo(sx-7,270);l.lineTo(sx+70,500);l.lineTo(sx+42,500);l.closePath();l.fill();ctx.drawImage(layer,0,0);ctx.restore();
 star(63,319,8+2*Math.sin(t*3),C,t*.4);star(447,387,9+2*Math.cos(t*3),C,-t*.3);
}
const specs=boardOnly?[{id:'02-board-only',title:'② 盤面だけ',duration:4.8,render:concept2BoardOnly,poster:3.6}]:[{id:'01-mix-colors',title:'混ざる瞬間',duration:3.6,render:concept1,poster:2.3},{id:'02-trace-and-clear',title:'小さな盤面を一気にクリア',duration:4.8,render:concept2,poster:3.6},{id:'03-one-tile-left',title:'あと1マス！',duration:3.6,render:concept3,poster:.35},{id:'04-logo-motion',title:'ロゴの混色マークが動く',duration:3.6,render:concept4,poster:1.7}];
async function run(args){return new Promise((resolve,reject)=>{const p=spawn(ffmpeg,args,{windowsHide:true});let err='';p.stderr.on('data',d=>err+=d);p.on('error',reject);p.on('close',code=>code?reject(Error(err)):resolve(err));});}
await fs.mkdir(path.join(root,'qa'),{recursive:true});
const records=[];
for(const spec of specs){
 const dir=path.join(root,'frames',spec.id);await fs.mkdir(dir,{recursive:true});const total=Math.round(spec.duration*FPS);
 for(let i=0;i<total;i++){spec.render(i/FPS);await fs.writeFile(path.join(dir,String(i).padStart(4,'0')+'.png'),cv.toBuffer('image/png'));}
 spec.render(spec.poster);await fs.writeFile(path.join(root,spec.id+'-poster.png'),cv.toBuffer('image/png'));
 for(const size of [512,256]){
 const out=path.join(root,`${spec.id}-${size}.gif`);
 await run(['-y','-hide_banner','-loglevel','error','-framerate',String(FPS),'-i',path.join(dir,'%04d.png'),'-filter_complex',`[0:v]scale=${size}:${size}:flags=lanczos,split[a][b];[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=none:diff_mode=rectangle`,'-loop','0',out]);
 const info=await sharp(out,{animated:true}).metadata();if(info.width!==size||info.pageHeight!==size||info.pages!==total||info.loop!==0)throw Error('Invalid GIF '+out);
 const duration=info.delay.reduce((a,b)=>a+b,0)/1000;if(Math.abs(duration-spec.duration)>.02)throw Error('Invalid duration');
 // Decode the deliverable, not merely the render source, for visual review.
 await run(['-y','-hide_banner','-loglevel','error','-i',out,'-ss',String(spec.poster),'-frames:v','1',path.join(root,'qa',`${spec.id}-${size}.png`)]);
 const record={id:spec.id,title:spec.title,file:path.basename(out),width:size,height:size,frames:info.pages,durationSeconds:duration,bytes:(await fs.stat(out)).size,loop:'infinite'};records.push(record);console.log(JSON.stringify(record));
 }
}
const contact=createCanvas(256*specs.length,600),cc=contact.getContext('2d');cc.fillStyle='#EFF7FC';cc.fillRect(0,0,contact.width,600);for(let i=0;i<specs.length;i++){const img=await loadImage(path.join(root,'qa',specs[i].id+'-512.png'));cc.drawImage(img,i*256,48,256,256);cc.drawImage(img,i*256+64,360,128,128);cc.fillStyle=N;cc.textAlign='center';cc.font='22px Lilita';cc.fillText(specs[i].id.slice(0,2),i*256+128,31);cc.font='15px Lilita';cc.fillText('128 px',i*256+128,520);}await fs.writeFile(path.join(root,'qa','comparison.png'),contact.toBuffer('image/png'));
await fs.writeFile(path.join(root,'manifest.json'),JSON.stringify({game:'Blend Trails: Color Puzzle',fps:FPS,method:'Existing approved logo/motif plus canvas motion; 3x3 board is a valid promotional demonstration, not a recorded game stage.',exports:records},null,2));
const cards=specs.map((s,i)=>`<article><h2>${i+1}. ${s.title}</h2><img class="large" src="${s.id}-512.gif" alt="${s.title}"><p>${s.duration}秒・無限ループ</p><p><a download href="${s.id}-512.gif">512px GIF</a> · <a download href="${s.id}-256.gif">256px GIF</a> · <a download href="${s.id}-poster.png">静止画</a></p><img width="128" height="128" src="${s.id}-256.gif" alt="小さな表示の確認"></article>`).join('');
await fs.writeFile(path.join(root,'index.html'),`<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blend Trails — GIF候補</title><style>body{margin:0;padding:32px;background:#eff7fc;color:#081d4f;font-family:system-ui,sans-serif}h1{font-size:26px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:24px}article{padding:20px;background:white;border-radius:20px;border:1px solid #d8e6ee}h2{font-size:18px}img.large{width:100%;max-width:512px;border-radius:12px}a{color:#006dcc}p{font-size:14px}</style><h1>Blend Trails / unityroom GIF候補①〜④</h1><p>512px版と軽量256px版。下段の小さな画像で一覧表示時の見え方も確認できます。</p><main>${cards}</main></html>`);
await fs.writeFile(path.join(root,'README.md'),'# unityroom GIFサムネイル候補\n\n①混ざる瞬間、②小さな盤面を一気にクリア、③あと1マス、④ロゴの混色マークが動く、の4案。各512×512 / 256×256、20fps、無限ループ。\n\nindex.htmlで4案を同時比較できます。manifest.jsonに尺・容量・フレーム数を記録しています。qaには完成GIFを復号した画像と縮小比較画像があります。\n\n既存のBlendTrailsLogo.png / BlendTrailsMix.pngを利用。②③の3×3盤面はサムネイル用のデモ盤面で、実ゲームの録画ではありません。2本の上下左右経路で9マスを埋め、指定した2マスのみで赤と青を重ねます。\n\n再生成: node output/unityroom-thumbnails/render.mjs\n');
console.log('Complete: '+root);
if(boardOnly){
 await fs.writeFile(path.join(root,'README.md'),'# ② 盤面だけ v2\n\nロゴ・文字・外枠・紙吹雪を外し、元の②と同じ3×3のマスと経路だけを拡大。マスの周りと隙間は水色背景（#28B9F0）。赤→青→混色→完成を保持→巻き戻しの4.8秒ループ。512px版と256px版、20fps。\n\n線はStagePaletteVisuals / GlobalScriptsの赤#FF3E43・青#0068FF・紫#8A25F5。ToonSkinBuilderの線幅0.24と始点スプライト幅0.57（マス幅0.95）を同じ比率で使用。始点は実際のStartRed.png / StartBlue.png。線の光沢、始点の白い反射、移動中の白い点は削除。重複辺は紫、混色の交点はBoardLineRendererと同様に角型。\n\n再生成: node output/unityroom-thumbnails/render.mjs --board-only\n検証: node output/unityroom-thumbnails/verify.mjs --board-only\n');
 const htmlPath=path.join(root,'index.html');let html=await fs.readFile(htmlPath,'utf8');html=html.replace('Blend Trails / unityroom GIF候補①〜④','Blend Trails / ② 盤面だけ v2').replace('4案を同時比較','盤面だけのGIFを確認');await fs.writeFile(htmlPath,html);
}
