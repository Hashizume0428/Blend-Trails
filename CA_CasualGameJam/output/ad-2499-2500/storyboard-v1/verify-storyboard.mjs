import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import crypto from 'node:crypto';
const require=createRequire(import.meta.url);
const sharp=require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root=path.dirname(fileURLToPath(import.meta.url));
const manifest=JSON.parse(await fs.readFile(path.join(root,'manifest.json'),'utf8'));
const board=JSON.parse(await fs.readFile(path.join(root,'board-data.json'),'utf8'));
const assert=(condition,message)=>{if(!condition)throw new Error(message);};
const result={frameCount:manifest.scenes.length,dimensions:[],routes:[],holePixels:[],handAlpha:false};
assert(manifest.scenes.length===8,'Expected eight delivered keyframes');
assert(manifest.video.durationSeconds>=30&&manifest.video.durationSeconds<=59,'Video duration');
for(const scene of manifest.scenes){
  const file=path.join(root,scene.file),data=await fs.readFile(file),meta=await sharp(data).metadata();
  assert(meta.width===1400&&meta.height===1800,scene.file+': dimensions');
  assert(crypto.createHash('sha256').update(data).digest('hex')===scene.sha256,scene.file+': hash');
  result.dimensions.push({file:scene.file,width:meta.width,height:meta.height});
  if(scene.board==='B'){
    const v=scene.viewport,x=Math.round(v.x+25.5*v.pitch),y=Math.round(v.y+25.5*v.pitch);
    const pixel=await sharp(data).extract({left:x,top:y,width:1,height:1}).removeAlpha().raw().toBuffer();
    assert([...pixel].every(v=>v>=250),scene.file+': hole must be white and unobscured');
    assert(scene.empty===1,scene.file+': exactly one model cell must remain white');
    result.holePixels.push({file:scene.file,x,y,rgb:[...pixel]});
  }
}
for(const name of ['routeA','routeB']){
  const route=board[name];assert(route.length===2500&&new Set(route).size===2500,name+': coverage');
  assert(route.every(id=>id>=0&&id<2500),name+': range');
  for(let i=1;i<route.length;i++){
    const a=route[i-1],b=route[i],distance=Math.abs(Math.floor(a/50)-Math.floor(b/50))+Math.abs(a%50-b%50);
    assert(distance===1,name+': adjacent cells');
  }
  result.routes.push({name,cells:route.length,unique:new Set(route).size,adjacent:true});
}
assert(board.routeA.slice(-50).every(id=>id%50===49),'A must finish at final column');
assert(board.routeB.at(-1)===25*50+25,'B center hole');
assert(new Set(board.routeB.slice(0,2499)).size===2499,'B unique filled cells');
const hand=await sharp(path.join(root,'assets/swipe-hand.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
let zero=0,nearOpaque=0;for(let i=3;i<hand.data.length;i+=4){if(hand.data[i]===0)zero++;if(hand.data[i]>=250)nearOpaque++;}
assert(zero>10000&&nearOpaque>10000,'Hand requires real transparent background and a solid subject');result.handAlpha=true;
result.status='passed';await fs.writeFile(path.join(root,'verification.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
