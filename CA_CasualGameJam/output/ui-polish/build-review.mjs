import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createCanvas,loadImage} from 'file:///C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas/index.js';
const root=path.dirname(fileURLToPath(import.meta.url));
const check=JSON.parse(await fs.readFile(path.join(root,'manual-checks.json'),'utf8'));
const canvas=createCanvas(1278,748),ctx=canvas.getContext('2d');ctx.fillStyle='#071b52';ctx.fillRect(0,0,1278,748);
for(const [i,name] of ['title-portrait','reset-buttons','clear-large'].entries()) {
  const image=await loadImage(path.join(root,name+'.png'));ctx.drawImage(image,24+i*418,24,394,700);
}
await fs.writeFile(path.join(root,'overview.png'),canvas.toBuffer('image/png'));
const images=[
['title-portrait','タイトル・通常比率','空と雲は全画面で一続き。ロゴと混色イラストは背景透過素材。'],
['title-tall','タイトル・縦長比率','941×2039。縦横比が変わっても背景の境界線を出さない。'],
['title-tablet','タイトル・タブレット比率','1254×1672。イラストの縦横比を保ち、共通背景が画面全体へ追従。'],
['tutorial-blue','青チュートリアル',`赤ラインを離してから実測${check.blueGuideSeconds.toFixed(2)}秒で青ガイドを開始。`],
['reset-buttons','新しいリセット矢印','生成した透過PNGを3ボタンで共用。小さい表示に合わせてインポート設定と大きさを調整。'],
['clear-large','STAGE CLEAR!・拡大時',`文字の倍率：${(check.headingScaleLarge*100).toFixed(1)}%。文字だけをループで拡大縮小。`],
['clear-small','STAGE CLEAR!・縮小時',`文字の倍率：${(check.headingScaleSmall*100).toFixed(1)}%。ボタン操作と完成盤面はそのまま。`],
];
const cards=images.map(([file,title,description])=>`<article><h2>${title}</h2><p>${description}</p><a href="${file}.png" target="_blank"><img src="${file}.png" alt="${title}" loading="lazy"></a></article>`).join('\n');
await fs.writeFile(path.join(root,'index.html'),`<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blend Trails — UI改善</title><style>*{box-sizing:border-box}body{margin:0;background:#071b52;color:#fff6d8;font:16px/1.65 system-ui,sans-serif}header{padding:32px max(24px,5vw)}h1{font-size:clamp(24px,4vw,42px);line-height:1.2;margin:0 0 12px}header p{margin:0;color:#c1dcff}main{padding:0 max(24px,5vw) 48px;display:grid;grid-template-columns:repeat(auto-fit,minmax(270px,1fr));gap:24px}article{background:#112b63;border:1px solid #36558a;border-radius:20px;padding:18px}h2{margin:0 0 6px;font-size:21px}p{margin:0 0 16px;min-height:3.3em}img{display:block;width:100%;height:auto;border-radius:12px}a{color:#fff6d8}.asset{background:#2db5f3;border-radius:12px}</style><header><h1>Blend Trails: Color Puzzle</h1><p>UI改善後のUnity実画面 · 振動の実機確認は後日</p></header><main>${cards}<article><h2>生成した矢印の透過素材</h2><p>背景透過PNG。白い矢印をボタン上で使用し、黄色のボタンでは濃紺へ着色。</p><a href="../../Assets/UI/ToonBold/ResetArrowGenerated.png" target="_blank"><img class="asset" src="../../Assets/UI/ToonBold/ResetArrowGenerated.png" alt="透過矢印"></a><p><a href="image-prompts.json">画像生成プロンプト</a></p></article></main></html>`);
console.log('Wrote UI polish gallery and overview');
