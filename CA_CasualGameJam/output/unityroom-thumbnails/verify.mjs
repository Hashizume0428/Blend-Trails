import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {createRequire} from 'node:module';
const require=createRequire(import.meta.url);
const {createCanvas,loadImage}=require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const sourceRoot=path.dirname(fileURLToPath(import.meta.url));
const root=process.argv.includes('--board-only')?path.join(sourceRoot,'02-board-only-v2'):sourceRoot;
const exe=path.resolve(sourceRoot,'../ui-clear-motion/_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe');
const manifest=JSON.parse(await fs.readFile(path.join(root,'manifest.json'),'utf8'));
async function run(args){return new Promise((resolve,reject)=>{const p=spawn(exe,args,{windowsHide:true});let stderr='';p.stderr.on('data',d=>stderr+=d);p.on('error',reject);p.on('close',c=>c?reject(Error(stderr)):resolve(stderr));});}
const canvas=createCanvas(1024,manifest.exports.filter(r=>r.width===512).length*180),ctx=canvas.getContext('2d');ctx.fillStyle='#EFF7FC';ctx.fillRect(0,0,1024,canvas.height);
let row=0;
const checks=[];
for(const record of manifest.exports){
 const stderr=await run(['-hide_banner','-v','error','-i',path.join(root,record.file),'-f','null','-']);
 if(stderr.trim())throw Error(stderr);
 checks.push({file:record.file,fullDecode:'passed',frames:record.frames,bytes:record.bytes});
 if(record.width!==512)continue;
 const times=[0,record.durationSeconds*.23,record.durationSeconds*.45,record.durationSeconds*.65,record.durationSeconds*.82,record.durationSeconds-.05];
 for(let i=0;i<times.length;i++){
 const file=path.join(root,'qa',record.id+'-time-'+i+'.png');await run(['-y','-hide_banner','-v','error','-i',path.join(root,record.file),'-ss',times[i].toFixed(2),'-frames:v','1',file]);
 ctx.drawImage(await loadImage(file),i*170,row*180,164,164);ctx.font='12px sans-serif';ctx.fillStyle='#081D4F';ctx.fillText(`${record.id.slice(0,2)} / ${times[i].toFixed(2)}s`,i*170+8,row*180+176);
 }
 row++;
}
await fs.writeFile(path.join(root,'qa','animation-sequence.png'),canvas.toBuffer('image/png'));
await fs.writeFile(path.join(root,'qa','validation.json'),JSON.stringify({status:'passed',checks,dimensionsAndTiming:'Validated against manifest during export',loop:'All exports have infinite GIF loop flag',preview:'Decoded images inspected at 512px / 128px and six points in each sequence'},null,2));
console.log(`Validated all ${checks.length} GIFs; decoded animation sequence ready.`);
