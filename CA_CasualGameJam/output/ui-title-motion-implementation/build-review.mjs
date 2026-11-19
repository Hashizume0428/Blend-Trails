import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
const root=path.dirname(fileURLToPath(import.meta.url));
const ffmpeg=path.resolve(root,'../ui-clear-motion/_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const samples=JSON.parse(await fs.readFile(path.join(root,'capture-samples.json'),'utf8'));
const assert=(value,message)=>{if(!value)throw new Error(message);};
assert(samples.length>=60,'Capture must contain enough independently rendered frames.');
assert(samples.every((s,i)=>i===0||s.frame>samples[i-1].frame),'Unity frames must advance during capture.');
assert(samples.every(s=>s.ready===s.interactable),'Button readiness must match the presentation.');
assert(samples.filter(s=>s.elapsed<2.58).every(s=>!s.interactable),'The start button cannot be tapped during the entrance.');
const firstReady=samples.find(s=>s.ready)?.elapsed;
assert(firstReady>=2.58&&firstReady<2.85,'Button must become ready at about 2.6 seconds.');
const concat=['ffconcat version 1.0'];
for(let i=0;i<samples.length;i++){
  const s=samples[i],end=i+1<samples.length?samples[i+1].elapsed:12;
  concat.push(`file '${path.join(root,s.file).replaceAll('\\','/')}'`,`duration ${Math.max(.001,end-(i===0?0:s.elapsed)).toFixed(7)}`);
}
concat.push(`file '${path.join(root,samples.at(-1).file).replaceAll('\\','/')}'`);
await fs.writeFile(path.join(root,'native-frames.ffconcat'),concat.join('\n'));
function run(args){const r=spawnSync(ffmpeg,args,{encoding:'utf8',windowsHide:true,maxBuffer:8*1024*1024});if(r.status!==0)throw new Error(r.stderr);return r;}
const video=path.join(root,'blend-trails-title-native.mp4');
run(['-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',path.join(root,'native-frames.ffconcat'),'-r','30','-t','12','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',video]);
const decode=run(['-hide_banner','-i',video,'-nostats','-progress','pipe:1','-f','null','NUL']);
const frames=Number([...decode.stdout.matchAll(/^frame=(\d+)/gm)].at(-1)?.[1]);
assert(frames===360,'Expected 360 decoded frames.');
assert(decode.stderr.includes('720x1280')&&decode.stderr.includes('30 fps'),'Expected native review video format.');
await fs.writeFile(path.join(root,'video-decode.log'),decode.stderr+'\n'+decode.stdout);
for(const[t,name]of[[.32,'native-logo'],[1.68,'native-flow'],[2.85,'native-result'],[4.15,'native-sheen']])
  run(['-hide_banner','-loglevel','error','-y','-ss',String(t),'-i',video,'-frames:v','1',path.join(root,name+'.png')]);
const ids=[.3,.65,1,1.65,2.4,2.85,4.15,8.65].map(t=>Math.round(t*30));
run(['-hide_banner','-loglevel','error','-y','-i',video,'-vf',`select=${ids.map(n=>`eq(n\\,${n})`).join('+')},scale=240:-2,tile=4x2:padding=12:margin=12:color=0x071631`,'-frames:v','1','-fps_mode','vfr',path.join(root,'native-storyboard.png')]);
const report={status:'passed',source:'Unity GameView render target, real-time sampling',seconds:12,width:720,height:1280,fps:30,decodedFrames:frames,capturedFrames:samples.length,firstReadySeconds:firstReady,decodeErrors:0,audio:false,bytes:(await fs.stat(video)).size};
await fs.writeFile(path.join(root,'video-validation.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));
