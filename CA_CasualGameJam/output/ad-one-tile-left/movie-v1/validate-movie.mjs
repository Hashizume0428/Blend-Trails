import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
const root=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(root,'../../..');
const ffmpeg=path.join(repo,'output/ui-clear-motion/_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const input=path.join(root,'BlendTrails_ONE_TILE_LEFT_1400x1800_36s.mp4');
const output=path.join(root,'BlendTrails_ONE_TILE_LEFT_1400x1800_36s_final.mp4');
function ff(args){const result=spawnSync(ffmpeg,args,{encoding:'utf8',maxBuffer:4*1024*1024});if(result.status!==0)throw Error(result.stderr||result.error);return result;}
// Normalize only the original generated soundtrack; keep the verified video bitstream.
const measure=ff(['-hide_banner','-i',input,'-map','0:a:0','-af','loudnorm=I=-16:TP=-1.5:LRA=9:print_format=json','-f','null','NUL']);
const found=measure.stderr.match(/\{\s*"input_i"[\s\S]*?\}/);if(!found)throw Error('Missing loudness analysis');
const levels=JSON.parse(found[0]);
ff(['-hide_banner','-loglevel','error','-y','-i',input,'-map','0:v:0','-map','0:a:0','-c:v','copy','-af',`loudnorm=I=-16:TP=-1.5:LRA=9:measured_I=${levels.input_i}:measured_TP=${levels.input_tp}:measured_LRA=${levels.input_lra}:measured_thresh=${levels.input_thresh}:offset=${levels.target_offset}:linear=true`,'-c:a','aac','-b:a','192k','-ar','48000','-t','36','-movflags','+faststart',output]);
const metadata=spawnSync(ffmpeg,['-hide_banner','-i',output],{encoding:'utf8'}).stderr;
if(!/1400x1800/.test(metadata)||!/30 fps/.test(metadata)||!/00:00:36\.00/.test(metadata)||!/Video: h264/.test(metadata)||!/Audio: aac/.test(metadata)||!/stereo/.test(metadata))throw Error('Unexpected stream metadata '+metadata);
const decode=ff(['-hide_banner','-v','error','-xerror','-i',output,'-map','0:v:0','-map','0:a:0','-f','null','NUL','-progress','pipe:1']);
const frames=[...decode.stdout.matchAll(/frame=(\d+)/g)].map(m=>+m[1]);if(frames.at(-1)!==1080)throw Error('Frame count '+frames.at(-1));
const audio=ff(['-hide_banner','-i',output,'-map','0:a:0','-af','loudnorm=I=-16:TP=-1.5:LRA=9:print_format=json','-f','null','NUL']);
const finalLoudness=JSON.parse(audio.stderr.match(/\{\s*"input_i"[\s\S]*?\}/)[0]);
const samples=[2.7,11.65,12.6,25,26.9,32.4,35];await fs.mkdir(path.join(root,'encoded-samples'),{recursive:true});for(const t of samples)ff(['-hide_banner','-loglevel','error','-y','-ss',`${t}`,'-i',output,'-frames:v','1',path.join(root,'encoded-samples',`${t.toFixed(2)}s.png`)]);
const stats=await fs.stat(output),validation={file:output,width:1400,height:1800,fps:30,durationSeconds:36,decodedFrames:1080,decodeErrors:0,codec:'H.264',pixelFormat:'yuv420p',audio:{codec:'AAC',sampleRate:48000,channels:2,integratedLoudnessLUFS:Number(finalLoudness.input_i),truePeakDBTP:Number(finalLoudness.input_tp)},bytes:stats.size,puzzleAudit:JSON.parse(await fs.readFile(path.join(root,'puzzle-audit.json'),'utf8')),sampledSeconds:samples};
await fs.writeFile(path.join(root,'validation.json'),JSON.stringify(validation,null,2));await fs.writeFile(path.join(root,'stream-info.txt'),metadata);console.log(JSON.stringify(validation));
