import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
const require=createRequire(import.meta.url);
const sharp=require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/dist/index.cjs');
const root=path.dirname(fileURLToPath(import.meta.url));
const spec=JSON.parse(await fs.readFile(path.join(root,'orange-variant-spec.json'),'utf8'));
const manifest=JSON.parse(await fs.readFile(path.join(root,'manifest.json'),'utf8'));
manifest.exports=manifest.exports.filter(x=>x.concept!=='04');
await fs.copyFile(spec.source,path.join(root,'originals','04-fresh-trails-orange-icon.png'));
for (const size of [1024,512]) {
  const raw=await sharp(spec.source).resize(size,size,{kernel:'lanczos3'}).toColourspace('srgb').ensureAlpha(1).raw().toBuffer({resolveWithObject:true});
  const encode=data=>sharp(data.data,{raw:data.info}).png({compressionLevel:9,adaptiveFiltering:true,palette:false}).toBuffer();
  let bytes=await encode(raw),reducedColours=null;
  if(bytes.length>1000000){
    for(const colours of [256,192,128]){
      const compact=await sharp(raw.data,{raw:raw.info}).png({palette:true,colours,dither:0,effort:10}).toBuffer();
      const decoded=await sharp(compact).ensureAlpha(1).raw().toBuffer({resolveWithObject:true});
      bytes=await encode(decoded);reducedColours=colours;if(bytes.length<=1000000)break;
    }
  }
  const file=`${size===1024?'icons':'google-play/icons'}/04-fresh-trails-orange-${size}.png`;
  await fs.writeFile(path.join(root,file),bytes);
  const info=await sharp(bytes).metadata();
  if(bytes.length>1000000||info.width!==size||info.height!==size||info.channels!==4||info.bitsPerSample!==8)throw Error('Invalid variant export');
  const record={concept:'04',name:spec.name,type:'icon',file,width:size,height:size,bytes:bytes.length,format:'png',channels:4,alpha:true,bitDepth:8,colourSpace:info.space,reducedColours,sha256:createHash('sha256').update(bytes).digest('hex')};
  manifest.exports.push(record);console.log(JSON.stringify(record));
}
await fs.writeFile(path.join(root,'manifest.json'),JSON.stringify(manifest,null,2));
const main=manifest.exports.find(x=>x.concept==='04'&&x.width===1024);
const play=manifest.exports.find(x=>x.concept==='04'&&x.width===512);
let html=await fs.readFile(path.join(root,'index.html'),'utf8');
html=html.replace(/<!-- orange-variant-start -->[\s\S]*?<!-- orange-variant-end -->/,'');
const section=`<!-- orange-variant-start --><article style="margin-top:28px;display:flex;gap:28px;align-items:center;flex-wrap:wrap"><a href="${main.file}" target="_blank"><img src="${main.file}" width="220" height="220" alt="赤と黄色の経路を重ねてオレンジになるアイコン"></a><div><h2>追加案：Cの赤＋黄→オレンジ版</h2><p>選択されたアイコンの構図・背景を保ち、経路と混色の配色を変更。</p><p>1024×1024 PNG / ${(main.bytes/1000).toFixed(0)} KB</p><div class="links"><a href="${main.file}" download>1024px PNG</a><a href="${play.file}" download>入稿用512px PNG</a></div><p><a href="orange-variant-spec.json">編集プロンプト</a></p></div></article><!-- orange-variant-end -->`;
html=html.replace('<div class="grid">',section+'<div class="grid">');
await fs.writeFile(path.join(root,'index.html'),html);
