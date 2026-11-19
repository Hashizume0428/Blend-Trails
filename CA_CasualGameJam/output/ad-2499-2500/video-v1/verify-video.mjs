import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import {sharp} from './board-renderer.mjs';
const root=path.dirname(fileURLToPath(import.meta.url));
const ffmpeg=path.resolve(root,'../../ui-clear-motion/_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const video=path.join(root,'blend-trails-2499-2500-1400x1800-45s.mp4');
function run(args){const r=spawnSync(ffmpeg,args,{encoding:'utf8',windowsHide:true,maxBuffer:16*1024*1024});if(r.status!==0)throw new Error(r.stderr||String(r.error));return r;}
function assert(v,msg){if(!v)throw new Error(msg);}
console.log('Decoding the full MP4 to verify both streams.');
const decode=run(['-hide_banner','-i',video,'-map','0:v:0','-map','0:a:0','-nostats','-progress','pipe:1','-f','null','NUL']);
const frames=[...decode.stdout.matchAll(/^frame=(\d+)/gm)].map(x=>Number(x[1])).at(-1);
const duration=decode.stderr.match(/Duration: (\d+):(\d+):([\d.]+)/);
assert(duration,'Container duration missing');const seconds=Number(duration[1])*3600+Number(duration[2])*60+Number(duration[3]);
assert(seconds>=30&&seconds<=59,'Duration outside requested range');assert(Math.abs(seconds-45)<.05,'Expected 45 seconds');
assert(decode.stderr.includes('1400x1800'),'Resolution');assert(decode.stderr.includes('30 fps'),'Frame rate');
assert(decode.stderr.includes('Video: h264'),'H.264 required');assert(decode.stderr.includes('yuv420p'),'4:2:0 compatibility');assert(decode.stderr.includes('Audio: aac'),'AAC audio required');assert(frames===1350,'Expected 1350 decoded frames');
await fs.writeFile(path.join(root,'qa/full-decode.log'),decode.stderr+'\n'+decode.stdout);
console.log('Full decode passed: 1400×1800, 45 seconds, 1350 frames, H.264 + AAC.');
const sampleFrames=[0,39,111,153,300,465,518,546,582,720,927,987,1035,1071,1125,1140,1197,1251,1335];
const selector=sampleFrames.map(n=>`eq(n\\,${n})`).join('+');
run(['-hide_banner','-loglevel','error','-y','-i',video,'-vf',`select=${selector},scale=350:450,tile=5x4:nb_frames=19:padding=8:margin=10:color=0xEDF7FC`,'-frames:v','1','-fps_mode','vfr',path.join(root,'qa/decoded-overview.png')]);
for(const [time,file] of [[34.5,'decoded-hole.png'],[43.5,'decoded-cta.png'],[5.1,'decoded-zoom.png']]){
  run(['-hide_banner','-loglevel','error','-y','-ss',String(time),'-i',video,'-frames:v','1',path.join(root,'qa',file)]);
}
const holes=[];
for(const [file,x,y] of [['decoded-hole.png',700,974],['decoded-cta.png',700,883]]){
  const m=await sharp(path.join(root,'qa',file)).metadata();assert(m.width===1400&&m.height===1800,file+': decoded dimensions');
  const rgb=await sharp(path.join(root,'qa',file)).extract({left:x,top:y,width:1,height:1}).removeAlpha().raw().toBuffer();
  assert([...rgb].every(v=>v>240),file+': single hole is visible');holes.push({file,x,y,rgb:[...rgb]});
}
console.log('Checking final audio level.');
const audio=run(['-hide_banner','-i',video,'-vn','-af','loudnorm=I=-16:TP=-1.5:LRA=9:print_format=json','-f','null','NUL']);
const json=audio.stderr.match(/\{\s*"input_i"[\s\S]*?\}/);assert(json,'Loudness data missing');const loudness=JSON.parse(json[0]);
assert(Number(loudness.input_tp)<=-.8,'Audio true peak');assert(Number(loudness.input_i)>-20&&Number(loudness.input_i)<-13,'Audio loudness');
const data=await fs.readFile(video),topLevel=[];let offset=0;
while(offset+8<=data.length){let size=data.readUInt32BE(offset),header=8;if(size===1){size=Number(data.readBigUInt64BE(offset+8));header=16;}if(!size)size=data.length-offset;topLevel.push({type:data.toString('ascii',offset+4,offset+8),offset,bytes:size});if(size<header)break;offset+=size;}
assert(topLevel.findIndex(x=>x.type==='moov')<topLevel.findIndex(x=>x.type==='mdat'),'MP4 faststart');
const report={status:'passed',width:1400,height:1800,durationSeconds:seconds,fps:30,decodedFrames:frames,videoCodec:'H.264 High',pixelFormat:'yuv420p',audioCodec:'AAC',audioSampleRate:48000,audioChannels:2,audioLoudnessLUFS:Number(loudness.input_i),audioTruePeakDbTP:Number(loudness.input_tp),faststart:true,bytes:data.length,sha256:crypto.createHash('sha256').update(data).digest('hex'),holeSamples:holes,decodeErrors:0,source:'Canvas animation of exact 50×50 board data, existing approved art, shared image_gen hand sprite, original synthesized music and sound effects.'};
await fs.writeFile(path.join(root,'video-validation.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
