import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
const require=createRequire(import.meta.url);
const {createCanvas,loadImage,GlobalFonts}=require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const root=path.dirname(fileURLToPath(import.meta.url));
const entries=[
  ['title','タイトル / 採用画像＋可変番号'],
  ['stage3','通常プレイ / 赤＋青＝紫'],
  ['clear','クリア / 次ステージへ'],
  ['stage6','赤＋黄＝オレンジ'],
  ['stage11','青＋黄＝緑'],
  ['color-selector','終点からの再開 / 色選択'],
  ['stage1','Stage1 / 指ガイド'],
  ['incorrect','不正解 / 線と着色を保持'],
];
GlobalFonts.registerFromPath('C:/Windows/Fonts/meiryo.ttc','Meiryo');
const cw=470,gap=25,margin=38,top=145,ch=918;
const canvas=createCanvas(margin*2+cw*3+gap*2,top+ch*2+30),ctx=canvas.getContext('2d');
ctx.fillStyle='#eaf6fc';ctx.fillRect(0,0,canvas.width,canvas.height);
ctx.fillStyle='#071b52';ctx.font='bold 36px Meiryo';ctx.fillText('B案 UI｜Unity実装画面',margin,57);
ctx.font='19px Meiryo';ctx.fillText('採用タイトル・全30ステージ・3配色・色選択・クリア / Unity Editorで撮影',margin,99);
for(const [i,[file,title]] of entries.slice(0,6).entries()){
  const x=margin+(i%3)*(cw+gap),y=top+Math.floor(i/3)*ch;
  ctx.fillStyle='#fff';ctx.beginPath();ctx.roundRect(x,y,cw,ch-20,14);ctx.fill();
  ctx.fillStyle='#071b52';ctx.font='bold 21px Meiryo';ctx.fillText(title,x+15,y+35);
  const img=await loadImage(path.join(root,file+'.png'));
  ctx.drawImage(img,x+10,y+55,cw-20,(cw-20)*img.height/img.width);
}
await fs.writeFile(path.join(root,'overview.png'),canvas.toBuffer('image/png'));
await fs.writeFile(path.join(root,'index.html'),`<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>B案 UI・実装画面</title><style>body{margin:0;background:#eaf6fc;color:#071b52;font-family:"Yu Gothic UI",Meiryo,sans-serif;line-height:1.7}main{max-width:1440px;margin:auto;padding:32px}h1{font-size:36px;margin:0 0 12px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:22px}article{background:white;border:2px solid #071b52;border-radius:16px;overflow:hidden}h2{font-size:18px;padding:12px 16px;margin:0}img{width:100%;height:auto;display:block}a{color:#071b52;font-weight:bold}p{max-width:1040px}footer{margin-top:24px}@media(max-width:800px){.grid{grid-template-columns:1fr}main{padding:18px}}</style><main><h1>B案 UIを実装しました</h1><p>以下は画像生成による案ではなく、Unity Editorで実際に再生して撮影した画面です。タイトルは採用された赤・黄・オレンジで固定し、番号は保存された進行に連動します。プレイ画面は既存の問題配置・操作・3配色を使用しています。</p><p><a href="overview.png">主要6画面の一覧PNG</a> ／ <a href="implementation-results.md">実装・検証結果</a></p><section class="grid">${entries.map(([file,title])=>`<article><h2>${title}</h2><a href="${file}.png"><img src="${file}.png" alt="${title}" width="941" height="1672" loading="lazy"></a></article>`).join('')}</section><footer>番号は撮影時の表示例。操作不能な色別リセットは薄く表示します。検証中の進行データは終了後に元の状態へ戻します。</footer></main>`);
console.log('Saved implementation gallery and overview.png');
